using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Utility class that configures Addressables group build/load paths
    /// according to the active Addressables profile.
    /// </summary>
    /// <remarks>
    /// If the active profile has a valid RemoteLoadPath, groups are configured as remote:
    ///
    /// BuildPath = RemoteBuildPath
    /// LoadPath = RemoteLoadPath
    ///
    /// If RemoteLoadPath is empty, groups are configured as local:
    ///
    /// BuildPath = LocalBuildPath
    /// LoadPath = LocalLoadPath
    ///
    /// This allows the same THEODEN export pipeline to support both:
    /// - remote content updates through server-hosted Addressables bundles;
    /// - local content included in the application build.
    /// </remarks>
    public static class AddressablesGroupPathConfigurator
    {
        /// <summary>
        /// Configures the build and load paths of an Addressables group.
        /// </summary>
        /// <param name="settings">
        /// Active Addressables settings.
        /// </param>
        /// <param name="group">
        /// Group to configure.
        /// </param>
        /// <returns>
        /// True if the group was configured successfully; otherwise false.
        /// </returns>
        public static bool ConfigureGroupPaths(
            AddressableAssetSettings settings,
            AddressableAssetGroup group)
        {
            if (settings == null)
            {
                Debug.LogError("[AddressablesGroupPathConfigurator] Settings are null.");
                return false;
            }

            if (group == null)
            {
                Debug.LogError("[AddressablesGroupPathConfigurator] Group is null.");
                return false;
            }

            BundledAssetGroupSchema bundledSchema =
                group.GetSchema<BundledAssetGroupSchema>();

            if (bundledSchema == null)
            {
                bundledSchema = group.AddSchema<BundledAssetGroupSchema>();
            }

            ContentUpdateGroupSchema contentUpdateSchema =
                group.GetSchema<ContentUpdateGroupSchema>();

            if (contentUpdateSchema == null)
            {
                contentUpdateSchema = group.AddSchema<ContentUpdateGroupSchema>();
            }
            bool useRemotePaths =
                HasProfilePath(
                    settings,
                    AddressableAssetSettings.kRemoteBuildPath
                ) &&
                HasProfilePath(
                    settings,
                    AddressableAssetSettings.kRemoteLoadPath
                );

            string buildPathVariable = useRemotePaths
                ? AddressableAssetSettings.kRemoteBuildPath
                : AddressableAssetSettings.kLocalBuildPath;

            string loadPathVariable = useRemotePaths
                ? AddressableAssetSettings.kRemoteLoadPath
                : AddressableAssetSettings.kLocalLoadPath;

            bool buildPathConfigured =
                bundledSchema.BuildPath.SetVariableByName(
                    settings,
                    buildPathVariable
                );

            bool loadPathConfigured =
                bundledSchema.LoadPath.SetVariableByName(
                    settings,
                    loadPathVariable
                );

            if (!buildPathConfigured || !loadPathConfigured)
            {
                Debug.LogError(
                    "[AddressablesGroupPathConfigurator] Could not configure " +
                    $"the paths for group '{group.Name}'. " +
                    $"Build variable: '{buildPathVariable}', " +
                    $"Load variable: '{loadPathVariable}'."
                );

                return false;
            }

            Debug.Log(
                "[AddressablesGroupPathConfigurator] Group configured as " +
                $"{(useRemotePaths ? "REMOTE" : "LOCAL")}: {group.Name}"
            );

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.GroupSchemaModified,
                group,
                true
            );

            return true;
        }
        
        /// <summary>
        /// Checks whether the active Addressables profile has a valid path for the variable.
        /// </summary>
        /// <param name="settings">
        /// Active Addressables settings.
        /// </param>
        /// <returns>
        /// True if the path is configured; otherwise false.
        /// </returns>
        private static bool HasProfilePath(
            AddressableAssetSettings settings,
            string variableName)
        {
            string value = ResolveProfilePath(
                settings,
                variableName
            );

            return !string.IsNullOrWhiteSpace(value) &&
                   !value.Contains($"[{variableName}]");
        }

        /// <summary>
        /// Checks whether the active Addressables profile has a valid RemoteLoadPath.
        /// </summary>
        /// <param name="settings">
        /// Active Addressables settings.
        /// </param>
        /// <returns>
        /// True if RemoteLoadPath is configured; otherwise false.
        /// </returns>
        public static bool HasRemoteLoadPath(
            AddressableAssetSettings settings)
        {
            return HasProfilePath(
                settings,
                AddressableAssetSettings.kRemoteLoadPath
            );
        }

        /// <summary>
        /// Resolves a profile variable value from the active Addressables profile.
        /// </summary>
        /// <param name="settings">
        /// Active Addressables settings.
        /// </param>
        /// <param name="variableName">
        /// Profile variable name.
        /// </param>
        /// <returns>
        /// Evaluated profile value, or an empty string if it cannot be resolved.
        /// </returns>
        private static string ResolveProfilePath(
            AddressableAssetSettings settings,
            string variableName)
        {
            if (settings == null || settings.profileSettings == null)
                return string.Empty;

            string profileId = settings.activeProfileId;

            string evaluated =
                settings.profileSettings.EvaluateString(profileId, "[" + variableName + "]");

            if (string.IsNullOrWhiteSpace(evaluated) ||
                evaluated.Contains("[") ||
                evaluated.Contains("]"))
            {
                evaluated = settings.profileSettings.GetValueByName(
                    profileId,
                    variableName
                );
            }

            return evaluated ?? string.Empty;
        }
    }
}