using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Downloader
{
    /// <summary>
    /// Scarica immagini del codex, le converte in texture/sprite e le salva localmente come PNG.
    /// </summary>
    public class CheckAndDownloadCodexImages : CheckAndDownload
    {
        
        /// <summary>
        /// Identificatore del codex da cui scaricare le immagini.
        /// </summary>
        public int codexId;
        
        public bool storyType;
        private bool _isProcessAborted;
        private bool _isUpdateComplete;

        public override bool IsUpdateComplete()
        {
            return _isUpdateComplete;
        }

        public override string SavePath()
        {
            return "codex/images";
        }

        public override bool IsProcessAborted()
        {
            return _isProcessAborted;
        }

        /// <summary>
        /// Controlla se le versioni locali sono aggiornate rispetto all'endpoint di versione immagini del codex.
        /// </summary>
        /// <returns>True se le versioni coincidono; false altrimenti o in caso di errore.</returns>
        public override bool IsLastVersion()
        {
            try
            {
                var localVersionString = GetCurrentLocalVersion(SavePath());
                if (string.IsNullOrEmpty(localVersionString) || localVersionString == "[]" ||
                    localVersionString == "{}") return true;

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
        /// Legge il file versions.json relativo al percorso di salvataggio passato.
        /// </summary>
        /// <param name="savePath">Percorso di salvataggio (es. "codex/images").</param>
        /// <returns>Stringa JSON con le versioni locali.</returns>
        private static string GetCurrentLocalVersion(string savePath)
        {
            return JSONDownloadManager.LoadJsonFromLocalStorage(savePath + "/versions.json");
        }

        /// <summary>
        /// Recupera la versione remota per le immagini del codex (parametrizzato per codexId e storyType).
        /// </summary>
        /// <returns>Stringa JSON con i dati di versione remoti.</returns>
        private string GetCurrentRemoteVersion()
        {
            return JSONDownloadManager.DownloadJson("codex/main/images/version/" + codexId + '/' + (storyType ? 1 : 0));
        }
        

        /// <summary>
        /// Scarica le immagini remote, le converte in PNG e le salva in Application.persistentDataPath.
        /// </summary>
        /// <remarks>
        /// L'implementazione usa richieste sincrone e un ciclo while su request.isDone; questo è bloccante.
        /// Considerare l'uso di coroutine per evitare freeze dell'app.
        /// </remarks>
        public override void UpdateToCurrentVersion()
        {
            try
            {
                var localVersionString = GetCurrentLocalVersion(SavePath());
                if (string.IsNullOrEmpty(localVersionString) || localVersionString == "[]" || localVersionString == "{}")
                {
                    _isUpdateComplete = true;
                    return;
                }

                //update files
                var remoteImageString = JSONDownloadManager.DownloadJson("codex/main/images/list/" + codexId);
                if (string.IsNullOrEmpty(remoteImageString) || remoteImageString == "[]" || remoteImageString == "{}")
                {
                    _isProcessAborted = true;
                    return;
                }

                JSONDownloadManager.SaveJsonToLocalStorage(SavePath() + "/list_" + codexId, remoteImageString);
                var remoteFilteredImageList = JsonConvert
                    .DeserializeObject<List<Dictionary<string, string>>>(remoteImageString)
                    .FindAll(e => bool.Parse(e["story_type"]) == storyType);
                foreach (var image in remoteFilteredImageList)
                {
                    var request = UnityWebRequestTexture.GetTexture(CommonVariables.URL + "/codex/main/images/redirect?" +
                                                                    "image_name=" + image["name"] + '&' +
                                                                    "codex_id=" + codexId + '&' +
                                                                    "story_type=" + (storyType ? 1 : 0));
                    request.SendWebRequest();
                    while (!request.isDone)
                    {
                    }

                    if (request.result != UnityWebRequest.Result.Success) _isProcessAborted = true;
                    var texture = DownloadHandlerTexture.GetContent(request);
                    var sprite = Sprite.Create(texture, new Rect(0F, 0F, texture.width, texture.height), Vector2.zero);
                    var imageBytes = sprite.texture.EncodeToPNG();
                    FileUtilities.WriteFile(
                        Application.persistentDataPath + "/" + SavePath() + "/" + codexId + '_' + image["name"] + '_' +
                        (storyType ? 1 : 0) +
                        ".png", imageBytes);
                }

                //Update version
                var localVersionList =
                    JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(localVersionString);
                var remoteVersion =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(GetCurrentRemoteVersion());
                foreach (var t in localVersionList.Where(t => t["name"].Equals(remoteVersion["name"])))
                {
                    t["version"] = remoteVersion["version"];
                    break;
                }

                JSONDownloadManager.SaveJsonToLocalStorage(SavePath() + "/versions",
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