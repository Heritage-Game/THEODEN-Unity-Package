using System;
using System.Threading.Tasks;
using ContentLoading;
using UnityEngine;

/// <summary>
/// Loads media assets referenced by a runtime POI model.
/// </summary>
/// <remarks>
/// The POI JSON translator stores Addressables addresses inside the runtime model.
/// This resolver uses those addresses to load the actual Unity assets at runtime.
///
/// This class does not parse JSON. It only resolves media references after a
/// <see cref="POIModel"/> has already been created.
/// </remarks>
public static class POIAssetResolver
{
    /// <summary>
    /// Loads all media assets referenced by a POI model.
    /// </summary>
    /// <param name="model">
    /// POI model whose media references should be loaded.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous media loading operation.
    /// </returns>
    public static async Task ResolveAssetsAsync(POIModel model)
    {
        if (model == null)
        {
            Debug.LogError("[POIAssetResolver] Model is null.");
            return;
        }

        await ResolveImagesAsync(model);
        await ResolveMusicAsync(model);
        await ResolveAudioDescriptionAsync(model);
        await ResolveBadgeAsync(model);
    }

    /// <summary>
    /// Loads all POI image sprites.
    /// </summary>
    private static async Task ResolveImagesAsync(POIModel model)
    {
        if (model.images == null)
            return;

        foreach (POIModel.ImageReference image in model.images)
        {
            if (image == null || string.IsNullOrWhiteSpace(image.address))
                continue;

            try
            {
                image.sprite =
                    await TheodenRuntimeContentLoader.LoadAssetAsync<Sprite>(
                        image.address
                    );

                Debug.Log("[POIAssetResolver] Loaded image: " + image.address);
            }
            catch (Exception ex)
            {
                Debug.LogError("[POIAssetResolver] Failed to load image: " + image.address);
                Debug.LogException(ex);
            }
        }
    }

    /// <summary>
    /// Loads the POI music audio clip.
    /// </summary>
    private static async Task ResolveMusicAsync(POIModel model)
    {
        if (string.IsNullOrWhiteSpace(model.musicAddress))
            return;

        try
        {
            model.music =
                await TheodenRuntimeContentLoader.LoadAssetAsync<AudioClip>(
                    model.musicAddress
                );

            Debug.Log("[POIAssetResolver] Loaded music: " + model.musicAddress);
        }
        catch (Exception ex)
        {
            Debug.LogError("[POIAssetResolver] Failed to load music: " + model.musicAddress);
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Loads the POI audio description clip.
    /// </summary>
    private static async Task ResolveAudioDescriptionAsync(POIModel model)
    {
        if (string.IsNullOrWhiteSpace(model.audioDescriptionAddress))
            return;

        try
        {
            model.audioDescription =
                await TheodenRuntimeContentLoader.LoadAssetAsync<AudioClip>(
                    model.audioDescriptionAddress
                );

            Debug.Log("[POIAssetResolver] Loaded audio description: " + model.audioDescriptionAddress);
        }
        catch (Exception ex)
        {
            Debug.LogError("[POIAssetResolver] Failed to load audio description: " + model.audioDescriptionAddress);
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Loads the POI badge sprite.
    /// </summary>
    private static async Task ResolveBadgeAsync(POIModel model)
    {
        if (string.IsNullOrWhiteSpace(model.poiBadgeAddress))
            return;

        try
        {
            model.poiBadge =
                await TheodenRuntimeContentLoader.LoadAssetAsync<Sprite>(
                    model.poiBadgeAddress
                );

            Debug.Log("[POIAssetResolver] Loaded badge: " + model.poiBadgeAddress);
        }
        catch (Exception ex)
        {
            Debug.LogError("[POIAssetResolver] Failed to load badge: " + model.poiBadgeAddress);
            Debug.LogException(ex);
        }
    }
}