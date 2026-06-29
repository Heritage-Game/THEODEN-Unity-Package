




namespace Addressing
{
    /// <summary>
    /// This script holds the convention of file naming for the Addressables assets that are contained inside the POIs.
    /// NOTE: NAMING CONVENTION HAS BEEN CENTRALIZED IN THE CLASS <see cref="TheodenAddressablesNaming"/>,
    /// THIS CLASS IS BEING KEPT AS WRAPPER. ALL REFERENCES TO IT WILL BE CHANGES IN THE FUTURE
    /// </summary>
    public static class PoiAddressNaming
    {
        public static string GetPoiJsonAddress(string poiId, LanguageList language)
        {
            return TheodenAddressablesNaming.GetPoiJsonAddress(poiId, language);
        }

        public static string GetImageAddress(string poiId, string assetName)
        {
            return TheodenAddressablesNaming.GetPoiImageAddress(poiId, assetName);
        }

        public static string GetBadgeAddress(string poiId, string assetName)
        {
            return TheodenAddressablesNaming.GetPoiBadgeAddress(poiId, assetName);
        }

        public static string GetMusicAddress(string poiId)
        {
            return TheodenAddressablesNaming.GetPoiMusicAddress(poiId);
        }

        public static string GetAudioDescriptionAddress(string poiId)
        {
            return TheodenAddressablesNaming.GetPoiAudioDescriptionAddress(poiId);
        }

        public static string GetGenericMediaAddress(string poiId, string assetName)
        {
            return TheodenAddressablesNaming.GetPoiGenericMediaAddress(poiId, assetName);
        }
    }
}