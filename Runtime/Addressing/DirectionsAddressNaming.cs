namespace Addressing
{
    /// <summary>
    /// Provides Addressables address naming rules for Directions assets.
    /// </summary>
    public static class DirectionsAddressNaming
    {
        /// <summary>
        /// Returns the Addressables address for a directions JSON file.
        /// </summary>
        public static string GetDirectionsJsonAddress(string poiId, LanguageList language)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/json/{SanitizeAddressPart(poiId)}_directions_{language}";
        }

        /// <summary>
        /// Returns the Addressables address for a directions image.
        /// </summary>
        public static string GetImageAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/images/{SanitizeAddressPart(assetName)}";
        }

        /// <summary>
        /// Returns the Addressables address for a directions audio description.
        /// </summary>
        public static string GetAudioDescriptionAddress(string poiId)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/audio/audio_description";
        }

        /// <summary>
        /// Returns a generic fallback address for directions media.
        /// </summary>
        public static string GetGenericMediaAddress(string poiId, string assetName)
        {
            return $"poi/{SanitizeAddressPart(poiId)}/directions/media/{SanitizeAddressPart(assetName)}";
        }

        /// <summary>
        /// Sanitizes one address segment.
        /// </summary>
        private static string SanitizeAddressPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            return value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("\\", "/");
        }
    }
}