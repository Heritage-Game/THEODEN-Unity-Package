using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
//using Defective.JSON;

namespace Theoden.Editor
{
    public static class UploadAssetBundlesAndJsonToServer
    {
        private const string BaseURL = CommonVariables.URL + "/asset";
        private const string AssetBundleDirectory = "Assets/AssetBundles";

        public static void UploadAllAssetBundles()
        {
            UploadAndroid();
            UploadiOS();
        }
    
        [MenuItem("Server Upload/Upload AssetBundles and JSON to Server/iOS")]
        private static void UploadiOS()
        {
            Upload("iOS");
        }

        [MenuItem("Server Upload/Upload AssetBundles and JSON to Server/Android")]
        private static void UploadAndroid()
        {
            Upload("Android");
        }

        private static void Upload(string os)
        {
            var assetBundlesJson = AssetBundleDirectory + '/' + os + '/' + "AssetBundles.json";
            var json = JSONObject.Create(string.Join("\n", FileUtilities.ReadCompleteFile(assetBundlesJson)));
            json = SortAssetBundles(json);
            for (var i = 0; i < json.Count; ++i)
            {
                var jsonDependencies = json[i]["dependencies"];
                var dependencies = "";
                for (var j = 0; j < jsonDependencies.Count; ++j)
                    if (jsonDependencies[j] != null)
                        dependencies += CleanJson.GetString(jsonDependencies[j]) + ";";

                var form = new WWWForm();
                form.AddField("name", CleanJson.GetString(json[i]["assetBundle"]));
                form.AddField("crc", CleanJson.GetString(json[i]["crc"]));
                form.AddField("destination_os", CleanJson.GetString(json[i]["destination_os"]));
                form.AddField("dependencies", dependencies);
                form.AddBinaryData("assetBundle",
                    File.ReadAllBytes(AssetBundleDirectory + '/' + os + '/' + CleanJson.GetString(json[i]["assetBundle"])),
                    CleanJson.GetString(json[i]["assetBundle"]));
                form.AddField("codex_stop", CleanJson.GetString(json[i]["codex_stop"]));
                var request = UnityWebRequest.Post(BaseURL, form);
                request.SetRequestHeader("Authorization", "Token " + WebRequest.token);
                request.SendWebRequest();
                while (!request.isDone)
                {
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(request.error);
                    Debug.LogError(request.downloadHandler.text);
                }
                else
                {
                    Debug.Log("Form " + CleanJson.GetString(json[i]["assetBundle"]) + " upload complete!");
                    Debug.LogWarning(request.downloadHandler.text);
                }
            }

            Debug.Log("All AssetBundles Have Been Uploaded to server");
        }

        private static JSONObject SortAssetBundles(JSONObject json)
        {
            var sortedJson = CleanJson.Filter(json, o => o["dependencies"].Count == 0);
            while (sortedJson.Count != json.Count)
                for (var i = 0; i < json.Count; i++)
                {
                    if (JsonContains(sortedJson, CleanJson.GetString(json[i]["assetBundle"]), "assetBundle")) continue;
                    var areAllDependenciesContained = true;
                    for (var j = 0; j < json[i]["dependencies"].Count; j++)
                    {
                        if (JsonContains(sortedJson, CleanJson.GetString(json[i]["dependencies"][j]), "assetBundle"))
                            continue;
                        areAllDependenciesContained = false;
                        break;
                    }

                    if (areAllDependenciesContained) sortedJson.Add(json[i]);
                }

            return sortedJson;
        }

        private static bool JsonContains(JSONObject json, string value, string label)
        {
            for (var i = 0; i < json.Count; i++)
                if (CleanJson.GetString(json[i][label]).Equals(value))
                    return true;
            return false;
        }
    }
}