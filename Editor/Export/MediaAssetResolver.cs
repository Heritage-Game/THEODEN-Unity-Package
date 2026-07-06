using System.IO;
using System.Reflection;
using Addressing;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Resolves Addressables addresses for media assets referenced by POI template data.
    /// </summary>
    /// <remarks>
    /// This resolver is used during the editor-side POI export pipeline.
    /// It receives a Unity asset reference discovered inside a template and assigns
    /// an Addressables address according to the semantic role of the field that contains it.
    ///
    /// For example:
    /// - a Sprite found in a field containing "badge" is treated as the POI badge;
    /// - a Sprite found elsewhere is treated as a generic POI image;
    /// - an AudioClip found in a field containing "music" is treated as POI music;
    /// - an AudioClip found in a field containing "description" is treated as an audio description.
    ///
    /// The generated addresses use <see cref="TheodenAddressablesNaming"/> so that
    /// editor export tools and runtime loading systems share the same naming convention.
    /// </remarks>
    public static class MediaAddressResolver
    {
        /// <summary>
        /// Resolves the Addressables address that should be assigned to a POI media asset.
        /// </summary>
        /// <param name="asset">
        /// The Unity asset whose Addressables address must be resolved.
        /// </param>
        /// <param name="poiId">
        /// The unique id of the Point of Interest associated with the asset.
        /// </param>
        /// <param name="sourceField">
        /// The reflected field from which the asset was discovered.
        /// This is used to infer the semantic role of the asset.
        /// </param>
        /// <returns>
        /// The Addressables address that should be assigned to the asset,
        /// or <c>null</c> if the asset is null.
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
                return ResolveAudioAddress(poiId, assetName, fieldName);

            if (asset is Sprite)
                return ResolveSpriteAddress(poiId, assetName, fieldName);

            return TheodenAddressablesNaming.GetPoiGenericMediaAddress(poiId, assetName);
        }

        /// <summary>
        /// Resolves the Addressables address for a POI audio asset.
        /// </summary>
        /// <param name="poiId">
        /// The unique id of the Point of Interest associated with the audio asset.
        /// </param>
        /// <param name="assetName">
        /// The file name of the audio asset without extension.
        /// </param>
        /// <param name="fieldName">
        /// The lower-case name of the field from which the asset was discovered.
        /// </param>
        /// <returns>
        /// The Addressables address for the audio asset.
        /// </returns>
        private static string ResolveAudioAddress(
            string poiId,
            string assetName,
            string fieldName)
        {
            if (fieldName.Contains("music"))
                return TheodenAddressablesNaming.GetPoiMusicAddress(poiId);

            if (fieldName.Contains("audiodescription") ||
                fieldName.Contains("audio_description") ||
                fieldName.Contains("description"))
            {
                return TheodenAddressablesNaming.GetPoiAudioDescriptionAddress(poiId);
            }

            return TheodenAddressablesNaming.GetPoiGenericMediaAddress(poiId, assetName);
        }

        /// <summary>
        /// Resolves the Addressables address for a POI sprite asset.
        /// </summary>
        /// <param name="poiId">
        /// The unique id of the Point of Interest associated with the sprite asset.
        /// </param>
        /// <param name="assetName">
        /// The file name of the sprite asset without extension.
        /// </param>
        /// <param name="fieldName">
        /// The lower-case name of the field from which the asset was discovered.
        /// </param>
        /// <returns>
        /// The Addressables address for the sprite asset.
        /// </returns>
        private static string ResolveSpriteAddress(
            string poiId,
            string assetName,
            string fieldName)
        {
            if (fieldName.Contains("badge"))
                return TheodenAddressablesNaming.GetPoiBadgeAddress(poiId, assetName);

            return TheodenAddressablesNaming.GetPoiImageAddress(poiId, assetName);
        }
    }
}