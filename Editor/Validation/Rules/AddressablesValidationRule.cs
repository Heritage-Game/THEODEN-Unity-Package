using System;
using System.Collections.Generic;
using System.IO;
using Addressing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Validates the Addressables configuration of the JSON files
    /// and media referenced by a THEODEN project.
    /// </summary>
    public sealed class AddressablesValidationRule
        : ITheodenValidationRule
    {
        /// <summary>
        /// Identifies the kind of media referenced by an Addressables address.
        /// </summary>
        private enum AddressableContentKind
        {
            PoiBadge,
            PoiImage,
            PoiMusic,
            PoiAudioDescription,
            DirectionsImage,
            DirectionsAudioDescription
        }

        /// <inheritdoc/>
        public void Validate(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            /*
             * Missing configuration data is handled by
             * ProjectConfigurationValidationRule.
             */
            if (context == null ||
                context.theodenProjectConfig == null ||
                context.availableLanguages == null ||
                context.availablePois == null)
            {
                return;
            }

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                report.AddError(
                    "MISSING_ADDRESSABLE_SETTINGS",
                    "The Unity project does not contain valid Addressables settings.",
                    context.projectFolderPath
                );

                return;
            }

            Dictionary<string, AddressableAssetEntry> entriesByAddress =
                BuildEntryLookup(settings, report);

            ValidateJsonEntries(
                context,
                settings,
                report
            );

            List<ContentAddressReference> mediaReferences =
                CollectMediaReferences(context);

            ValidateMediaReferences(
                mediaReferences,
                context.projectId,
                entriesByAddress,
                report
            );
        }

        /// <summary>
        /// Creates a lookup that associates every explicit Addressables address
        /// with its corresponding asset entry.
        /// </summary>
        private static Dictionary<string, AddressableAssetEntry>
            BuildEntryLookup(
                AddressableAssetSettings settings,
                TheodenValidationReport report)
        {
            Dictionary<string, AddressableAssetEntry> entriesByAddress =
                new(StringComparer.Ordinal);

            HashSet<string> reportedDuplicates =
                new(StringComparer.Ordinal);

            if (settings.groups == null)
                return entriesByAddress;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null || group.entries == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry == null ||
                        string.IsNullOrWhiteSpace(entry.address))
                    {
                        continue;
                    }

                    if (!entriesByAddress.ContainsKey(entry.address))
                    {
                        entriesByAddress.Add(
                            entry.address,
                            entry
                        );

                        continue;
                    }

                    if (!reportedDuplicates.Add(entry.address))
                        continue;

                    string assetPath =
                        AssetDatabase.GUIDToAssetPath(entry.guid);

                    report.AddError(
                        "DUPLICATE_ADDRESSABLE_ADDRESS",
                        $"The Addressables address '{entry.address}' " +
                        "is assigned to more than one asset entry.",
                        assetPath
                    );
                }
            }

            return entriesByAddress;
        }

        // --------------------------------------------------------------------
        // JSON entries
        // --------------------------------------------------------------------

        /// <summary>
        /// Validates the Addressables entries of all Codex, Directions,
        /// and POI JSON files.
        /// </summary>
        private static void ValidateJsonEntries(
            TheodenProjectContext context,
            AddressableAssetSettings settings,
            TheodenValidationReport report)
        {
            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            foreach (LanguageEntry languageEntry
                     in context.availableLanguages)
            {
                if (languageEntry == null)
                    continue;

                LanguageList language =
                    languageEntry.language;

                ValidateCodexJsonEntry(
                    projectConfig,
                    language,
                    settings,
                    report
                );

                foreach (POIRegistryEntry poi
                         in context.availablePois)
                {
                    if (poi == null ||
                        string.IsNullOrWhiteSpace(poi.PoiId))
                    {
                        continue;
                    }

                    ValidateDirectionsJsonEntry(
                        projectConfig,
                        poi.PoiId,
                        language,
                        settings,
                        report
                    );

                    ValidatePoiJsonEntry(
                        projectConfig,
                        poi.PoiId,
                        language,
                        settings,
                        report
                    );
                }
            }
        }

        /// <summary>
        /// Validates the Addressables entry of a Codex JSON.
        /// </summary>
        private static void ValidateCodexJsonEntry(
            TheodenProjectConfig projectConfig,
            LanguageList language,
            AddressableAssetSettings settings,
            TheodenValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(
                    projectConfig.codexFolderPath))
            {
                return;
            }

            string fileName =
                TheodenFileNaming.GetCodexJsonFileName(language);

            string assetPath = CombineAssetPath(
                projectConfig.codexFolderPath,
                fileName
            );

            ValidateJsonAssetEntry(
                assetPath,
                TheodenAddressablesNaming.GetCodexJsonAddress(
                    projectConfig.projectId,
                    language
                ),
                TheodenAddressablesNaming.GetCodexGroupName(
                    projectConfig.projectId
                ),
                TheodenAddressablesNaming.GetCodexLabel(
                    projectConfig.projectId
                ),
                "Codex JSON",
                settings,
                report
            );
        }

        /// <summary>
        /// Validates the Addressables entry of a Directions JSON.
        /// </summary>
        private static void ValidateDirectionsJsonEntry(
            TheodenProjectConfig projectConfig,
            string poiId,
            LanguageList language,
            AddressableAssetSettings settings,
            TheodenValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(
                    projectConfig.directionsFolderPath))
            {
                return;
            }

            string fileName =
                TheodenFileNaming.GetDirectionsJsonFileName(
                    poiId,
                    language
                );

            string assetPath = CombineAssetPath(
                projectConfig.directionsFolderPath,
                fileName
            );

            ValidateJsonAssetEntry(
                assetPath,
                TheodenAddressablesNaming
                    .GetDirectionsJsonAddress(
                        projectConfig.projectId,
                        poiId,
                        language
                    ),
                TheodenAddressablesNaming
                    .GetDirectionsGroupName(
                        projectConfig.projectId,
                        poiId
                    ),
                TheodenAddressablesNaming
                    .GetDirectionsLabel(
                        projectConfig.projectId,
                        poiId
                    ),
                $"Directions JSON for POI '{poiId}'",
                settings,
                report
            );
        }

        /// <summary>
        /// Validates the Addressables entry of a POI JSON.
        /// </summary>
        private static void ValidatePoiJsonEntry(
            TheodenProjectConfig projectConfig,
            string poiId,
            LanguageList language,
            AddressableAssetSettings settings,
            TheodenValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(
                    projectConfig.poisFolderPath))
            {
                return;
            }

            string dataFolderPath = CombineAssetPath(
                CombineAssetPath(
                    projectConfig.poisFolderPath,
                    poiId
                ),
                "Data"
            );

            string fileName =
                TheodenFileNaming.GetPoiJsonFileName(
                    poiId,
                    language
                );

            string assetPath = CombineAssetPath(
                dataFolderPath,
                fileName
            );

            ValidateJsonAssetEntry(
                assetPath,
                TheodenAddressablesNaming
                    .GetPoiJsonAddress(
                        projectConfig.projectId,
                        poiId,
                        language
                    ),
                TheodenAddressablesNaming
                    .GetPoiGroupName(
                        projectConfig.projectId,
                        poiId
                    ),
                TheodenAddressablesNaming
                    .GetPoiLabel(
                        projectConfig.projectId,
                        poiId
                    ),
                $"POI JSON for '{poiId}'",
                settings,
                report
            );
        }

        /// <summary>
        /// Validates the Addressables configuration of a JSON asset.
        /// Missing JSON files are ignored because they are reported by
        /// LocalizedContentValidationRule.
        /// </summary>
        private static void ValidateJsonAssetEntry(
            string assetPath,
            string expectedAddress,
            string expectedGroupName,
            string expectedLabel,
            string contentDescription,
            AddressableAssetSettings settings,
            TheodenValidationReport report)
        {
            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            if (jsonAsset == null)
                return;

            string guid =
                AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrWhiteSpace(guid))
            {
                report.AddError(
                    "INVALID_JSON_ASSET_GUID",
                    $"The {contentDescription} does not have a valid Unity GUID.",
                    assetPath
                );

                return;
            }

            AddressableAssetEntry entry =
                settings.FindAssetEntry(guid);

            if (entry == null)
            {
                report.AddError(
                    "JSON_NOT_ADDRESSABLE",
                    $"The {contentDescription} is not registered as Addressable.",
                    assetPath
                );

                return;
            }

            ValidateEntryConvention(
                entry,
                expectedAddress,
                expectedGroupName,
                expectedLabel,
                assetPath,
                report
            );
        }

        // --------------------------------------------------------------------
        // Media references
        // --------------------------------------------------------------------

        /// <summary>
        /// Collects all media addresses referenced by POI and Directions JSON files.
        /// </summary>
        private static List<ContentAddressReference>
            CollectMediaReferences(
                TheodenProjectContext context)
        {
            List<ContentAddressReference> references = new();

            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            foreach (LanguageEntry languageEntry
                     in context.availableLanguages)
            {
                if (languageEntry == null)
                    continue;

                LanguageList language =
                    languageEntry.language;

                foreach (POIRegistryEntry poi
                         in context.availablePois)
                {
                    if (poi == null ||
                        string.IsNullOrWhiteSpace(poi.PoiId))
                    {
                        continue;
                    }

                    CollectPoiMediaReferences(
                        projectConfig.poisFolderPath,
                        poi.PoiId,
                        language,
                        references
                    );

                    CollectDirectionsMediaReferences(
                        projectConfig.directionsFolderPath,
                        poi.PoiId,
                        language,
                        references
                    );
                }
            }

            return references;
        }

        /// <summary>
        /// Collects media addresses from a POI JSON.
        /// </summary>
        private static void CollectPoiMediaReferences(
            string poisFolderPath,
            string poiId,
            LanguageList language,
            List<ContentAddressReference> references)
        {
            if (string.IsNullOrWhiteSpace(poisFolderPath))
                return;

            string dataFolderPath = CombineAssetPath(
                CombineAssetPath(poisFolderPath, poiId),
                "Data"
            );

            string fileName =
                TheodenFileNaming.GetPoiJsonFileName(
                    poiId,
                    language
                );

            string assetPath = CombineAssetPath(
                dataFolderPath,
                fileName
            );

            if (!TryLoadJson(assetPath, out JObject root))
                return;

            AddOptionalAddress(
                root.SelectToken("challenge.poiBadge"),
                poiId,
                AddressableContentKind.PoiBadge,
                typeof(Sprite),
                "POI badge",
                assetPath,
                references
            );

            AddAddressArray(
                root.SelectToken(
                    "gameData.pointOfInterest.media.images"
                ),
                poiId,
                AddressableContentKind.PoiImage,
                typeof(Sprite),
                "POI image",
                assetPath,
                references
            );

            AddOptionalAddress(
                root.SelectToken(
                    "gameData.pointOfInterest.media.audio.music"
                ),
                poiId,
                AddressableContentKind.PoiMusic,
                typeof(AudioClip),
                "POI music",
                assetPath,
                references
            );

            AddOptionalAddress(
                root.SelectToken(
                    "gameData.pointOfInterest.media.audio.audioDescription"
                ),
                poiId,
                AddressableContentKind.PoiAudioDescription,
                typeof(AudioClip),
                "POI audio description",
                assetPath,
                references
            );
        }

        /// <summary>
        /// Collects media addresses from a Directions JSON.
        /// </summary>
        private static void CollectDirectionsMediaReferences(
            string directionsFolderPath,
            string poiId,
            LanguageList language,
            List<ContentAddressReference> references)
        {
            if (string.IsNullOrWhiteSpace(
                    directionsFolderPath))
            {
                return;
            }

            string fileName =
                TheodenFileNaming.GetDirectionsJsonFileName(
                    poiId,
                    language
                );

            string assetPath = CombineAssetPath(
                directionsFolderPath,
                fileName
            );

            if (!TryLoadJson(assetPath, out JObject root))
                return;

            AddAddressArray(
                root["images"],
                poiId,
                AddressableContentKind.DirectionsImage,
                typeof(Sprite),
                "Directions image",
                assetPath,
                references
            );

            AddOptionalAddress(
                root["audioDescription"],
                poiId,
                AddressableContentKind.DirectionsAudioDescription,
                typeof(AudioClip),
                "Directions audio description",
                assetPath,
                references
            );
        }

        /// <summary>
        /// Adds valid string addresses from a JSON array.
        /// Invalid array elements are reported by ContentReferenceValidationRule.
        /// </summary>
        private static void AddAddressArray(
            JToken addressesToken,
            string poiId,
            AddressableContentKind contentKind,
            Type expectedAssetType,
            string description,
            string sourceAssetPath,
            List<ContentAddressReference> references)
        {
            if (addressesToken is not JArray addresses)
                return;

            foreach (JToken addressToken in addresses)
            {
                if (addressToken.Type != JTokenType.String)
                    continue;

                string address =
                    addressToken.Value<string>();

                if (string.IsNullOrWhiteSpace(address))
                    continue;

                references.Add(new ContentAddressReference(
                    address,
                    poiId,
                    contentKind,
                    expectedAssetType,
                    description,
                    sourceAssetPath
                ));
            }
        }

        /// <summary>
        /// Adds a valid optional string address.
        /// </summary>
        private static void AddOptionalAddress(
            JToken addressToken,
            string poiId,
            AddressableContentKind contentKind,
            Type expectedAssetType,
            string description,
            string sourceAssetPath,
            List<ContentAddressReference> references)
        {
            if (addressToken == null ||
                addressToken.Type != JTokenType.String)
            {
                return;
            }

            string address =
                addressToken.Value<string>();

            if (string.IsNullOrWhiteSpace(address))
                return;

            references.Add(new ContentAddressReference(
                address,
                poiId,
                contentKind,
                expectedAssetType,
                description,
                sourceAssetPath
            ));
        }

        /// <summary>
        /// Validates every unique media address/type/context combination.
        /// </summary>
        private static void ValidateMediaReferences(
            List<ContentAddressReference> references,
            string projectId,
            Dictionary<string, AddressableAssetEntry> entriesByAddress,
            TheodenValidationReport report)
        {
            HashSet<string> validatedReferences =
                new(StringComparer.Ordinal);

            foreach (ContentAddressReference reference in references)
            {
                string validationKey =
                    $"{projectId}|" +
                    $"{reference.Address}|" +
                    $"{reference.PoiId}|" +
                    $"{reference.ContentKind}|" +
                    $"{reference.ExpectedAssetType.FullName}";

                if (!validatedReferences.Add(validationKey))
                    continue;

                ValidateMediaReference(
                    reference,
                    projectId,
                    entriesByAddress,
                    report
                );
            }
        }

        /// <summary>
        /// Validates one media reference and its Addressables configuration.
        /// </summary>
        private static void ValidateMediaReference(
            ContentAddressReference reference,
            string projectId,
            Dictionary<string, AddressableAssetEntry> entriesByAddress,
            TheodenValidationReport report)
        {
            if (!entriesByAddress.TryGetValue(
                    reference.Address,
                    out AddressableAssetEntry entry))
            {
                report.AddError(
                    "MISSING_ADDRESSABLE_ENTRY",
                    $"The JSON file references the " +
                    $"{reference.Description} address " +
                    $"'{reference.Address}', but no Addressables entry " +
                    $"uses that address.",
                    reference.SourceAssetPath
                );

                return;
            }

            string targetAssetPath =
                AssetDatabase.GUIDToAssetPath(
                    entry.guid
                );

            if (string.IsNullOrWhiteSpace(targetAssetPath))
            {
                report.AddError(
                    "INVALID_ADDRESSABLE_GUID",
                    $"The Addressables entry '{reference.Address}' " +
                    "does not reference a valid Unity asset.",
                    reference.SourceAssetPath
                );

                return;
            }

            string expectedAddress =
                GetExpectedMediaAddress(
                    projectId,
                    reference,
                    targetAssetPath
                );

            string expectedGroupName =
                GetExpectedMediaGroupName(
                    projectId,
                    reference
                );

            string expectedLabel =
                GetExpectedMediaLabel(
                    projectId,
                    reference
                );

            ValidateEntryConvention(
                entry,
                expectedAddress,
                expectedGroupName,
                expectedLabel,
                targetAssetPath,
                report
            );

            UnityEngine.Object targetAsset =
                AssetDatabase.LoadAssetAtPath(
                    targetAssetPath,
                    reference.ExpectedAssetType
                );

            if (targetAsset != null)
                return;

            report.AddError(
                "INVALID_ADDRESSABLE_ASSET_TYPE",
                $"The address '{reference.Address}' is used as " +
                $"{reference.ExpectedAssetType.Name}, but its target " +
                "asset is not of that type.",
                targetAssetPath
            );
        }

        // --------------------------------------------------------------------
        // Naming convention
        // --------------------------------------------------------------------

        /// <summary>
        /// Gets the expected Addressables address for a media reference.
        /// </summary>
        private static string GetExpectedMediaAddress(
            string projectId,
            ContentAddressReference reference,
            string targetAssetPath)
        {
            string assetName =
                Path.GetFileNameWithoutExtension(
                    targetAssetPath
                );

            return reference.ContentKind switch
            {
                AddressableContentKind.PoiBadge =>
                    TheodenAddressablesNaming
                        .GetPoiBadgeAddress(
                            projectId,
                            reference.PoiId,
                            assetName
                        ),

                AddressableContentKind.PoiImage =>
                    TheodenAddressablesNaming
                        .GetPoiImageAddress(
                            projectId,
                            reference.PoiId,
                            assetName
                        ),

                AddressableContentKind.PoiMusic =>
                    TheodenAddressablesNaming
                        .GetPoiMusicAddress(
                            projectId,
                            reference.PoiId
                        ),

                AddressableContentKind.PoiAudioDescription =>
                    TheodenAddressablesNaming
                        .GetPoiAudioDescriptionAddress(
                            projectId,
                            reference.PoiId
                        ),

                AddressableContentKind.DirectionsImage =>
                    TheodenAddressablesNaming
                        .GetDirectionsImageAddress(
                            projectId,
                            reference.PoiId,
                            assetName
                        ),

                AddressableContentKind.DirectionsAudioDescription =>
                    TheodenAddressablesNaming
                        .GetDirectionsAudioDescriptionAddress(
                            projectId,
                            reference.PoiId
                        ),

                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Gets the expected Addressables group for a media reference.
        /// </summary>
        private static string GetExpectedMediaGroupName(
            string projectId,
            ContentAddressReference reference)
        {
            return IsDirectionsContent(reference.ContentKind)
                ? TheodenAddressablesNaming
                    .GetDirectionsGroupName(
                        projectId,
                        reference.PoiId
                    )
                : TheodenAddressablesNaming
                    .GetPoiGroupName(
                        projectId,
                        reference.PoiId
                    );
        }
        /// <summary>
        /// Gets the expected Addressables label for a media reference.
        /// </summary>
        private static string GetExpectedMediaLabel(
            string projectId,
            ContentAddressReference reference)
        {
            return IsDirectionsContent(reference.ContentKind)
                ? TheodenAddressablesNaming
                    .GetDirectionsLabel(
                        projectId,
                        reference.PoiId
                    )
                : TheodenAddressablesNaming
                    .GetPoiLabel(
                        projectId,
                        reference.PoiId
                    );
        }
        /// <summary>
        /// Returns whether a media kind belongs to Directions content.
        /// </summary>
        private static bool IsDirectionsContent(
            AddressableContentKind contentKind)
        {
            return contentKind ==
                   AddressableContentKind.DirectionsImage ||
                   contentKind ==
                   AddressableContentKind.DirectionsAudioDescription;
        }

        /// <summary>
        /// Validates address, group, and label of an Addressables entry.
        /// </summary>
        private static void ValidateEntryConvention(
            AddressableAssetEntry entry,
            string expectedAddress,
            string expectedGroupName,
            string expectedLabel,
            string assetPath,
            TheodenValidationReport report)
        {
            if (!string.Equals(
                    entry.address,
                    expectedAddress,
                    StringComparison.Ordinal))
            {
                report.AddError(
                    "INVALID_ADDRESSABLE_ADDRESS",
                    $"The asset uses address '{entry.address}', " +
                    $"but '{expectedAddress}' was expected.",
                    assetPath
                );
            }

            string actualGroupName =
                entry.parentGroup != null
                    ? entry.parentGroup.name
                    : "";

            if (!string.Equals(
                    actualGroupName,
                    expectedGroupName,
                    StringComparison.Ordinal))
            {
                report.AddError(
                    "INVALID_ADDRESSABLE_GROUP",
                    $"The asset is in group '{actualGroupName}', " +
                    $"but group '{expectedGroupName}' was expected.",
                    assetPath
                );
            }

            if (entry.labels == null ||
                !entry.labels.Contains(expectedLabel))
            {
                report.AddError(
                    "MISSING_ADDRESSABLE_LABEL",
                    $"The asset does not contain the required " +
                    $"Addressables label '{expectedLabel}'.",
                    assetPath
                );
            }
        }

        // --------------------------------------------------------------------
        // JSON and path utilities
        // --------------------------------------------------------------------

        /// <summary>
        /// Loads a valid JSON object.
        /// Missing, empty, and malformed files are ignored because
        /// previous validation rules report those errors.
        /// </summary>
        private static bool TryLoadJson(
            string assetPath,
            out JObject root)
        {
            root = null;

            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    assetPath
                );

            if (jsonAsset == null ||
                string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                return false;
            }

            try
            {
                root = JObject.Parse(jsonAsset.text);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Combines two parts of a Unity project-relative asset path.
        /// </summary>
        private static string CombineAssetPath(
            string parentPath,
            string childPath)
        {
            return $"{parentPath.TrimEnd('/')}/{childPath.Trim('/')}";
        }

        /// <summary>
        /// Describes one media address found inside a JSON file.
        /// </summary>
        private sealed class ContentAddressReference
        {
            public string Address { get; }
            public string PoiId { get; }
            public AddressableContentKind ContentKind { get; }
            public Type ExpectedAssetType { get; }
            public string Description { get; }
            public string SourceAssetPath { get; }

            public ContentAddressReference(
                string address,
                string poiId,
                AddressableContentKind contentKind,
                Type expectedAssetType,
                string description,
                string sourceAssetPath)
            {
                Address = address;
                PoiId = poiId;
                ContentKind = contentKind;
                ExpectedAssetType = expectedAssetType;
                Description = description;
                SourceAssetPath = sourceAssetPath;
            }
        }
    }
}
