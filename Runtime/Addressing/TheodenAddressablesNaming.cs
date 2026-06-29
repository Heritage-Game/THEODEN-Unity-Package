namespace Addressing
{
    /// <summary>
    /// Provides shared Addressables group, label, and address naming conventions
    /// used by both editor export tools and runtime loading systems.
    /// </summary>
    public static class TheodenAddressablesNaming
    {
        /// <summary>
        /// Returns the Addressables group name used for the runtime POI content.
        /// </summary>
        public static string GetPoiGroupName(string poiId)
        {
            return $"POI_{SanitizeAddressPart(poiId)}";
        }

        /// <summary>
        /// Returns the Addressables label used to download/load all runtime POI content.
        /// </summary>
        public static string GetPoiLabel(string poiId)
        {
            return $"poi_{SanitizeAddressPart(poiId)}";
        }

        /// <summary>
        /// Returns the Addressables group name used for directions content.
        /// </summary>
        public static string GetDirectionsGroupName(string poiId)
        {
            return $"Directions_{SanitizeAddressPart(poiId)}";
        }

        /// <summary>
        /// Returns the Addressables label used to download/load all directions content.
        /// </summary>
        public static string GetDirectionsLabel(string poiId)
        {
            return $"directions_{SanitizeAddressPart(poiId)}";
        }

        /// <summary>
        /// Returns the Addressables address for a POI JSON file.
        /// </summary>
        public static string GetPoiJsonAddress(string poiId, LanguageList language)
        {
            string sanitizedPoiId = SanitizeAddressPart(poiId);
            return $"poi/{sanitizedPoiId}/json/{sanitizedPoiId}_{language}";
        }

        /// <summary>
        /// Returns the Addressables address for a directions JSON file.
        /// </summary>
        public static string GetDirectionsJsonAddress(string poiId, LanguageList language)
        {
            string sanitizedPoiId = SanitizeAddressPart(poiId);
            return $"poi/{sanitizedPoiId}/directions/json/{sanitizedPoiId}_directions_{language}";
        }

        /// <summary>
        /// Returns the Addressables address for a directions image.
        /// </summary>
        public static string GetDirectionsImageAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/images/{SanitizeAddressPart(assetName)}";
        }

        /// <summary>
        /// Returns the Addressables address for a directions audio description.
        /// </summary>
        public static string GetDirectionsAudioDescriptionAddress(string poiId)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/audio/audio_description";
        }

        /// <summary>
        /// Returns the Addressables address for a POI badge asset.
        /// </summary>
        public static string GetPoiBadgeAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/badge/{SanitizeAddressPart(assetName)}";
        }

        /// <summary>
        /// Returns the Addressables address for a POI image asset.
        /// </summary>
        public static string GetPoiImageAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/images/{SanitizeAddressPart(assetName)}";
        }

        /// <summary>
        /// Returns the Addressables address for a POI music asset.
        /// </summary>
        public static string GetPoiMusicAddress(string poiId)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/audio/music";
        }

        /// <summary>
        /// Returns the Addressables address for a POI audio description asset.
        /// </summary>
        public static string GetPoiAudioDescriptionAddress(string poiId)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/audio/audio_description";
        }

        /// <summary>
        /// Returns a generic fallback Addressables address for POI media.
        /// </summary>
        public static string GetPoiGenericMediaAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/media/{SanitizeAddressPart(assetName)}";
        }

        /// <summary>
        /// Returns a generic fallback Addressables address for directions media.
        /// </summary>
        public static string GetDirectionsGenericMediaAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/media/{SanitizeAddressPart(assetName)}";
        }
        
        /// <summary>
        /// Returns the Addressables group name used for codex/menu data.
        /// </summary>
        public static string GetCodexGroupName()
        {
            return "Codex";
        }

        /// <summary>
        /// Returns the Addressables label used to download/load all codex/menu data.
        /// </summary>
        public static string GetCodexLabel()
        {
            return "codex";
        }

        /// <summary>
        /// Returns the Addressables address for a codex/menu JSON file.
        /// </summary>
        public static string GetCodexJsonAddress(LanguageList language)
        {
            return $"codex/json/codex_{language}";
        }

        /// <summary>
        /// Sanitizes one Addressables address segment.
        /// </summary>
        private static string SanitizeAddressPart(string value)
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
    }
}