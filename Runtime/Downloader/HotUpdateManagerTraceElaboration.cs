using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace Downloader
{
    public class HotUpdateManagerTraceElaboration : MonoBehaviour, IHotUpdateManager
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        //THIS SHOULD BECOME A COROUTINE
        public void LoadAssets(IEnumerable<string> labelsOrKeys)
        {
            //this method needs to collect the keys passed and load the asset into menory
            //it needs to probably pass the reference of the asset loaded back to the caller
            var assetKeys = CollectKeysFromCatalogueS(labelsOrKeys);
            //load the assets
            //AsyncOperationHandle<IList<GameObject>> loadhandle;
            var loadhandle = Addressables.LoadAssetsAsync<GameObject>(assetKeys);
            
        }


        #region Helpers: keys / labels collection & utilities

        /// <summary>
        /// This method allows to retrieve all the keys of the assets specified in the collection <code>labelsOrKeys</code>.
        /// from the catalogs of the Addressable assets that Unity keeps.
        /// If the parameter passed is <code>null</code> the method returns all the keys of all the assets in the catalogs.
        /// </summary>
        /// <param name="labelsOrKeys"></param>
        /// <returns>All the keys of the assets matching the label/key given.</returns>
        private List<object> CollectKeysFromCatalogue(IEnumerable<string> labelsOrKeys)
        {
            var allKeys = CollectAllKeysFromResourceLocators();
            //If param labelsOrKeys is null, returns all the keys for all the assets in the catalogues
            if(labelsOrKeys == null) return allKeys;
            //If the param contains keys we collect them 
            var collectedKeys = new List<object>();
            foreach (var asset in labelsOrKeys)
            {
                if (allKeys.Contains(asset)) 
                    collectedKeys.Add(asset);
            }
            collectedKeys.Add(CollectKeysToMatchLabels(labelsOrKeys));
            return collectedKeys;
        }

        
        //not sure this works 
        //documentation 
        //https://docs.unity3d.com/Packages/com.unity.addressables@2.7/api/UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync.html#UnityEngine_AddressableAssets_Addressables_LoadAssetsAsync__1_System_String_System_Action___0__
        private string CollectKeysFromCatalogueS(IEnumerable<string> labelsOrKeys)
        {
            var allKeys = CollectAllKeysFromResourceLocators().ToString();
            //If param labelsOrKeys is null, returns all the keys for all the assets in the catalogues
            if(labelsOrKeys == null) return allKeys;
            //If the param contains keys we collect them 
            var collectedKeys = string.Empty;
            foreach (var asset in labelsOrKeys)
            {
                if (allKeys.Contains(asset)) 
                    collectedKeys += asset;
            }

            var collectedKeysFromLabels = CollectKeysToMatchLabels(labelsOrKeys).ToString();
            collectedKeys += collectedKeysFromLabels;
            return collectedKeys;
        }
        /// <summary>
        /// This method collects all the keys of the assets from all the Catalogs known to the Addressable system.
        /// </summary>
        /// <returns>A list of all the keys</returns>
        private static List<object> CollectAllKeysFromResourceLocators()
        {
            //Collecting all keys from the ResourceLocators
            var allKeys = new List<object>();
            foreach (var locators in Addressables.ResourceLocators)
            {
                foreach (var Key in locators.Keys)
                {
                    if(!allKeys.Contains(Key)) allKeys.Add(Key);
                }
            }
            return allKeys;
        }

        /// <summary>
        /// This method locates the key corresponding to an asset given its group label
        /// </summary>
        /// <param name="labelsOrKeys"></param>
        /// <returns>The list of keys</returns>
        private List<object> CollectKeysToMatchLabels(IEnumerable<string> labelsOrKeys)
        {
            var keysFromLabels= new List<object>();
            foreach (var item in labelsOrKeys)
            {
                var locHandle= Addressables.LoadResourceLocationsAsync(item, typeof(object));
                locHandle.WaitForCompletion(); // attendiamo; è OK dentro coroutine ma evita blocchi UI se chiamato altrove
                if (locHandle is { Status: AsyncOperationStatus.Succeeded, Result: not null })
                {
                    foreach (var loc in locHandle.Result)
                    {
                        keysFromLabels.Add(loc.PrimaryKey ?? (object)loc.InternalId);
                    }
                }
                Addressables.Release(locHandle);
            }
            return keysFromLabels;
        }

        #endregion
    }
}
