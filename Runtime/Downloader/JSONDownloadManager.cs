using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class JSONDownloadManager
{
    public static string DownloadJson(string url)
    {
        var www = UnityWebRequest.Get(CommonVariables.URL + "/" + url);
        www.SendWebRequest();
        while (!www.isDone)
        {
        }

        return www.result == UnityWebRequest.Result.Success ? www.downloadHandler.text.Trim() : null;
    }

    public static string LoadJsonFromLocalStorage(string path)
    {
        var content = FileUtilities.ReadCompleteFile(Application.persistentDataPath + "/" + path + ".json");
        return content.Length == 0 ? null : string.Join("\n", content).Trim();
    }

    public static void SaveJsonToLocalStorage(string path, string json)
    {
        var folderArray = path.Split('/').ToList();
        folderArray.RemoveAt(folderArray.Count - 1);
        var folder = string.Join("/", folderArray);
        if (!Directory.Exists(Application.persistentDataPath + "/" + folder))
            Directory.CreateDirectory(Application.persistentDataPath + "/" + folder);
        FileUtilities.WriteFile(Application.persistentDataPath + "/" + path + ".json", json.Trim());
    }
}