using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RuntimeModelsForEditor;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Import
{
    /// <summary>
    /// Loads an exported Directions JSON file back into its editor-side model.
    /// </summary>
    /// <remarks>
    /// Addressables address strings stored in the JSON are converted back into
    /// direct Unity asset references so that images and audio can be edited
    /// through the Directions editor window.
    /// </remarks>
    public static class DirectionsDefinitionLoadService
    {
        /// <summary>
        /// Loads and validates a Directions definition stored inside Assets.
        /// </summary>
        /// <param name="jsonAssetPath">
        /// Unity project-relative path of the Directions JSON file.
        /// </param>
        /// <param name="expectedPoiId">
        /// POI selected in the editor window. It must match the identity stored
        /// in the JSON file.
        /// </param>
        /// <param name="directionsData">
        /// Loaded editor-side Directions data when the operation succeeds.
        /// </param>
        /// <param name="error">
        /// Human-readable error when the operation fails.
        /// </param>
        /// <returns>True when the file was loaded and validated.</returns>
        public static bool TryLoad(
            string jsonAssetPath,
            string expectedPoiId,
            out DirectionsToPOIData directionsData,
            out string error)
        {
            directionsData = null;
            error = null;

            if (!IsAssetPath(jsonAssetPath))
            {
                error =
                    "The Directions JSON path must be inside Assets: " +
                    $"'{jsonAssetPath}'.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedPoiId))
            {
                error = "The expected POI ID is missing.";
                return false;
            }

            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);

            if (jsonAsset == null)
            {
                error =
                    $"Directions JSON not found at '{jsonAssetPath}'.";

                return false;
            }

            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    ObjectCreationHandling =
                        ObjectCreationHandling.Replace,
                    MissingMemberHandling =
                        MissingMemberHandling.Ignore,
                    Converters =
                    {
                        new AddressableAssetReferenceJsonConverter()
                    }
                };

                directionsData =
                    JsonConvert.DeserializeObject<DirectionsToPOIData>(
                        jsonAsset.text,
                        settings
                    );

                if (directionsData == null)
                {
                    error =
                        "The Directions JSON could not be deserialized.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(directionsData.poiId))
                {
                    error =
                        "The Directions JSON does not contain a POI ID.";

                    directionsData = null;
                    return false;
                }

                if (!string.Equals(
                        directionsData.poiId,
                        expectedPoiId,
                        StringComparison.Ordinal))
                {
                    error =
                        $"The selected POI is '{expectedPoiId}', but the " +
                        $"Directions JSON belongs to " +
                        $"'{directionsData.poiId}'.";

                    directionsData = null;
                    return false;
                }

                directionsData.images ??= new List<Sprite>();
                directionsData.description ??= string.Empty;

                return true;
            }
            catch (JsonException exception)
            {
                error =
                    $"Invalid Directions JSON '{jsonAssetPath}':\n" +
                    exception.Message;

                directionsData = null;
                return false;
            }
            catch (Exception exception)
            {
                error =
                    $"Failed to load Directions JSON " +
                    $"'{jsonAssetPath}':\n" + exception.Message;

                directionsData = null;
                return false;
            }
        }

        private static bool IsAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string normalizedPath = path.Replace("\\", "/");

            return normalizedPath.Equals(
                       "Assets",
                       StringComparison.Ordinal) ||
                   normalizedPath.StartsWith(
                       "Assets/",
                       StringComparison.Ordinal
                   );
        }
    }
}