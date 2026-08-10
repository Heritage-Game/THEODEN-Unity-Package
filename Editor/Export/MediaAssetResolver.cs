using System;
using System.IO;
using System.Reflection;
using Addressing;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Resolves namespaced Addressables addresses for media assets
    /// referenced by a POI template.
    /// </summary>
    public static class MediaAddressResolver
    {
        /// <summary>
        /// Resolves the namespaced Addressables address assigned
        /// to a POI media asset.
        /// </summary>
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
                    "[MediaAddressResolver] Project id is missing."
                );

                return null;
            }

            if (string.IsNullOrWhiteSpace(poiId))
            {
                Debug.LogError(
                    "[MediaAddressResolver] POI id is missing."
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
                return ResolveAudioAddress(
                    projectId,
                    poiId,
                    assetName,
                    fieldName
                );
            }

            if (asset is Sprite)
            {
                return ResolveSpriteAddress(
                    projectId,
                    poiId,
                    assetName,
                    fieldName
                );
            }

            return TheodenAddressablesNaming
                .GetPoiGenericMediaAddress(
                    projectId,
                    poiId,
                    assetName
                );
        }

        /// <summary>
        /// Resolves an audio address based on the source field.
        /// </summary>
        private static string ResolveAudioAddress(
            string projectId,
            string poiId,
            string assetName,
            string fieldName)
        {
            if (fieldName.Contains("music"))
            {
                return TheodenAddressablesNaming
                    .GetPoiMusicAddress(
                        projectId,
                        poiId
                    );
            }

            if (fieldName.Contains("audiodescription") ||
                fieldName.Contains("audio_description") ||
                fieldName.Contains("description"))
            {
                return TheodenAddressablesNaming
                    .GetPoiAudioDescriptionAddress(
                        projectId,
                        poiId
                    );
            }

            return TheodenAddressablesNaming
                .GetPoiGenericMediaAddress(
                    projectId,
                    poiId,
                    assetName
                );
        }

        /// <summary>
        /// Resolves a Sprite address based on the source field.
        /// </summary>
        private static string ResolveSpriteAddress(
            string projectId,
            string poiId,
            string assetName,
            string fieldName)
        {
            if (fieldName.Contains("badge"))
            {
                return TheodenAddressablesNaming
                    .GetPoiBadgeAddress(
                        projectId,
                        poiId,
                        assetName
                    );
            }

            return TheodenAddressablesNaming
                .GetPoiImageAddress(
                    projectId,
                    poiId,
                    assetName
                );
        }

        // ============================================================
        // TEMPORARY LEGACY OVERLOAD
        // ============================================================

        /// <summary>
        /// Temporary overload used by call sites that have not yet
        /// been migrated to project-scoped addresses.
        /// </summary>
        [Obsolete(
            "Use ResolveAddress(asset, projectId, poiId, sourceField)."
        )]
        public static string ResolveAddress(
            UnityEngine.Object asset,
            string poiId,
            FieldInfo sourceField)
        {
            if (asset == null)
                return null;

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
                if (fieldName.Contains("music"))
                {
#pragma warning disable CS0618
                    return TheodenAddressablesNaming
                        .GetPoiMusicAddress(poiId);
#pragma warning restore CS0618
                }

                if (fieldName.Contains("audiodescription") ||
                    fieldName.Contains("audio_description") ||
                    fieldName.Contains("description"))
                {
#pragma warning disable CS0618
                    return TheodenAddressablesNaming
                        .GetPoiAudioDescriptionAddress(poiId);
#pragma warning restore CS0618
                }
            }

            if (asset is Sprite)
            {
                if (fieldName.Contains("badge"))
                {
#pragma warning disable CS0618
                    return TheodenAddressablesNaming
                        .GetPoiBadgeAddress(
                            poiId,
                            assetName
                        );
#pragma warning restore CS0618
                }

#pragma warning disable CS0618
                return TheodenAddressablesNaming
                    .GetPoiImageAddress(
                        poiId,
                        assetName
                    );
#pragma warning restore CS0618
            }

#pragma warning disable CS0618
            return TheodenAddressablesNaming
                .GetPoiGenericMediaAddress(
                    poiId,
                    assetName
                );
#pragma warning restore CS0618
        }
    }
}