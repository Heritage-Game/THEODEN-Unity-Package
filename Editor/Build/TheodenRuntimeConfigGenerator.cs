using System;
using Config;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Build
{
    /// <summary>
    /// Creates or updates the local configuration required by the
    /// THEODEN runtime before Addressables content can be loaded.
    /// </summary>
    public static class TheodenRuntimeConfigGenerator
    {
        public const string RuntimeConfigFolderPath =
            "Assets/Resources/THEODEN";

        public const string RuntimeConfigAssetPath =
            RuntimeConfigFolderPath +
            "/TheodenRuntimeConfig.asset";

        /// <summary>
        /// Generates the runtime configuration for the selected project.
        /// </summary>
        public static bool CreateOrUpdate(
            TheodenProjectContext context,
            out string error)
        {
            error = null;

            if (context == null)
            {
                error = "The THEODEN project context is null.";
                return false;
            }

            if (context.theodenProjectConfig == null)
            {
                error = "The selected project configuration is missing.";
                return false;
            }

            return CreateOrUpdate(
                context.theodenProjectConfig,
                out error
            );
        }

        /// <summary>
        /// Generates the runtime configuration from a project config.
        /// </summary>
        public static bool CreateOrUpdate(
            TheodenProjectConfig projectConfig,
            out string error)
        {
            error = null;

            if (projectConfig == null)
            {
                error = "The THEODEN project configuration is null.";
                return false;
            }

            string projectId = projectConfig.projectId?.Trim();

            if (string.IsNullOrWhiteSpace(projectId))
            {
                error =
                    "The THEODEN project identifier is empty.";

                return false;
            }

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

                if (runtimeConfig == null)
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

                SerializedProperty projectIdProperty =
                    serializedConfig.FindProperty("projectId");

                if (projectIdProperty == null)
                {
                    error =
                        "Could not find the serialized projectId " +
                        "field in TheodenRuntimeConfig.";

                    return false;
                }

                projectIdProperty.stringValue = projectId;

                serializedConfig.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(runtimeConfig);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"[TheodenRuntimeConfigGenerator] Runtime config " +
                    $"{(wasCreated ? "created" : "updated")}: " +
                    $"{RuntimeConfigAssetPath} | projectId: " +
                    $"'{projectId}'"
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

        private static bool EnsureOutputFolder(
            out string error)
        {
            error = null;

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
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

                if (string.IsNullOrWhiteSpace(theodenFolderGuid))
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