using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
//using Defective.JSON;

namespace Theoden.Editor
{
    public static class CreateJsonFromAssetBundleManifest
    {
        private const string AssetBundleDirectory = "Assets/AssetBundles";
        public static List<Dictionary<string, string>> codex;
        public static void CreateAllJson()
        {
            CreateJsonAndroid();
            CreateJsoniOS();
        }
    
        [MenuItem("Asset Bundles/Create JSON with AssetBundle.manifest/iOS")]
        private static void CreateJsoniOS()
        {
            CreateJson("iOS");
        }

        [MenuItem("Asset Bundles/Create JSON with AssetBundle.manifest/Android")]
        private static void CreateJsonAndroid()
        {
            CreateJson("Android");
        }

        private static void CreateJson(string destinationOS)
        {
            if (!Directory.Exists(AssetBundleDirectory + '/' + destinationOS)) return;
            codex = GetCodex();
            var manifest =
                FileUtilities.ReadCompleteFile(AssetBundleDirectory + '/' + destinationOS + '/' + destinationOS +
                                               ".manifest");
            var assetBundles = GetAssetBundleToDependenciesDictionary(manifest);
            var json = AssetBundleDirectoryWithCRCToJson(assetBundles, destinationOS);
            var writer = new StreamWriter(Path.Combine(AssetBundleDirectory, destinationOS, "AssetBundles.json"));
            writer.Write(json.ToString(true));
            writer.Close();
            Debug.Log("Created JSON From AssetBundlesManifest");
        }

        private static List<Dictionary<string, string>> GetCodex()
        {
            var json = string.Join("\n", FileUtilities.ReadCompleteFile("Assets/Resources/Codex/codex.json"));
            var codex = JSONObject.Create(json);
            var output = new List<Dictionary<string, string>>();
            for (var i = 0; i < codex.Count; i++)
            {
                var dict = new Dictionary<string, string>
                {
                    {CleanJson.GetString(codex[i]["name"]).ToLower(), CleanJson.GetString(codex[i]["id"])}
                };
                output.Add(dict);
            }
            return output;
        }

        private static JSONObject AssetBundleDirectoryWithCRCToJson(Dictionary<string, List<string>> assetBundles,
            string destinationOS)
        {
            var json = new JSONObject(JSONObject.Type.OBJECT);
            foreach (var assetBundle in assetBundles.Keys)
            {
                var assetBundleManifest =
                    FileUtilities.ReadCompleteFile(Path.Combine(AssetBundleDirectory, destinationOS,
                        assetBundle + ".manifest"));
                var crc = assetBundleManifest[1].Split(':')[1].Trim();
                var jsonObject = new JSONObject(JSONObject.Type.OBJECT);
                jsonObject.AddField("crc", crc);
                jsonObject.AddField("assetBundle", assetBundle);
                jsonObject.AddField("destination_os", destinationOS.ToLower());
                var codexStop = FindCodexStopId(assetBundle);
                jsonObject.AddField("codex_stop", codexStop);
                var dependencies = new JSONObject(JSONObject.Type.ARRAY);
                foreach (var dependency in assetBundles[assetBundle]) dependencies.Add(dependency);
                jsonObject.AddField("dependencies", dependencies);
                json.Add(jsonObject);
            }

            return json;
        }

        private static string FindCodexStopId(string assetBundleName)
        {
            Dictionary<string, string> codexStop;
            return (codexStop = codex.Find(e => e.ContainsKey(assetBundleName))) != null ? codexStop[assetBundleName] : "null";
        }

        private static Dictionary<string, List<string>> GetAssetBundleToDependenciesDictionary(
            IReadOnlyList<string> manifest)
        {
            var i = 0;
            var assetBundle = new Dictionary<string, List<string>>();
            while (i < manifest.Count)
            {
                if (!manifest[i].Trim().StartsWith("Info_"))
                {
                    i += 1;
                    continue;
                }

                i += 1;
                var assetBundleName = manifest[i].Split(':')[1].Trim();
                var assetBundleDependencies = new List<string>();
                assetBundle[assetBundleName] = assetBundleDependencies;
                if (i + 2 >= manifest.Count || !manifest[i + 2].Trim().StartsWith("Dependency_")) continue;
                i += 2;
                while (i < manifest.Count && !manifest[i].Trim().StartsWith("Info_"))
                {
                    assetBundleDependencies.Add(manifest[i].Split(':')[1].Trim());
                    i += 1;
                }

                assetBundle[assetBundleName] = assetBundleDependencies;
            }

            return assetBundle;
        }
    }
}