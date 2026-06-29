using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableHotUpdateManager : IAddressableHotUpdateManager
{
    #region Dowload on demand

    //cached handles for asset loads
    private readonly Dictionary<string, object> _singgleHandles = new (StringComparer.Ordinal);
    //cached handles for loads of assets by label
    private readonly Dictionary<string, AsyncOperationHandle> _lableHandles = new (StringComparer.Ordinal);
    /// <summary>
    /// <inheritdoc cref="IAddressableHotUpdateManager.PrefetchDependenciesAsync"/>
    /// </summary>
    /// <param name="keyOrLabel"></param>
    /// <param name="onProgress"></param>
    /// <param name="remoteCatalogUrl"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<bool> PrefetchDependenciesAsync(string keyOrLabel)
    {
        if (string.IsNullOrEmpty(keyOrLabel))
            throw new ArgumentNullException("keyOrLabel");
        //check if remote catalogue is loaded
        var downloadHandle = Addressables.DownloadDependenciesAsync(keyOrLabel);
        try
        {
            await downloadHandle.Task;
            bool success = downloadHandle.Status == AsyncOperationStatus.Succeeded;
            if (!success)
                Debug.LogError(downloadHandle.Status.ToString() + $"DowloadDependenciesAsync failed for {keyOrLabel}");
            return success;
        }
        catch (Exception e)
        {
            Debug.LogError($"DownloadDependenciesAsync exception for {keyOrLabel}: {e.Message}");
            return false;
        }
        finally
        {
            if (downloadHandle.IsValid()) Addressables.Release(downloadHandle);
        }
    }

    /// <summary>
    /// <inheritdoc cref="IAddressableHotUpdateManager.LoadAssetByKeyAsync{T}"/>
    /// </summary>
    /// <param name="keyOrAddress"></param>
    /// <param name="remoteCatalogUrl"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<AsyncOperationHandle<T>> LoadAssetByKeyAsync<T>(string keyOrAddress)
    {
        if (string.IsNullOrEmpty(keyOrAddress))
            throw new ArgumentNullException("keyOrAddress");
        //loading remote catalogue?
        //check if the handle is in Dictionary ?
        //then
        var loadHandle = Addressables.LoadAssetAsync<T>(keyOrAddress);
        try
        {
            await loadHandle.Task;
            return loadHandle;
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadAssetAsync exception for {keyOrAddress}: {e.Message}");
            throw;
        }

    }


    public Task<AsyncOperationHandle<IList<T>>> LoadAssetsByLabelAsync<T>(string label)
    {
        throw new NotImplementedException();
    }

    public Task<GameObject> InstantiateAssetByKeyAsync(string addressOrKey, Vector3 position, Quaternion rotation,
        string remoteCatalogUrl = null)
    {
        throw new NotImplementedException();
    }

    public void ReleaseHandle(AsyncOperationHandle handle)
    {
        throw new NotImplementedException();
    }

    public void ReleaseInstance(GameObject instance)
    {
        throw new NotImplementedException();
    }

    public void ReleaseAll()
    {
        throw new NotImplementedException();
    }

    #endregion
    
    
    #region Internals
    

    /// <summary>
    ///This Method ensures that the remote asset catalog is loaded.
    /// </summary>
    /// <param name="remoteCatalogUrl"> the url to the remote catalog</param>
    /// <returns>True if the catalog is successfully loaded, false otherwise</returns>
    private async Task<bool> LoadRemoteCatalog(string remoteCatalogUrl)
    {
        if (!string.IsNullOrEmpty(remoteCatalogUrl))
        {
            var catalogHandle = Addressables.LoadContentCatalogAsync(remoteCatalogUrl);
            try
            {
                await catalogHandle.Task;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load remote catalog '{remoteCatalogUrl}': {e.Message}");
                if (catalogHandle.IsValid()) Addressables.Release(catalogHandle);
                return false;
            }
            finally
            {
                if (catalogHandle.IsValid()) Addressables.Release(catalogHandle);
                
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Check the size of the dowload for an asset key or a label identifier.
    /// NOTE: The result of this method takes into account any previously downloaded AssetBundles that are still in Unity's AssetBundle cache.
    /// </summary>
    /// <param name="keyOrLabel">The key or label identifying the asset/assets</param>
    /// <returns>A string containing the download size in bytes</returns>
    public async Task<string> GetDowloadSizeAsync(string keyOrLabel)
    {
        // 1) Check how many bytes would be downloaded
        var sizeHandle = Addressables.GetDownloadSizeAsync(keyOrLabel);
        try
        {
            await sizeHandle.Task;
            long bytesToDownload = sizeHandle.Result;

            // Nothing to download — already cached
            if (bytesToDownload <= 0)
            {
                return "Nothing to download";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"GetDownloadSizeAsync failed for '{keyOrLabel}': {e.Message}");
        }
        finally
        {
            if (sizeHandle.IsValid())Addressables.Release(sizeHandle); 
           
        } return "Download size: " + sizeHandle.Result.ToString();
    }
    
    #endregion Internals
}
