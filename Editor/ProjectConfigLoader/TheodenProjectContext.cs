

using System;
using System.Collections.Generic;
/// <summary>
/// This class serves as a context holder for the information loaded from the configuration assets of a project.
/// The configuration assets are created during the set-up fase of the project creation
/// </summary>
[Serializable]
public class TheodenProjectContext
{
    public string projectFolderPath;

    public TheodenProjectConfig theodenProjectConfig;
    public LanguageConfig languageConfig;
    public POIRegistry poiRegistry;

    public string projectId => theodenProjectConfig.projectId;
    public string poisFolderPath => theodenProjectConfig.poisFolderPath;
    public string codexFolderPath => theodenProjectConfig.codexFolderPath;
    public string directionsFolderPath => theodenProjectConfig.directionsFolderPath;
    public string mediaFolderPath => theodenProjectConfig.mediaFolderPath;
    public string qrCodesFolderPath => theodenProjectConfig.qrCodeFolderPath;
    
    public List<LanguageEntry> availableLanguages = new();
    public List<POIRegistryEntry> availablePois = new();

    public bool IsValid =>
        theodenProjectConfig != null &&
        languageConfig != null &&
        poiRegistry != null &&
        !string.IsNullOrEmpty(projectId) &&
        !string.IsNullOrWhiteSpace(poisFolderPath) &&
        !string.IsNullOrWhiteSpace(codexFolderPath) &&
        !string.IsNullOrWhiteSpace(directionsFolderPath) &&
        !string.IsNullOrWhiteSpace(mediaFolderPath) &&
        !string.IsNullOrWhiteSpace(qrCodesFolderPath) &&
        availableLanguages.Count > 0 &&
        availablePois.Count > 0;
}