using System.IO;
using Addressing;
/// <summary>
/// This class holds the paths for nested folders inside the project folder.
/// It was created to have an easy access point to the Data and Media folder of the POIs as well as the folders to save
/// codex, directions and Qrcodes in the project.
/// </summary>
public static class TheodenProjectPaths
{
    public static string GetPoiRootFolder(string poisRootFolderPath, string poiId)
    {
        return NormalizeUnityPath(Path.Combine(poisRootFolderPath, poiId));
    }

    public static string GetPoiDataFolder(string poisRootFolderPath, string poiId)
    {
        return NormalizeUnityPath(Path.Combine(
            GetPoiRootFolder(poisRootFolderPath, poiId),
            "Data"
        ));
    }

    public static string GetPoiMediaFolder(string poisRootFolderPath, string poiId)
    {
        return NormalizeUnityPath(Path.Combine(
            GetPoiRootFolder(poisRootFolderPath, poiId),
            "Media"
        ));
    }

    public static string GetCodexJsonPath(string codexFolderPath, LanguageList language)
    {
        return NormalizeUnityPath(Path.Combine(
            codexFolderPath,
            TheodenFileNaming.GetCodexJsonFileName(language)
        ));
    }

    public static string GetDirectionsJsonPath(
        string directionsFolderPath,
        string poiId,
        LanguageList language)
    {
        return NormalizeUnityPath(Path.Combine(
            directionsFolderPath,
            TheodenFileNaming.GetDirectionsJsonFileName(poiId, language)
        ));
    }

    public static string GetQrCodePath(string qrCodesFolderPath, string poiId)
    {
        return NormalizeUnityPath(Path.Combine(
            qrCodesFolderPath,
            TheodenFileNaming.GetQrCodeFileName(poiId)
        ));
    }

    public static string NormalizeUnityPath(string path)
    {
        return path.Replace("\\", "/");
    }
}
