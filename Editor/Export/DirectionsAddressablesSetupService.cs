using Addressing;
using RuntimeModelsForEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor.Export
{
    /// <summary>
    /// Registers directions media assets inside the Addressables system.
    /// </summary>
    public static class DirectionsAddressablesSetupService
    {
        /// <summary>
        /// Sets up Addressables entries for all Unity assets referenced by directions data.
        /// </summary>
        /// <param name="directionsData">
        /// The directions export data that contains references to sprites and audio clips.
        /// </param>
        /// <param name="poiId">
        /// The id of the POI associated with the directions data.
        /// </param>
        /// <param name="mediaFolderPath">
        /// The Unity project-relative Media folder path.
        /// Expected format: Assets/&lt;ProjectName&gt;/Media.
        /// </param>
        /// <param name="error">
        /// Output error message if the setup fails.
        /// </param>
        /// <returns>
        /// True if all valid media assets were registered correctly; otherwise false.
        /// </returns>
        public static bool SetupAddressablesForDirections(
            DirectionsToPOIData directionsData,
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

            if (string.IsNullOrWhiteSpace(poiId))
            {
                error = "POI id is missing.";
                return false;
            }

            mediaFolderPath = NormalizeUnityPath(mediaFolderPath);

            if (string.IsNullOrWhiteSpace(mediaFolderPath) ||
                !mediaFolderPath.StartsWith("Assets"))
            {
                error = $"Invalid Media folder path: {mediaFolderPath}";
                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                error = "Addressables settings not found. Initialize Addressables first.";
                return false;
            }

            string groupName = TheodenAddressablesNaming.GetDirectionsGroupName(poiId);
            string label = TheodenAddressablesNaming.GetDirectionsLabel(poiId);

            AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);

            if (!AddressablesGroupPathConfigurator.ConfigureGroupPaths(settings, group))
            {
                error = $"Could not configure Addressables paths for group: {groupName}";
                return false;
            }

            var references = UnityAssetReferenceCollector.CollectUnityObjectReferences(directionsData);

            foreach (var reference in references)
            {
                UnityEngine.Object asset = reference.Asset;

                if (asset == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(asset);

                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    error = $"Asset '{asset.name}' is not inside the project and cannot be Addressable.";
                    return false;
                }

                assetPath = NormalizeUnityPath(assetPath);

                if (!IsInsideFolder(assetPath, mediaFolderPath))
                {
                    error =
                        $"Asset '{asset.name}' is outside the project Media folder.\n" +
                        $"Expected inside: {mediaFolderPath}\n" +
                        $"Actual path: {assetPath}";

                    return false;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (string.IsNullOrWhiteSpace(guid))
                {
                    error = $"Could not resolve GUID for asset: {assetPath}";
                    return false;
                }

                string address = DirectionsMediaAddressResolver.ResolveAddress(
                    asset,
                    poiId,
                    reference.SourceField
                );

                if (string.IsNullOrWhiteSpace(address))
                {
                    error = $"Could not resolve Addressables address for asset: {assetPath}";
                    return false;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

                entry.address = address;
                entry.SetLabel(label, true, true);
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
        /// Gets an existing Addressables group or creates it if missing.
        /// </summary>
        private static AddressableAssetGroup GetOrCreateGroup(
            AddressableAssetSettings settings,
            string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);

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

        /// <summary>
        /// Checks whether an asset path is inside a given folder path.
        /// </summary>
        private static bool IsInsideFolder(string assetPath, string folderPath)
        {
            assetPath = NormalizeUnityPath(assetPath);
            folderPath = NormalizeUnityPath(folderPath).TrimEnd('/');

            return assetPath.Equals(folderPath) ||
                   assetPath.StartsWith(folderPath + "/");
        }

        /// <summary>
        /// Normalizes a Unity project path to forward-slash format.
        /// </summary>
        private static string NormalizeUnityPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }
    }
}