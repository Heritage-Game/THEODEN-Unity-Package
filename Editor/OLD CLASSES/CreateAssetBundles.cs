using System.IO;
using UnityEditor;

namespace Theoden.Editor
{
    public static class CreateAssetBundles
    {
        private static void BuildAllAssetBundles(BuildTarget os)
        {
            var assetBundleDirectory = "Assets/AssetBundles/" + os;
            if (!Directory.Exists(assetBundleDirectory)) Directory.CreateDirectory(assetBundleDirectory);

            BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                BuildAssetBundleOptions.None,
                os);
        }

        [MenuItem("Asset Bundles/Build AssetBundles/iOS")]
        private static void BuildiOSAssetBundles()
        {
            BuildAllAssetBundles(BuildTarget.iOS);
        }

        [MenuItem("Asset Bundles/Build AssetBundles/Android")]
        private static void BuildAndroidAssetBundles()
        {
            BuildAllAssetBundles(BuildTarget.Android);
            //forse dovrebbe essere 
        }

        public static void BuildAllAssetBundles()
        {
            BuildAndroidAssetBundles();
            BuildiOSAssetBundles();
        }
    }
}