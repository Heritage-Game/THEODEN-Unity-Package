using System;
using System.Threading.Tasks;
using ContentLoading;
using Core.Models;
using UnityEngine;

/// <summary>
/// Loads media assets referenced by a runtime directions model.
/// </summary>
/// <remarks>
/// The directions JSON translator stores Addressables addresses inside the runtime model.
/// This resolver uses those addresses to load the actual Unity assets at runtime.
/// </remarks>
public static class DirectionsAssetResolver
{
    /// <summary>
    /// Loads all media assets referenced by a directions model.
    /// </summary>
    /// <param name="model">
    /// Directions model whose media references should be loaded.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous media loading operation.
    /// </returns>
    public static async Task ResolveAssetsAsync(DirectionsToNextPOIModel model)
    {
        if (model == null)
        {
            Debug.LogError("[DirectionsAssetResolver] Model is null.");
            return;
        }

        await ResolveImagesAsync(model);
        await ResolveAudioDescriptionAsync(model);
    }

    /// <summary>
    /// Loads all direction image sprites.
    /// </summary>
    /// <param name="model">
    /// Directions model.
    /// </param>
    private static async Task ResolveImagesAsync(DirectionsToNextPOIModel model)
    {
        if (model.images == null)
            return;

        foreach (DirectionsToNextPOIModel.ImageReference image in model.images)
        {
            if (image == null || string.IsNullOrWhiteSpace(image.address))
                continue;

            try
            {
                image.sprite =
                    await TheodenRuntimeContentLoader.LoadAssetAsync<Sprite>(
                        image.address
                    );

                Debug.Log("[DirectionsAssetResolver] Loaded image: " + image.address);
            }
            catch (Exception ex)
            {
                Debug.LogError("[DirectionsAssetResolver] Failed to load image: " + image.address);
                Debug.LogException(ex);
            }
        }
    }

    /// <summary>
    /// Loads the optional directions audio description.
    /// </summary>
    /// <param name="model">
    /// Directions model.
    /// </param>
    private static async Task ResolveAudioDescriptionAsync(DirectionsToNextPOIModel model)
    {
        if (string.IsNullOrWhiteSpace(model.audioDescriptionAddress))
            return;

        try
        {
            model.audioDescription =
                await TheodenRuntimeContentLoader.LoadAssetAsync<AudioClip>(
                    model.audioDescriptionAddress
                );

            Debug.Log("[DirectionsAssetResolver] Loaded audio description: " + model.audioDescriptionAddress);
        }
        catch (Exception ex)
        {
            Debug.LogError("[DirectionsAssetResolver] Failed to load audio description: " + model.audioDescriptionAddress);
            Debug.LogException(ex);
        }
    }
}