using System.Collections.Generic;
using UnityEditor;

namespace Theoden.Editor.Build
{
    /// <summary>
    /// Provides the ordered collection of runtime scenes included
    /// in every THEODEN application build.
    /// </summary>
    public static class TheodenBuildSceneProvider
    {
        private const string ScenesRoot =
            "Packages/it.unicam.theoden/Runtime/Scenes";

        /// <summary>
        /// Ordered runtime scenes. The first scene is the application
        /// entry point.
        /// </summary>
        private static readonly string[] ScenePaths =
        {
            $"{ScenesRoot}/SplashUIToolkit.unity",
            $"{ScenesRoot}/InstructionsUIToolkit.unity",
            $"{ScenesRoot}/LanguageUIToolkit.unity",
            $"{ScenesRoot}/NicknameUIToolkit.unity",
            $"{ScenesRoot}/MenuUIToolkit.unity",
            $"{ScenesRoot}/CodexUIToolkit.unity",
            $"{ScenesRoot}/CodexDetailUIToolkit.unity",
            $"{ScenesRoot}/QRScannerUIToolkit.unity",
            $"{ScenesRoot}/POISummaryUIToolkit.unity",
            $"{ScenesRoot}/CodexInitialUIToolkit.unity",
            $"{ScenesRoot}/ChallengeUIToolkit.unity",
            $"{ScenesRoot}/POIRecapUIToolkit.unity",
            $"{ScenesRoot}/LeaderboardUIToolkit.unity"
        };

        /// <summary>
        /// Returns a copy of the ordered scene paths used by the build.
        /// </summary>
        public static string[] GetScenePaths()
        {
            return (string[])ScenePaths.Clone();
        }

        /// <summary>
        /// Returns all configured scene paths that cannot be found.
        /// </summary>
        public static IReadOnlyList<string> GetMissingScenePaths()
        {
            List<string> missingScenes = new();

            foreach (string scenePath in ScenePaths)
            {
                SceneAsset scene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

                if (scene == null)
                    missingScenes.Add(scenePath);
            }

            return missingScenes;
        }
    }
}
