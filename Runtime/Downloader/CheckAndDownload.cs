using UnityEngine;

namespace Downloader
{
    /// <summary>
    /// This abstract class defines the contract for objects that dowload resources after checking their status
    /// in the project.
    /// Its implementation must contain the code logic to verify the version, update and a path to save the resource.
    /// 
    /// Classe astratta che definisce il contratto per oggetti che controllano e scaricano risorse.
    /// Implementazioni concrete devono fornire logica per verifica versione, aggiornamento e path di salvataggio.
    /// </summary>
    public abstract class CheckAndDownload : MonoBehaviour
    {
        /// <summary>
        /// Boolean value that indicates if the update action has been successful or not.
        /// Indica se l'operazione di aggiornamento è stata completata con successo.
        /// </summary>
        /// <returns>True if the update is complete, False otherwise</returns>
        public abstract bool IsUpdateComplete();
        
        /// <summary>
        /// This method returns the sting that desctibes the path used to save the file locally.
        /// Restituisce il percorso/chiave usato per salvare la risorsa localmente.
        /// </summary>
        /// <returns>String containing the path (e.g. "collectible/123_en").</returns>
        public abstract string SavePath();
        
        /// <summary>
        /// Boolean that registers the abortion of the process due to errors during its execution.
        /// Indica se il processo è stato abortito a causa di errori.
        /// </summary>
        /// <returns>True if the process has been aborted</returns>
        public abstract bool IsProcessAborted();

        /// <summary>
        /// Check to see if the local version of an asset is updated to the last version available on the server.
        /// Verifica se la risorsa locale corrisponde all'ultima versione disponibile sul server.
        /// </summary>
        /// <returns>True if the local version is the last available one, false if an update is necessary or if an error occurs</returns>
        public abstract bool IsLastVersion();
        
        /// <summary>
        /// Starts the process to update the local version of an asset.
        /// Avvia il processo di aggiornamento per portare la risorsa alla versione corrente.
        /// </summary>
        /// <remarks>
        /// This method should set the flags IsUpdateComplete / IsProcessAborted depending of the outcome of
        /// the update.
        /// L'implementazione dovrebbe impostare i flag IsUpdateComplete / IsProcessAborted in base
        /// all'esito dell'operazione.
        /// </remarks>
        public abstract void UpdateToCurrentVersion();
    }
}
