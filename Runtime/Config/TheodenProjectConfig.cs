using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Asset that stores the setup and configuration data for a THEODEN project.
/// It is not meant to be created manually: its creation is handled by the Setup Wizard Editor Window.
/// </summary>
///
/// ONLY Keep THE ASSET MENU SHORTCUT during development for debugging.
//[CreateAssetMenu(fileName = "TheodenProjectConfig", menuName = "THEODEN/TheodenProjectConfig")]
public class TheodenProjectConfig : ScriptableObject
{
    [Header("Application")]
    public string applicationName;
    public string folderPath;
    
    [Header("Languages")]
    public List<LanguageList> languages = new();
    [FormerlySerializedAs("LanguageConfig")] public LanguageConfig languageConfig;
    
    [Header("POIs")]
    public POIRegistry poiRegistry;
    
    
    [Header("Folders")]
    public string configFolderPath;
    public string codexFolderPath;
    public string directionsFolderPath;
    public string poisFolderPath;
    public string mediaFolderPath;
    public string qrCodeFolderPath;

    [Header("Addressables")] 
    public bool useAddressables = true;
    public string remoteBuildPath;
    public string remoteLoadPath;
    //public bool updateAtFirstOpeningOnly;

}
