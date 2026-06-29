using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Downloader
{
    /// <summary>
    /// Ckecks the state of the AssetBundles and uploads them if necessary, using AssetBundleManager.
    /// Controlla e scarica AssetBundles usando AssetBundleManager.
    /// </summary>
    public class CheckAndDownloadAssetBundles : CheckAndDownload
    {
        private bool _isProcessAborted;
        private bool _isUpdateComplete;

		/// <inheritdoc />
        public override bool IsUpdateComplete()
        {
            return _isUpdateComplete;
        }

		/// <inheritdoc />
        public override string SavePath()
        {
            throw new NotSupportedException();
        }

		/// <inheritdoc />
        public override bool IsProcessAborted()
        {
            return _isProcessAborted;
        }

/// <summary>
/// Confronta la versione locale (file json) con la lista remota di asset bundle controllando i CRC.
/// Se è già l'ultima versione ricarica gli asset da cache.
/// </summary>
/// <returns>True se la versione locale è allineata alla remota; false altrimenti o in caso di errore.</returns>
        public override bool IsLastVersion()
        {
            try
            {
                var localVersionString = GetCurrentLocalVersion();
                if (string.IsNullOrEmpty(localVersionString)) return false;
                var localVersionList =
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                var baseVersionList = BaseVersionList();
                if (baseVersionList.Select(dictionary => localVersionList.Find(x => x["name"] == dictionary["name"]))
                    .Any(local => local == null)) return false;
                var remoteVersionList = GetCurrentRemoteVersion();
                var isLastVersion = !(from version in remoteVersionList
                    let localVersion =
                        localVersionList.Find(e => e["name"].Equals(version["name"]))
                    where localVersion != null
                    where !localVersion["crc"].Equals(version["crc"])
                    select version).Any();
                if (isLastVersion) AssetBundleManager.LoadAllCachedAssetBundles();
                return isLastVersion;
            }
            catch (Exception)
            {
                _isProcessAborted = true;
            }

            return false;
        }
		
		/// <summary>
/// Carica la stringa della versione locale tramite AssetBundleManager.
/// </summary>
/// <returns>Stringa JSON con le informazioni di versione locali.</returns>
        private static string GetCurrentLocalVersion()
        {
            return AssetBundleManager.LoadLocalVersionFile();
        }

/// <summary>
/// Recupera la lista di versioni remota attraverso AssetBundleManager.
/// </summary>
/// <returns>Enumerabile di dizionari che rappresentano le versioni remote.</returns>
        private static IEnumerable<Dictionary<string, string>> GetCurrentRemoteVersion()
        {
            return AssetBundleManager.LoadRemoteVersionList();
        }

/// <summary>
/// Scarica i bundle mancanti o con CRC diversi rispetto alla lista locale e aggiorna il file di versione locale.
/// </summary>
/// <remarks>
/// In caso di eccezione imposta _isProcessAborted a true.
/// </remarks>
        public override void UpdateToCurrentVersion()
        {
            try
            {
                var localVersionString = GetCurrentLocalVersion();
                var localVersionList = BaseVersionList();
                if (!string.IsNullOrEmpty(localVersionString) && localVersionString != "{}" && localVersionString != "[]")
                {
                    var tmpLocalVersionList =
                        JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                    localVersionList.RemoveAll(e =>
                        tmpLocalVersionList.Exists(e2 =>
                            e2["name"].Equals(e["name"])));
                    localVersionList.AddRange(tmpLocalVersionList);
                }

                var remoteVersionList = GetCurrentRemoteVersion();
                var filteredRemoteVersionList = remoteVersionList.Where(remote =>
                    localVersionList.Exists(localVersion =>
                        localVersion["name"].Equals(remote["name"]))).ToList();
                foreach (var version in filteredRemoteVersionList)
                    try
                    {
                        AssetBundleManager.GetAssetBundle(version["name"], uint.Parse(version["crc"]),
                            uint.Parse(version["crc"]));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        _isProcessAborted = true;
                        return;
                    }

                AssetBundleManager.SaveLocalVersionFile(filteredRemoteVersionList);
                AssetBundleManager.LoadAllCachedAssetBundles();
                _isUpdateComplete = true;
            }
            catch (Exception)
            {
                _isProcessAborted = true;
            }
        }


/// <remarks>THIS NEEDS TO BE CHANGED </remarks>
/// <summary>
/// Lista base di asset che l'app si aspetta sempre di avere (fonts, themes, tutorial).
/// </summary>
/// <returns>Lista di dizionari con "name" e "crc" di default.</returns>
        private static List<Dictionary<string, string>> BaseVersionList()
        {
            return new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "name", "fonts" },
                    { "crc", "0" }
                },
                new Dictionary<string, string>
                {
                    { "name", "themes" },
                    { "crc", "0" }
                },
                new Dictionary<string, string>
                {
                    { "name", "tutorial" },
                    { "crc", "0" }
                }
            };
        }
    }
}