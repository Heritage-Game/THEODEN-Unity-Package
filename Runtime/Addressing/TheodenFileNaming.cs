namespace Addressing
{
    public static class TheodenFileNaming
    {
        public static string GetPoiJsonFileName(string poiId, LanguageList language)
        {
            return $"{SanitizeFileNamePart(poiId)}_{language}.json";
        }

        public static string GetDirectionsJsonFileName(string poiId, LanguageList language)
        {
            return $"{SanitizeFileNamePart(poiId)}_directions_{language}.json";
        }

        public static string GetCodexJsonFileName(LanguageList language)
        {
            return $"codex_{language}.json";
        }

        public static string GetQrCodeFileName(string poiId)
        {
            return $"{SanitizeFileNamePart(poiId)}_qr.png";
        }

        private static string SanitizeFileNamePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "unnamed";

            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');

            return value.Trim();
        }
    }
}