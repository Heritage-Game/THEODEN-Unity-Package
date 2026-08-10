using System;
using System.Collections.Generic;
using System.Linq;
using Addressing;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Registers Unity assets referenced by a POI template inside the Addressables system.
    /// </summary>
    /// <remarks>
    /// This service is part of the editor-side POI export pipeline.
    /// It scans a <see cref="LevelTemplateBase"/> instance, finds all referenced Unity assets
    /// such as <see cref="Sprite"/> and <see cref="AudioClip"/>, creates or moves Addressables
    /// entries into the POI-specific group, assigns deterministic addresses, and applies the
    /// POI-specific label.
    ///
    /// The actual address for each media asset is resolved by <see cref="MediaAddressResolver"/>.
    /// Group and label names are resolved through <see cref="TheodenAddressablesNaming"/> so that
    /// editor export and runtime loading use the same naming convention.
    /// </remarks>
    public static class PoiAddressablesSetupService
    {
        /// <summary>
        /// Sets up Addressables entries for the Unity assets referenced
        /// by a POI template.
        /// </summary>
        public static bool SetupAddressablesForTemplate(
            LevelTemplateBase template,
            string projectId,
            string poiId,
            string projectRootFolderPath,
            out string error)
        {
            error = null;

            if (template == null)
            {
                error = "Template is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                error = "THEODEN project id is missing.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(poiId))
            {
                error = "POI id is missing.";
                return false;
            }

            projectRootFolderPath =
                NormalizeUnityPath(projectRootFolderPath)
                    .TrimEnd('/');

            if (string.IsNullOrWhiteSpace(projectRootFolderPath) ||
                !projectRootFolderPath.Equals(
                    "Assets",
                    StringComparison.Ordinal) &&
                !projectRootFolderPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                error =
                    "Invalid THEODEN project root folder path: " +
                    projectRootFolderPath;

                return false;
            }

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                error =
                    "Addressables settings not found. " +
                    "Initialize Addressables first.";

                return false;
            }

            string groupName =
                TheodenAddressablesNaming.GetPoiGroupName(
                    projectId,
                    poiId
                );

            string label =
                TheodenAddressablesNaming.GetPoiLabel(
                    projectId,
                    poiId
                );

            AddressableAssetGroup group =
                GetOrCreateGroup(
                    settings,
                    groupName
                );

            if (!AddressablesGroupPathConfigurator
                    .ConfigureGroupPaths(settings, group))
            {
                error =
                    "Could not configure Addressables paths for group: " +
                    groupName;

                return false;
            }

            List<UnityAssetReferenceCollector.UnityObjectReference>
                references =
                    UnityAssetReferenceCollector
                        .CollectUnityObjectReferences(template);

            HashSet<string> processedGuids =
                new(StringComparer.Ordinal);

            Dictionary<string, string> addressOwners =
                new(StringComparer.Ordinal);

            foreach (
                UnityAssetReferenceCollector.UnityObjectReference reference
                in references)
            {
                UnityEngine.Object asset =
                    reference.Asset;

                if (asset == null)
                    continue;

                string assetPath =
                    NormalizeUnityPath(
                        AssetDatabase.GetAssetPath(asset)
                    );

                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    error =
                        $"Asset '{asset.name}' is not inside the " +
                        "Unity project and cannot be Addressable.";

                    return false;
                }

                if (!IsInsideFolder(
                        assetPath,
                        projectRootFolderPath))
                {
                    error =
                        $"Asset '{asset.name}' does not belong to the " +
                        $"selected THEODEN project.\n" +
                        $"Expected inside: {projectRootFolderPath}\n" +
                        $"Actual path: {assetPath}";

                    return false;
                }

                string guid =
                    AssetDatabase.AssetPathToGUID(assetPath);

                if (string.IsNullOrWhiteSpace(guid))
                {
                    error =
                        "Could not resolve GUID for asset: " +
                        assetPath;

                    return false;
                }

                if (!processedGuids.Add(guid))
                    continue;

                string address =
                    MediaAddressResolver.ResolveAddress(
                        asset,
                        projectId,
                        poiId,
                        reference.SourceField
                    );

                if (string.IsNullOrWhiteSpace(address))
                {
                    error =
                        "Could not resolve Addressables address for asset: " +
                        assetPath;

                    return false;
                }

                if (addressOwners.TryGetValue(
                        address,
                        out string existingGuid) &&
                    !string.Equals(
                        existingGuid,
                        guid,
                        StringComparison.Ordinal))
                {
                    string existingAssetPath =
                        AssetDatabase.GUIDToAssetPath(
                            existingGuid
                        );

                    error =
                        $"Two different assets resolve to the same " +
                        $"Addressables address '{address}'.\n" +
                        $"First asset: {existingAssetPath}\n" +
                        $"Second asset: {assetPath}";

                    return false;
                }

                addressOwners[address] = guid;

                AddressableAssetEntry entry =
                    settings.CreateOrMoveEntry(
                        guid,
                        group
                    );

                RemoveOldTheodenLabels(entry);

                entry.address = address;
                entry.SetLabel(
                    label,
                    true,
                    true
                );
            }

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                group,
                true
            );

            AssetDatabase.SaveAssets();

            return true;
        }

        /// <summary>
        /// Temporary compatibility overload. Existing callers compile,
        /// but must be migrated before exporting again.
        /// </summary>
        [Obsolete(
            "Use SetupAddressablesForTemplate with projectId and " +
            "projectRootFolderPath."
        )]
        public static bool SetupAddressablesForTemplate(
            LevelTemplateBase template,
            string poiId,
            string poiRootFolderPath,
            out string error)
        {
            error =
                "The POI exporter still uses the legacy Addressables " +
                "setup method. Migrate PoiExportService before exporting.";

            return false;
        }

        /// <summary>
        /// Removes labels generated by older THEODEN export operations.
        /// Labels unrelated to THEODEN are preserved.
        /// </summary>
        internal static void RemoveOldTheodenLabels(
            AddressableAssetEntry entry)
        {
            if (entry?.labels == null)
                return;

            string[] existingLabels =
                entry.labels.ToArray();

            foreach (string existingLabel in existingLabels)
            {
                if (!IsTheodenManagedLabel(existingLabel))
                    continue;

                entry.SetLabel(
                    existingLabel,
                    false,
                    false
                );
            }
        }

        private static bool IsTheodenManagedLabel(
            string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return false;

            return
                label.StartsWith(
                    "theoden_",
                    StringComparison.OrdinalIgnoreCase) ||
                label.StartsWith(
                    "poi_",
                    StringComparison.OrdinalIgnoreCase) ||
                label.StartsWith(
                    "directions_",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    label,
                    "codex",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static AddressableAssetGroup GetOrCreateGroup(
            AddressableAssetSettings settings,
            string groupName)
        {
            AddressableAssetGroup group =
                settings.FindGroup(groupName);

            if (group != null)
                return group;

            return settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                settings.DefaultGroup.Schemas
            );
        }

        private static bool IsInsideFolder(
            string assetPath,
            string folderPath)
        {
            assetPath =
                NormalizeUnityPath(assetPath);

            folderPath =
                NormalizeUnityPath(folderPath)
                    .TrimEnd('/');

            return
                assetPath.Equals(
                    folderPath,
                    StringComparison.Ordinal) ||
                assetPath.StartsWith(
                    folderPath + "/",
                    StringComparison.Ordinal);
        }

        private static string NormalizeUnityPath(
            string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }
    }
}