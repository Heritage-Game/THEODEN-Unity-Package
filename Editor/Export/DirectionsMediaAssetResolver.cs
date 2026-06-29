using System.IO;
using System.Reflection;
using Addressing;
using UnityEditor;
using UnityEngine;

namespace Editor.Export
{
    /// <summary>
    /// Resolves Addressables addresses for media assets referenced by directions data.
    /// </summary>
    public static class DirectionsMediaAddressResolver
    {
        /// <summary>
        /// Resolves the Addressables address for a directions media asset.
        /// </summary>
        /// <param name="asset">
        /// The Unity asset whose Addressables address must be resolved.
        /// </param>
        /// <param name="poiId">
        /// The id of the Point of Interest associated with the directions data.
        /// </param>
        /// <param name="sourceField">
        /// The field from which the asset was discovered during reflection-based scanning.
        /// </param>
        /// <returns>
        /// The Addressables address that should be assigned to the asset,
        /// or null if the asset is null.
        /// </returns>
        public static string ResolveAddress(
            UnityEngine.Object asset,
            string poiId,
            FieldInfo sourceField)
        {
            if (asset == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(asset);
            string assetName = !string.IsNullOrWhiteSpace(assetPath)
                ? Path.GetFileNameWithoutExtension(assetPath)
                : asset.name;

            string fieldName = sourceField != null
                ? sourceField.Name.ToLowerInvariant()
                : string.Empty;

            if (asset is AudioClip)
            {
                if (fieldName.Contains("audiodescription") ||
                    fieldName.Contains("audio_description"))
                {
                    return TheodenAddressablesNaming.GetDirectionsAudioDescriptionAddress(poiId);
                }

                return TheodenAddressablesNaming.GetDirectionsGenericMediaAddress(poiId, assetName);
            }

            if (asset is Sprite)
            {
                return TheodenAddressablesNaming.GetDirectionsImageAddress(poiId, assetName);
            }

            return TheodenAddressablesNaming.GetDirectionsGenericMediaAddress(poiId, assetName);
        }
    }
}