using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Downloader.HotUpdateManager.Implementation
{
    public class CatalogsUpdateManager:ICatalogsUpdateManager
    {
        /// <summary>
        /// Checks for catalog updates. Returns a list of catalog IDs with updates (empty list if none).
        /// Does not auto-release the check handle because we await and release here; callers get the result.
        /// </summary>
        /// <returns>List of catalog ids (strings) that have available updates. Empty list if none.</returns>
        public async Task<List<string>> CheckForCatalogUpdatesAsync()
        {
            // Ensure Addressables is initialized first (some versions require it)
            await InitializeAsync();
            var checkHandle = Addressables.CheckForCatalogUpdates(false); // don't auto-release so we can manage it
            try
            {
                await checkHandle.Task;
                if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result != null)
                    return new List<string>(checkHandle.Result);
                // If failed, return empty and log
                Debug.LogWarning($"CheckForCatalogUpdates failed or returned null (status: {checkHandle.Status}).");
                return new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while checking for catalog updates: {e}");
                return new List<string>();
            }
            finally
            {
                if (checkHandle.IsValid()) Addressables.Release(checkHandle);
            }
        }

        /// <summary>
        /// Update the catalogs specified by catalogIds. If catalogIds is null or empty, Addressables will check all loaded catalogs.
        /// NOTE: UpdateCatalogs blocks other Addressables requests while running.
        /// </summary>
        /// <param name="catalogIds">The ids of the catalogs to update</param>
        /// <returns>Returns true on success (catalogs updated or no updates needed), false on failure.</returns>
        public async Task<bool> UpdateCatalogsAsync(List<string> catalogIds = null)
        {
            // Ensure Addressables is initialized first
            await InitializeAsync();
            AsyncOperationHandle<List<IResourceLocator>> updateHandle;
            if (catalogIds == null || catalogIds.Count == 0)
                updateHandle = Addressables.UpdateCatalogs(false); // check all
            else
                updateHandle = Addressables.UpdateCatalogs(catalogIds, false);
            try
            {
                Debug.Log("Updating catalogs (this will block other Addressables loads until finished)...");
                await updateHandle.Task;
                if (updateHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"UpdateCatalogs succeeded — {updateHandle.Result?.Count ?? 0} locators returned.");
                    return true;
                }
                else
                {
                    Debug.LogError($"UpdateCatalogs failed with status {updateHandle.Status}.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while updating catalogs: {e}");
                return false;
            }
            finally
            {
                if (updateHandle.IsValid()) Addressables.Release(updateHandle);
            }
        }

        /// <summary>
        /// Checks for updates in the catalogs passed as parameter and, if any, updates them.
        /// </summary>
        /// <returns>True if the update succeded(or no update is required), False if an error has occurred</returns>
        /// <returns>The list of updated catalog ids</returns>
        public async Task<(bool success, List<string> updatedCatalogIds)> CheckAndUpdateAsync()
        {
            var catalogsIds = await CheckForCatalogUpdatesAsync();
            if (catalogsIds == null || catalogsIds.Count == 0)
                return (true, new List<string>());

            bool success = await UpdateCatalogsAsync(catalogsIds);
            return (success, success ? catalogsIds : new List<string>());
        }

        #region Internals

        /// <summary>
        /// Ensure Addressables is initialized. Safe to call multiple times.
        /// </summary>
        public async Task InitializeAsync()
        {
            var initHandle = Addressables.InitializeAsync();
            try
            { await initHandle.Task;
            }
            finally
            { if (initHandle.IsValid()) Addressables.Release(initHandle);
            }
        }

        #endregion
    }
}
