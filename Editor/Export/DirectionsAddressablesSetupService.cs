using System;
using System.Collections.Generic;
using Addressing;
using RuntimeModelsForEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Registers Directions media inside a project-specific
    /// Addressables group.
    /// </summary>
    public static class DirectionsAddressablesSetupService
    {
        public static bool SetupAddressablesForDirections(
            DirectionsToPOIData directionsData,
            string projectId,
            string poiId,
            string mediaFolderPath,
            out string error)
        {
            error = null;

            if (directionsData == null)
            {
                error = "Directions data is null.";
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

            mediaFolderPath =
                NormalizeUnityPath(mediaFolderPath)
                    .TrimEnd('/');

            if (string.IsNullOrWhiteSpace(mediaFolderPath) ||
                !mediaFolderPath.Equals(
                    "Assets",
                    StringComparison.Ordinal) &&
                !mediaFolderPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                error =
                    "Invalid Media folder path: " +
                    mediaFolderPath;

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
                TheodenAddressablesNaming
                    .GetDirectionsGroupName(
                        projectId,
                        poiId
                    );

            string label =
                TheodenAddressablesNaming
                    .GetDirectionsLabel(
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
                    "Could not configure Addressables paths " +
                    "for group: " +
                    groupName;

                return false;
            }

            List<UnityAssetReferenceCollector.UnityObjectReference>
                references =
                    UnityAssetReferenceCollector
                        .CollectUnityObjectReferences(
                            directionsData
                        );

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
                        mediaFolderPath))
                {
                    error =
                        $"Asset '{asset.name}' is outside the " +
                        $"selected project's Media folder.\n" +
                        $"Expected inside: {mediaFolderPath}\n" +
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
                    DirectionsMediaAddressResolver.ResolveAddress(
                        asset,
                        projectId,
                        poiId,
                        reference.SourceField
                    );

                if (string.IsNullOrWhiteSpace(address))
                {
                    error =
                        "Could not resolve Addressables address " +
                        "for asset: " +
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
                        $"Directions address '{address}'.\n" +
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

                PoiAddressablesSetupService
                    .RemoveOldTheodenLabels(entry);

                entry.address = address;

                entry.SetLabel(
                    label,
                    true,
                    true
                );
            }

            settings.SetDirty(
                AddressableAssetSettings
                    .ModificationEvent.EntryMoved,
                group,
                true
            );

            AssetDatabase.SaveAssets();

            return true;
        }

        /// <summary>
        /// Temporary overload for the existing Directions exporter.
        /// </summary>
        [Obsolete(
            "Use SetupAddressablesForDirections with projectId."
        )]
        public static bool SetupAddressablesForDirections(
            DirectionsToPOIData directionsData,
            string poiId,
            string mediaFolderPath,
            out string error)
        {
            error =
                "DirectionsExportService still uses the legacy " +
                "Addressables setup method. Migrate it before exporting.";

            return false;
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