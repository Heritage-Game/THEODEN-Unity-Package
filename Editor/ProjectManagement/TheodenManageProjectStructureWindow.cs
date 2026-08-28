using System;
using System.Collections.Generic;
using System.Linq;
using Theoden.Editor.ProjectManagement;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window used to manage the POIs and active languages of an existing
/// THEODEN project.
/// </summary>
public class TheodenManageProjectStructureWindow : EditorWindow
{
    private DefaultAsset _projectFolder;
    private TheodenProjectContext _projectContext;
    private Vector2 _scrollPosition;

    private readonly List<LanguageDraft> _languageDrafts = new();
    private readonly List<PoiDraft> _poiDrafts = new();
    private readonly List<InactivePoiDraft> _inactivePoiDrafts = new();

    private string _newPoiDisplayName = string.Empty;
    private string _inactivePoiScanError;
    private bool _languagesDirty;

    /// <summary>
    /// Opens the project-structure management window.
    /// </summary>
    [MenuItem("THEODEN/Manage Project Structure")]
    public static void ShowWindow()
    {
        var window =
            GetWindow<TheodenManageProjectStructureWindow>();

        window.titleContent =
            new GUIContent("Manage Project Structure");
        window.minSize = new Vector2(600, 620);
        window.Show();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawProjectSelection();

        if (_projectContext == null || !_projectContext.IsValid)
            return;

        _scrollPosition = EditorGUILayout.BeginScrollView(
            _scrollPosition
        );

        DrawProjectSummary();
        GUILayout.Space(10);
        DrawLanguagesSection();
        GUILayout.Space(14);
        DrawPoisSection();
        GUILayout.Space(14);
        DrawInactivePoisSection();
        GUILayout.Space(14);
        DrawAddPoiSection();

        EditorGUILayout.EndScrollView();
    }

    private static void DrawHeader()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(
            "Manage THEODEN Project Structure",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Use this window to add or remove active languages and " +
            "POIs after the initial project setup. Stable POI IDs cannot " +
            "be edited.",
            MessageType.Info
        );

        EditorGUILayout.Space(6);
    }

    private void DrawProjectSelection()
    {
        DefaultAsset newProjectFolder =
            (DefaultAsset)EditorGUILayout.ObjectField(
                "Project Folder",
                _projectFolder,
                typeof(DefaultAsset),
                false
            );

        if (newProjectFolder != _projectFolder)
        {
            if (!ConfirmDiscardDraftChanges())
                return;

            _projectFolder = newProjectFolder;
            LoadProjectContext();
        }

        if (_projectContext == null || !_projectContext.IsValid)
        {
            EditorGUILayout.HelpBox(
                "Select the root folder of an existing THEODEN project.",
                MessageType.Info
            );
        }
    }

    private void DrawProjectSummary()
    {
        EditorGUILayout.HelpBox(
            $"Project: {_projectContext.projectId}\n" +
            $"Root: {_projectContext.projectFolderPath}\n" +
            $"Active languages: {_languageDrafts.Count(item => item.IsActive)}\n" +
            $"Registered POIs: {_poiDrafts.Count}\n" +
            $"Inactive POI folders: {_inactivePoiDrafts.Count}",
            MessageType.None
        );
    }

    #region Languages

    private void DrawLanguagesSection()
    {
        EditorGUILayout.LabelField(
            "Languages",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Disabling a language removes it from the active project " +
            "configuration. Existing localized JSON and media files are " +
            "preserved for recovery.",
            MessageType.None
        );

        foreach (LanguageDraft draft in _languageDrafts)
        {
            EditorGUILayout.BeginHorizontal();

            bool newActiveValue = EditorGUILayout.Toggle(
                draft.IsActive,
                GUILayout.Width(20)
            );

            if (newActiveValue != draft.IsActive)
            {
                draft.IsActive = newActiveValue;
                _languagesDirty = true;
            }

            EditorGUILayout.LabelField(
                draft.Language.ToString(),
                GUILayout.Width(80)
            );

            using (new EditorGUI.DisabledScope(!draft.IsActive))
            {
                string newDisplayedName = EditorGUILayout.TextField(
                    draft.DisplayedName
                );

                if (!string.Equals(
                        newDisplayedName,
                        draft.DisplayedName,
                        StringComparison.Ordinal))
                {
                    draft.DisplayedName = newDisplayedName;
                    _languagesDirty = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(6);

        using (new EditorGUI.DisabledScope(!_languagesDirty))
        {
            if (GUILayout.Button("Apply Language Changes"))
                ApplyLanguageChanges();
        }
    }

    private void ApplyLanguageChanges()
    {
        List<LanguageDraft> activeDrafts = _languageDrafts
            .Where(draft => draft.IsActive)
            .ToList();

        if (activeDrafts.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Invalid Languages",
                "A THEODEN project must contain at least one active " +
                "language.",
                "OK"
            );

            return;
        }

        List<LanguageList> removedLanguages =
            GetProjectConfig().languages
                .Where(language =>
                    activeDrafts.All(
                        draft => draft.Language != language
                    ))
                .ToList();

        if (removedLanguages.Count > 0)
        {
            string removedNames = string.Join(
                ", ",
                removedLanguages
            );

            bool confirmed = EditorUtility.DisplayDialog(
                "Remove Active Languages",
                $"The following languages will be removed from the " +
                $"active project configuration:\n\n{removedNames}\n\n" +
                "Their existing local files will be preserved. Continue?",
                "Apply Changes",
                "Cancel"
            );

            if (!confirmed)
                return;
        }

        var updates = activeDrafts
            .Select(draft => new ProjectLanguageUpdate(
                draft.Language,
                draft.DisplayedName
            ))
            .ToList();

        if (!TheodenProjectStructureService.TryApplyLanguages(
                GetProjectConfig(),
                updates,
                out string error))
        {
            DisplayOperationError("Language Update Failed", error);
            return;
        }

        EditorUtility.DisplayDialog(
            "Languages Updated",
            "The active project languages were updated successfully.",
            "OK"
        );

        ReloadProjectContext();
        GUIUtility.ExitGUI();
    }

    #endregion

    #region POIs

    private void DrawPoisSection()
    {
        EditorGUILayout.LabelField(
            "Points of Interest",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Changing a display name does not change the stable POI ID, " +
            "folder paths, QR value, or Addressables addresses.",
            MessageType.None
        );

        foreach (PoiDraft draft in _poiDrafts)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("POI ID", draft.PoiId);
            EditorGUILayout.LabelField("Folder", draft.FolderPath);
            EditorGUILayout.LabelField(
                "Configured",
                draft.IsConfigured ? "Yes" : "No"
            );

            draft.DisplayName = EditorGUILayout.TextField(
                "Display Name",
                draft.DisplayName
            );

            EditorGUILayout.BeginHorizontal();

            bool nameChanged = !string.Equals(
                draft.DisplayName?.Trim(),
                draft.OriginalDisplayName,
                StringComparison.Ordinal
            );

            using (new EditorGUI.DisabledScope(!nameChanged))
            {
                if (GUILayout.Button("Update Display Name"))
                {
                    UpdatePoiDisplayName(draft);
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(_poiDrafts.Count <= 1))
            {
                if (GUILayout.Button("Remove From Active Project"))
                {
                    RemovePoi(draft);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }

    private void UpdatePoiDisplayName(PoiDraft draft)
    {
        if (!TheodenProjectStructureService.TryUpdatePoiDisplayName(
                GetProjectConfig(),
                draft.PoiId,
                draft.DisplayName,
                out string error))
        {
            DisplayOperationError("POI Update Failed", error);
            return;
        }

        EditorUtility.DisplayDialog(
            "POI Updated",
            $"The display name of POI '{draft.PoiId}' was updated.",
            "OK"
        );

        ReloadProjectContext();
    }

    private void RemovePoi(PoiDraft draft)
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Remove POI From Active Project",
            $"Remove '{draft.OriginalDisplayName}' " +
            $"({draft.PoiId}) from the active project?\n\n" +
            "The POI will also be removed from existing Codex JSON " +
            "files. Its local POI, Directions, media, and QR files will " +
            "be preserved for recovery.\n\nAddressables entries are not " +
            "deleted by this operation.",
            "Remove POI",
            "Cancel"
        );

        if (!confirmed)
            return;

        if (!TheodenProjectStructureService
                .TryRemovePoiFromActiveProject(
                    GetProjectConfig(),
                    draft.PoiId,
                    out string error))
        {
            DisplayOperationError("POI Removal Failed", error);
            return;
        }

        EditorUtility.DisplayDialog(
            "POI Removed",
            $"POI '{draft.PoiId}' was removed from the active project. " +
            "Its local files were preserved.",
            "OK"
        );

        ReloadProjectContext();
    }

    private void DrawAddPoiSection()
    {
        EditorGUILayout.LabelField(
            "Add Point of Interest",
            EditorStyles.boldLabel
        );

        _newPoiDisplayName = EditorGUILayout.TextField(
            "Display Name",
            _newPoiDisplayName
        );

        string generatedId =
            TheodenProjectStructureService.GeneratePoiId(
                _newPoiDisplayName
            );

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField(
                "Generated POI ID",
                generatedId
            );
        }

        bool canAdd =
            !string.IsNullOrWhiteSpace(generatedId) &&
            !_projectContext.poiRegistry.ContainsId(generatedId) &&
            _inactivePoiDrafts.All(
                draft => !string.Equals(
                    draft.PoiId,
                    generatedId,
                    StringComparison.Ordinal
                )
            );

        using (new EditorGUI.DisabledScope(!canAdd))
        {
            if (GUILayout.Button("Add POI"))
            {
                AddPoi();
                GUIUtility.ExitGUI();
            }
        }

        bool matchesInactivePoi = _inactivePoiDrafts.Any(draft =>
            string.Equals(
                draft.PoiId,
                generatedId,
                StringComparison.Ordinal
            )
        );

        if (matchesInactivePoi)
        {
            EditorGUILayout.HelpBox(
                $"A preserved folder already uses POI ID " +
                $"'{generatedId}'. Restore it from the Inactive POIs " +
                "Found section instead of creating a new POI.",
                MessageType.Warning
            );
        }
        else if (!string.IsNullOrWhiteSpace(generatedId) && !canAdd)
        {
            EditorGUILayout.HelpBox(
                $"POI ID '{generatedId}' already exists.",
                MessageType.Warning
            );
        }
    }

    private void AddPoi()
    {
        if (!TheodenProjectStructureService.TryAddPoi(
                GetProjectConfig(),
                _newPoiDisplayName,
                out string createdPoiId,
                out string error))
        {
            DisplayOperationError("POI Creation Failed", error);
            return;
        }

        _newPoiDisplayName = string.Empty;

        EditorUtility.DisplayDialog(
            "POI Added",
            $"POI '{createdPoiId}' was added successfully. Its Data " +
            "and Media folders are ready for content creation.",
            "OK"
        );

        ReloadProjectContext();
    }

    private void DrawInactivePoisSection()
    {
        EditorGUILayout.LabelField(
            "Inactive POIs Found",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "These folders are still present inside the project POIs " +
            "folder but are not registered as active POIs. Restoring one " +
            "preserves its stable ID and all existing files. The restored " +
            "POI starts as unconfigured and is not automatically added " +
            "back to Codex files.",
            MessageType.None
        );

        if (!string.IsNullOrWhiteSpace(_inactivePoiScanError))
        {
            EditorGUILayout.HelpBox(
                _inactivePoiScanError,
                MessageType.Error
            );

            return;
        }

        if (_inactivePoiDrafts.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No inactive POI folders were found.",
                MessageType.Info
            );

            return;
        }

        foreach (InactivePoiDraft draft in _inactivePoiDrafts)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("POI ID", draft.PoiId);
            EditorGUILayout.LabelField("Folder", draft.FolderPath);

            draft.DisplayName = EditorGUILayout.TextField(
                "Display Name",
                draft.DisplayName
            );

            using (new EditorGUI.DisabledScope(
                       string.IsNullOrWhiteSpace(draft.DisplayName)))
            {
                if (GUILayout.Button("Restore POI"))
                {
                    RestorePoi(draft);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void RestorePoi(InactivePoiDraft draft)
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Restore POI",
            $"Restore POI '{draft.PoiId}' to the active project?\n\n" +
            "Its existing JSON and media files will not be modified. " +
            "The POI will start as unconfigured and must be added back " +
            "to the desired Codex files manually.",
            "Restore POI",
            "Cancel"
        );

        if (!confirmed)
            return;

        if (!TheodenProjectStructureService.TryRestorePoi(
                GetProjectConfig(),
                draft.PoiId,
                draft.DisplayName,
                out string error))
        {
            DisplayOperationError("POI Restore Failed", error);
            return;
        }

        EditorUtility.DisplayDialog(
            "POI Restored",
            $"POI '{draft.PoiId}' was restored with its original ID " +
            "and folder. Existing files were preserved. Add it back " +
            "to the required Codex definitions when ready.",
            "OK"
        );

        ReloadProjectContext();
    }

    #endregion

    #region Project Context

    private void LoadProjectContext()
    {
        _projectContext = null;
        _languageDrafts.Clear();
        _poiDrafts.Clear();
        _inactivePoiDrafts.Clear();
        _inactivePoiScanError = null;
        _languagesDirty = false;

        if (_projectFolder == null)
            return;

        string folderPath = AssetDatabase.GetAssetPath(_projectFolder);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            DisplayOperationError(
                "Invalid Project Folder",
                "Select the root folder of an existing THEODEN project."
            );

            _projectFolder = null;
            return;
        }

        if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                folderPath,
                out _projectContext,
                out string error))
        {
            DisplayOperationError(
                "Invalid THEODEN Project",
                error
            );

            _projectContext = null;
            _projectFolder = null;
            return;
        }

        TheodenProjectConfig config = GetProjectConfig();

        if (config == null ||
            config.poiRegistry == null ||
            config.languageConfig == null)
        {
            DisplayOperationError(
                "Invalid THEODEN Project",
                "The project context does not contain all required " +
                "configuration assets. Check TheodenProjectConfig, " +
                "POIRegistry, and LanguageConfig."
            );

            _projectContext = null;
            return;
        }

        RebuildDrafts();
    }

    private void ReloadProjectContext()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        LoadProjectContext();
        Repaint();
    }

    private void RebuildDrafts()
    {
        _languageDrafts.Clear();
        _poiDrafts.Clear();
        _inactivePoiDrafts.Clear();
        _inactivePoiScanError = null;
        _languagesDirty = false;

        TheodenProjectConfig config = GetProjectConfig();
        IReadOnlyList<LanguageList> activeLanguages =
            config.languages ?? new List<LanguageList>();
        IReadOnlyList<LanguageEntry> configuredLanguages =
            config.languageConfig.languages ??
            new List<LanguageEntry>();

        foreach (LanguageList language in
                 Enum.GetValues(typeof(LanguageList)))
        {
            LanguageEntry existingEntry =
                configuredLanguages.FirstOrDefault(
                    entry =>
                        entry != null &&
                        entry.language.Equals(language)
                );

            string displayedName =
                existingEntry != null &&
                !string.IsNullOrWhiteSpace(
                    existingEntry.displayedName)
                    ? existingEntry.displayedName
                    : existingEntry != null &&
                      !string.IsNullOrWhiteSpace(
                          existingEntry.displayName)
                        ? existingEntry.displayName
                        : language.ToString();

            _languageDrafts.Add(new LanguageDraft
            {
                Language = language,
                IsActive = activeLanguages.Contains(language),
                DisplayedName = displayedName
            });
        }

        foreach (POIRegistryEntry poi in config.poiRegistry.Pois)
        {
            _poiDrafts.Add(new PoiDraft
            {
                PoiId = poi.PoiId,
                DisplayName = poi.DisplayName,
                OriginalDisplayName = poi.DisplayName,
                FolderPath = poi.FolderPath,
                IsConfigured = poi.IsConfigured
            });
        }

        if (!TheodenProjectStructureService
                .TryFindInactivePoiFolders(
                    config,
                    out List<InactivePoiFolder> inactiveFolders,
                    out _inactivePoiScanError))
        {
            Debug.LogError(_inactivePoiScanError);
            return;
        }

        foreach (InactivePoiFolder inactiveFolder in inactiveFolders)
        {
            string suggestedDisplayName =
                BuildSuggestedDisplayName(inactiveFolder.PoiId);

            _inactivePoiDrafts.Add(new InactivePoiDraft
            {
                PoiId = inactiveFolder.PoiId,
                FolderPath = inactiveFolder.FolderPath,
                DisplayName = suggestedDisplayName,
                OriginalDisplayName = suggestedDisplayName
            });
        }
    }

    private TheodenProjectConfig GetProjectConfig()
    {
        return _projectContext?.theodenProjectConfig;
    }

    private bool ConfirmDiscardDraftChanges()
    {
        bool poiNameChanged = _poiDrafts.Any(draft =>
            !string.Equals(
                draft.DisplayName?.Trim(),
                draft.OriginalDisplayName,
                StringComparison.Ordinal
            )
        );

        bool hasNewPoiDraft =
            !string.IsNullOrWhiteSpace(_newPoiDisplayName);

        bool inactivePoiNameChanged = _inactivePoiDrafts.Any(draft =>
            !string.Equals(
                draft.DisplayName?.Trim(),
                draft.OriginalDisplayName,
                StringComparison.Ordinal
            )
        );

        if (!_languagesDirty &&
            !poiNameChanged &&
            !hasNewPoiDraft &&
            !inactivePoiNameChanged)
        {
            return true;
        }

        return EditorUtility.DisplayDialog(
            "Unsaved Project Changes",
            "The window contains unapplied project changes. Do you " +
            "want to discard them?",
            "Discard",
            "Cancel"
        );
    }

    private static void DisplayOperationError(
        string title,
        string error)
    {
        Debug.LogError(error);
        EditorUtility.DisplayDialog(title, error, "OK");
    }

    private static string BuildSuggestedDisplayName(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId))
            return string.Empty;

        string[] words = poiId.Split(
            new[] { '_', '-' },
            StringSplitOptions.RemoveEmptyEntries
        );

        for (int index = 0; index < words.Length; index++)
        {
            string word = words[index];

            words[index] = word.Length == 1
                ? char.ToUpperInvariant(word[0]).ToString()
                : char.ToUpperInvariant(word[0]) + word.Substring(1);
        }

        return words.Length > 0
            ? string.Join(" ", words)
            : poiId;
    }

    #endregion

    private sealed class LanguageDraft
    {
        public LanguageList Language;
        public bool IsActive;
        public string DisplayedName;
    }

    private sealed class PoiDraft
    {
        public string PoiId;
        public string DisplayName;
        public string OriginalDisplayName;
        public string FolderPath;
        public bool IsConfigured;
    }

    private sealed class InactivePoiDraft
    {
        public string PoiId;
        public string FolderPath;
        public string DisplayName;
        public string OriginalDisplayName;
    }
}
