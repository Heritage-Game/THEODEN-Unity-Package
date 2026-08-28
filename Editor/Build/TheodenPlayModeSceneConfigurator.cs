using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Build
{
    /// <summary>
    /// Ensures that all THEODEN runtime scenes are available
    /// when testing the application in Play Mode.
    /// </summary>
    public static class TheodenPlayModeSceneConfigurator
    {
        /// <summary>
        /// Adds missing THEODEN scenes to the active Editor
        /// scene list and enables any disabled ones.
        ///
        /// Existing non-THEODEN scenes are preserved.
        /// </summary>
        public static bool EnsureScenesAreAvailable(
            out string error)
        {
            error = null;

            IReadOnlyList<string> missingSceneAssets =
                TheodenBuildSceneProvider
                    .GetMissingScenePaths();

            if (missingSceneAssets.Count > 0)
            {
                error =
                    "The following THEODEN scene assets " +
                    "could not be found:\n" +
                    string.Join(
                        "\n",
                        missingSceneAssets
                    );

                return false;
            }

            try
            {
                List<EditorBuildSettingsScene>
                    configuredScenes =
                        new List<EditorBuildSettingsScene>(
                            EditorBuildSettings.scenes
                        );

                Dictionary<string, int> sceneIndices =
                    new Dictionary<string, int>(
                        StringComparer.OrdinalIgnoreCase
                    );

                for (int index = 0;
                     index < configuredScenes.Count;
                     index++)
                {
                    string path =
                        configuredScenes[index].path;

                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    if (!sceneIndices.ContainsKey(path))
                        sceneIndices.Add(path, index);
                }

                string[] theodenScenePaths =
                    TheodenBuildSceneProvider
                        .GetScenePaths();

                int addedSceneCount = 0;
                int enabledSceneCount = 0;

                foreach (string scenePath
                         in theodenScenePaths)
                {
                    if (sceneIndices.TryGetValue(
                            scenePath,
                            out int existingIndex))
                    {
                        if (configuredScenes[
                                existingIndex].enabled)
                        {
                            continue;
                        }

                        configuredScenes[existingIndex] =
                            new EditorBuildSettingsScene(
                                scenePath,
                                true
                            );

                        enabledSceneCount++;
                        continue;
                    }

                    configuredScenes.Add(
                        new EditorBuildSettingsScene(
                            scenePath,
                            true
                        )
                    );

                    sceneIndices.Add(
                        scenePath,
                        configuredScenes.Count - 1
                    );

                    addedSceneCount++;
                }

                bool configurationChanged =
                    addedSceneCount > 0 ||
                    enabledSceneCount > 0;

                if (configurationChanged)
                {
                    EditorBuildSettings.scenes =
                        configuredScenes.ToArray();

                    AssetDatabase.SaveAssets();
                }

                Debug.Log(
                    "[TheodenPlayModeSceneConfigurator] " +
                    "Play Mode scene list configured. " +
                    $"Added: {addedSceneCount} | " +
                    $"Enabled: {enabledSceneCount}"
                );

                return true;
            }
            catch (Exception exception)
            {
                error =
                    "Could not configure the THEODEN scenes " +
                    "for Play Mode: " +
                    exception.Message;

                Debug.LogException(exception);
                return false;
            }
        }
    }
}