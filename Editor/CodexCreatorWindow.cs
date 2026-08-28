using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Addressing;
using Theoden.Editor.Export;
using Theoden.Editor.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Editor window used to create new localized Codex definitions and edit
/// Codex JSON files that were previously exported by THEODEN.
/// </summary>
/// <remarks>
/// The window manages UI and editing state. JSON writing and Addressables
/// registration remain the responsibility of <see cref="CodexExportService"/>.
/// </remarks>
public class CodexCreatorWindow : EditorWindow
{
    /// <summary>
    /// Represents the operation currently performed by the window.
    /// </summary>
    private enum CodexEditorMode
    {
        None,
        Create,
        Edit
    }

    [SerializeField]
    private CodexMenu menuData = new CodexMenu();

    private Vector2 _scrollPosition;
    private SerializedObject _serializedWindow;
    private SerializedProperty _itemsProperty;
    private ReorderableList _reorderableList;

    private DefaultAsset _selectedProjectFolder;
    private TheodenProjectContext _projectContext;
    private int _selectedLanguageIndex;

    private CodexEditorMode _mode;
    private string _loadedJsonAssetPath;
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Opens the Codex definition editor.
    /// </summary>
    [MenuItem("THEODEN/4.Create or Edit Codex")]
    public static void ShowWindow()
    {
        GetWindow<CodexCreatorWindow>("Codex");
    }

    private void OnEnable()
    {
        menuData ??= new CodexMenu();
        menuData.items ??= new List<CodexItem>();

        InitializeSerializedState();
    }

    private void OnGUI()
    {
        if (_serializedWindow == null)
            InitializeSerializedState();

        _serializedWindow.Update();

        DrawProjectFolderSection();

        if (_projectContext == null || !_projectContext.IsValid)
        {
            _serializedWindow.ApplyModifiedProperties();
            return;
        }

        DrawLanguageSection();

        if (!HasValidDefinitionSelection())
        {
            if (_projectContext.availablePois == null ||
                _projectContext.availablePois.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "The selected project does not contain any " +
                    "registered POIs.",
                    MessageType.Warning
                );
            }

            _serializedWindow.ApplyModifiedProperties();
            return;
        }

        GUILayout.Space(8);
        DrawDefinitionStatusAndModeActions();

        bool contentChanged = false;

        if (_mode != CodexEditorMode.None)
        {
            GUILayout.Space(8);

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition
            );

            DrawCodexInfoBox();
            GUILayout.Space(8);

            EditorGUI.BeginChangeCheck();
            _reorderableList.DoLayoutList();
            contentChanged = EditorGUI.EndChangeCheck();

            EditorGUILayout.EndScrollView();
        }

        bool serializedDataChanged =
            _serializedWindow.ApplyModifiedProperties();

        if (_mode != CodexEditorMode.None &&
            (contentChanged || serializedDataChanged))
        {
            _hasUnsavedChanges = true;
        }

        if (_mode != CodexEditorMode.None)
        {
            GUILayout.Space(8);
            DrawCodexEditingActions();
        }
    }

    #region Serialized Data and Reorderable List

    /// <summary>
    /// Rebuilds the SerializedObject and reorderable list after the complete
    /// Codex model has been replaced by new or loaded data.
    /// </summary>
    private void InitializeSerializedState()
    {
        _serializedWindow = new SerializedObject(this);
        _itemsProperty =
            _serializedWindow.FindProperty("menuData.items");

        SetupReorderableList();
        _serializedWindow.Update();
    }

    private void SetupReorderableList()
    {
        _reorderableList = new ReorderableList(
            _serializedWindow,
            _itemsProperty,
            true,
            true,
            false,
            true
        );

        _reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Codex Menu Items");
        };

        _reorderableList.elementHeightCallback = index =>
        {
            SerializedProperty element =
                _reorderableList.serializedProperty
                    .GetArrayElementAtIndex(index);

            SerializedProperty actionTypeProperty =
                element.FindPropertyRelative("actionType");

            var actionType =
                (MenuActionType)actionTypeProperty.enumValueIndex;

            float baseHeight =
                EditorGUIUtility.singleLineHeight * 5 + 24;

            if (actionType == MenuActionType.OpenPopUp ||
                actionType == MenuActionType.FinishScene)
            {
                baseHeight +=
                    EditorGUIUtility.singleLineHeight + 8;
            }

            return baseHeight;
        };

        _reorderableList.drawElementCallback =
            (rect, index, isActive, isFocused) =>
            {
                DrawCodexItemElement(rect, index);
            };

        _reorderableList.onChangedCallback = _ =>
        {
            if (_mode != CodexEditorMode.None)
                _hasUnsavedChanges = true;
        };
    }

    private void DrawCodexItemElement(Rect rect, int index)
    {
        SerializedProperty element =
            _reorderableList.serializedProperty
                .GetArrayElementAtIndex(index);

        SerializedProperty nameProperty =
            element.FindPropertyRelative("name");
        SerializedProperty actionTypeProperty =
            element.FindPropertyRelative("actionType");
        SerializedProperty parameterProperty =
            element.FindPropertyRelative("parameter");
        SerializedProperty poiIdProperty =
            element.FindPropertyRelative("poiId");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        const float spacing = 4;

        rect.y += 4;

        var poiRect = new Rect(
            rect.x,
            rect.y,
            rect.width,
            lineHeight
        );

        var nameRect = new Rect(
            rect.x,
            rect.y + lineHeight + spacing,
            rect.width,
            lineHeight
        );

        var actionRect = new Rect(
            rect.x,
            rect.y + (lineHeight + spacing) * 2,
            rect.width,
            lineHeight
        );

        var parameterRect = new Rect(
            rect.x,
            rect.y + (lineHeight + spacing) * 3,
            rect.width,
            lineHeight
        );

        var idRect = new Rect(
            rect.x,
            rect.y + (lineHeight + spacing) * 4,
            rect.width,
            lineHeight
        );

        DrawPoiPopup(
            poiRect,
            poiIdProperty,
            nameProperty,
            parameterProperty
        );

        EditorGUI.PropertyField(nameRect, nameProperty);
        EditorGUI.PropertyField(actionRect, actionTypeProperty);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.PropertyField(
                parameterRect,
                parameterProperty
            );

            EditorGUI.PropertyField(idRect, poiIdProperty);
        }

        DrawActionTypeWarningIfNeeded(
            rect,
            actionTypeProperty,
            lineHeight,
            spacing
        );
    }

    private static void DrawActionTypeWarningIfNeeded(
        Rect rect,
        SerializedProperty actionTypeProperty,
        float lineHeight,
        float spacing)
    {
        var actionType =
            (MenuActionType)actionTypeProperty.enumValueIndex;

        if (actionType != MenuActionType.OpenPopUp &&
            actionType != MenuActionType.FinishScene)
        {
            return;
        }

        float warningY =
            rect.y + (lineHeight + spacing) * 5 + 4;

        var warningRect = new Rect(
            rect.x,
            warningY,
            rect.width,
            lineHeight * 1.3f
        );

        if (actionType == MenuActionType.OpenPopUp)
        {
            EditorGUI.HelpBox(
                warningRect,
                "OpenPopUp is not implemented yet.",
                MessageType.Warning
            );
        }
        else
        {
            EditorGUI.HelpBox(
                warningRect,
                "Use this for the final POI. Parameter still points " +
                "to the Directions JSON.",
                MessageType.Info
            );
        }
    }

    /// <summary>
    /// Draws the POI associated with an item. A POI ID that no longer exists
    /// in the registry is shown explicitly instead of being visually replaced
    /// by the first available POI.
    /// </summary>
    private void DrawPoiPopup(
        Rect rect,
        SerializedProperty poiIdProperty,
        SerializedProperty nameProperty,
        SerializedProperty parameterProperty)
    {
        IReadOnlyList<POIRegistryEntry> pois =
            _projectContext?.availablePois;

        if (pois == null || pois.Count == 0)
        {
            EditorGUI.LabelField(rect, "POI", "No POIs registered");
            return;
        }

        int registryIndex = FindPoiIndexById(
            pois,
            poiIdProperty.stringValue
        );

        bool needsPlaceholder = registryIndex < 0;

        string[] registryOptions = BuildPoiOptions(pois);
        string[] displayedOptions;
        int displayedIndex;

        if (needsPlaceholder)
        {
            displayedOptions = new string[registryOptions.Length + 1];
            displayedOptions[0] = string.IsNullOrWhiteSpace(
                poiIdProperty.stringValue
            )
                ? "Select a POI"
                : $"Missing POI ({poiIdProperty.stringValue})";

            Array.Copy(
                registryOptions,
                0,
                displayedOptions,
                1,
                registryOptions.Length
            );

            displayedIndex = 0;
        }
        else
        {
            displayedOptions = registryOptions;
            displayedIndex = registryIndex >= 0 ? registryIndex : 0;
        }

        EditorGUI.BeginChangeCheck();

        int newDisplayedIndex = EditorGUI.Popup(
            rect,
            "POI",
            displayedIndex,
            displayedOptions
        );

        if (!EditorGUI.EndChangeCheck())
            return;

        int newRegistryIndex = needsPlaceholder
            ? newDisplayedIndex - 1
            : newDisplayedIndex;

        if (newRegistryIndex < 0 ||
            newRegistryIndex >= pois.Count)
        {
            return;
        }

        POIRegistryEntry selectedPoi = pois[newRegistryIndex];

        poiIdProperty.stringValue = selectedPoi.PoiId;
        nameProperty.stringValue = selectedPoi.DisplayName;
        parameterProperty.stringValue =
            BuildDirectionsParameter(selectedPoi.PoiId);
    }

    private static string[] BuildPoiOptions(
        IReadOnlyList<POIRegistryEntry> pois)
    {
        var options = new string[pois.Count];

        for (int i = 0; i < pois.Count; i++)
        {
            POIRegistryEntry poi = pois[i];
            options[i] = $"{poi.DisplayName} ({poi.PoiId})";
        }

        return options;
    }

    private static int FindPoiIndexById(
        IReadOnlyList<POIRegistryEntry> pois,
        string poiId)
    {
        for (int i = 0; i < pois.Count; i++)
        {
            if (string.Equals(
                    pois[i].PoiId,
                    poiId,
                    StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region Project and Language Selection

    private void DrawProjectFolderSection()
    {
        EditorGUILayout.LabelField(
            "Project",
            EditorStyles.boldLabel
        );

        DefaultAsset newProjectFolder =
            (DefaultAsset)EditorGUILayout.ObjectField(
                "Project Folder",
                _selectedProjectFolder,
                typeof(DefaultAsset),
                false
            );

        if (newProjectFolder != _selectedProjectFolder)
        {
            if (ConfirmDiscardUnsavedChanges())
            {
                EndEditingSession();
                _selectedProjectFolder = newProjectFolder;
                LoadProjectFromFolder(newProjectFolder);
            }
        }

        if (_projectContext == null || !_projectContext.IsValid)
        {
            EditorGUILayout.HelpBox(
                "Select the root folder of a THEODEN project, for " +
                "example Assets/RomanBorder.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Loaded project: {_projectContext.projectFolderPath}\n" +
                $"Codex folder: {_projectContext.codexFolderPath}",
                MessageType.None
            );
        }

        GUILayout.Space(8);
    }

    private void DrawLanguageSection()
    {
        EditorGUILayout.LabelField(
            "Language",
            EditorStyles.boldLabel
        );

        if (_projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No languages were found in the selected project " +
                "context.",
                MessageType.Warning
            );

            return;
        }

        string[] languageOptions =
            _projectContext.availableLanguages
                .Select(language =>
                {
                    string displayedName =
                        string.IsNullOrWhiteSpace(language.displayedName)
                            ? language.language.ToString()
                            : language.displayedName;

                    return
                        $"{displayedName} ({language.language})";
                })
                .ToArray();

        _selectedLanguageIndex = Mathf.Clamp(
            _selectedLanguageIndex,
            0,
            languageOptions.Length - 1
        );

        int newLanguageIndex = EditorGUILayout.Popup(
            "Codex Language",
            _selectedLanguageIndex,
            languageOptions
        );

        if (newLanguageIndex != _selectedLanguageIndex &&
            ConfirmDiscardUnsavedChanges())
        {
            _selectedLanguageIndex = newLanguageIndex;
            EndEditingSession();
        }
    }

    private void LoadProjectFromFolder(DefaultAsset folderAsset)
    {
        _projectContext = null;
        _selectedLanguageIndex = 0;

        if (folderAsset == null)
            return;

        string folderPath = AssetDatabase.GetAssetPath(folderAsset);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog(
                "Invalid Selection",
                "Please select the root folder of a THEODEN project.",
                "OK"
            );

            _selectedProjectFolder = null;
            return;
        }

        if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                folderPath,
                out _projectContext,
                out string error))
        {
            EditorUtility.DisplayDialog(
                "Invalid THEODEN Project",
                error,
                "OK"
            );

            _projectContext = null;
            _selectedProjectFolder = null;
        }
    }

    private LanguageList GetSelectedLanguage()
    {
        if (_projectContext == null ||
            _projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            return default;
        }

        _selectedLanguageIndex = Mathf.Clamp(
            _selectedLanguageIndex,
            0,
            _projectContext.availableLanguages.Count - 1
        );

        return _projectContext
            .availableLanguages[_selectedLanguageIndex]
            .language;
    }

    #endregion

    #region Create and Edit Workflow

    private void DrawDefinitionStatusAndModeActions()
    {
        bool fileExists = SelectedDefinitionExists();
        string jsonAssetPath = GetSelectedJsonAssetPath();

        if (_mode == CodexEditorMode.None)
        {
            EditorGUILayout.HelpBox(
                fileExists
                    ? "Existing Codex definition found:\n" +
                      jsonAssetPath
                    : "No Codex definition exists for the selected " +
                      "language. A new one can be created at:\n" +
                      jsonAssetPath,
                MessageType.Info
            );

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(fileExists);
            if (GUILayout.Button("Create New"))
                StartCreateSession();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!fileExists);
            if (GUILayout.Button("Load Existing"))
                LoadSelectedDefinition();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            return;
        }

        string unsavedMessage = _hasUnsavedChanges
            ? "\nThere are unsaved changes."
            : string.Empty;

        if (_mode == CodexEditorMode.Create)
        {
            EditorGUILayout.HelpBox(
                fileExists
                    ? "The target Codex JSON was created externally. " +
                      "Close this definition and load the existing file."
                    : "Create mode. A new Codex definition will be " +
                      $"written to:\n{jsonAssetPath}" +
                      unsavedMessage,
                fileExists ? MessageType.Error : MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                fileExists
                    ? "Edit mode. The existing Codex definition was " +
                      $"loaded from:\n{jsonAssetPath}" +
                      unsavedMessage
                    : "The Codex JSON being edited is no longer " +
                      "available on disk.",
                fileExists ? MessageType.Info : MessageType.Error
            );
        }
    }

    /// <summary>
    /// Starts a creation session for the selected project and language.
    /// </summary>
    private void StartCreateSession()
    {
        if (!HasValidDefinitionSelection())
            return;

        if (SelectedDefinitionExists())
        {
            EditorUtility.DisplayDialog(
                "Definition Already Exists",
                "A Codex definition already exists for the selected " +
                "language. Load it to make changes.",
                "OK"
            );

            return;
        }

        var newMenu = new CodexMenu
        {
            language = GetSelectedLanguage(),
            items = new List<CodexItem>()
        };

        _mode = CodexEditorMode.Create;
        _loadedJsonAssetPath = null;
        _hasUnsavedChanges = false;

        ReplaceMenuData(newMenu);
    }

    /// <summary>
    /// Loads the Codex JSON selected by the current language.
    /// </summary>
    private void LoadSelectedDefinition()
    {
        if (!HasValidDefinitionSelection())
            return;

        string jsonAssetPath = GetSelectedJsonAssetPath();
        LanguageList language = GetSelectedLanguage();

        if (!CodexDefinitionLoadService.TryLoad(
                jsonAssetPath,
                language,
                out CodexMenu loadedMenu,
                out string error))
        {
            Debug.LogError(error);
            EditorUtility.DisplayDialog(
                "Load Failed",
                error,
                "OK"
            );

            return;
        }

        _mode = CodexEditorMode.Edit;
        _loadedJsonAssetPath = jsonAssetPath;
        _hasUnsavedChanges = false;

        ReplaceMenuData(loadedMenu);
    }

    /// <summary>
    /// Clears the open Codex while preserving the project and language
    /// selections.
    /// </summary>
    private void EndEditingSession()
    {
        _mode = CodexEditorMode.None;
        _loadedJsonAssetPath = null;
        _hasUnsavedChanges = false;

        ReplaceMenuData(new CodexMenu());
    }

    private void ReplaceMenuData(CodexMenu replacement)
    {
        menuData = replacement ?? new CodexMenu();
        menuData.items ??= new List<CodexItem>();

        InitializeSerializedState();
        Repaint();
    }

    /// <summary>
    /// Warns before a selection change discards unsaved Codex values.
    /// </summary>
    private bool ConfirmDiscardUnsavedChanges()
    {
        if (!_hasUnsavedChanges)
            return true;

        return EditorUtility.DisplayDialog(
            "Unsaved Changes",
            "The current Codex definition contains unsaved changes. " +
            "Do you want to discard them?",
            "Discard",
            "Cancel"
        );
    }

    #endregion

    #region Codex Editing

    private static void DrawCodexInfoBox()
    {
        EditorGUILayout.HelpBox(
            "For normal POIs use LoadScene.\n" +
            "For the last POI use FinishScene if completing that POI " +
            "should end the game.\n" +
            "OpenPopUp is not implemented yet, so avoid using it for " +
            "now.\n\n" +
            "The parameter is generated automatically from the " +
            "selected POI and language.",
            MessageType.Info
        );
    }

    private void DrawCodexEditingActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Item From POI Registry"))
            AddItemFromRegistry();

        if (GUILayout.Button("Refresh Direction Parameters"))
        {
            _serializedWindow.ApplyModifiedProperties();

            if (UpdateDirectionParametersForCurrentLanguage())
            {
                _hasUnsavedChanges = true;
                _serializedWindow.Update();
            }
        }

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(!CanSaveCurrentDefinition());

        string saveButtonText = _mode == CodexEditorMode.Create
            ? "Create JSON"
            : "Save Changes";

        if (GUILayout.Button(saveButtonText))
            SaveCurrentDefinition();

        EditorGUI.EndDisabledGroup();

        string closeButtonText = _hasUnsavedChanges
            ? "Discard Changes"
            : "Close Definition";

        if (GUILayout.Button(closeButtonText) &&
            ConfirmDiscardUnsavedChanges())
        {
            EndEditingSession();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Adds the first POI that is not already represented in the Codex.
    /// </summary>
    private void AddItemFromRegistry()
    {
        if (_projectContext?.availablePois == null ||
            _projectContext.availablePois.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No POIs",
                "The selected project does not contain registered POIs.",
                "OK"
            );

            return;
        }

        _serializedWindow.ApplyModifiedProperties();
        menuData.items ??= new List<CodexItem>();

        var usedPoiIds = new HashSet<string>(
            menuData.items
                .Where(item => item != null)
                .Select(item => item.poiId)
                .Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal
        );

        int availablePoiIndex = -1;

        for (int i = 0;
             i < _projectContext.availablePois.Count;
             i++)
        {
            if (usedPoiIds.Contains(
                    _projectContext.availablePois[i].PoiId))
            {
                continue;
            }

            availablePoiIndex = i;
            break;
        }

        if (availablePoiIndex < 0)
        {
            EditorUtility.DisplayDialog(
                "No POIs Available",
                "Every registered POI is already present in the Codex.",
                "OK"
            );

            return;
        }

        POIRegistryEntry availablePoi =
            _projectContext.availablePois[availablePoiIndex];

        menuData.items.Add(new CodexItem
        {
            name = availablePoi.DisplayName,
            actionType = MenuActionType.LoadScene,
            parameter = BuildDirectionsParameter(availablePoi.PoiId),
            poiId = availablePoi.PoiId
        });

        _hasUnsavedChanges = true;
        _serializedWindow.Update();
        EditorUtility.SetDirty(this);
        Repaint();
    }

    /// <summary>
    /// Rebuilds every generated Directions parameter for the selected language.
    /// </summary>
    /// <returns>True when at least one parameter changed.</returns>
    private bool UpdateDirectionParametersForCurrentLanguage()
    {
        if (menuData?.items == null)
            return false;

        bool changed = false;

        foreach (CodexItem item in menuData.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.poiId))
                continue;

            string expectedParameter =
                BuildDirectionsParameter(item.poiId);

            if (string.Equals(
                    item.parameter,
                    expectedParameter,
                    StringComparison.Ordinal))
            {
                continue;
            }

            item.parameter = expectedParameter;
            changed = true;
        }

        if (changed)
            Repaint();

        return changed;
    }

    private string BuildDirectionsParameter(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId))
            return string.Empty;

        string fileName =
            TheodenFileNaming.GetDirectionsJsonFileName(
                poiId,
                GetSelectedLanguage()
            );

        return Path.GetFileNameWithoutExtension(fileName);
    }

    #endregion

    #region Save and Validation

    /// <summary>
    /// Creates or updates the selected Codex JSON through the central export
    /// service.
    /// </summary>
    private void SaveCurrentDefinition()
    {
        if (!CanSaveCurrentDefinition())
        {
            EditorUtility.DisplayDialog(
                "Save Failed",
                "The selected Codex cannot be saved in its current state.",
                "OK"
            );

            return;
        }

        _serializedWindow.ApplyModifiedProperties();

        string selectedJsonAssetPath = GetSelectedJsonAssetPath();
        bool fileExists = SelectedDefinitionExists();

        if (_mode == CodexEditorMode.Create && fileExists)
        {
            EditorUtility.DisplayDialog(
                "Creation Conflict",
                "The target Codex JSON now exists. Close this " +
                "definition and load the existing file.",
                "OK"
            );

            return;
        }

        if (_mode == CodexEditorMode.Edit &&
            (!fileExists ||
             !string.Equals(
                 selectedJsonAssetPath,
                 _loadedJsonAssetPath,
                 StringComparison.Ordinal)))
        {
            EditorUtility.DisplayDialog(
                "Invalid Edit Target",
                "The original Codex JSON is no longer available at " +
                "the expected path. The file was not saved.",
                "OK"
            );

            return;
        }

        LanguageList language = GetSelectedLanguage();
        menuData.language = language;
        UpdateDirectionParametersForCurrentLanguage();
        _serializedWindow.Update();

        if (!ValidateBeforeSave())
            return;

        string projectId = _projectContext.projectId;
        string fileName =
            TheodenFileNaming.GetCodexJsonFileName(language);
        bool wasCreated = _mode == CodexEditorMode.Create;

        if (!CodexExportService.ExportCodex(
                menuData,
                projectId,
                language,
                _projectContext.codexFolderPath,
                fileName,
                out string error))
        {
            Debug.LogError(error);
            EditorUtility.DisplayDialog(
                "Save Failed",
                error,
                "OK"
            );

            return;
        }

        _mode = CodexEditorMode.Edit;
        _loadedJsonAssetPath = selectedJsonAssetPath;
        _hasUnsavedChanges = false;

        string operation = wasCreated ? "created" : "updated";

        Debug.Log(
            $"Codex definition '{selectedJsonAssetPath}' {operation}."
        );

        EditorUtility.DisplayDialog(
            "Success",
            $"The Codex definition for '{language}' was " +
            $"successfully {operation}.",
            "OK"
        );
    }

    private bool ValidateBeforeSave()
    {
        if (_projectContext == null || !_projectContext.IsValid)
        {
            DisplayValidationError(
                "Missing Project",
                "Select a valid THEODEN project before saving."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(_projectContext.projectId))
        {
            DisplayValidationError(
                "Missing Project ID",
                "The selected THEODEN project has no valid project ID."
            );

            return false;
        }

        if (!IsAssetPath(_projectContext.codexFolderPath))
        {
            DisplayValidationError(
                "Invalid Codex Folder",
                "The project does not contain a valid Codex folder path."
            );

            return false;
        }

        if (menuData.items == null || menuData.items.Count == 0)
        {
            DisplayValidationError(
                "Empty Codex",
                "Add at least one Codex item before saving."
            );

            return false;
        }

        var knownPoiIds = new HashSet<string>(
            _projectContext.availablePois.Select(poi => poi.PoiId),
            StringComparer.Ordinal
        );

        var usedPoiIds = new HashSet<string>(
            StringComparer.Ordinal
        );

        foreach (CodexItem item in menuData.items)
        {
            if (item == null)
            {
                DisplayValidationError(
                    "Invalid Codex Item",
                    "The Codex contains a null item."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(item.name))
            {
                DisplayValidationError(
                    "Invalid Codex Item",
                    "One Codex item has an empty name."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(item.poiId))
            {
                DisplayValidationError(
                    "Invalid Codex Item",
                    $"The item '{item.name}' has an empty POI ID."
                );

                return false;
            }

            if (!knownPoiIds.Contains(item.poiId))
            {
                DisplayValidationError(
                    "Unknown POI",
                    $"The item '{item.name}' references POI " +
                    $"'{item.poiId}', which is not present in the " +
                    "project registry."
                );

                return false;
            }

            if (!usedPoiIds.Add(item.poiId))
            {
                DisplayValidationError(
                    "Duplicate POI",
                    $"POI '{item.poiId}' appears more than once in " +
                    "the Codex."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(item.parameter))
            {
                DisplayValidationError(
                    "Invalid Codex Item",
                    $"The item '{item.name}' has an empty parameter."
                );

                return false;
            }

            if (item.actionType != MenuActionType.OpenPopUp)
                continue;

            bool continueAnyway = EditorUtility.DisplayDialog(
                "OpenPopUp Not Implemented",
                $"The item '{item.name}' uses OpenPopUp, but this " +
                "action is not implemented yet.\n\nDo you want to " +
                "save anyway?",
                "Save Anyway",
                "Cancel"
            );

            if (!continueAnyway)
                return false;
        }

        return true;
    }

    private static void DisplayValidationError(
        string title,
        string message)
    {
        EditorUtility.DisplayDialog(title, message, "OK");
    }

    private bool CanSaveCurrentDefinition()
    {
        if (_mode == CodexEditorMode.None ||
            !HasValidDefinitionSelection() ||
            string.IsNullOrWhiteSpace(_projectContext.projectId) ||
            !IsAssetPath(_projectContext.codexFolderPath))
        {
            return false;
        }

        bool fileExists = SelectedDefinitionExists();

        return _mode switch
        {
            CodexEditorMode.Create => !fileExists,
            CodexEditorMode.Edit =>
                fileExists &&
                string.Equals(
                    GetSelectedJsonAssetPath(),
                    _loadedJsonAssetPath,
                    StringComparison.Ordinal
                ),
            _ => false
        };
    }

    private bool HasValidDefinitionSelection()
    {
        return
            _projectContext != null &&
            _projectContext.IsValid &&
            _projectContext.availablePois != null &&
            _projectContext.availablePois.Count > 0 &&
            _projectContext.availableLanguages != null &&
            _selectedLanguageIndex >= 0 &&
            _selectedLanguageIndex <
                _projectContext.availableLanguages.Count &&
            IsAssetPath(_projectContext.codexFolderPath);
    }

    /// <summary>
    /// Builds the deterministic path for the Codex selected by project and
    /// language.
    /// </summary>
    private string GetSelectedJsonAssetPath()
    {
        if (!HasValidDefinitionSelection())
            return null;

        string folderPath =
            _projectContext.codexFolderPath
                .Replace("\\", "/")
                .TrimEnd('/');

        string fileName =
            TheodenFileNaming.GetCodexJsonFileName(
                GetSelectedLanguage()
            );

        return $"{folderPath}/{fileName}";
    }

    private bool SelectedDefinitionExists()
    {
        string jsonAssetPath = GetSelectedJsonAssetPath();

        return
            !string.IsNullOrWhiteSpace(jsonAssetPath) &&
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                jsonAssetPath
            ) != null;
    }

    private static bool IsAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string normalizedPath = path.Replace("\\", "/");

        return normalizedPath.Equals(
                   "Assets",
                   StringComparison.Ordinal) ||
               normalizedPath.StartsWith(
                   "Assets/",
                   StringComparison.Ordinal
               );
    }

    #endregion
}