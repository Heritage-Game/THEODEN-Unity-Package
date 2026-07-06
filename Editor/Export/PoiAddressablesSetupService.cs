using Addressing;
using Theoden.Editor.POIDefinitionClasses;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

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
        /// Sets up Addressables entries for all Unity asset references contained in a POI template.
        /// </summary>
        /// <param name="template">
        /// The POI template to scan for Unity asset references.
        /// </param>
        /// <param name="poiId">
        /// The unique id of the Point of Interest associated with the template.
        /// </param>
        /// <param name="poiRootFolderPath">
        /// The Unity project-relative root folder of the POI.
        /// Expected format: Assets/&lt;ProjectName&gt;/POIs/&lt;poiId&gt;.
        /// </param>
        /// <param name="error">
        /// Output error message if the setup fails.
        /// </param>
        /// <returns>
        /// True if Addressables setup completed successfully; otherwise false.
        /// </returns>
        public static bool SetupAddressablesForTemplate(
            LevelTemplateBase template,
            string poiId,
            string poiRootFolderPath,
            out string error)
        {
            error = null;

            if (template == null)
            {
                error = "Template is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(poiId))
            {
                error = "POI id is missing.";
                return false;
            }

            poiRootFolderPath = NormalizeUnityPath(poiRootFolderPath);

            if (string.IsNullOrWhiteSpace(poiRootFolderPath) ||
                !poiRootFolderPath.StartsWith("Assets"))
            {
                error = $"Invalid POI root folder path: {poiRootFolderPath}";
                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                error = "Addressables settings not found. Initialize Addressables first.";
                return false;
            }

            string groupName = TheodenAddressablesNaming.GetPoiGroupName(poiId);
            string label = TheodenAddressablesNaming.GetPoiLabel(poiId);

            AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);
            
            if (!AddressablesGroupPathConfigurator.ConfigureGroupPaths(settings, group))
            {
                error = $"Could not configure Addressables paths for group: {groupName}";
                return false;
            }

            var references = UnityAssetReferenceCollector.CollectUnityObjectReferences(template);

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

                if (!IsInsideFolder(assetPath, poiRootFolderPath))
                {
                    Debug.LogWarning(
                        $"Asset '{asset.name}' is outside the POI folder.\n" +
                        $"Expected inside: {poiRootFolderPath}\n" +
                        $"Actual path: {assetPath}"
                    );
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                if (string.IsNullOrWhiteSpace(guid))
                {
                    error = $"Could not resolve GUID for asset: {assetPath}";
                    return false;
                }

                string address = MediaAddressResolver.ResolveAddress(
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
        /// Gets an existing Addressables group or creates it if it does not exist.
        /// </summary>
        /// <param name="settings">
        /// The active Addressables settings asset.
        /// </param>
        /// <param name="groupName">
        /// The name of the group to find or create.
        /// </param>
        /// <returns>
        /// The existing or newly created Addressables group.
        /// </returns>
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
        /// Checks whether a Unity asset path is inside a given folder path.
        /// </summary>
        /// <param name="assetPath">
        /// The Unity project-relative asset path.
        /// </param>
        /// <param name="folderPath">
        /// The Unity project-relative folder path.
        /// </param>
        /// <returns>
        /// True if the asset path is inside the folder path; otherwise false.
        /// </returns>
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
        /// <param name="path">
        /// The path to normalize.
        /// </param>
        /// <returns>
        /// The normalized path, or an empty string if the input is null or whitespace.
        /// </returns>
        private static string NormalizeUnityPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }
    }
}