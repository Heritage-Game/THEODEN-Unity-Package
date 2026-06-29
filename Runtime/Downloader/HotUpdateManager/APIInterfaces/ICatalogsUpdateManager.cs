using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public interface ICatalogsUpdateManager
{
    /// <summary>
    /// Checks for catalog updates. Returns a list of catalog IDs with updates (empty list if none).
    /// Does not auto-release the check handle because we await and release here; callers get the result.
    /// </summary>
    /// <returns>List of catalog ids (strings) that have available updates. Empty list if none.</returns>
    Task<List<string>> CheckForCatalogUpdatesAsync();
    /// <summary>
    /// Update the catalogs specified by catalogIds. If catalogIds is null or empty, Addressables will check all loaded catalogs.
    /// NOTE: UpdateCatalogs blocks other Addressables requests while running.
    /// </summary>
    /// <param name="catalogIds">The ids of the catalogs to update</param>
    /// <returns>Returns true on success (catalogs updated or no updates needed), false on failure.</returns>
    Task<bool> UpdateCatalogsAsync(List<string> catalogIds = null);
    /// <summary>
    /// Checks for updates in the catalogs passed as parameter and, if any, updates them.
    /// </summary>
    /// <returns>True if the update succeded(or no update is required), False if an error has occurred</returns>
    /// <returns>The list of updated catalog ids</returns>
    Task<(bool success, List<string> updatedCatalogIds)> CheckAndUpdateAsync();
}
