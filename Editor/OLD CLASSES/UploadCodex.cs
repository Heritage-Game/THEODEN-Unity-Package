using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
//using Defective.JSON;

namespace Theoden.Editor
{
    public static class UploadCodex
    {
        private const string BaseUrl = CommonVariables.URL + "/codex";
        private const string CodexJsonDirectory = "Assets/Resources/Codex";

        [MenuItem("Server Upload/Codex/Upload Generic All")]
        public static void UploadGenericStop()
        {
            var json = JSONObject.Create(string.Join("\n",
                FileUtilities.ReadCompleteFile(CodexJsonDirectory + "/codex.json")));
            for (var i = 0; i < json.Count; i++)
            {
                var body = "{" +
                           "\"id\": " + CleanJson.GetString(json[i]["id"]) + "," +
                           "\"path\": \"" + CleanJson.GetString(json[i]["path"]) + "\"," +
                           "\"name\": \"" + CleanJson.GetString(json[i]["name"]) + "\"," +
                           "\"printable_name\": \"" + CleanJson.GetString(json[i]["printable_name"]) + "\"," +
                           "\"position\": {" +
                           "\"x\": " + CleanJson.GetFloat(json[i]["position"]["x"]) + "," +
                           "\"y\": " + CleanJson.GetFloat(json[i]["position"]["y"]) +
                           "}," +
                           "\"stop_type\": " + CleanJson.GetString(json[i]["stop_type"]) + "," +
                           "\"link_maps\": \"" + CleanJson.GetString(json[i]["link_maps"]) + "\"}";

                var request = WebRequest.Post(BaseUrl + "/all", body);
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogError("Failed to upload " + CleanJson.GetString(json[i]["name"]) + ": " + request.error);
            }
        }

        [MenuItem("Server Upload/Codex/Upload Main")]
        public static void UploadMainStop()
        {
            /*
             * foreach (var language in UploadLanguages.Languages)
            {
                var json = JSONObject.Create(string.Join("\n",
                    FileUtilities.ReadCompleteFile(CodexJsonDirectory + "/codex_main_stop_" + language + ".json")));
                for (var i = 0; i < json.Count; i++)
                {
                    var body = "{\"codex_id\": \"" + CleanJson.GetString(json[i]["codex_id"]) + "\"," +
                               "\"game_story\": \"" + CleanJson.GetString(json[i]["game_story"]) + "\"," +
                               "\"real_story\": \"" + CleanJson.GetString(json[i]["real_story"]) + "\"," +
                               "\"language_code\": \"" + language + "\"}";
                    var request = WebRequest.Post(BaseUrl + "/main", body);
                    if (request.result != UnityWebRequest.Result.Success)
                        Debug.LogError("Failed to upload " + CleanJson.GetString(json[i]["codex_id"]) + ": " + request.error);
                }
            }
             */
            
        }

        [MenuItem("Server Upload/Codex/Upload Side")]
        public static void UploadSideStop()
        {
            var json = JSONObject.Create(string.Join("\n",
                FileUtilities.ReadCompleteFile(CodexJsonDirectory + "/codex_side_stop.json")));
            for (var i = 0; i < json.Count; i++)
            {
                var body = "{\"codex_id\": \"" + CleanJson.GetString(json[i]["codex_id"]) + "\"," +
                           "\"link_video\": \"" + CleanJson.GetString(json[i]["link_video"]) + "\"}";
                var request = WebRequest.Post(BaseUrl + "/side", body);
                if (request.result != UnityWebRequest.Result.Success)
                    Debug.LogError("Failed to upload " + CleanJson.GetString(json[i]["codex_id"]) + ": " + request.error);
            }
        }

        [MenuItem("Server Upload/Codex/Upload Error")]
        public static void UploadError()
        {
            /*
             * foreach (var language in UploadLanguages.Languages)
            {

                var json = JSONObject.Create(string.Join("\n",
                    FileUtilities.ReadCompleteFile(CodexJsonDirectory + "/error_messages_" + language + ".json")));
                for (var i = 0; i < json.Count; i++)
                {
                    var body = "{\"type\": \"" + CleanJson.GetString(json[i]["type"]) + "\"," +
                               "\"message\": \"" + CleanJson.GetString(json[i]["message"]) + "\"," +
                               "\"language_code\": \"" + language + "\"}";
                    var request = WebRequest.Post(BaseUrl + "/error", body);
                    if (request.result != UnityWebRequest.Result.Success)
                        Debug.LogError("Failed to upload " + CleanJson.GetString(json[i]["type"]) + ": " + request.error);
                    else
                        Debug.Log(request.downloadHandler.text);
                }
            }
             */
        }

        [MenuItem("Server Upload/Codex/Upload Codex Images")]
        public static void UploadCodexImages()
        {
            const string codexImagesJsonPath = CodexJsonDirectory + "/Images/";
            var json = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(string.Join("\n",
                FileUtilities.ReadCompleteFile(codexImagesJsonPath + "codex_images.json")));
            foreach (var element in json)
            {
                var form = new WWWForm();
                form.AddField("name", element["name"]);
                form.AddField("codex_id", element["codex_id"]);
                form.AddField("story_type", element["story_type"]);
                form.AddBinaryData("file",
                    File.ReadAllBytes(codexImagesJsonPath + element["name"] + '_' + element["story_type"] + ".jpg"),
                    element["name"] + ".jpg");
                const string url = BaseUrl + "/main/images";
                var request = UnityWebRequest.Post(url, form);
                request.SetRequestHeader("Authorization", "Token " + WebRequest.token);
                request.SendWebRequest();
                while (!request.isDone)
                {
                }

                Debug.LogWarning(request.downloadHandler.text);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(request.error);
                    Debug.LogError(request.downloadHandler.text);
                }
                else
                {
                    Debug.Log("Form " + element["name"] + '_' + element["story_type"] + " upload complete!");
                }
            }
        }

        [MenuItem("Server Upload/Codex/Full Update")]
        private static void FullUpdate()
        {
            UploadGenericStop();
            UploadMainStop();
            UploadSideStop();
            UploadError();
            UploadCodexImages();
        }
    }
}