using System;
using Config;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Build
{
    /// <summary>
    /// Creates or updates the configuration required by the
    /// THEODEN application at runtime.
    /// </summary>
    public static class TheodenRuntimeConfigGenerator
    {
        public const string RuntimeConfigFolderPath =
            "Assets/Resources/THEODEN";

        public const string RuntimeConfigAssetPath =
            RuntimeConfigFolderPath +
            "/TheodenRuntimeConfig.asset";

        /// <summary>
        /// Generates the runtime configuration for the
        /// selected THEODEN project.
        /// </summary>
        public static bool CreateOrUpdate(
            TheodenProjectContext context,
            out string error)
        {
            error = null;

            if (context == null)
            {
                error =
                    "The THEODEN project context is null.";

                return false;
            }

            if (context.theodenProjectConfig == null)
            {
                error =
                    "The selected project configuration is missing.";

                return false;
            }

            if (context.poiRegistry == null)
            {
                error =
                    "The selected project's POIRegistry is missing.";

                return false;
            }

            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            string projectId =
                projectConfig.projectId?.Trim();

            if (string.IsNullOrWhiteSpace(projectId))
            {
                error =
                    "The THEODEN project identifier is empty.";

                return false;
            }

            bool useLeaderboard =
                projectConfig.useLeaderboard;

            string leaderboardBaseUrl =
                projectConfig.leaderboardBaseUrl?.Trim();

            if (useLeaderboard &&
                !TryValidateLeaderboardUrl(
                    leaderboardBaseUrl,
                    out error))
            {
                return false;
            }

            if (!useLeaderboard)
                leaderboardBaseUrl = string.Empty;
            else
                leaderboardBaseUrl =
                    leaderboardBaseUrl.TrimEnd('/');

            int totalPoiCount =
                context.poiRegistry.Pois?.Count ?? 0;

            try
            {
                if (!EnsureOutputFolder(out error))
                    return false;

                UnityEngine.Object existingAsset =
                    AssetDatabase.LoadMainAssetAtPath(
                        RuntimeConfigAssetPath
                    );

                if (existingAsset != null &&
                    existingAsset is not TheodenRuntimeConfig)
                {
                    error =
                        $"An asset already exists at " +
                        $"'{RuntimeConfigAssetPath}', but it is not a " +
                        $"{nameof(TheodenRuntimeConfig)}.";

                    return false;
                }

                TheodenRuntimeConfig runtimeConfig =
                    existingAsset as TheodenRuntimeConfig;

                bool wasCreated = false;

                if (!runtimeConfig)
                {
                    runtimeConfig =
                        ScriptableObject.CreateInstance<
                            TheodenRuntimeConfig>();

                    AssetDatabase.CreateAsset(
                        runtimeConfig,
                        RuntimeConfigAssetPath
                    );

                    wasCreated = true;
                }

                SerializedObject serializedConfig =
                    new SerializedObject(runtimeConfig);

                serializedConfig.Update();

                SerializedProperty projectIdProperty =
                    serializedConfig.FindProperty(
                        "projectId"
                    );

                SerializedProperty useLeaderboardProperty =
                    serializedConfig.FindProperty(
                        "useLeaderboard"
                    );

                SerializedProperty leaderboardBaseUrlProperty =
                    serializedConfig.FindProperty(
                        "leaderboardBaseUrl"
                    );

                SerializedProperty totalPoiCountProperty =
                    serializedConfig.FindProperty(
                        "totalPoiCount"
                    );

                if (projectIdProperty == null ||
                    useLeaderboardProperty == null ||
                    leaderboardBaseUrlProperty == null ||
                    totalPoiCountProperty == null)
                {
                    error =
                        "One or more serialized fields are missing " +
                        "from TheodenRuntimeConfig.";

                    return false;
                }

                projectIdProperty.stringValue =
                    projectId;

                useLeaderboardProperty.boolValue =
                    useLeaderboard;

                leaderboardBaseUrlProperty.stringValue =
                    leaderboardBaseUrl;

                totalPoiCountProperty.intValue =
                    totalPoiCount;

                serializedConfig
                    .ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(runtimeConfig);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[TheodenRuntimeConfigGenerator] Runtime config " +
                    $"{(wasCreated ? "created" : "updated")}: " +
                    $"{RuntimeConfigAssetPath} | " +
                    $"projectId: '{projectId}' | " +
                    $"leaderboard: " +
                    $"{(useLeaderboard ? "enabled" : "disabled")} | " +
                    $"total POIs: {totalPoiCount}"
                );

                return true;
            }
            catch (Exception exception)
            {
                error =
                    "Could not generate the THEODEN runtime " +
                    "configuration: " +
                    exception.Message;

                Debug.LogException(exception);
                return false;
            }
        }

        private static bool TryValidateLeaderboardUrl(
            string leaderboardBaseUrl,
            out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(
                    leaderboardBaseUrl))
            {
                error =
                    "The leaderboard is enabled, but its API " +
                    "URL is empty.";

                return false;
            }

            bool isValidUrl =
                Uri.TryCreate(
                    leaderboardBaseUrl,
                    UriKind.Absolute,
                    out Uri parsedUri
                ) &&
                (parsedUri.Scheme == Uri.UriSchemeHttp ||
                 parsedUri.Scheme == Uri.UriSchemeHttps);

            if (!isValidUrl)
            {
                error =
                    "The leaderboard API URL must be a valid " +
                    "absolute HTTP or HTTPS URL.";

                return false;
            }

            return true;
        }

        private static bool EnsureOutputFolder(
            out string error)
        {
            error = null;

            if (!AssetDatabase.IsValidFolder(
                    "Assets/Resources"))
            {
                string resourcesGuid =
                    AssetDatabase.CreateFolder(
                        "Assets",
                        "Resources"
                    );

                if (string.IsNullOrWhiteSpace(resourcesGuid))
                {
                    error =
                        "Could not create Assets/Resources.";

                    return false;
                }
            }

            if (!AssetDatabase.IsValidFolder(
                    RuntimeConfigFolderPath))
            {
                string theodenFolderGuid =
                    AssetDatabase.CreateFolder(
                        "Assets/Resources",
                        "THEODEN"
                    );

                if (string.IsNullOrWhiteSpace(
                        theodenFolderGuid))
                {
                    error =
                        $"Could not create " +
                        $"{RuntimeConfigFolderPath}.";

                    return false;
                }
            }

            return true;
        }
    }
}