using System.IO;
using System.Reflection;
using Addressing;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Resolves project-scoped Addressables addresses for media
    /// referenced by Directions data.
    /// </summary>
    public static class DirectionsMediaAddressResolver
    {
        public static string ResolveAddress(
            UnityEngine.Object asset,
            string projectId,
            string poiId,
            FieldInfo sourceField)
        {
            if (asset == null)
                return null;

            if (string.IsNullOrWhiteSpace(projectId))
            {
                Debug.LogError(
                    "[DirectionsMediaAddressResolver] " +
                    "Project id is missing."
                );

                return null;
            }

            if (string.IsNullOrWhiteSpace(poiId))
            {
                Debug.LogError(
                    "[DirectionsMediaAddressResolver] " +
                    "POI id is missing."
                );

                return null;
            }

            string assetPath =
                AssetDatabase.GetAssetPath(asset);

            string assetName =
                !string.IsNullOrWhiteSpace(assetPath)
                    ? Path.GetFileNameWithoutExtension(assetPath)
                    : asset.name;

            string fieldName =
                sourceField != null
                    ? sourceField.Name.ToLowerInvariant()
                    : string.Empty;

            if (asset is AudioClip)
            {
                if (fieldName.Contains("audiodescription") ||
                    fieldName.Contains("audio_description"))
                {
                    return TheodenAddressablesNaming
                        .GetDirectionsAudioDescriptionAddress(
                            projectId,
                            poiId
                        );
                }

                return TheodenAddressablesNaming
                    .GetDirectionsGenericMediaAddress(
                        projectId,
                        poiId,
                        assetName
                    );
            }

            if (asset is Sprite)
            {
                return TheodenAddressablesNaming
                    .GetDirectionsImageAddress(
                        projectId,
                        poiId,
                        assetName
                    );
            }

            return TheodenAddressablesNaming
                .GetDirectionsGenericMediaAddress(
                    projectId,
                    poiId,
                    assetName
                );
        }
    }
}