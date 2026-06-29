using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides management utilities for downloading, caching, loading, and retrieving Unity AssetBundles.
/// This class handles version tracking via local and remote JSON metadata, ensures AssetBundle integrity
/// through CRC checks, and manages AssetBundle dependencies.
/// </summary>
public static class AssetBundleManager
{
    /// <summary>
    /// A thread-safe dictionary that keeps track of loaded AssetBundles by their name.
    /// </summary>
    private static readonly ConcurrentDictionary<string, AssetBundle> LoadedAssetBundles =
        new ConcurrentDictionary<string, AssetBundle>();

    /// <summary>
    /// Downloads and loads an AssetBundle corresponding to a given level identifier.
    /// If a local version already exists and matches the remote CRC, the download is skipped.
    /// Updates the local version file with the new metadata when changes are detected.
    /// </summary>
    /// <param name="levelId">The numeric identifier of the level whose AssetBundle should be downloaded.</param>
    /// <param name="isDependency">
    /// Indicates whether the AssetBundle being requested is a dependency (true) or a primary AssetBundle (false).
    /// This flag determines which key is used for lookup in the remote version list.
    /// </param>
    public static void DownloadAssetBundle(int levelId, bool isDependency = false)
    {
        var localVersionJsonString = LoadLocalVersionFile();
        var localVersion = new List<Dictionary<string, string>>();
        if (!string.IsNullOrEmpty(localVersionJsonString) && localVersionJsonString != "{}" &&
            localVersionJsonString != "[]")
            localVersion = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionJsonString);

        var remoteVersion = LoadRemoteVersionList();
        var remote = remoteVersion
            .Find(e => int.TryParse(!isDependency ? e["codex_stop"] : e["id"], out var stop)
                       && stop.Equals(levelId));
        if (remote == null)
        {
            DownloadAssetBundle(levelId, isDependency);
            return;
        }

        var assetBundleName = remote["name"];
        var local = localVersion.Find(e => int.Parse(e["id"]).Equals(levelId));
        if (local != null && local["crc"].Equals(remote["crc"])) return;
        var assetBundle = GetAssetBundle(assetBundleName, uint.Parse(remote["crc"]), uint.Parse(remote["crc"]));
        LoadedAssetBundles.TryAdd(assetBundle.name, assetBundle);
        var newVersion = new List<Dictionary<string, string>>();
        var isUpdated = false;
        foreach (var version in localVersion)
            if (int.Parse(version["id"]).Equals(levelId))
            {
                newVersion.Add(new Dictionary<string, string>
                {
                    { "id", version["id"] },
                    { "name", assetBundleName },
                    { "crc", remote["crc"] }
                });
                isUpdated = true;
            }
            else
            {
                newVersion.Add(version);
            }

        if (!isUpdated)
            newVersion.Add(new Dictionary<string, string>
            {
                { "id", remote["id"] },
                { "name", assetBundleName },
                { "crc", remote["crc"] }
            });

        SaveLocalVersionFile(newVersion);
    }

    /// <summary>
    /// Saves the provided version metadata into the local version file.
    /// </summary>
    /// <param name="newVersion">The new list of version entries to store locally, each represented as a dictionary.</param>

    public static void SaveLocalVersionFile(List<Dictionary<string, string>> newVersion)
    {
        var path = Application.persistentDataPath + "/asset/versions.json";
        FileUtilities.WriteFile(path, JsonConvert.SerializeObject(newVersion));
    }

    /// <summary>
    /// Loads the contents of the local version file as a raw JSON string.
    /// </summary>
    /// <returns>A string containing the JSON contents of the local version file, or an empty string if not found.</returns>

    public static string LoadLocalVersionFile()
    {
        var path = Application.persistentDataPath + "/asset/versions.json";
        return string.Join("\n", FileUtilities.ReadCompleteFile(path));
    }

    
    /// <summary>
    /// Downloads and parses the remote version list of available AssetBundles.
    /// The request is based on the current operating system, and the response is deserialized from JSON.
    /// </summary>
    /// <returns>A list of dictionaries representing the remote AssetBundle metadata.</returns>

    public static List<Dictionary<string, string>> LoadRemoteVersionList()
    {
        var currentOS = SystemInfo.operatingSystem.Split(' ')[0];
        var url = CommonVariables.URL + "/asset/" + currentOS.ToLower() + "/list";
        var www = UnityWebRequest.Get(url);
        www.SendWebRequest();
        while (!www.isDone)
        {
        }

        return JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(www.downloadHandler.text);
    }
    

    
    /// <summary>
    /// Retrieves an AssetBundle from a remote server using its name, version, and CRC.
    /// If already loaded, returns the cached instance from memory.
    /// </summary>
    /// <param name="assetBundleName">The name of the AssetBundle to retrieve.</param>
    /// <param name="version">The version number used for cache validation.</param>
    /// <param name="crc">The CRC value used for integrity checking.</param>
    /// <returns>The loaded AssetBundle instance.</returns>
    public static AssetBundle GetAssetBundle(string assetBundleName, uint version, uint crc)
    {
        assetBundleName = assetBundleName.ToLower();
        if (LoadedAssetBundles.ContainsKey(assetBundleName)) return LoadedAssetBundles[assetBundleName];

        var currentOS = SystemInfo.operatingSystem.Split(' ')[0];
        var url = CommonVariables.URL + "/asset/" + currentOS.ToLower() + "/redirect?name=" + assetBundleName;
        var www = UnityWebRequestAssetBundle.GetAssetBundle(url, version, crc);
        www.SendWebRequest();
        while (!www.isDone)
        {
        }

        var assetBundle = DownloadHandlerAssetBundle.GetContent(www);
        LoadedAssetBundles.TryAdd(assetBundleName, assetBundle);
        return assetBundle;
    }

    /// <summary>
    /// Attempts to retrieve a previously loaded AssetBundle from memory.
    /// </summary>
    /// <param name="assetBundleName">The name of the AssetBundle to load.</param>
    /// <returns>
    /// The cached AssetBundle instance if it exists, or <c>null</c> if the AssetBundle has not been loaded.
    /// </returns>
    public static AssetBundle LoadLocalAssetBundle(string assetBundleName)
    {
        assetBundleName = assetBundleName.ToLower();
        return !LoadedAssetBundles.ContainsKey(assetBundleName) ? null : LoadedAssetBundles[assetBundleName];
    }

    /// <summary>
    /// Loads all cached AssetBundles based on local version metadata and dependency information.
    /// AssetBundles are loaded in an order that respects dependency relationships to ensure proper initialization.
    /// </summary>
    public static void LoadAllCachedAssetBundles()
    {
        var path = Application.persistentDataPath + "/asset/versions.json";
        var dependenciesPath = Application.persistentDataPath + "/asset/dependency.json";
        if (!File.Exists(path)) return;
        var localVersion = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(File.ReadAllText(path));
        var dependencies = new List<Dictionary<string, string>>();
        if (File.Exists(dependenciesPath))
            dependencies =
                JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(File.ReadAllText(dependenciesPath));

        var sortedVersion = localVersion
            .FindAll(version =>
                !dependencies.Exists(dep => dep["parent"].Equals(version["name"])));
        while (sortedVersion.Count != localVersion.Count)
            foreach (var version in
                     from version in localVersion
                     where !sortedVersion.Exists(e => e["name"].Equals(version["name"]))
                     let dependency = dependencies
                         .FindAll(dep => dep["parent"].Equals(version["name"]))
                         .Select(e => e["child"])
                     where dependency.All(e => sortedVersion.Exists(v => v["name"].Equals(e)))
                     select version)
                sortedVersion.Add(version);
        localVersion = sortedVersion;
        foreach (var assetBundle in from version in localVersion
                 where !LoadedAssetBundles.ContainsKey(version["name"])
                 select GetAssetBundle(version["name"], uint.Parse(version["crc"]), uint.Parse(version["crc"])))
            LoadedAssetBundles.TryAdd(assetBundle.name, assetBundle);
    }
}