using System;
using Addressing;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Registers the project MapDefinition in Addressables.
    /// The referenced map sprite is included as a dependency.
    /// </summary>
    public static class MapAddressablesSetupService
    {
        public static bool SetupMapDefinition(
            MapDefinition mapDefinition,
            string projectId,
            string projectRootFolderPath,
            out string error)
        {
            error = null;

            if (mapDefinition == null)
            {
                error = "MapDefinition is null.";
                return false;
            }

            if (mapDefinition.MapImage == null)
            {
                error =
                    "The MapDefinition does not contain a map image.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                error = "THEODEN project id is missing.";
                return false;
            }

            projectRootFolderPath =
                NormalizeUnityPath(projectRootFolderPath)
                    .TrimEnd('/');

            if (!IsValidProjectFolder(
                    projectRootFolderPath))
            {
                error =
                    "Invalid THEODEN project root folder: " +
                    projectRootFolderPath;

                return false;
            }

            string mapDefinitionPath =
                NormalizeUnityPath(
                    AssetDatabase.GetAssetPath(mapDefinition)
                );

            if (string.IsNullOrWhiteSpace(
                    mapDefinitionPath))
            {
                error =
                    "MapDefinition is not stored as a Unity asset.";

                return false;
            }

            if (!IsInsideFolder(
                    mapDefinitionPath,
                    projectRootFolderPath))
            {
                error =
                    "MapDefinition does not belong to the selected " +
                    "THEODEN project.\n" +
                    $"Expected inside: {projectRootFolderPath}\n" +
                    $"Actual path: {mapDefinitionPath}";

                return false;
            }

            string mapImagePath =
                NormalizeUnityPath(
                    AssetDatabase.GetAssetPath(
                        mapDefinition.MapImage
                    )
                );

            if (string.IsNullOrWhiteSpace(mapImagePath))
            {
                error =
                    "The map image is not stored as a Unity asset.";

                return false;
            }

            if (!IsInsideFolder(
                    mapImagePath,
                    projectRootFolderPath))
            {
                error =
                    "The map image does not belong to the selected " +
                    "THEODEN project.\n" +
                    $"Expected inside: {projectRootFolderPath}\n" +
                    $"Actual path: {mapImagePath}";

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
                TheodenAddressablesNaming.GetMapGroupName(
                    projectId
                );

            string label =
                TheodenAddressablesNaming.GetMapLabel(
                    projectId
                );

            AddressableAssetGroup group =
                GetOrCreateGroup(
                    settings,
                    groupName
                );

            if (!AddressablesGroupPathConfigurator
                    .ConfigureGroupPaths(
                        settings,
                        group
                    ))
            {
                error =
                    "Could not configure Addressables paths " +
                    $"for map group '{groupName}'.";

                return false;
            }

            string guid =
                AssetDatabase.AssetPathToGUID(
                    mapDefinitionPath
                );

            if (string.IsNullOrWhiteSpace(guid))
            {
                error =
                    "Could not resolve the MapDefinition GUID.";

                return false;
            }

            AddressableAssetEntry entry =
                settings.CreateOrMoveEntry(
                    guid,
                    group
                );

            PoiAddressablesSetupService
                .RemoveOldTheodenLabels(entry);

            entry.address =
                TheodenAddressablesNaming
                    .GetMapDefinitionAddress(projectId);

            entry.SetLabel(
                label,
                true,
                true
            );

            settings.SetDirty(
                AddressableAssetSettings
                    .ModificationEvent.EntryMoved,
                group,
                true
            );

            AssetDatabase.SaveAssets();

            return true;
        }

        private static AddressableAssetGroup GetOrCreateGroup(
            AddressableAssetSettings settings,
            string groupName)
        {
            AddressableAssetGroup group =
                settings.FindGroup(groupName);

            if (group != null)
            {
                return group;
            }

            return settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                settings.DefaultGroup.Schemas
            );
        }

        private static bool IsValidProjectFolder(
            string folderPath)
        {
            return
                folderPath.Equals(
                    "Assets",
                    StringComparison.Ordinal) ||
                folderPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal);
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