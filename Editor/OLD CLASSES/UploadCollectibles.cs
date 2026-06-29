using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
//using Defective.JSON;

namespace Theoden.Editor
{
    public static class UploadCollectibles
    {
        private const string CollectiblesCsvName = "Assets/Resources/Collezionabili";
        private const string BaseUrl = CommonVariables.URL + "/collectible";

        private static void Upload(string stopName, string language)
        {
            var json = JSONObject.Create(
                string.Join("\n",
                    FileUtilities.ReadCompleteFile(CollectiblesCsvName + '/' + stopName + '_' + language + ".json")));
            for (var i = 0; i < json.Count; i++)
            {
                var body = "{" +
                           "\"stop_id\": " + CleanJson.GetString(json[i]["codex_id"]) + "," +
                           "\"language\": \"" + CleanJson.GetString(json[i]["language"]) + "\"," +
                           "\"collectible_code\": " + CleanJson.GetString(json[i]["collectible_code"]) + "," +
                           "\"name\": \"" + CleanJson.GetString(json[i]["name"]) + "\", " +
                           "\"description\": \"" + CleanJson.GetString(json[i]["description"]) + "\"" +
                           "}";
                var request = WebRequest.Post(BaseUrl, body);
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogError("Failed to upload " + CleanJson.GetString(json[i]["name"]) + ": " + request.error + " " +
                                   request.downloadHandler.text);
                else
                    Debug.LogWarning(request.result + " " + request.downloadHandler.text);
            }

            Debug.Log("Upload Complete for collectible: " + stopName + " in language: " + language);
        }

        private static void UploadLists(IEnumerable<string> stopNames, string[] languages)
        {
            foreach (var stopName in stopNames)
            {
                Debug.Log("Uploading collectibles for " + stopName);
                foreach (var language in languages)
                    if (File.Exists(Path.Combine(CollectiblesCsvName, stopName + "_" + language + ".json")))
                        Upload(stopName, language);
                    else
                        Debug.LogError("File not found: " + stopName + "_" + language + ".json");
            }
        }

        //used to upload all collectibles
        [MenuItem("Server Upload/Collectibles/UploadAll")]
        public static void UploadAll()
        {
            var stopList = new List<string>();
            foreach (var tmp in 
                     CreateJsonFromAssetBundleManifest.codex
                         .Select(dictionary => dictionary.Keys.ToList()))
            {
                stopList.AddRange(tmp);
            }
            //UploadLists(stopList.ToArray(), UploadLanguages.Languages.ToArray());
        }
    }
}