using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;

public static class AddressablesPoiLoadTest
{
    [MenuItem("THEODEN/Test/Load Test POI Media With Init")]
    private static async void LoadTestPoiMediaWithInit()
    {
        var initWatch = Stopwatch.StartNew();

        var initHandle = Addressables.InitializeAsync();
        await initHandle.Task;

        initWatch.Stop();
        Debug.Log($"[Media Load Test] Addressables initialized in {initWatch.ElapsedMilliseconds} ms");

        await TestLoadAsset<Sprite>("poi/test_poi_1/images/bathhouse_01");
        await TestLoadAsset<Sprite>("poi/test_poi_1/images/bathhouse_02");
        await TestLoadAsset<Sprite>("poi/test_poi_1/images/bathhouse_03");
        await TestLoadAsset<Sprite>("poi/test_poi_1/badges/colleseum");

        await TestLoadAsset<AudioClip>("poi/test_poi_1/audio/music");
        await TestLoadAsset<AudioClip>("poi/test_poi_1/audio/audio_description");

        Debug.Log("[Media Load Test] Completed.");
    }

    private static async Task TestLoadAsset<T>(string address)
        where T : UnityEngine.Object
    {
        var stopwatch = Stopwatch.StartNew();

        Debug.Log($"[Media Load Test] Loading {typeof(T).Name}: {address}");

        var handle = Addressables.LoadAssetAsync<T>(address);
        await handle.Task;

        stopwatch.Stop();

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError(
                $"[Media Load Test] FAILED loading {typeof(T).Name}: {address} " +
                $"after {stopwatch.ElapsedMilliseconds} ms"
            );

            if (handle.IsValid())
                Addressables.Release(handle);

            return;
        }

        T asset = handle.Result;

        if (asset == null)
        {
            Debug.LogError(
                $"[Media Load Test] Loaded asset is null: {address} " +
                $"after {stopwatch.ElapsedMilliseconds} ms"
            );

            if (handle.IsValid())
                Addressables.Release(handle);

            return;
        }

        Debug.Log(
            $"[Media Load Test] SUCCESS {typeof(T).Name}: {asset.name} " +
            $"in {stopwatch.ElapsedMilliseconds} ms"
        );

        if (handle.IsValid())
            Addressables.Release(handle);
    }
}