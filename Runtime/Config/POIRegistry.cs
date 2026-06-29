using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// Registry containing all POIs added during setup mode.
/// It stores the id, display name, folder path and configuration state of each POI.
/// Exposes some services to access the information of the registry and update them:
///    
///     1. search POI via id;
///     2. Add a POI;
///     3. Update the display name of a POI;
///     4. Update the folder path for a POI;
///     5. Update the status of a POI as configured/Unconfigured;
///     6. Remove a POI from registry;
///     7. Validate duplicates in the registry entries
///     8. Search for POIs by status.
///
/// </summary>
[CreateAssetMenu(fileName = "POIRegistry", menuName = "THEODEN/POIRegistry")]
public class POIRegistry : ScriptableObject
{
    [SerializeField] private List<POIRegistryEntry> pois =  new();
    public IReadOnlyList<POIRegistryEntry> Pois => pois;

    public bool ContainsId(string id)
    {
        return pois.Exists(poi => poi.PoiId == id);
    }

    public void AddPoiUnsafe(string poiId, string poiName, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(poiId))
        {
            Debug.LogError("poiId fiels is null or whitespace. Cannot add poi");
            return;
        }


        if (ContainsId(poiId))
        {
            Debug.LogWarning("poiId already exists");
            return;
        }
            
        pois.Add(new POIRegistryEntry(poiId, poiName, folderPath));
    }

    public bool AddPoi(string poiId, string poiName, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(poiId))
        {
            Debug.LogError("poiId fiels is null or whitespace. Cannot add poi");
            return false;
        }

        if (string.IsNullOrWhiteSpace(poiName))
        {
            Debug.LogError("poiName fiels is null or whitespace. Cannot add poi");
            return false;
        }

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Debug.LogError("folderPath is null or whitespace. Cannot add poi");
            return false;
        }

        if (ContainsId(poiId))
        {
            Debug.LogWarning("poiId already exists");
            return false;
        }
        
        pois.Add(new POIRegistryEntry(poiId, poiName, folderPath));
        return true;
    }

    /// <summary>
    /// USE WITH CAUTION
    /// </summary>
    /// <param name="poiId"></param>
    /// <returns></returns>
    public bool RemovePoi(string poiId)
    {
        POIRegistryEntry poi = GetById(poiId);

        if (poi == null)
        {
            Debug.LogWarning("poiId not found with id: {poiId}");
            return false;
        }
        pois.Remove(poi);
        return true;
    }
    public POIRegistryEntry GetById(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId))
        {
            return null;
        }
        return pois.Find(poi => poi.PoiId == poiId);
    }

    public bool TryGetById(string poiId, out POIRegistryEntry entry)
    {
        entry = GetById(poiId);
        return entry != null;
    }

    public List<string> GetAllIds()
    {
        return pois.Select(poi => poi.PoiId).ToList();
    }

    public List<string> GetAllFolderPaths()
    {
        return pois.Select(poi => poi.FolderPath).ToList();
    }

    public List<string> GetDysplayNames()
    {
        return pois.Select(poi => poi.DisplayName).ToList();
    }

    public IReadOnlyList<POIRegistryEntry> GetConfiguredPois()
    {
        return pois.Where(poi => poi.IsConfigured).ToList();
    }

    public IReadOnlyList<POIRegistryEntry> GetUnconfiguredPois()
    {
        return pois.Where(poi => !poi.IsConfigured).ToList();
    }

    #region UpdateEntries

    public bool UpdateDiplayedName(string poiId, string newDisplayName)
    {
        POIRegistryEntry poi = GetById(poiId);
        if (poi == null)
        {
            Debug.LogError("poiId not found with id: {poiId}");
            return false;
        }
        poi.UpdateDisplayName(newDisplayName);
        return true;
    }

    public bool UpdateFolderPath(string poiId, string newFolderPath)
    {
        POIRegistryEntry poi = GetById(poiId);
        if (poi == null)
        {
            Debug.LogError("poiId not found with id: {poiId}");
            return false;
        }
        poi.UpdateFolderPath(newFolderPath);
        return true;
    }

    public bool MarkPoiAsConfigured(string poiId)
    {
        POIRegistryEntry poi = GetById(poiId);
        if (poi == null)
        {
            Debug.LogError("poiId not found with id: {poiId}");
            return false;
        }
        poi.MarkAsConfigured();
        return true;
    }

    public bool MarkPoiAsUnconfigured(string poiId)
    {
        POIRegistryEntry poi = GetById(poiId);
        if (poi == null)
        {
            Debug.LogError("poiId not found with id: {poiId}");
            return false;
        }
        poi.UnmarkAsConfigured();
        return true;
    }

    #endregion

    #region RegistryValidation

    public List<string> ValidateRegistry()
    {
        List<string> errors = new();
        HashSet<string> poiIds = new HashSet<string>();

        foreach (POIRegistryEntry poi in pois)
        {
            if (string.IsNullOrWhiteSpace(poi.PoiId))
            {
                errors.Add($"poi has an empty Id: {poi.DisplayName}");
            }

            if (string.IsNullOrWhiteSpace(poi.DisplayName))
            {
                errors.Add($"poi has an empty DisplayName: {poi.PoiId}");
            }

            if (string.IsNullOrWhiteSpace(poi.FolderPath))
            {
                errors.Add($"poi has an empty FolderPath: {poi.PoiId}");
            }

            if (!poiIds.Add(poi.PoiId))
            {
                errors.Add($"poiId {poi.PoiId} already exists");
            }
            
        } return errors;
    }

    #endregion
    
}
