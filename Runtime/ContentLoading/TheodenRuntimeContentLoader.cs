using System;
using System.Threading.Tasks;
using Addressing;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Config;

namespace ContentLoading
{
    /// <summary>
    /// Centralized runtime service used to load THEODEN content through Addressables.
    /// </summary>
    /// <remarks>
    /// This class is the runtime counterpart of the editor export pipeline.
    /// Editor tools generate Addressables groups, labels, addresses and JSON files using
    /// <see cref="TheodenAddressablesNaming"/>. This loader reconstructs the same addresses
    /// at runtime and loads the exported content.
    ///
    /// Runtime scenes and UI controllers should not manually build Addressables addresses.
    /// They should call this service instead.
    /// </remarks>
    public static class TheodenRuntimeContentLoader
    {
        // The project id from the Runtime Config
        private static string ActiveProjectId =>
            TheodenRuntimeConfigProvider.ProjectId;
        /// <summary>
        /// Loads the codex menu JSON for the selected language.
        /// </summary>
        /// <param name="language">
        /// The current application language.
        /// </param>
        /// <returns>
        /// The deserialized <see cref="CodexMenu"/> for the requested language.
        /// </returns>
        public static async Task<CodexMenu> LoadCodexAsync(LanguageList language)
        {
            string address =
                TheodenAddressablesNaming.GetCodexJsonAddress(
                    ActiveProjectId,
                    language
                );

            string json = await LoadJsonTextAsync(address);

            return DeserializeJson<CodexMenu>(json, address);
        }
        
        /// <summary>
        /// Loads the codex JSON TextAsset for the selected language.
        /// </summary>
        /// <param name="language">
        /// The current application language.
        /// </param>
        /// <returns>
        /// The raw codex JSON string.
        /// </returns>
        public static async Task<string> LoadCodexJsonAsync(LanguageList language)
        {
            string address =
                TheodenAddressablesNaming.GetCodexJsonAddress(
                    ActiveProjectId,
                    language
                );

            return await LoadJsonTextAsync(address);
        }

        /// <summary>
        /// Downloads all Addressables dependencies associated with the directions content of a POI.
        /// </summary>
        /// <param name="poiId">
        /// The id of the Point of Interest.
        /// </param>
        public static async Task DownloadDirectionsAsync(string poiId)
        {
            string label =
                TheodenAddressablesNaming.GetDirectionsLabel(
                    ActiveProjectId,
                    poiId
                );
            await DownloadDependenciesAsync(label);
        }

        /// <summary>
        /// Downloads all Addressables dependencies associated with the POI content.
        /// </summary>
        /// <param name="poiId">
        /// The id of the Point of Interest.
        /// </param>
        public static async Task DownloadPoiAsync(string poiId)
        {
            string label =
                TheodenAddressablesNaming.GetPoiLabel(
                    ActiveProjectId,
                    poiId
                );
            await DownloadDependenciesAsync(label);
        }
        
        /// <summary>
        /// Loads the POI JSON TextAsset for a POI and language and returns the raw JSON string.
        /// </summary>
        public static async Task<string> LoadPoiJsonAsync(
            string poiId,
            LanguageList language)
        {
            await DownloadPoiAsync(poiId);

            string address =
                TheodenAddressablesNaming.GetPoiJsonAddress(
                    ActiveProjectId,
                    poiId,
                    language
                );

            return await LoadJsonTextAsync(address);
        }

        /// <summary>
        /// Downloads and loads the directions JSON for a POI and language.
        /// </summary>
        /// <typeparam name="TDirections">
        /// Runtime data type used to deserialize the directions JSON.
        /// </typeparam>
        /// <param name="poiId">
        /// The id of the Point of Interest.
        /// </param>
        /// <param name="language">
        /// The current application language.
        /// </param>
        /// <returns>
        /// The deserialized directions data.
        /// </returns>
        public static async Task<TDirections> LoadDirectionsAsync<TDirections>(
            string poiId,
            LanguageList language)
        {
            await DownloadDirectionsAsync(poiId);

            string address =
                TheodenAddressablesNaming.GetDirectionsJsonAddress(
                    ActiveProjectId,
                    poiId,
                    language
                );

            string json = await LoadJsonTextAsync(address);

            return DeserializeJson<TDirections>(json, address);
        }

        /// <summary>
        /// Loads the directions JSON TextAsset for a POI and language and returns the raw JSON string.
        /// </summary>
        /// <param name="poiId">
        /// Id of the Point of Interest.
        /// </param>
        /// <param name="language">
        /// Current application language.
        /// </param>
        /// <returns>
        /// Raw directions JSON string.
        /// </returns>
        public static async Task<string> LoadDirectionsJsonAsync(
            string poiId,
            LanguageList language)
        {
            await DownloadDirectionsAsync(poiId);

            string address =
                TheodenAddressablesNaming.GetDirectionsJsonAddress(
                    ActiveProjectId,
                    poiId,
                    language
                );

            return await LoadJsonTextAsync(address);
        }
        
        /// <summary>
        /// Downloads and loads the POI JSON for a POI and language.
        /// </summary>
        /// <typeparam name="TPoi">
        /// Runtime data type used to deserialize the POI JSON.
        /// </typeparam>
        /// <param name="poiId">
        /// The id of the Point of Interest.
        /// </param>
        /// <param name="language">
        /// The current application language.
        /// </param>
        /// <returns>
        /// The deserialized POI data.
        /// </returns>
        public static async Task<TPoi> LoadPoiAsync<TPoi>(
            string poiId,
            LanguageList language)
        {
            await DownloadPoiAsync(poiId);

            string address =
                TheodenAddressablesNaming.GetPoiJsonAddress(
                    ActiveProjectId,
                    poiId,
                    language
                );

            string json = await LoadJsonTextAsync(address);

            return DeserializeJson<TPoi>(json, address);
        }

        /// <summary>
        /// Loads an Addressable asset by address.
        /// </summary>
        /// <typeparam name="TAsset">
        /// Type of the asset to load.
        /// </typeparam>
        /// <param name="address">
        /// Addressables address of the asset.
        /// </param>
        /// <returns>
        /// The loaded asset.
        /// </returns>
        /// <remarks>
        /// This method is useful for loading media references stored inside exported JSON files,
        /// such as sprites, audio clips, badges and other Addressable assets.
        /// </remarks>
        public static async Task<TAsset> LoadAssetAsync<TAsset>(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is null or empty.", nameof(address));

            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(address);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                throw new Exception($"Failed to load Addressable asset at address: {address}");
            }

            return handle.Result;
        }

        /// <summary>
        /// Loads a JSON file stored as an Addressable TextAsset,
        /// copies its text, and releases the Addressables handle.
        /// </summary>
        /// <param name="address">
        /// Addressables address of the JSON TextAsset.
        /// </param>
        /// <returns>
        /// The raw JSON string.
        /// </returns>
        private static async Task<string> LoadJsonTextAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address is null or empty.", nameof(address));

            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(address);

            try
            {
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new Exception(
                        $"Failed to load Addressable JSON at address: {address}",
                        handle.OperationException
                    );
                }

                return handle.Result.text;
            }
            finally
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Downloads all Addressables dependencies for a label.
        /// </summary>
        /// <param name="label">
        /// Addressables label to download.
        /// </param>
        private static async Task DownloadDependenciesAsync(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Label is null or empty.", nameof(label));

            AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync(label);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                throw new Exception($"Failed to download Addressables dependencies for label: {label}");
            }

            Addressables.Release(handle);
        }

        /// <summary>
        /// Deserializes JSON into the requested runtime data type.
        /// </summary>
        /// <typeparam name="T">
        /// Type to deserialize the JSON into.
        /// </typeparam>
        /// <param name="json">
        /// JSON string.
        /// </param>
        /// <param name="sourceAddress">
        /// Address of the source JSON file, used only for error reporting.
        /// </param>
        /// <returns>
        /// Deserialized object.
        /// </returns>
        private static T DeserializeJson<T>(string json, string sourceAddress)
        {
            try
            {
                T result = JsonConvert.DeserializeObject<T>(json);

                if (result == null)
                    throw new Exception("Deserialized object is null.");

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to deserialize JSON loaded from address: {sourceAddress}",
                    ex
                );
            }
        }
        
        /// <summary>
        /// Loads the active project's Addressable MapDefinition asset.
        /// </summary>
        public static async Task<MapDefinition> LoadMapDefinitionAsync()
        {
            string address =
                TheodenAddressablesNaming.GetMapDefinitionAddress(
                    ActiveProjectId
                );

            return await LoadAssetAsync<MapDefinition>(address);
        }

        /// <summary>
        /// Releases an Addressable Unity asset previously returned by this loader.
        /// </summary>
        public static void ReleaseAsset<TAsset>(TAsset asset)
            where TAsset : UnityEngine.Object
        {
            if (asset != null)
                Addressables.Release(asset);
        }

    }
}