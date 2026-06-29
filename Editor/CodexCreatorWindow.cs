using System.Collections.Generic;
using System.IO;
using System.Linq;
using Addressing;
using Editor.Export;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Editor window used to create and export the Codex menu JSON for a THEODEN project.
/// </summary>
/// <remarks>
/// This window lets the user select a THEODEN project folder, choose the codex language,
/// create menu entries from the registered POIs, and export the codex JSON through
/// <see cref="CodexExportService"/>.
/// 
/// The window only manages UI and user input. The actual JSON writing and Addressables
/// registration are handled by the export service.
/// </remarks>
public class CodexCreatorWindow : EditorWindow
{
    /// <summary>
    /// Editable codex menu data.
    /// </summary>
    [SerializeField]
    private CodexMenu menuData = new CodexMenu();

    private SerializedObject _serializedWindow;
    private SerializedProperty _itemsProperty;

    private ReorderableList _reorderableList;

    private DefaultAsset _selectedProjectFolder;
    private TheodenProjectContext _projectContext;

    private int _selectedLanguageIndex;

    /// <summary>
    /// Opens the Codex Creator window from the THEODEN menu.
    /// </summary>
    [MenuItem("THEODEN/4.Create Codex")]
    public static void ShowWindow()
    {
        GetWindow<CodexCreatorWindow>("Create Codex");
    }

    /// <summary>
    /// Initializes serialized data and the reorderable codex item list.
    /// </summary>
    private void OnEnable()
    {
        if (menuData == null)
            menuData = new CodexMenu();

        if (menuData.items == null)
            menuData.items = new List<CodexItem>();

        _serializedWindow = new SerializedObject(this);
        _itemsProperty = _serializedWindow.FindProperty("menuData.items");

        SetupReorderableList();
    }

    /// <summary>
    /// Draws the editor window UI.
    /// </summary>
    private void OnGUI()
    {
        if (_serializedWindow == null)
            return;

        _serializedWindow.Update();

        DrawProjectFolderSection();

        if (_projectContext == null || !_projectContext.IsValid)
        {
            _serializedWindow.ApplyModifiedProperties();
            return;
        }

        DrawLanguageSection();

        GUILayout.Space(10);

        DrawCodexInfoBox();

        GUILayout.Space(10);

        _reorderableList.DoLayoutList();

        GUILayout.Space(10);

        if (GUILayout.Button("Add Item From POI Registry"))
            AddItemFromRegistry();

        GUILayout.Space(10);

        if (GUILayout.Button("Refresh Direction Parameters"))
            UpdateDirectionParametersForCurrentLanguage();

        GUILayout.Space(10);

        if (GUILayout.Button("Save JSON"))
            SaveJson();

        _serializedWindow.ApplyModifiedProperties();
    }

    /// <summary>
    /// Draws the THEODEN project folder selection field.
    /// </summary>
    private void DrawProjectFolderSection()
    {
        EditorGUILayout.LabelField("Project", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        _selectedProjectFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Project Folder",
            _selectedProjectFolder,
            typeof(DefaultAsset),
            false
        );

        if (EditorGUI.EndChangeCheck())
            LoadProjectFromFolder(_selectedProjectFolder);

        if (_projectContext == null || !_projectContext.IsValid)
        {
            EditorGUILayout.HelpBox(
                "Select the root folder of a THEODEN project, for example Assets/RomanBorder.",
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

        GUILayout.Space(10);
    }

    /// <summary>
    /// Draws the language selector for the codex file.
    /// </summary>
    private void DrawLanguageSection()
    {
        EditorGUILayout.LabelField("Language", EditorStyles.boldLabel);

        if (_projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No languages found in the selected project context.",
                MessageType.Warning
            );

            return;
        }

        string[] languageOptions = _projectContext.availableLanguages
            .Select(language =>
            {
                string displayedName = string.IsNullOrWhiteSpace(language.displayedName)
                    ? language.language.ToString()
                    : language.displayedName;

                return $"{displayedName} ({language.language})";
            })
            .ToArray();

        _selectedLanguageIndex = Mathf.Clamp(
            _selectedLanguageIndex,
            0,
            languageOptions.Length - 1
        );

        EditorGUI.BeginChangeCheck();

        _selectedLanguageIndex = EditorGUILayout.Popup(
            "Codex Language",
            _selectedLanguageIndex,
            languageOptions
        );

        if (EditorGUI.EndChangeCheck())
        {
            menuData.language = GetSelectedLanguage();
            UpdateDirectionParametersForCurrentLanguage();
        }

        menuData.language = GetSelectedLanguage();

        GUILayout.Space(10);
    }

    /// <summary>
    /// Draws an information box explaining how codex items are expected to be configured.
    /// </summary>
    private void DrawCodexInfoBox()
    {
        EditorGUILayout.HelpBox(
            "For normal POIs use LoadScene.\n" +
            "For the last POI use FinishScene if completing that POI should end the game.\n" +
            "OpenPopUp is not implemented yet, so avoid using it for now.\n\n" +
            "The parameter is generated automatically from the selected POI and language, " +
            "for example: roman_empire_directions_ENG.",
            MessageType.Info
        );
    }

    /// <summary>
    /// Configures the reorderable list used to edit codex menu items.
    /// </summary>
    private void SetupReorderableList()
    {
        _reorderableList = new ReorderableList(
            _serializedWindow,
            _itemsProperty,
            true,
            true,
            true,
            true
        );

        _reorderableList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Codex Menu Items");
        };

        _reorderableList.elementHeightCallback = index =>
        {
            SerializedProperty element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty actionTypeProperty = element.FindPropertyRelative("actionType");

            MenuActionType actionType = (MenuActionType)actionTypeProperty.enumValueIndex;

            float baseHeight = EditorGUIUtility.singleLineHeight * 5 + 24;

            if (actionType == MenuActionType.OpenPopUp)
                baseHeight += EditorGUIUtility.singleLineHeight + 8;

            if (actionType == MenuActionType.FinishScene)
                baseHeight += EditorGUIUtility.singleLineHeight + 8;

            return baseHeight;
        };

        _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            DrawCodexItemElement(rect, index);
        };
    }

    /// <summary>
    /// Draws one codex item row inside the reorderable list.
    /// </summary>
    /// <param name="rect">The drawing rectangle of the list element.</param>
    /// <param name="index">The index of the codex item.</param>
    private void DrawCodexItemElement(Rect rect, int index)
    {
        SerializedProperty element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);

        SerializedProperty nameProperty = element.FindPropertyRelative("name");
        SerializedProperty actionTypeProperty = element.FindPropertyRelative("actionType");
        SerializedProperty parameterProperty = element.FindPropertyRelative("parameter");
        SerializedProperty poiIdProperty = element.FindPropertyRelative("poiId");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 4;

        rect.y += 4;

        Rect poiRect = new Rect(rect.x, rect.y, rect.width, lineHeight);
        Rect nameRect = new Rect(rect.x, rect.y + (lineHeight + spacing), rect.width, lineHeight);
        Rect actionRect = new Rect(rect.x, rect.y + (lineHeight + spacing) * 2, rect.width, lineHeight);
        Rect parameterRect = new Rect(rect.x, rect.y + (lineHeight + spacing) * 3, rect.width, lineHeight);
        Rect idRect = new Rect(rect.x, rect.y + (lineHeight + spacing) * 4, rect.width, lineHeight);

        DrawPOIPopup(poiRect, poiIdProperty, nameProperty, parameterProperty);

        EditorGUI.PropertyField(nameRect, nameProperty);
        EditorGUI.PropertyField(actionRect, actionTypeProperty);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.PropertyField(parameterRect, parameterProperty);
            EditorGUI.PropertyField(idRect, poiIdProperty);
        }

        DrawActionTypeWarningIfNeeded(
            rect,
            actionTypeProperty,
            lineHeight,
            spacing
        );
    }

    /// <summary>
    /// Draws a warning for action types that require special attention.
    /// </summary>
    private void DrawActionTypeWarningIfNeeded(
        Rect rect,
        SerializedProperty actionTypeProperty,
        float lineHeight,
        float spacing)
    {
        MenuActionType actionType = (MenuActionType)actionTypeProperty.enumValueIndex;

        if (actionType != MenuActionType.OpenPopUp &&
            actionType != MenuActionType.FinishScene)
        {
            return;
        }

        float warningY = rect.y + (lineHeight + spacing) * 5 + 4;
        Rect warningRect = new Rect(rect.x, warningY, rect.width, lineHeight * 1.3f);

        if (actionType == MenuActionType.OpenPopUp)
        {
            EditorGUI.HelpBox(
                warningRect,
                "OpenPopUp is not implemented yet.",
                MessageType.Warning
            );
        }

        if (actionType == MenuActionType.FinishScene)
        {
            EditorGUI.HelpBox(
                warningRect,
                "Use this for the final POI. Parameter still points to the directions JSON.",
                MessageType.Info
            );
        }
    }

    /// <summary>
    /// Draws a POI selector for a codex item and updates the item fields when the selected POI changes.
    /// </summary>
    private void DrawPOIPopup(
        Rect rect,
        SerializedProperty poiIdProperty,
        SerializedProperty nameProperty,
        SerializedProperty parameterProperty)
    {
        if (_projectContext == null || _projectContext.availablePois == null)
        {
            EditorGUI.LabelField(rect, "POI", "No project context found");
            return;
        }

        IReadOnlyList<POIRegistryEntry> pois = _projectContext.availablePois;

        if (pois == null || pois.Count == 0)
        {
            EditorGUI.LabelField(rect, "POI", "No POIs registered");
            return;
        }

        string[] options = BuildPOIOptions(pois);

        int currentIndex = FindPOIIndexById(pois, poiIdProperty.stringValue);

        if (currentIndex < 0)
            currentIndex = 0;

        EditorGUI.BeginChangeCheck();

        int newIndex = EditorGUI.Popup(rect, "POI", currentIndex, options);

        if (EditorGUI.EndChangeCheck())
        {
            POIRegistryEntry selectedPoi = pois[newIndex];

            poiIdProperty.stringValue = selectedPoi.PoiId;
            nameProperty.stringValue = selectedPoi.DisplayName;
            parameterProperty.stringValue = BuildDirectionsParameter(selectedPoi.PoiId);
        }
    }

    /// <summary>
    /// Builds user-facing POI dropdown labels.
    /// </summary>
    private string[] BuildPOIOptions(IReadOnlyList<POIRegistryEntry> pois)
    {
        string[] options = new string[pois.Count];

        for (int i = 0; i < pois.Count; i++)
        {
            POIRegistryEntry poi = pois[i];
            options[i] = $"{poi.DisplayName} ({poi.PoiId})";
        }

        return options;
    }

    /// <summary>
    /// Finds the index of a POI by id inside the available POI list.
    /// </summary>
    private int FindPOIIndexById(IReadOnlyList<POIRegistryEntry> pois, string poiId)
    {
        for (int i = 0; i < pois.Count; i++)
        {
            if (pois[i].PoiId == poiId)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Loads the THEODEN project context from the selected project folder.
    /// </summary>
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
            return;
        }

        if (_projectContext.availableLanguages != null &&
            _projectContext.availableLanguages.Count > 0)
        {
            _selectedLanguageIndex = 0;
            menuData.language = GetSelectedLanguage();
            UpdateDirectionParametersForCurrentLanguage();
        }
    }

    /// <summary>
    /// Adds a new codex item using the first POI in the selected project context.
    /// </summary>
    private void AddItemFromRegistry()
    {
        if (_projectContext == null ||
            _projectContext.availablePois == null ||
            _projectContext.availablePois.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No POIs",
                "Select a valid THEODEN project folder with at least one registered POI.",
                "OK"
            );

            return;
        }

        if (menuData.items == null)
            menuData.items = new List<CodexItem>();

        POIRegistryEntry firstPoi = _projectContext.availablePois[0];

        CodexItem item = new CodexItem
        {
            name = firstPoi.DisplayName,
            actionType = MenuActionType.LoadScene,
            parameter = BuildDirectionsParameter(firstPoi.PoiId),
            poiId = firstPoi.PoiId
        };

        menuData.items.Add(item);

        _serializedWindow.Update();
        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Updates all codex item parameters according to the currently selected language.
    /// </summary>
    private void UpdateDirectionParametersForCurrentLanguage()
    {
        if (menuData == null || menuData.items == null)
            return;

        foreach (CodexItem item in menuData.items)
        {
            if (!string.IsNullOrWhiteSpace(item.poiId))
                item.parameter = BuildDirectionsParameter(item.poiId);
        }

        Repaint();
    }

    /// <summary>
    /// Builds the directions parameter for a POI using the shared file naming convention.
    /// </summary>
    private string BuildDirectionsParameter(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId))
            return "";

        if (_projectContext == null ||
            _projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            return $"{poiId}_directions";
        }

        LanguageList language = GetSelectedLanguage();

        string fileName = TheodenFileNaming.GetDirectionsJsonFileName(
            poiId,
            language
        );

        return Path.GetFileNameWithoutExtension(fileName);
    }

    /// <summary>
    /// Validates the current codex data before export.
    /// </summary>
    private bool ValidateBeforeSave()
    {
        if (_projectContext == null || !_projectContext.IsValid)
        {
            EditorUtility.DisplayDialog(
                "Missing Project",
                "Please select a valid THEODEN project folder before saving.",
                "OK"
            );

            return false;
        }

        if (_projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Missing Language",
                "The selected project does not define any available language.",
                "OK"
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(_projectContext.codexFolderPath) ||
            !_projectContext.codexFolderPath.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog(
                "Missing Codex Folder",
                "The selected project context does not contain a valid Codex folder path.",
                "OK"
            );

            return false;
        }

        if (menuData.items == null || menuData.items.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Empty Codex",
                "Add at least one Codex item before saving.",
                "OK"
            );

            return false;
        }

        foreach (CodexItem item in menuData.items)
        {
            if (string.IsNullOrWhiteSpace(item.name))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Codex Item",
                    "One Codex item has an empty name.",
                    "OK"
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(item.poiId))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Codex Item",
                    $"The Codex item '{item.name}' has an empty POI id.",
                    "OK"
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(item.parameter))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Codex Item",
                    $"The Codex item '{item.name}' has an empty parameter.",
                    "OK"
                );

                return false;
            }

            if (item.actionType == MenuActionType.OpenPopUp)
            {
                bool continueAnyway = EditorUtility.DisplayDialog(
                    "OpenPopUp Not Implemented",
                    $"The item '{item.name}' uses OpenPopUp, but this action is not implemented yet.\n\nDo you want to save anyway?",
                    "Save Anyway",
                    "Cancel"
                );

                if (!continueAnyway)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Saves the codex JSON through <see cref="CodexExportService"/>.
    /// </summary>
    private void SaveJson()
    {
        _serializedWindow.ApplyModifiedProperties();

        if (!ValidateBeforeSave())
            return;

        LanguageList language = GetSelectedLanguage();

        menuData.language = language;
        UpdateDirectionParametersForCurrentLanguage();

        string fileName = TheodenFileNaming.GetCodexJsonFileName(language);

        if (!CodexExportService.ExportCodex(
                menuData,
                language,
                _projectContext.codexFolderPath,
                fileName,
                out string error))
        {
            Debug.LogError(error);

            EditorUtility.DisplayDialog(
                "Export Failed",
                error,
                "OK"
            );

            return;
        }

        EditorUtility.DisplayDialog(
            "Export Status",
            $"Codex exported successfully:\n{fileName}",
            "OK"
        );
    }

    /// <summary>
    /// Gets the currently selected language.
    /// </summary>
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

        return _projectContext.availableLanguages[_selectedLanguageIndex].language;
    }
}