using System;
using UnityEngine;
/// <summary>
/// This class represents an Entry to the POIRegistry that cointains the POI id, name to dysplay, the path
/// to the folder where all the media and data relating to the POI are contained.
/// A POI is considered configured when a definition for the POI is created via the <see cref="LevelDefinitionBuilderWindow"/>.
/// </summary>
[Serializable]
public class POIRegistryEntry
{
    [SerializeField] private string poiId;
    [SerializeField] private string displayName;
    [SerializeField] private string folderPath;
    [SerializeField] private bool isConfigured;
    
    public string PoiId => poiId;
    public string DisplayName => displayName;
    public string FolderPath => folderPath;
    public bool IsConfigured => isConfigured;

    public POIRegistryEntry(string poiId, string displayName, string folderPath)
    {
        this.poiId = poiId;
        this.displayName = displayName;
        this.folderPath = folderPath;
        this.isConfigured = false;
    }

    /// <summary>
    /// This method marks a POI as configured. A POI is considered configured when a Level Definition is created for it.
    /// So this means that it cointains all the information to play the level.
    /// </summary>
    public void MarkAsConfigured()
    {
        isConfigured = true;
    }

    /// <summary>
    /// This method unmarks POI as configured. This can happen because the definition file for the POI is deleted or
    /// moved erroneously in a folder where it doesn't belong.
    /// </summary>
    public void UnmarkAsConfigured()
    {
        isConfigured = false;
    }

    /// <summary>
    /// Update the current display name.
    /// </summary>
    /// <param name="newDisplayName"> the new name </param>
    public void UpdateDisplayName(string newDisplayName)
    {
        if (string.IsNullOrWhiteSpace(newDisplayName))
        {
            Debug.LogWarning("Display name cannot be empty");
            return;
        }
        displayName = newDisplayName;
    }

    /// <summary>
    /// Udate the folderpath value.
    /// </summary>
    /// <param name="newFolderPath">the new path</param>
    public void UpdateFolderPath(string newFolderPath)
    {
        if (string.IsNullOrWhiteSpace(newFolderPath))
        {
            Debug.LogWarning("Folder path cannot be empty");
            return;
        }
        folderPath = newFolderPath;
    }
}
