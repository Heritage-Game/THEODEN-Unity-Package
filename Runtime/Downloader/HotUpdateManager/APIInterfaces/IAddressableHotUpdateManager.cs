using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public interface IAddressableHotUpdateManager
{
    /// <summary>
    /// This method downloads assets by key or label on demand. It fetches them from remote catalogs and downloads them
    /// locally. When the assets are downloaded they are available in the local addressable catalogue.
    /// NOTE: this method DOES NOT load the fetched assets, it DOES NOT instantiate the assets
    /// </summary>
    /// <param name="keyOrLabel" >the key or label identifying the asset</param>
    /// <param name="onProgress"></param>
    /// <param name="remoteCatalogUrl">TO BE REMOVED AND PUT INDEPENDENTLY</param>
    /// <returns>True if the operation succeeded, False otherwise</returns>
    Task<bool> PrefetchDependenciesAsync(string keyOrLabel);
    // Task<bool> PrefetchDependenciesAsync(string keyOrLabel, Action<float> onProgress = null,
    //string remoteCatalogUrl = null);
    /// <summary>
    /// This method loads the requested asset, identified by its key into the memory. It fetches it from the local
    /// catalogue if present, downloads it from the remote catalogue if necessary.
    /// NOTE:CALLER OWN THE RETURNED HANDLE AND MUST CALL <code>ReleaseHandle(handle)</code> WHEN DONE
    /// </summary>
    /// <param name="keyOrAddress">the key or path identifying the asset</param>
    /// <param name="remoteCatalogUrl">TO BE REMOVED AND PUT INDEPENDENTLY</param>
    /// <typeparam name="T">Type of the asset to be loaded</typeparam>
    /// <returns>The handle to the async operation</returns>
    Task<AsyncOperationHandle<T>> LoadAssetByKeyAsync<T>(string keyOrAddress);
    /// <summary>
    /// This method loads all the addressable assets identified by a label. It fetches them from the local
    /// catalogue if present, downloads them from the remote catalogue if necessary.
    ///NOTE:CALLER OWN THE RETURNED HANDLE AND MUST CALL <code>ReleaseHandle(handle)</code> WHEN DONE
    /// </summary>
    /// <param name="label">the label identifying the assets</param>
    /// <param name="remoteCatalogUrl">TO BE REMOVED AND PUT INDEPENDENTLY</param>
    /// <typeparam name="T">Type of the assets to be loaded</typeparam>
    /// <returns></returns>
    Task<AsyncOperationHandle<IList<T>>> LoadAssetsByLabelAsync<T>(string label);
    
    /*
     * /// <summary>
    /// This method instantiates the addressable asset, identified by the given key, into the game scene. It fetches it from the local
    /// catalogue if present, downloads it from the remote catalogue if necessary.
    /// </summary>
    /// <param name="addressOrKey">the key or path identifying the asset</param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="remoteCatalogUrl">TO BE REMOVED AND PUT INDEPENDENTLY</param>
    /// <returns>The reference to the instantiated object</returns>
    Task<GameObject> InstantiateAssetByKeyAsync(string addressOrKey, Vector3 position, Quaternion rotation, string remoteCatalogUrl = null);
     */
    void ReleaseHandle(AsyncOperationHandle handle);
    void ReleaseInstance(GameObject instance);
    void ReleaseAll();
}
