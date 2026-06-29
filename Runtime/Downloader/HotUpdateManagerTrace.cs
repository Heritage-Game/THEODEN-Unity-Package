using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Downloader
{
    /// <summary>
    /// HotUpdateManager è il coordinatore runtime che utilizza Addressables per:
    /// - controllare se ci sono aggiornamenti dei cataloghi remoti,
    /// - aggiornare i cataloghi (metadata),
    /// - calcolare la dimensione del download per un insieme di labels/keys,
    /// - scaricare le dipendenze (DownloadDependenciesAsync),
    /// - esporre eventi di progresso/completamento/errore/cancel.
    /// 
    /// L'API è pensata per essere semplice da integrare con UI (es. dialog "Scaricare X MB?"),
    /// con i widget del tuo UiManager e con il meccanismo POI (QR -> label).
    /// </summary>
    public class HotUpdateManagerTrace : MonoBehaviour
    {
        #region Events / Delegates

        /// <summary>
        /// Evento chiamato durante il processo di update con valore progressuale [0..1] e un messaggio opzionale.
        /// </summary>
        public event Action<float, string> OnProgress;

        /// <summary>
        /// Evento chiamato al termine dell'update. True = successo, false = fallimento/abort.
        /// </summary>
        public event Action<bool> OnCompleted;

        /// <summary>
        /// Evento chiamato se l'operazione è stata cancellata dall'app (es. l'utente annulla).
        /// </summary>
        public event Action OnCanceled;

        #endregion

        #region Stato pubblico / proprietà

        /// <summary>
        /// True se l'ultimo update è stato completato con successo.
        /// </summary>
        public bool IsUpdateComplete { get; private set; }

        /// <summary>
        /// True se l'ultimo processo è stato abortito o è fallito.
        /// </summary>
        public bool IsProcessAborted { get; private set; }

        /// <summary>
        /// Ultimo risultato di CheckForCatalogUpdates: true se non ci sono aggiornamenti.
        /// Questo campo è aggiornato dalla prima chiamata a CheckForUpdatesAsync (cached).
        /// </summary>
        public bool IsLastCatalogCheckNoUpdates { get; private set; } 

        /// <summary>
        /// Seleziona se i log estesi debbano essere scritti (solo per debugging).
        /// </summary>
        public bool VerboseLogging = false;

        #endregion

        #region Internals

        private Coroutine _runningCoroutine;
        private bool _cancelRequested = false;

        #endregion

        #region API - wrappers sincroni per chiamanti (avviano coroutine internamente)

        /// <summary>
        /// Avvia il flusso completo: check catalog -> update catalogs -> download dependencies.
        /// Fornisce progress via eventi OnProgress e chiamerà OnCompleted al termine.
        /// </summary>
        /// <param name="labelsOrKeys">Lista opzionale di labels o keys da scaricare (null o vuota = tutte le keys trovate nei locators).</param>
        public void StartCheckAndUpdate(IEnumerable<string> labelsOrKeys = null)
        {
            StopCurrentOperationIfAny();
            _runningCoroutine = StartCoroutine(CheckAndUpdateCoroutine(labelsOrKeys));
        }

        /// <summary>
        /// Avvia solo il check dei cataloghi remoti in background. Aggiorna la proprietà IsLastCatalogCheckNoUpdates.
        /// </summary>
        public void StartCatalogCheck()
        {
            StopCurrentOperationIfAny();
            _runningCoroutine = StartCoroutine(CheckCatalogsCoroutine());
        }

        /// <summary>
        /// Calcola (asincrono) la dimensione dei download per le keys/labels fornite.
        /// Genera un callback quando pronto.
        /// </summary>
        /// <param name="labelsOrKeys">Lista di label/key (opzionale: null = tutte le keys)</param>
        /// <param name="onResult">Callback: (success, bytes). Se success==false, bytes==0.</param>
        public void GetDownloadSize(IEnumerable<string> labelsOrKeys, Action<bool, long> onResult)
        {
            StopCurrentOperationIfAny();
            _runningCoroutine = StartCoroutine(GetDownloadSizeCoroutine(labelsOrKeys, onResult));
        }

        /// <summary>
        /// Richiesta di cancellazione dell'operazione corrente. Se è in corso un download, verrà fermato.
        /// Invoca OnCanceled.
        /// </summary>
        public void CancelCurrentOperation()
        {
            if (_runningCoroutine != null)
            {
                _cancelRequested = true;
            }
            else
            {
                OnCanceled?.Invoke();
            }
        }

        #endregion

        #region Core Coroutines

        /// <summary>
        /// Coroutine principale che esegue:
        /// 1) controlla i cataloghi remoti (CheckForCatalogUpdates)
        /// 2) (se presenti) aggiorna i cataloghi (UpdateCatalogs)
        /// 3) raccoglie keys da locators / labels
        /// 4) ottiene la download size
        /// 5) esegue DownloadDependenciesAsync sui keys determinati
        /// </summary>
        private IEnumerator CheckAndUpdateCoroutine(IEnumerable<string> labelsOrKeys)
        {
            IsUpdateComplete = false;
            IsProcessAborted = false;
            _cancelRequested = false;
            IsLastCatalogCheckNoUpdates = false;

            // 1) Check for catalog updates
            yield return StartCoroutine(CheckCatalogsCoroutine());

            if (_cancelRequested)
            {
                HandleCanceled();
                yield break;
            }

            // If no updates, still we may want to download dependencies (if keys specified)
            // but typically we only download if catalogs changed. We'll proceed with update logic anyway.

            // 2) Collect keys (after catalog update locators reflect new catalogs)
            OnProgress?.Invoke(0.20f, "Collecting keys...");
            var keys = CollectKeysFromLabels(labelsOrKeys);
            if (keys == null || keys.Count == 0)
            {
                OnProgress?.Invoke(1f, "No keys found to download");
                IsUpdateComplete = true;
                OnCompleted?.Invoke(true);
                yield break;
            }

            // 3) Get download size
            long bytesToDownload = 0;
            yield return StartCoroutine(GetDownloadSizeCoroutineInternal(keys, (ok, bytes) => { if (ok) bytesToDownload = bytes; }));

            if (_cancelRequested)
            {
                HandleCanceled();
                yield break;
            }

            if (bytesToDownload <= 0)
            {
                if (VerboseLogging) Debug.Log("[HotUpdateManager] Nothing to download; all dependencies cached.");
                OnProgress?.Invoke(1f, "No download required");
                IsUpdateComplete = true;
                OnCompleted?.Invoke(true);
                yield break;
            }

            // 4) Download dependencies
            OnProgress?.Invoke(0.35f, $"Downloading ~{FormatBytes(bytesToDownload)}...");
            var downloadHandle = Addressables.DownloadDependenciesAsync(keys);

            while (!downloadHandle.IsDone)
            {
                if (_cancelRequested)
                {
                    // Non esiste un cancel diretto sull'handle; possiamo rilasciare e segnare abort
                    // (Addressables non fornisce un Abort per DownloadDependenciesAsync).
                    // Qui chiediamo all'utente di riavviare o ignorare.
                    // Nota: lasciamo comunque che la richiesta termini o si interrompa per rete.
                    HandleCanceled();
                    yield break;
                }

                float mapped = 0.35f + Mathf.Clamp01(downloadHandle.PercentComplete) * 0.60f; // mappatura visiva
                OnProgress?.Invoke(mapped, $"Downloading... {(downloadHandle.PercentComplete * 100f):0.0}%");
                yield return null;
            }

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[HotUpdateManager] DownloadDependenciesAsync failed.");
                IsProcessAborted = true;
                OnCompleted?.Invoke(false);
                yield break;
            }

            // Release the handle (good practice)
            Addressables.Release(downloadHandle);

            // Complete
            IsUpdateComplete = true;
            OnProgress?.Invoke(1f, "Update complete");
            OnCompleted?.Invoke(true);
        }

        /// <summary>
        /// Coroutine che esegue solo il CheckForCatalogUpdates e, se necessari, UpdateCatalogs.
        /// Aggiorna la proprietà IsLastCatalogCheckNoUpdates (cached).
        /// </summary>
        private IEnumerator CheckCatalogsCoroutine()
        {
            // Launch check
            if (VerboseLogging) Debug.Log("[HotUpdateManager] Checking for catalog updates...");
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            yield return checkHandle;

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                // check fallito: consideriamo che ci sia un problema di rete ma non abortiamo l'app; segnaliamo errore
                Debug.LogWarning("[HotUpdateManager] CheckForCatalogUpdates failed or cancelled.");
                IsProcessAborted = true;
                IsLastCatalogCheckNoUpdates = false;
                yield break;
            }

            var catalogsToUpdate = checkHandle.Result;
            if (catalogsToUpdate == null || catalogsToUpdate.Count == 0)
            {
                IsLastCatalogCheckNoUpdates = true;
                if (VerboseLogging) Debug.Log("[HotUpdateManager] No catalog updates found.");
                yield break; // niente da aggiornare
            }

            // Update catalogs
            OnProgress?.Invoke(0.05f, "Updating remote catalogs...");
            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate);

            while (!updateHandle.IsDone)
            {
                OnProgress?.Invoke(0.05f + updateHandle.PercentComplete * 0.15f, "Updating catalogs...");
                yield return null;
            }

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[HotUpdateManager] UpdateCatalogs failed.");
                IsProcessAborted = true;
                yield break;
            }

            // success
            IsLastCatalogCheckNoUpdates = false;
            OnProgress?.Invoke(0.20f, "Catalogs updated");
            if (VerboseLogging) Debug.Log("[HotUpdateManager] Catalogs successfully updated.");
        }

        /// <summary>
        /// Coroutine wrapper per ottenere la download size data una lista di keys (interno).
        /// </summary>
        private IEnumerator GetDownloadSizeCoroutineInternal(List<object> keys, Action<bool, long> onResult)
        {
            if (keys == null || keys.Count == 0)
            {
                onResult?.Invoke(true, 0);
                yield break;
            }

            var sizeHandle = Addressables.GetDownloadSizeAsync(keys);
            yield return sizeHandle;

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogWarning("[HotUpdateManager] GetDownloadSizeAsync failed.");
                onResult?.Invoke(false, 0);
                yield break;
            }

            onResult?.Invoke(true, sizeHandle.Result);
        }

        /// <summary>
        /// Coroutine pubblico per ottenere la download size. Chiama il callback quando finito.
        /// </summary>
        private IEnumerator GetDownloadSizeCoroutine(IEnumerable<string> labelsOrKeys, Action<bool, long> onResult)
        {
            _cancelRequested = false;

            // raccolta chiavi
            var keys = CollectKeysFromLabels(labelsOrKeys);
            if (keys == null || keys.Count == 0)
            {
                onResult?.Invoke(true, 0);
                yield break;
            }

            yield return StartCoroutine(GetDownloadSizeCoroutineInternal(keys, onResult));
        }

        #endregion

        #region Helpers: keys / labels collection & utilities

        /// <summary>
        /// Raccoglie tutte le keys dai ResourceLocators e filtra per labels/keys specificate.
        /// - Se labelsOrKeys è null o vuoto, ritorna tutte le keys trovate.
        /// - Se una entry in labelsOrKeys corrisponde a una label, includerà le keys collegate a quella label.
        /// - Se corrisponde ad una key esatta, la includerà.
        /// Nota: questo metodo tenta di usare LoadResourceLocationsAsync per label se necessario,
        /// e lo attende brevemente quando viene chiamato all'interno di una coroutine.
        /// </summary>
        /// <param name="labelsOrKeys">Enumerable di label o key (può essere null).</param>
        /// <returns>Lista di object keys da passare ad Addressables.GetDownloadSizeAsync / DownloadDependenciesAsync</returns>
        private List<object> CollectKeysFromLabels(IEnumerable<string> labelsOrKeys)
        {
            // raccogliamo tutte le keys disponibili
            var allKeys = new List<object>();
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var k in locator.Keys)
                {
                    if (!allKeys.Contains(k)) allKeys.Add(k);
                }
            }

            // nessun filtro -> ritorniamo tutte le keys
            if (labelsOrKeys == null || !labelsOrKeys.Any()) return allKeys;

            // altrimenti, cerchiamo di risolvere labels => keys
            var result = new HashSet<object>();

            foreach (var item in labelsOrKeys)
            {
                // se la key esatta è presente tra le keys, la aggiungiamo
                if (allKeys.Contains(item))
                {
                    result.Add(item);
                    continue;
                }

                // altrimenti proviamo a trattarlo come label: otteniamo le locations per la label
                // NOTE: LoadResourceLocationsAsync accetta sia label che key; qui lo eseguiamo in modo sincrono
                // perché questo metodo viene chiamato dentro coroutine del manager. Se usi questo helper fuori
                // da coroutine, preferisci usare la versione asincrona.
                var locHandle = Addressables.LoadResourceLocationsAsync(item, typeof(object));
                locHandle.WaitForCompletion(); // attendiamo; è OK dentro coroutine ma evita blocchi UI se chiamato altrove
                if (locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result != null)
                {
                    foreach (var loc in locHandle.Result)
                    {
                        result.Add(loc.PrimaryKey ?? (object)loc.InternalId);
                    }
                }

                Addressables.Release(locHandle);
            }

            // fallback permissivo: se non abbiamo trovato nulla, ritorniamo tutte le keys
            if (result.Count == 0) return allKeys;

            return result.ToList();
        }

        /// <summary>
        /// Stoppa qualunque coroutine in esecuzione e resetta i flag interni.
        /// </summary>
        private void StopCurrentOperationIfAny()
        {
            if (_runningCoroutine != null)
            {
                StopCoroutine(_runningCoroutine);
                _runningCoroutine = null;
            }

            _cancelRequested = false;
            IsProcessAborted = false;
            IsUpdateComplete = false;
        }

        private void HandleCanceled()
        {
            StopCurrentOperationIfAny();
            IsProcessAborted = true;
            OnCanceled?.Invoke();
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{(bytes / 1024f):0.0} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{(bytes / (1024f * 1024f)):0.0} MB";
            return $"{(bytes / (1024f * 1024f * 1024f)):0.0} GB";
        }

        #endregion
    }
}
