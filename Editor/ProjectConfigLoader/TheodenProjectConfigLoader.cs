using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This class is a service class that loads the information contained inside the Configuration assets of the project
/// in a <see cref="TheodenProjectContext"/> instance to be easily accessible by the Editor Windows.
/// </summary>
public static class TheodenProjectConfigLoader
{
    public static bool TryLoadProjectContext(
        string projectFolderPath,
        out TheodenProjectContext context,
        out string error
    )
    {
        context = new TheodenProjectContext
        {
            projectFolderPath = projectFolderPath
        };

        error = null;

        if (string.IsNullOrWhiteSpace(projectFolderPath))
        {
            error = "Project folder path is empty.";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(projectFolderPath))
        {
            error = $"Selected path is not a valid Unity folder: {projectFolderPath}";
            return false;
        }

        context.theodenProjectConfig = FindSingleAssetInFolder<TheodenProjectConfig>(projectFolderPath);
        context.languageConfig = FindSingleAssetInFolder<LanguageConfig>(projectFolderPath);
        context.poiRegistry = FindSingleAssetInFolder<POIRegistry>(projectFolderPath);

        if (context.theodenProjectConfig == null)
        {
            error = $"TheodenProjectConfig file not found at: {projectFolderPath}";
            return false;
        }

        if (context.languageConfig == null)
        {
            error = $"LanguageConfig not found inside project folder: {projectFolderPath}";
            return false;
        }

        if (context.poiRegistry == null)
        {
            error = $"POI registry not found inside project folder: {projectFolderPath}";
            return false;
        }
        
        LoadLanguages(context);
        LoadPois(context);

        if (string.IsNullOrWhiteSpace(context.poisFolderPath))
        {
            error = "POIs folder path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.codexFolderPath))
        {
            error = "Codex folder path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.directionsFolderPath))
        {
            error = "Directions folder path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.mediaFolderPath))
        {
            error = "Media folder path is empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.qrCodesFolderPath))
        {
            error = "QR codes folder path is empty.";
            return false;
        }
        
        if (!AssetDatabase.IsValidFolder(context.poisFolderPath))
        {
            error = $"POIs folder is not a valid Unity folder: {context.poisFolderPath}";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(context.codexFolderPath))
        {
            error = $"Codex folder is not a valid Unity folder: {context.codexFolderPath}";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(context.directionsFolderPath))
        {
            error = $"Directions folder is not a valid Unity folder: {context.directionsFolderPath}";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(context.mediaFolderPath))
        {
            error = $"Media folder is not a valid Unity folder: {context.mediaFolderPath}";
            return false;
        }
        if (!AssetDatabase.IsValidFolder(context.qrCodesFolderPath))
        {
            error = $"QR codes folder is not a valid Unity folder: {context.qrCodesFolderPath}";
            return false;
        }

        if (context.availableLanguages.Count == 0)
        {
            error = "LanguageConfig was found, but it does not contain any language.";
            return false;
        }

        if (context.availablePois.Count == 0)
        {
            error = "POI registry was found, but it does not contain any POI.";
            return false;
        }

        return true;
    }

    private static T FindSingleAssetInFolder<T>(string folderPath) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });

        if (guids == null || guids.Length == 0)
            return null;

        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(assetPath);
    }

    private static void LoadLanguages(TheodenProjectContext context)
    {
        context.availableLanguages.Clear();

        //ADD all the elements in languageConfig.languages
        context.availableLanguages.AddRange(context.languageConfig.languages);
    }

    private static void LoadPois(TheodenProjectContext context)
    {
        context.availablePois.Clear();

        // Add all the elements in PoiRegistry
        context.availablePois.AddRange(context.poiRegistry.Pois);
    }
    
}