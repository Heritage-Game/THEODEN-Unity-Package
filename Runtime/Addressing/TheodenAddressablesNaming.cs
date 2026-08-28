using System;

namespace Addressing
{
    /// <summary>
    /// Provides shared Addressables naming conventions used by
    /// THEODEN editor tools and runtime systems.
    /// </summary>
    public static class TheodenAddressablesNaming
    {
        // ============================================================
        // POI
        // ============================================================

        public static string GetPoiGroupName(
            string projectId,
            string poiId)
        {
            return
                $"THEODEN_{SanitizeAddressPart(projectId)}_" +
                $"POI_{SanitizeAddressPart(poiId)}";
        }

        public static string GetPoiLabel(
            string projectId,
            string poiId)
        {
            return
                $"theoden_{SanitizeAddressPart(projectId)}_" +
                $"poi_{SanitizeAddressPart(poiId)}";
        }

        public static string GetPoiJsonAddress(
            string projectId,
            string poiId,
            LanguageList language)
        {
            string project = SanitizeAddressPart(projectId);
            string poi = SanitizeAddressPart(poiId);

            return
                $"theoden/{project}/poi/{poi}/json/" +
                $"{poi}_{language}";
        }

        public static string GetPoiBadgeAddress(
            string projectId,
            string poiId,
            string assetName)
        {
            return
                $"{GetPoiAddressRoot(projectId, poiId)}/badge/" +
                SanitizeAddressPart(assetName);
        }

        public static string GetPoiImageAddress(
            string projectId,
            string poiId,
            string assetName)
        {
            return
                $"{GetPoiAddressRoot(projectId, poiId)}/images/" +
                SanitizeAddressPart(assetName);
        }

        public static string GetPoiMusicAddress(
            string projectId,
            string poiId)
        {
            return
                $"{GetPoiAddressRoot(projectId, poiId)}/audio/music";
        }

        public static string GetPoiAudioDescriptionAddress(
            string projectId,
            string poiId)
        {
            return
                $"{GetPoiAddressRoot(projectId, poiId)}" +
                "/audio/audio_description";
        }

        public static string GetPoiGenericMediaAddress(
            string projectId,
            string poiId,
            string assetName)
        {
            return
                $"{GetPoiAddressRoot(projectId, poiId)}/media/" +
                SanitizeAddressPart(assetName);
        }

        // ============================================================
        // DIRECTIONS
        // ============================================================

        public static string GetDirectionsGroupName(
            string projectId,
            string poiId)
        {
            return
                $"THEODEN_{SanitizeAddressPart(projectId)}_" +
                $"Directions_{SanitizeAddressPart(poiId)}";
        }

        public static string GetDirectionsLabel(
            string projectId,
            string poiId)
        {
            return
                $"theoden_{SanitizeAddressPart(projectId)}_" +
                $"directions_{SanitizeAddressPart(poiId)}";
        }

        public static string GetDirectionsJsonAddress(
            string projectId,
            string poiId,
            LanguageList language)
        {
            string poi = SanitizeAddressPart(poiId);

            return
                $"{GetDirectionsAddressRoot(projectId, poiId)}/json/" +
                $"{poi}_directions_{language}";
        }

        public static string GetDirectionsImageAddress(
            string projectId,
            string poiId,
            string assetName)
        {
            return
                $"{GetDirectionsAddressRoot(projectId, poiId)}/images/" +
                SanitizeAddressPart(assetName);
        }

        public static string GetDirectionsAudioDescriptionAddress(
            string projectId,
            string poiId)
        {
            return
                $"{GetDirectionsAddressRoot(projectId, poiId)}" +
                "/audio/audio_description";
        }

        public static string GetDirectionsGenericMediaAddress(
            string projectId,
            string poiId,
            string assetName)
        {
            return
                $"{GetDirectionsAddressRoot(projectId, poiId)}/media/" +
                SanitizeAddressPart(assetName);
        }

        // ============================================================
        // CODEX
        // ============================================================

        public static string GetCodexGroupName(
            string projectId)
        {
            return
                $"THEODEN_{SanitizeAddressPart(projectId)}_Codex";
        }

        public static string GetCodexLabel(
            string projectId)
        {
            return
                $"theoden_{SanitizeAddressPart(projectId)}_codex";
        }

        public static string GetCodexJsonAddress(
            string projectId,
            LanguageList language)
        {
            string project = SanitizeAddressPart(projectId);

            return
                $"theoden/{project}/codex/json/codex_{language}";
        }

        // ============================================================
        // MAP
        // ============================================================

        /// <summary>
        /// Returns the Addressables group used by the project map.
        /// </summary>
        public static string GetMapGroupName(string projectId)
        {
            return
                $"THEODEN_{SanitizeAddressPart(projectId)}_Map";
        }

        /// <summary>
        /// Returns the Addressables label associated with the project map.
        /// </summary>
        public static string GetMapLabel(string projectId)
        {
            return
                $"theoden_{SanitizeAddressPart(projectId)}_map";
        }

        /// <summary>
        /// Returns the unique Addressables address of the project
        /// MapDefinition asset.
        /// </summary>
        public static string GetMapDefinitionAddress(string projectId)
        {
            string project =
                SanitizeAddressPart(projectId);

            return $"theoden/{project}/map/definition";
        }
        
        // ============================================================
        // INTERNAL ROOTS
        // ============================================================

        private static string GetPoiAddressRoot(
            string projectId,
            string poiId)
        {
            return
                $"theoden/{SanitizeAddressPart(projectId)}/poi/" +
                SanitizeAddressPart(poiId);
        }

        private static string GetDirectionsAddressRoot(
            string projectId,
            string poiId)
        {
            return
                $"{GetPoiAddressRoot(projectId, poiId)}/directions";
        }

        // ============================================================
        // SANITIZATION
        // ============================================================

        public static string SanitizeAddressPart(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            return value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("\\", "_")
                .Replace("/", "_");
        }

        // ============================================================
        // TEMPORARY LEGACY OVERLOADS
        // ============================================================

        /*
         * These overloads keep the project compiling while the old
         * call sites are migrated. Remove them when every exporter,
         * validator, and runtime loader uses projectId.
         */

        [Obsolete(
            "Use GetPoiGroupName(projectId, poiId)."
        )]
        public static string GetPoiGroupName(string poiId)
        {
            return $"POI_{SanitizeAddressPart(poiId)}";
        }

        [Obsolete(
            "Use GetPoiLabel(projectId, poiId)."
        )]
        public static string GetPoiLabel(string poiId)
        {
            return $"poi_{SanitizeAddressPart(poiId)}";
        }

        [Obsolete(
            "Use GetDirectionsGroupName(projectId, poiId)."
        )]
        public static string GetDirectionsGroupName(string poiId)
        {
            return $"Directions_{SanitizeAddressPart(poiId)}";
        }

        [Obsolete(
            "Use GetDirectionsLabel(projectId, poiId)."
        )]
        public static string GetDirectionsLabel(string poiId)
        {
            return $"directions_{SanitizeAddressPart(poiId)}";
        }

        [Obsolete(
            "Use GetPoiJsonAddress(projectId, poiId, language)."
        )]
        public static string GetPoiJsonAddress(
            string poiId,
            LanguageList language)
        {
            string poi = SanitizeAddressPart(poiId);

            return $"poi/{poi}/json/{poi}_{language}";
        }

        [Obsolete(
            "Use GetDirectionsJsonAddress(projectId, poiId, language)."
        )]
        public static string GetDirectionsJsonAddress(
            string poiId,
            LanguageList language)
        {
            string poi = SanitizeAddressPart(poiId);

            return
                $"poi/{poi}/directions/json/" +
                $"{poi}_directions_{language}";
        }

        [Obsolete(
            "Use GetDirectionsImageAddress(projectId, poiId, assetName)."
        )]
        public static string GetDirectionsImageAddress(
            string poiId,
            string assetName)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}/directions/images/" +
                SanitizeAddressPart(assetName);
        }

        [Obsolete(
            "Use GetDirectionsAudioDescriptionAddress(projectId, poiId)."
        )]
        public static string GetDirectionsAudioDescriptionAddress(
            string poiId)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}" +
                "/directions/audio/audio_description";
        }

        [Obsolete(
            "Use GetPoiBadgeAddress(projectId, poiId, assetName)."
        )]
        public static string GetPoiBadgeAddress(
            string poiId,
            string assetName)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}/badge/" +
                SanitizeAddressPart(assetName);
        }

        [Obsolete(
            "Use GetPoiImageAddress(projectId, poiId, assetName)."
        )]
        public static string GetPoiImageAddress(
            string poiId,
            string assetName)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}/images/" +
                SanitizeAddressPart(assetName);
        }

        [Obsolete(
            "Use GetPoiMusicAddress(projectId, poiId)."
        )]
        public static string GetPoiMusicAddress(string poiId)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}/audio/music";
        }

        [Obsolete(
            "Use GetPoiAudioDescriptionAddress(projectId, poiId)."
        )]
        public static string GetPoiAudioDescriptionAddress(
            string poiId)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}" +
                "/audio/audio_description";
        }

        [Obsolete(
            "Use GetPoiGenericMediaAddress(projectId, poiId, assetName)."
        )]
        public static string GetPoiGenericMediaAddress(
            string poiId,
            string assetName)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}/media/" +
                SanitizeAddressPart(assetName);
        }

        [Obsolete(
            "Use GetDirectionsGenericMediaAddress(projectId, poiId, assetName)."
        )]
        public static string GetDirectionsGenericMediaAddress(
            string poiId,
            string assetName)
        {
            return
                $"poi/{SanitizeAddressPart(poiId)}/directions/media/" +
                SanitizeAddressPart(assetName);
        }

        [Obsolete(
            "Use GetCodexGroupName(projectId)."
        )]
        public static string GetCodexGroupName()
        {
            return "Codex";
        }

        [Obsolete(
            "Use GetCodexLabel(projectId)."
        )]
        public static string GetCodexLabel()
        {
            return "codex";
        }

        [Obsolete(
            "Use GetCodexJsonAddress(projectId, language)."
        )]
        public static string GetCodexJsonAddress(
            LanguageList language)
        {
            return $"codex/json/codex_{language}";
        }
    }
}