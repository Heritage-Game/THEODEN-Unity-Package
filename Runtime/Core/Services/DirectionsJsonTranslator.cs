using System.Collections.Generic;
using Core.Models;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// Converts exported directions JSON data into the runtime directions model.
/// </summary>
/// <remarks>
/// The exported directions JSON contains authoring data and Addressables address strings.
/// This translator maps that JSON into <see cref="DirectionsToNextPOIModel"/>,
/// which is the model used by the runtime UI.
/// 
/// This class does not load image or audio assets. It only stores Addressables addresses.
/// Media loading is handled by <see cref="DirectionsAssetResolver"/>.
/// </remarks>
public static class DirectionsJsonTranslator
{
    /// <summary>
    /// Converts an exported directions JSON string into a runtime directions model.
    /// </summary>
    /// <param name="json">
    /// Exported directions JSON string.
    /// </param>
    /// <returns>
    /// Runtime directions model, or null if conversion fails.
    /// </returns>
    public static DirectionsToNextPOIModel FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[DirectionsJsonTranslator] JSON is empty.");
            return null;
        }

        JObject root = JObject.Parse(json);

        DirectionsToNextPOIModel model = new DirectionsToNextPOIModel
        {
            poiName = root["poiName"]?.ToString() ??
                      root["levelName"]?.ToString() ??
                      "",

            poiId = root["poiId"]?.ToString() ?? "",

            description = root["description"]?.ToString() ?? "",

            images = ReadImageReferences(root),

            audioDescriptionAddress =
                root["audioDescription"]?.ToString() ??
                root["audioDescriptionAddress"]?.ToString() ??
                ""
        };

        Debug.Log(
            "[DirectionsJsonTranslator] Directions translated: " +
            model.poiName +
            " | POI id: " +
            model.poiId +
            " | Images: " +
            model.images.Count
        );

        return model;
    }

    /// <summary>
    /// Reads image Addressables addresses from the exported directions JSON.
    /// </summary>
    /// <param name="root">
    /// Root JSON object.
    /// </param>
    /// <returns>
    /// List of runtime image references.
    /// </returns>
    private static List<DirectionsToNextPOIModel.ImageReference> ReadImageReferences(
        JObject root)
    {
        List<DirectionsToNextPOIModel.ImageReference> images =
            new List<DirectionsToNextPOIModel.ImageReference>();

        JArray imageAddresses = root["images"] as JArray;

        if (imageAddresses == null)
            return images;

        foreach (JToken imageAddressToken in imageAddresses)
        {
            string address = imageAddressToken?.ToString();

            if (string.IsNullOrWhiteSpace(address))
                continue;

            images.Add(new DirectionsToNextPOIModel.ImageReference
            {
                address = address
            });
        }

        return images;
    }
}