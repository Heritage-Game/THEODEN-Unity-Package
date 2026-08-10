using System;
using System.Collections;
using System.Threading.Tasks;
using ContentLoading;
using Core.Models;
using UnityEngine;

/// <summary>
/// Central runtime manager responsible for loading and storing THEODEN game data.
/// </summary>
/// <remarks>
/// This manager keeps the currently loaded Codex, Directions and POI data available
/// across runtime scenes.
///
/// The manager does not read JSON files directly from StreamingAssets.
/// Instead, it delegates runtime loading to <see cref="TheodenRuntimeContentLoader"/>,
/// which uses Addressables and the shared THEODEN naming conventions.
///
/// The runtime loading flow is:
///
/// Codex:
/// Addressables JSON -> CodexJsonTranslator -> CodexModel
///
/// Directions:
/// Addressables JSON -> DirectionsJsonTranslator -> DirectionsToNextPOIModel -> DirectionsAssetResolver
///
/// POI:
/// Addressables JSON -> POIJsonTranslator -> POIModel -> POIAssetResolver
///
/// Main responsibilities:
/// - load the codex for the selected language;
/// - keep track of the selected codex item;
/// - load directions for a selected POI;
/// - load POI data after a QR scan;
/// - manage codex item state transitions;
/// - expose compatibility wrappers for older coroutine-based callers.
/// </remarks>
public class DataManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the DataManager.
    /// </summary>
    public static DataManager Instance { get; private set; }

    // =====================================================
    // RUNTIME DATA
    // =====================================================

    /// <summary>
    /// Runtime codex model used by the Codex UI.
    /// </summary>
    public CodexModel CodexMenu { get; private set; }

    /// <summary>
    /// Currently selected codex item.
    /// </summary>
    public CodexItemDefinition SelectedCodexItem { get; private set; }

    /// <summary>
    /// Currently loaded directions data.
    /// </summary>
    public DirectionsToNextPOIModel SelectedDirections { get; private set; }

    /// <summary>
    /// Currently loaded POI data.
    /// </summary>
    public POIModel SelectedPOI { get; private set; }

    /// <summary>
    /// True when the codex has been loaded successfully.
    /// </summary>
    public bool IsDataLoaded { get; private set; }

    /// <summary>
    /// True when the selected directions data has been loaded successfully.
    /// </summary>
    public bool IsDirectionsLoaded { get; private set; }

    /// <summary>
    /// True when the selected POI data has been loaded successfully.
    /// </summary>
    public bool IsPOILoaded { get; private set; }

    // =====================================================
    // OLD COMPATIBILITY
    // =====================================================

    /*
     * These fields are kept temporarily so older scripts do not break immediately.
     * Prefer using CodexMenu, SelectedCodexItem, SelectedDirections and SelectedPOI
     * in the new Addressables-based runtime flow.
     */

    /// <summary>
    /// Legacy codex data root kept for temporary backwards compatibility.
    /// </summary>
    public CodexDataRoot CodexData { get; private set; }

    /// <summary>
    /// Legacy selected level kept for temporary backwards compatibility.
    /// </summary>
    public LevelData SelectedLevel { get; set; }

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    /// <summary>
    /// Initializes the singleton instance.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Loads the codex when the DataManager starts.
    /// </summary>
    private async void Start()
    {
        await LoadCodexMenuAsync();
    }

    // =====================================================
    // CODEX MENU LOADING
    // =====================================================

    /// <summary>
    /// Loads the codex JSON for the current language from Addressables
    /// and translates it into the runtime <see cref="CodexModel"/>.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous loading operation.
    /// </returns>
    /// <remarks>
    /// The exported JSON is loaded as a raw JSON string.
    /// It is then converted into the runtime model through <see cref="CodexJsonTranslator"/>.
    ///
    /// The translator is responsible for initializing runtime-only state,
    /// such as setting the first item to <see cref="CodexItemState.Directions"/>
    /// and the others to <see cref="CodexItemState.Locked"/>.
    /// </remarks>
    public async Task LoadCodexMenuAsync()
    {
        IsDataLoaded = false;
        CodexMenu = null;

        try
        {
            LanguageList language = GetCurrentLanguage();

            string json = await TheodenRuntimeContentLoader.LoadCodexJsonAsync(language);

            CodexMenu = CodexJsonTranslator.FromJson(json);

            if (CodexMenu == null || CodexMenu.items == null)
            {
                Debug.LogError("[DataManager] Codex menu could not be loaded.");
                return;
            }
            
            SynchronizeCodexStatesWithPlayerProgress();

            IsDataLoaded = true;

            Debug.Log("[DataManager] Codex menu loaded for language: " + language);
            Debug.Log("[DataManager] Codex items: " + CodexMenu.items.Count);
        }
        catch (Exception ex)
        {
            Debug.LogError("[DataManager] Failed to load Codex menu.");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Coroutine wrapper for loading the codex menu.
    /// </summary>
    /// <returns>
    /// Coroutine-compatible wait operation.
    /// </returns>
    /// <remarks>
    /// Use this only for older scripts that still rely on coroutines.
    /// New code should call <see cref="LoadCodexMenuAsync"/> directly.
    /// </remarks>
    public IEnumerator LoadCodexMenu()
    {
        Task task = LoadCodexMenuAsync();
        yield return WaitForTask(task);
    }

    // =====================================================
    // CODEX ITEM / DIRECTIONS
    // =====================================================

    /// <summary>
    /// Selects a codex item and loads the content required by its current state.
    /// </summary>
    /// <param name="item">
    /// Codex item selected by the user.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// If the item is locked, nothing is loaded.
    ///
    /// If the item is in Directions state, the directions content for its POI id is loaded.
    ///
    /// If the item is already unlocked, the POI content is loaded directly.
    /// </remarks>
    public async Task SelectCodexItemAndLoadDirectionsAsync(CodexItemDefinition item)
    {
        if (item == null)
        {
            Debug.LogError("[DataManager] Tried to select a null Codex item.");
            return;
        }

        if (item.state == CodexItemState.Locked)
        {
            Debug.LogWarning("[DataManager] Tried to open locked item: " + item.levelTitle);
            return;
        }

        SelectedCodexItem = item;

        if (SelectedCodexItem.state == CodexItemState.Directions)
        {
            await LoadDirectionsAsync(SelectedCodexItem.poiId);
            return;
        }

        if (SelectedCodexItem.state == CodexItemState.Unlocked)
        {
            await LoadPOIFromQrAsync(SelectedCodexItem.poiId);
        }
    }

    /// <summary>
    /// Coroutine wrapper for selecting a codex item and loading its associated content.
    /// </summary>
    /// <param name="item">
    /// Codex item selected by the user.
    /// </param>
    /// <returns>
    /// Coroutine-compatible wait operation.
    /// </returns>
    public IEnumerator SelectCodexItemAndLoadDirections(CodexItemDefinition item)
    {
        Task task = SelectCodexItemAndLoadDirectionsAsync(item);
        yield return WaitForTask(task);
    }

    /// <summary>
    /// Loads directions for a POI using Addressables.
    /// </summary>
    /// <param name="poiId">
    /// POI id, or an older directions parameter such as roman_empire_directions_ENG.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous loading operation.
    /// </returns>
    /// <remarks>
    /// This method loads the raw directions JSON through <see cref="TheodenRuntimeContentLoader"/>,
    /// converts it into <see cref="DirectionsToNextPOIModel"/> using
    /// <see cref="DirectionsJsonTranslator"/>, and then resolves image/audio assets through
    /// <see cref="DirectionsAssetResolver"/>.
    /// </remarks>
    public async Task LoadDirectionsAsync(string poiId)
    {
        IsDirectionsLoaded = false;
        SelectedDirections = null;

        poiId = ResolvePoiIdFromDirectionsInput(poiId);

        if (string.IsNullOrWhiteSpace(poiId))
        {
            Debug.LogError("[DataManager] POI id is empty. Cannot load directions.");
            return;
        }

        try
        {
            LanguageList language = GetCurrentLanguage();

            string json = await TheodenRuntimeContentLoader.LoadDirectionsJsonAsync(
                poiId,
                language
            );

            SelectedDirections = DirectionsJsonTranslator.FromJson(json);

            if (SelectedDirections == null)
            {
                Debug.LogError("[DataManager] Directions could not be translated for POI: " + poiId);
                return;
            }

            await DirectionsAssetResolver.ResolveAssetsAsync(SelectedDirections);

            IsDirectionsLoaded = true;

            Debug.Log("[DataManager] Directions loaded for POI: " + poiId);
            Debug.Log("[DataManager] Directions title: " + SelectedDirections.poiName);
        }
        catch (Exception ex)
        {
            Debug.LogError("[DataManager] Failed to load directions for POI: " + poiId);
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Coroutine wrapper for loading directions.
    /// </summary>
    /// <param name="directionsParameterOrPoiId">
    /// Either a POI id or an old directions parameter.
    /// </param>
    /// <returns>
    /// Coroutine-compatible wait operation.
    /// </returns>
    /// <remarks>
    /// This wrapper accepts the old direction parameter form, such as
    /// roman_empire_directions_ENG, and attempts to resolve it back to a POI id.
    /// </remarks>
    public IEnumerator LoadDirections(string directionsParameterOrPoiId)
    {
        Task task = LoadDirectionsAsync(directionsParameterOrPoiId);
        yield return WaitForTask(task);
    }

    // =====================================================
    // QR / POI LOADING
    // =====================================================

    /// <summary>
    /// Loads POI data from a QR scan result.
    /// </summary>
    /// <param name="qrCode">
    /// QR scan result. In the current THEODEN flow this should contain only the POI id.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous loading operation.
    /// </returns>
    /// <remarks>
    /// The QR value is treated directly as the POI id.
    ///
    /// This method loads the raw POI JSON through <see cref="TheodenRuntimeContentLoader"/>,
    /// converts it into the correct runtime model through <see cref="POIJsonTranslator"/>,
    /// and resolves referenced media assets through <see cref="POIAssetResolver"/>.
    /// </remarks>
    public async Task LoadPOIFromQrAsync(string qrCode)
    {
        IsPOILoaded = false;
        SelectedPOI = null;

        if (string.IsNullOrWhiteSpace(qrCode))
        {
            Debug.LogError("[DataManager] QR code is empty.");
            return;
        }

        string poiId = qrCode.Trim();

        try
        {
            LanguageList language = GetCurrentLanguage();

            string json = await TheodenRuntimeContentLoader.LoadPoiJsonAsync(
                poiId,
                language
            );

            SelectedPOI = POIJsonTraslator.FromJson(json);

            if (SelectedPOI == null)
            {
                Debug.LogError("[DataManager] POI could not be translated from QR: " + poiId);
                return;
            }

            // Verify the state of the POI - if it's already completed or not
            PlayerProgressService.ApplyCompletionState(SelectedPOI);
            await POIAssetResolver.ResolveAssetsAsync(SelectedPOI);

            IsPOILoaded = true;

            Debug.Log("[DataManager] POI model type: " + SelectedPOI.GetType().Name);
            Debug.Log("[DataManager] POI loaded from QR: " + poiId + " | " + SelectedPOI.poiName);
        }
        catch (Exception ex)
        {
            Debug.LogError("[DataManager] Failed to load POI from QR: " + poiId);
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Coroutine wrapper for loading POI data from a QR scan result.
    /// </summary>
    /// <param name="qrCode">
    /// QR scan result containing the POI id.
    /// </param>
    /// <returns>
    /// Coroutine-compatible wait operation.
    /// </returns>
    public IEnumerator LoadPOIFromQr(string qrCode)
    {
        Task task = LoadPOIFromQrAsync(qrCode);
        yield return WaitForTask(task);
    }

    // =====================================================
    // UNLOCK SYSTEM
    // =====================================================

    /// <summary>
    /// Refreshes Codex states after the current POI completion
    /// has been saved by PlayerProgressService.
    /// </summary>
    public void MarkCurrentPOICompleted()
    {
        if (SelectedCodexItem == null)
        {
            Debug.LogError(
                "[DataManager] Cannot refresh Codex progress. " +
                "SelectedCodexItem is null."
            );

            return;
        }

        if (!PlayerProgressService.IsPoiCompleted(
                SelectedCodexItem.poiId))
        {
            Debug.LogWarning(
                "[DataManager] The selected POI has not been saved " +
                "as completed yet: " +
                SelectedCodexItem.poiId
            );

            return;
        }

        SynchronizeCodexStatesWithPlayerProgress();
    }

    // =====================================================
    // RETRIEVE CODEX ITEMS BY ID
    // =====================================================

    /// <summary>
    /// Gets a codex item by POI id.
    /// </summary>
    /// <param name="poiId">
    /// POI id to search for.
    /// </param>
    /// <returns>
    /// Matching codex item, or null if no item was found.
    /// </returns>
    /// <remarks>
    /// The codex JSON contains the POI id directly, so the runtime can search
    /// the loaded codex model without relying on temporary dictionaries.
    /// </remarks>
    public CodexItemDefinition GetCodexItemByPoiId(string poiId)
    {
        if (CodexMenu == null || CodexMenu.items == null)
        {
            Debug.LogError("[DataManager] Cannot get Codex item. CodexMenu is null.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(poiId))
        {
            Debug.LogError("[DataManager] POI id is empty.");
            return null;
        }

        poiId = poiId.Trim();

        foreach (CodexItemDefinition item in CodexMenu.items)
        {
            if (item != null && item.poiId == poiId)
                return item;
        }

        Debug.LogError("[DataManager] No Codex item found for POI id: " + poiId);
        return null;
    }

    /// <summary>
    /// Gets the current codex state for a POI id.
    /// </summary>
    /// <param name="poiId">
    /// POI id to search for.
    /// </param>
    /// <returns>
    /// Current state of the codex item, or Locked if the item cannot be found.
    /// </returns>
    public CodexItemState GetCodexItemStateByPoiId(string poiId)
    {
        CodexItemDefinition item = GetCodexItemByPoiId(poiId);

        if (item == null)
            return CodexItemState.Locked;

        return item.state;
    }

    // =====================================================
    // LANGUAGE
    // =====================================================

    /// <summary>
    /// Gets the current language from <see cref="LanguageManager"/>.
    /// </summary>
    /// <returns>
    /// Current selected language, or ENG if the LanguageManager is missing.
    /// </returns>
    private LanguageList GetCurrentLanguage()
    {
        if (LanguageManager.Instance != null)
            return LanguageManager.Instance.CurrentLanguage;

        Debug.LogWarning("[DataManager] LanguageManager missing. Falling back to ENG.");
        return LanguageList.ENG;
    }

    // =====================================================
    // COMPATIBILITY HELPERS
    // =====================================================

    /// <summary>
    /// Resolves a POI id from either a direct POI id or an old directions parameter.
    /// </summary>
    /// <param name="input">
    /// Either a POI id, such as roman_empire, or a directions parameter,
    /// such as roman_empire_directions_ENG.
    /// </param>
    /// <returns>
    /// Resolved POI id, or the original trimmed input if no better match is found.
    /// </returns>
    private string ResolvePoiIdFromDirectionsInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        input = input.Trim();

        if (CodexMenu != null && CodexMenu.items != null)
        {
            foreach (CodexItemDefinition item in CodexMenu.items)
            {
                if (item == null)
                    continue;

                if (item.poiId == input)
                    return item.poiId;

                if (!string.IsNullOrWhiteSpace(item.target) &&
                    item.target == input)
                {
                    return item.poiId;
                }
            }
        }

        LanguageList language = GetCurrentLanguage();
        string suffix = "_directions_" + language;

        if (input.EndsWith(suffix, StringComparison.Ordinal))
            return input.Substring(0, input.Length - suffix.Length);

        if (input.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            string withoutExtension = input.Substring(0, input.Length - ".json".Length);

            if (withoutExtension.EndsWith(suffix, StringComparison.Ordinal))
                return withoutExtension.Substring(0, withoutExtension.Length - suffix.Length);

            return withoutExtension;
        }

        return input;
    }

    /// <summary>
    /// Waits for a task inside a coroutine.
    /// </summary>
    /// <param name="task">
    /// Task to wait for.
    /// </param>
    /// <returns>
    /// Coroutine-compatible wait operation.
    /// </returns>
    private IEnumerator WaitForTask(Task task)
    {
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
            Debug.LogException(task.Exception);
    }

    // =====================================================
    // OLD MOCK COMPATIBILITY METHODS
    // =====================================================

    /// <summary>
    /// Legacy method used to retrieve a level by id from old mock codex data.
    /// </summary>
    /// <param name="id">
    /// Level id.
    /// </param>
    /// <returns>
    /// Matching legacy level data, or null.
    /// </returns>
    public LevelData GetLevelById(string id)
    {
        if (CodexData == null || CodexData.levels == null)
            return null;

        foreach (LevelData level in CodexData.levels)
        {
            if (level.id == id)
                return level;
        }

        return null;
    }

    /// <summary>
    /// Legacy method used by the old mock flow to unlock the next level.
    /// </summary>
    /// <param name="currentLevelId">
    /// Current legacy level id.
    /// </param>
    public void UnlockNextLevel(string currentLevelId)
    {
        if (CodexData == null || CodexData.levels == null)
            return;

        for (int i = 0; i < CodexData.levels.Count; i++)
        {
            if (CodexData.levels[i].id == currentLevelId)
            {
                if (i + 1 < CodexData.levels.Count)
                {
                    CodexData.levels[i + 1].state = "unlocked";
                    Debug.Log("Next level unlocked: " + CodexData.levels[i + 1].title);
                }

                break;
            }
        }
    }
    
    /// <summary>
    /// Reconstructs all Codex item states using the player's
    /// persistent progress.
    /// </summary>
    public void SynchronizeCodexStatesWithPlayerProgress()
    {
        if (CodexMenu == null || CodexMenu.items == null)
        {
            Debug.LogError(
                "[DataManager] Cannot synchronize Codex states. " +
                "CodexMenu is null."
            );

            return;
        }

        bool directionsItemAssigned = false;

        foreach (CodexItemDefinition item in CodexMenu.items)
        {
            if (item == null)
                continue;

            if (string.IsNullOrWhiteSpace(item.poiId))
            {
                item.state = CodexItemState.Locked;

                Debug.LogWarning(
                    "[DataManager] Codex item has no valid POI ID: " +
                    item.levelTitle
                );

                continue;
            }

            if (PlayerProgressService.IsPoiCompleted(item.poiId))
            {
                item.state = CodexItemState.Unlocked;
                continue;
            }

            if (!directionsItemAssigned)
            {
                item.state = CodexItemState.Directions;
                directionsItemAssigned = true;
            }
            else
            {
                item.state = CodexItemState.Locked;
            }
        }

        Debug.Log(
            "[DataManager] Codex states synchronized with " +
            "persistent player progress."
        );
    }
}