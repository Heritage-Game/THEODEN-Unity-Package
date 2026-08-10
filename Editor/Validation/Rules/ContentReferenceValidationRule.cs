using System;
using System.Collections.Generic;
using System.IO;
using Addressing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Validates the internal references and structural consistency
    /// of the JSON content belonging to a THEODEN project.
    /// </summary>
    public sealed class ContentReferenceValidationRule
        : ITheodenValidationRule
    {
        /// <inheritdoc/>
        public void Validate(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (context == null ||
                context.theodenProjectConfig == null ||
                context.availableLanguages == null ||
                context.availableLanguages.Count == 0 ||
                context.availablePois == null ||
                context.availablePois.Count == 0)
            {
                return;
            }

            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            foreach (LanguageEntry languageEntry
                     in context.availableLanguages)
            {
                if (languageEntry == null)
                    continue;

                LanguageList language = languageEntry.language;

                if (!string.IsNullOrWhiteSpace(
                        projectConfig.codexFolderPath))
                {
                    ValidateCodex(
                        context,
                        projectConfig.codexFolderPath,
                        language,
                        report
                    );
                }

                foreach (POIRegistryEntry poi
                         in context.availablePois)
                {
                    if (poi == null ||
                        string.IsNullOrWhiteSpace(poi.PoiId))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(
                            projectConfig.directionsFolderPath))
                    {
                        ValidateDirections(
                            projectConfig.directionsFolderPath,
                            poi.PoiId,
                            language,
                            report
                        );
                    }

                    if (!string.IsNullOrWhiteSpace(
                            projectConfig.poisFolderPath))
                    {
                        ValidatePoi(
                            projectConfig.poisFolderPath,
                            poi.PoiId,
                            language,
                            report
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Validates the language and POI references contained in a Codex JSON.
        /// </summary>
        private static void ValidateCodex(
            TheodenProjectContext context,
            string codexFolderPath,
            LanguageList expectedLanguage,
            TheodenValidationReport report)
        {
            string fileName =
                TheodenFileNaming.GetCodexJsonFileName(
                    expectedLanguage
                );

            string assetPath = CombineAssetPath(
                codexFolderPath,
                fileName
            );

            if (!TryLoadJson(assetPath, report, out JObject root))
                return;

            ValidateLanguage(
                root["language"],
                expectedLanguage,
                assetPath,
                report
            );

            /*
             * JArray il the Newtosoft type for JSON arrays: array[...]
             * This check handles the cases where root["Items"] is null or anything
             * else that is not the expected array.
             * => Look for "items" property inside the JSON, if "items" is not a JArray
             * then AddError(), else save it inside the variable items. 
             */
            if (root["items"] is not JArray items)
            {
                report.AddError(
                    "INVALID_CODEX_ITEMS",
                    "The Codex JSON does not contain a valid 'items' array.",
                    assetPath
                );

                return;
            }

            if (items.Count == 0)
            {
                report.AddError(
                    "EMPTY_CODEX_ITEMS",
                    "The Codex JSON does not contain any menu items.",
                    assetPath
                );

                return;
            }

            HashSet<string> registeredPoiIds =
                GetRegisteredPoiIds(context);

            HashSet<string> codexPoiIds =
                new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not JObject item)
                {
                    report.AddError(
                        "INVALID_CODEX_ITEM",
                        $"The Codex item at index {i} is not a valid object.",
                        assetPath
                    );

                    continue;
                }

                ValidateCodexItem(
                    item,
                    i,
                    expectedLanguage,
                    registeredPoiIds,
                    codexPoiIds,
                    assetPath,
                    report
                );
            }

            foreach (string registeredPoiId in registeredPoiIds)
            {
                if (codexPoiIds.Contains(registeredPoiId))
                    continue;

                report.AddError(
                    "MISSING_CODEX_ITEM",
                    $"The registered POI '{registeredPoiId}' " +
                    $"is missing from the Codex for language " +
                    $"'{expectedLanguage}'.",
                    assetPath
                );
            }
        }

        /// <summary>
        /// Validates a single item contained in a Codex JSON.
        /// </summary>
        private static void ValidateCodexItem(
            JObject item,
            int itemIndex,
            LanguageList language,
            HashSet<string> registeredPoiIds,
            HashSet<string> codexPoiIds,
            string assetPath,
            TheodenValidationReport report)
        {
            string poiId = item.Value<string>("poiId");

            if (string.IsNullOrWhiteSpace(poiId))
            {
                report.AddError(
                    "EMPTY_CODEX_POI_ID",
                    $"The Codex item at index {itemIndex} " +
                    "has an empty POI id.",
                    assetPath
                );

                return;
            }

            if (!registeredPoiIds.Contains(poiId))
            {
                report.AddError(
                    "UNREGISTERED_CODEX_POI",
                    $"The Codex references the unregistered POI '{poiId}'.",
                    assetPath
                );
            }

            if (!codexPoiIds.Add(poiId))
            {
                report.AddError(
                    "DUPLICATE_CODEX_POI",
                    $"The POI '{poiId}' appears more than once " +
                    "in the same Codex.",
                    assetPath
                );
            }

            string actualParameter =
                item.Value<string>("parameter");

            string directionsFileName =
                TheodenFileNaming.GetDirectionsJsonFileName(
                    poiId,
                    language
                );

            string expectedParameter =
                Path.GetFileNameWithoutExtension(
                    directionsFileName
                );

            if (!string.Equals(
                    actualParameter,
                    expectedParameter,
                    StringComparison.Ordinal))
            {
                report.AddError(
                    "INVALID_CODEX_PARAMETER",
                    $"The Codex parameter for POI '{poiId}' is " +
                    $"'{actualParameter}', but '{expectedParameter}' " +
                    "was expected.",
                    assetPath
                );
            }
        }

        /// <summary>
        /// Validates the POI id and media addresses contained
        /// in a Directions JSON.
        /// </summary>
        private static void ValidateDirections(
            string directionsFolderPath,
            string expectedPoiId,
            LanguageList language,
            TheodenValidationReport report)
        {
            string fileName =
                TheodenFileNaming.GetDirectionsJsonFileName(
                    expectedPoiId,
                    language
                );

            string assetPath = CombineAssetPath(
                directionsFolderPath,
                fileName
            );

            if (!TryLoadJson(assetPath, report, out JObject root))
                return;

            ValidatePoiId(
                root["poiId"],
                expectedPoiId,
                "Directions JSON",
                assetPath,
                report
            );

            ValidateAddressArray(
                root["images"],
                "Directions images",
                assetPath,
                report
            );

            ValidateOptionalAddress(
                root["audioDescription"],
                "Directions audio description",
                assetPath,
                report
            );
        }

        /// <summary>
        /// Validates the identity, language, and media addresses
        /// contained in a POI JSON.
        /// </summary>
        private static void ValidatePoi(
            string poisFolderPath,
            string expectedPoiId,
            LanguageList expectedLanguage,
            TheodenValidationReport report)
        {
            string dataFolderPath = CombineAssetPath(
                CombineAssetPath(
                    poisFolderPath,
                    expectedPoiId
                ),
                "Data"
            );

            string fileName =
                TheodenFileNaming.GetPoiJsonFileName(
                    expectedPoiId,
                    expectedLanguage
                );

            string assetPath = CombineAssetPath(
                dataFolderPath,
                fileName
            );

            if (!TryLoadJson(assetPath, report, out JObject root))
                return;

            ValidatePoiId(
                root.SelectToken("poi.poiId"),
                expectedPoiId,
                "POI JSON",
                assetPath,
                report
            );

            ValidateLanguage(
                root["language"],
                expectedLanguage,
                assetPath,
                report
            );

            ValidateOptionalAddress(
                root.SelectToken("challenge.poiBadge"),
                "POI badge",
                assetPath,
                report
            );

            ValidateAddressArray(
                root.SelectToken(
                    "gameData.pointOfInterest.media.images"
                ),
                "POI images",
                assetPath,
                report
            );

            ValidateOptionalAddress(
                root.SelectToken(
                    "gameData.pointOfInterest.media.audio.music"
                ),
                "POI music",
                assetPath,
                report
            );

            ValidateOptionalAddress(
                root.SelectToken(
                    "gameData.pointOfInterest.media.audio.audioDescription"
                ),
                "POI audio description",
                assetPath,
                report
            );
        }

        /// <summary>
        /// Loads and parses a JSON file.
        /// Missing files are ignored because they are already reported
        /// by LocalizedContentValidationRule.
        /// </summary>
        private static bool TryLoadJson(
            string assetPath,
            TheodenValidationReport report,
            out JObject root)
        {
            root = null;

            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            if (jsonAsset == null)
                return false;

            if (string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                report.AddError(
                    "EMPTY_JSON_FILE",
                    "The JSON file is empty.",
                    assetPath
                );

                return false;
            }

            try
            {
                root = JObject.Parse(jsonAsset.text);
                return true;
            }
            catch (JsonException exception)
            {
                report.AddError(
                    "MALFORMED_JSON",
                    $"The JSON file could not be parsed: " +
                    exception.Message,
                    assetPath
                );

                return false;
            }
        }

        /// <summary>
        /// Validates that a JSON language matches the expected language.
        /// </summary>
        private static void ValidateLanguage(
            JToken languageToken,
            LanguageList expectedLanguage,
            string assetPath,
            TheodenValidationReport report)
        {
            string actualLanguage =
                languageToken?.Value<string>();

            if (string.IsNullOrWhiteSpace(actualLanguage))
            {
                report.AddError(
                    "MISSING_CONTENT_LANGUAGE",
                    "The JSON file does not define a language.",
                    assetPath
                );

                return;
            }

            string expectedLanguageName =
                expectedLanguage.ToString();

            if (!string.Equals(
                    actualLanguage,
                    expectedLanguageName,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.AddError(
                    "CONTENT_LANGUAGE_MISMATCH",
                    $"The JSON language is '{actualLanguage}', " +
                    $"but '{expectedLanguageName}' was expected.",
                    assetPath
                );
            }
        }

        /// <summary>
        /// Validates that a JSON POI id matches the expected POI.
        /// </summary>
        private static void ValidatePoiId(
            JToken poiIdToken,
            string expectedPoiId,
            string contentDescription,
            string assetPath,
            TheodenValidationReport report)
        {
            string actualPoiId =
                poiIdToken?.Value<string>();

            if (string.IsNullOrWhiteSpace(actualPoiId))
            {
                report.AddError(
                    "MISSING_CONTENT_POI_ID",
                    $"The {contentDescription} does not define a POI id.",
                    assetPath
                );

                return;
            }

            if (!string.Equals(
                    actualPoiId,
                    expectedPoiId,
                    StringComparison.OrdinalIgnoreCase))
            {
                report.AddError(
                    "CONTENT_POI_ID_MISMATCH",
                    $"The {contentDescription} belongs to POI " +
                    $"'{actualPoiId}', but '{expectedPoiId}' was expected.",
                    assetPath
                );
            }
        }

        /// <summary>
        /// Validates an array of Addressables addresses.
        /// </summary>
        private static void ValidateAddressArray(
            JToken addressesToken,
            string contentDescription,
            string assetPath,
            TheodenValidationReport report)
        {
            /*
             * A missing array is currently considered valid because media
             * collections may be optional.
             */
            if (addressesToken == null ||
                addressesToken.Type == JTokenType.Null)
            {
                return;
            }

            if (addressesToken is not JArray addresses)
            {
                report.AddError(
                    "INVALID_ADDRESS_ARRAY",
                    $"The {contentDescription} value must be an array.",
                    assetPath
                );

                return;
            }

            for (int i = 0; i < addresses.Count; i++)
            {
                JToken addressToken = addresses[i];

                if (addressToken.Type != JTokenType.String ||
                    string.IsNullOrWhiteSpace(
                        addressToken.Value<string>()))
                {
                    report.AddError(
                        "INVALID_ASSET_ADDRESS",
                        $"The {contentDescription} address at index {i} " +
                        "is empty or invalid.",
                        assetPath
                    );
                }
            }
        }

        /// <summary>
        /// Validates the format of an optional Addressables address.
        /// </summary>
        private static void ValidateOptionalAddress(
            JToken addressToken,
            string contentDescription,
            string assetPath,
            TheodenValidationReport report)
        {
            /*
             * Missing, null, and empty addresses are interpreted as
             * optional media that has not been configured.
             */
            if (addressToken == null ||
                addressToken.Type == JTokenType.Null)
            {
                return;
            }

            if (addressToken.Type != JTokenType.String)
            {
                report.AddError(
                    "INVALID_ASSET_ADDRESS",
                    $"The {contentDescription} address is not a string.",
                    assetPath
                );
            }
        }

        /// <summary>
        /// Gets all valid POI ids registered in the selected project.
        /// </summary>
        private static HashSet<string> GetRegisteredPoiIds(
            TheodenProjectContext context)
        {
            HashSet<string> registeredIds =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (POIRegistryEntry poi
                     in context.availablePois)
            {
                if (poi != null &&
                    !string.IsNullOrWhiteSpace(poi.PoiId))
                {
                    registeredIds.Add(poi.PoiId);
                }
            }

            return registeredIds;
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
    }
}