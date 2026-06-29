using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Downloader
{
    /// <summary>
    /// Scarica JSON generici secondo url e opzioni di dipendenza da lingua/OS.
    /// </summary>
    public class CheckAndDownloadJSONData : CheckAndDownload
    {
        /// <summary>
        /// Endpoint base (es. "json/data") da cui richiedere /version e /list.
        /// </summary>
        public string url;
        
        /// <summary>
        /// Se true, il salvataggio include la lingua corrente (es. "file_en").
        /// </summary>
        public bool isLanguageDependent;
        
        /// <summary>
        /// Nome del file da salvare localmente.
        /// </summary>
        public string saveFileName;
        
        /// <summary>
        /// Se true, include la parte relativa all'OS negli endpoint remoti.
        /// </summary>
        public bool isOsDependent;
        
        private string _completeSaveFileName = string.Empty;
        private bool _isProcessAborted;
        private bool _isUpdateComplete;
        private string _currentOs;

        /// <summary>
        /// Awake salva il nome dell'OS corrente per costruire richieste condizionate.
        /// </summary>
        private void Awake()
        {
            _currentOs = SystemInfo.operatingSystem.Split(' ')[0].ToLower();
        }

        
        public override bool IsUpdateComplete()
        {
            return _isUpdateComplete;
        }

        /// <summary>
        /// Costruisce e restituisce il nome file completo includendo eventualmente la lingua.
        /// </summary>
        /// <returns>Nome completo del file di salvataggio locale.</returns>
        public override string SavePath()
        {
            if (string.IsNullOrEmpty(_completeSaveFileName))
                _completeSaveFileName = isLanguageDependent ? saveFileName + "_" + CommonVariables.Language : saveFileName;
            return _completeSaveFileName;
        }

        public override bool IsProcessAborted()
        {
            return _isProcessAborted;
        }

        /// <summary>
        /// Confronta la versione locale (json_versions) con la versione remota ottenuta dall'endpoint url/version.
        /// </summary>
        /// <returns>True se le versioni coincidono; false altrimenti o in caso di errore.</returns>
        public override bool IsLastVersion()
        {
            try
            {
                var localVersionString = GetCurrentLocalVersion();
                if (string.IsNullOrEmpty(localVersionString)) return false;
                var localVersionList =
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                var remoteVersion = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetCurrentRemoteVersion());
                var localVersion = localVersionList.FirstOrDefault(x => x["name"] == remoteVersion["name"]);
                return localVersion != null && localVersion["version"] == remoteVersion["version"];
            }
            catch (Exception)
            {
                _isProcessAborted = true;
            }

            return false;
        }
        
        /// <summary>
        /// Restituisce la stringa JSON che contiene le versioni salvate localmente per tutti i json gestiti.
        /// </summary>
        /// <returns>Stringa JSON corrispondente a "json_versions" locale.</returns>
        private static string GetCurrentLocalVersion()
        {
            return JSONDownloadManager.LoadJsonFromLocalStorage("json_versions");
        }

        /// <summary>
        /// Richiama l'endpoint remoto per ottenere la versione, includendo opzionalmente OS e lingua.
        /// </summary>
        /// <returns>Stringa JSON con i dati di versione remoti.</returns>
        private string GetCurrentRemoteVersion()
        {
            return JSONDownloadManager.DownloadJson(url + "/version" +CheckCurrentOs()+ CheckLanguage());
        }

        /// <summary>
        /// Scarica la lista remota tramite url/list[os/lang], salva il contenuto locale e aggiorna json_versions.
        /// </summary>
        public override void UpdateToCurrentVersion()
        {
            try
            {
                SavePath();
                var jsonContent =
                    JSONDownloadManager.DownloadJson(url + "/list" + CheckCurrentOs() + CheckLanguage());
                if (jsonContent == null)
                {
                    Debug.Log("Could not download json list");
                    _isProcessAborted = true;
                    return;
                }

                //update the version
                var localVersionString = GetCurrentLocalVersion();
                var localVersionList = new List<Dictionary<string, string>>();
                if (!string.IsNullOrEmpty(localVersionString) && localVersionString != "{}" && localVersionString != "[]")
                    localVersionList =
                        JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                var remoteVersion = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetCurrentRemoteVersion());
                var found = false;
                foreach (var version in localVersionList.Where(e => e["name"].Equals(remoteVersion["name"])))
                {
                    version["version"] = remoteVersion["version"];
                    found = true;
                    break;
                }

                if (!found) localVersionList.Add(remoteVersion);

                //save the new json
                JSONDownloadManager.SaveJsonToLocalStorage(_completeSaveFileName, jsonContent);
                //save the new version
                JSONDownloadManager.SaveJsonToLocalStorage("json_versions", JsonConvert.SerializeObject(localVersionList));
                _isUpdateComplete = true;
            }
            catch (Exception)
            {
                _isProcessAborted = true;
            }
        }

        /// <summary>
        /// Restituisce la parte di URL relativa alla lingua, se attiva.
        /// </summary>
        /// <returns>Stringa "/{language}" o stringa vuota.</returns>
        private string CheckLanguage()
        {
            return (isLanguageDependent ? "/" + CommonVariables.Language : "");
        }

        /// <summary>
        /// Restituisce la parte di URL relativa all'OS corrente, se attiva.
        /// </summary>
        /// <returns>Stringa "/{os}" o stringa vuota.
        /// </returns>
        private string CheckCurrentOs()
        {
            return isOsDependent ? "/" + _currentOs : "";
        }
    }
}