using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Downloader
{
    /// <summary>
    /// Scarica JSON relativi ai collectibles (collezionabili) per un codex specifico e lingua.
    /// </summary>
    public class CheckAndDownloadJSONCollectibles : CheckAndDownload
    {
        /// <summary>
        /// Identifier of the codex for wich the collectibles are to be dowloaded.
        /// Identificatore del codex di cui scaricare i collectibles.
        /// </summary>
        public string codexId;
        private bool _isProcessAborted;
        private bool _isUpdateComplete;

        /// <inheritdoc />
        public override bool IsUpdateComplete()
        {
            return _isUpdateComplete;
        }

        /// <summary>
        /// Costruisce il path di salvataggio in base alla lingua corrente; attende che CommonVariables.Language sia valorizzato.
        /// </summary>
        /// <returns>Stringa con il path dove salvare i dati dei collectibles.</returns>
        /// <remarks>
        /// ATTENZIONE: il ciclo while qui presente è bloccante e può congelare il thread principale.
        /// </remarks>
        public override string SavePath()
        {
            while (string.IsNullOrEmpty(CommonVariables.Language))
            {
            }

            return "collectible/" + codexId + '_' + CommonVariables.Language;
        }

        /// <inheritdoc />
        public override bool IsProcessAborted()
        {
            return _isProcessAborted;
        }

        /// <summary>
        /// Controlla se la versione locale per i collectibles è uguale alla versione remota per il codex e la lingua corrente.
        /// </summary>
        /// <returns>True se la versione locale è uguale alla remota, false altrimenti o in caso di errore.</returns>
        public override bool IsLastVersion()
        {
            try
            {
                var localVersionString = GetCurrentLocalVersion();
                if (string.IsNullOrEmpty(localVersionString)) return true;
                var localVersionList =
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                var remoteVersion =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(GetCurrentRemoteVersion());
                var localVersion = localVersionList.FirstOrDefault(e => remoteVersion["name"].Equals(e["name"]));
                return localVersion != null && localVersion["version"].Equals(remoteVersion["version"]);
            }
            catch (Exception)
            {
                _isProcessAborted = true;
            }

            return false;
        }

        /// <summary>
        /// Legge il file di versione locale per i collectibles.
        /// </summary>
        /// <returns>Stringa JSON con le versioni locali.</returns>
        private static string GetCurrentLocalVersion()
        {
            return JSONDownloadManager.LoadJsonFromLocalStorage("collectible/version");
        }

        /// <summary>
        /// Recupera la versione remota per il codex e lingua correnti.
        /// </summary>
        /// <returns>Stringa JSON della versione remota.</returns>
        private string GetCurrentRemoteVersion()
        {
            return JSONDownloadManager.DownloadJson("collectible/version/" + codexId + '/' + CommonVariables.Language);
        }

        /// <summary>
        /// Scarica la lista dei collectibles remoti e aggiorna il salvataggio locale e il file di versioni.
        /// </summary>
        public override void UpdateToCurrentVersion()
        {
            try
            {
                var remoteCollectiblesJsonString =
                    JSONDownloadManager.DownloadJson("collectible/list/" + codexId + '/' + CommonVariables.Language);
                if (string.IsNullOrEmpty(remoteCollectiblesJsonString) || remoteCollectiblesJsonString == "{}" ||
                    remoteCollectiblesJsonString == "[]")
                {
                    _isProcessAborted = true;
                    return;
                }

                //update local version
                var localVersionString = GetCurrentLocalVersion();
                if (string.IsNullOrEmpty(localVersionString))
                {
                    _isUpdateComplete = true;
                    return;
                }

                var localVersionList =
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                var remoteVersion = JsonConvert.DeserializeObject<Dictionary<string, string>>(GetCurrentRemoteVersion());
                var changed = false;
                foreach (var version in localVersionList.Where(version =>
                             version["name"].Equals(SavePath())))
                {
                    version["version"] = remoteVersion["version"];
                    changed = true;
                    break;
                }

                if (!changed) localVersionList.Add(remoteVersion);

                //save collectibles to local storage
                JSONDownloadManager.SaveJsonToLocalStorage(SavePath(), remoteCollectiblesJsonString);
                //save updated local version
                JSONDownloadManager.SaveJsonToLocalStorage("collectible/version",
                    JsonConvert.SerializeObject(localVersionList));
                _isUpdateComplete = true;
            }
            catch (Exception)
            {
                _isProcessAborted = true;
            }
        }
    }
}