using System;
using System.Collections.Generic;
using System.Linq;
using Addressing;
using RuntimeModelsForEditor;
using Theoden.Editor.Export;
using Theoden.Editor.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Editor window used to create new localized Directions definitions and edit
/// Directions JSON files that were previously exported by THEODEN.
/// </summary>
public class DirectionsToPOICreatorWindow : EditorWindow
{
    /// <summary>
    /// Represents the operation currently performed by the window.
    /// </summary>
    private enum DirectionsEditorMode
    {
        None,
        Create,
        Edit
    }

    [SerializeField]
    private DirectionsToPOIData data = new DirectionsToPOIData();

    private SerializedObject _serializedObject;
    private SerializedProperty _descriptionProperty;
    private SerializedProperty _imageListProperty;
    private SerializedProperty _audioDescriptionProperty;
    private ReorderableList _imageList;

    private DefaultAsset _projectFolder;
    private TheodenProjectContext _projectContext;
    private int _selectedPoiIndex;
    private int _selectedLanguageIndex;

    private DirectionsEditorMode _mode;
    private string _loadedJsonAssetPath;
    private bool _hasUnsavedChanges;

    /// <summary>
    /// Opens the Directions definition editor.
    /// </summary>
    [MenuItem("THEODEN/3.Create or Edit Directions To POI")]
    public static void ShowWindow()
    {
        GetWindow<DirectionsToPOICreatorWindow>(
            "Directions To POI"
        );
    }

    private void OnEnable()
    {
        data ??= new DirectionsToPOIData();
        data.images ??= new List<Sprite>();
        data.description ??= string.Empty;

        InitializeSerializedState();
    }

    private void OnGUI()
    {
        if (_serializedObject == null)
            InitializeSerializedState();

        _serializedObject.Update();

        GUILayout.Label(
            "Directions To POI",
            EditorStyles.boldLabel
        );
        EditorGUILayout.Space();

        DrawProjectFolderField();
        EditorGUILayout.Space();

        DrawProjectContextFields();

        if (HasValidDefinitionSelection())
        {
            EditorGUILayout.Space();
            DrawSelectedPoiInfo();
            EditorGUILayout.Space();
            DrawDefinitionStatusAndModeActions();
        }

        bool contentChanged = false;

        if (_mode != DirectionsEditorMode.None)
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawDescriptionField();
            EditorGUILayout.Space();
            DrawImageList();
            EditorGUILayout.Space();
            DrawAudioDescriptionField();
            contentChanged = EditorGUI.EndChangeCheck();
        }

        bool serializedDataChanged =
            _serializedObject.ApplyModifiedProperties();

        if (_mode != DirectionsEditorMode.None &&
            (contentChanged || serializedDataChanged))
        {
            _hasUnsavedChanges = true;
        }

        if (_mode != DirectionsEditorMode.None)
        {
            EditorGUILayout.Space();
            DrawEditingActions();
        }
    }

    #region Serialized Data and Reorderable List

    /// <summary>
    /// Rebuilds the SerializedObject and cached properties after the entire
    /// Directions data instance has been replaced by a new or loaded model.
    /// </summary>
    private void InitializeSerializedState()
    {
        _serializedObject = new SerializedObject(this);
        _descriptionProperty =
            _serializedObject.FindProperty("data.description");
        _imageListProperty =
            _serializedObject.FindProperty("data.images");
        _audioDescriptionProperty =
            _serializedObject.FindProperty("data.audioDescription");

        BuildImageList();
        _serializedObject.Update();
    }

    private void BuildImageList()
    {
        _imageList = new ReorderableList(
            _serializedObject,
            _imageListProperty,
            true,
            true,
            true,
            true
        );

        _imageList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Images");
        };

        _imageList.elementHeight = 70;

        _imageList.drawElementCallback =
            (rect, index, isActive, isFocused) =>
            {
                DrawImageListElement(rect, index);
            };

        _imageList.onChangedCallback = _ =>
        {
            if (_mode != DirectionsEditorMode.None)
                _hasUnsavedChanges = true;
        };
    }

    private void DrawImageListElement(Rect rect, int index)
    {
        SerializedProperty element =
            _imageListProperty.GetArrayElementAtIndex(index);

        rect.y += 5;
        const float previewSize = 60;

        Sprite sprite = element.objectReferenceValue as Sprite;

        if (sprite != null && sprite.texture != null)
        {
            DrawSpritePreview(
                new Rect(
                    rect.x,
                    rect.y,
                    previewSize,
                    previewSize
                ),
                sprite
            );
        }

        EditorGUI.ObjectField(
            new Rect(
                rect.x + previewSize + 10,
                rect.y + 20,
                rect.width - previewSize - 10,
                EditorGUIUtility.singleLineHeight
            ),
            element,
            GUIContent.none
        );
    }

    private static void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;

        Rect uv = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height
        );

        GUI.DrawTextureWithTexCoords(
            rect,
            texture,
            uv,
            true
        );
    }

    #endregion

    #region Project and Definition Selection

    private void DrawProjectFolderField()
    {
        DefaultAsset newProjectFolder =
            (DefaultAsset)EditorGUILayout.ObjectField(
                "Project Folder",
                _projectFolder,
                typeof(DefaultAsset),
                false
            );

        if (newProjectFolder == _projectFolder)
            return;

        if (!ConfirmDiscardUnsavedChanges())
            return;

        EndEditingSession();
        _projectFolder = newProjectFolder;
        LoadProjectContext();
    }

    private void LoadProjectContext()
    {
        _projectContext = null;
        _selectedPoiIndex = 0;
        _selectedLanguageIndex = 0;

        if (_projectFolder == null)
            return;

        string projectFolderPath =
            AssetDatabase.GetAssetPath(_projectFolder);

        if (!AssetDatabase.IsValidFolder(projectFolderPath))
        {
            Debug.LogError(
                $"Selected asset is not a valid folder: " +
                projectFolderPath
            );

            _projectFolder = null;
            return;
        }

        if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                projectFolderPath,
                out _projectContext,
                out string error))
        {
            Debug.LogError(error);
            EditorUtility.DisplayDialog(
                "Invalid THEODEN Project",
                error,
                "OK"
            );

            _projectContext = null;
            _projectFolder = null;
        }
    }

    private void DrawProjectContextFields()
    {
        if (_projectContext == null || !_projectContext.IsValid)
        {
            EditorGUILayout.HelpBox(
                "Select a valid THEODEN project folder to choose " +
                "a POI and language.",
                MessageType.Info
            );

            return;
        }

        DrawPoiDropdown();
        DrawLanguageDropdown();
    }

    private void DrawPoiDropdown()
    {
        if (_projectContext.availablePois == null ||
            _projectContext.availablePois.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No POIs were found in the selected project " +
                "configuration.",
                MessageType.Warning
            );

            return;
        }

        string[] poiOptions = _projectContext.availablePois
            .Select(poi => $"{poi.DisplayName} ({poi.PoiId})")
            .ToArray();

        int newPoiIndex = EditorGUILayout.Popup(
            "Point of Interest",
            _selectedPoiIndex,
            poiOptions
        );

        if (newPoiIndex == _selectedPoiIndex)
            return;

        if (!ConfirmDiscardUnsavedChanges())
            return;

        _selectedPoiIndex = newPoiIndex;
        EndEditingSession();
    }

    private void DrawLanguageDropdown()
    {
        if (_projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No languages were found in the selected project " +
                "configuration.",
                MessageType.Warning
            );

            return;
        }

        string[] languageOptions =
            _projectContext.availableLanguages
                .Select(language => language.displayedName)
                .ToArray();

        int newLanguageIndex = EditorGUILayout.Popup(
            "Language",
            _selectedLanguageIndex,
            languageOptions
        );

        if (newLanguageIndex == _selectedLanguageIndex)
            return;

        if (!ConfirmDiscardUnsavedChanges())
            return;

        _selectedLanguageIndex = newLanguageIndex;
        EndEditingSession();
    }

    private void DrawSelectedPoiInfo()
    {
        var selectedPoi =
            _projectContext.availablePois[_selectedPoiIndex];

        EditorGUILayout.HelpBox(
            $"Selected POI:\n" +
            $"Name: {selectedPoi.DisplayName}\n" +
            $"ID: {selectedPoi.PoiId}",
            MessageType.None
        );
    }

    #endregion

    #region Create and Edit Workflow

    private void DrawDefinitionStatusAndModeActions()
    {
        bool fileExists = SelectedDefinitionExists();
        string jsonAssetPath = GetSelectedJsonAssetPath();

        if (_mode == DirectionsEditorMode.None)
        {
            EditorGUILayout.HelpBox(
                fileExists
                    ? "Existing Directions definition found:\n" +
                      jsonAssetPath
                    : "No Directions definition exists for the " +
                      "selected POI and language. A new one can be " +
                      "created at:\n" + jsonAssetPath,
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

        if (_mode == DirectionsEditorMode.Create)
        {
            EditorGUILayout.HelpBox(
                fileExists
                    ? "The target JSON was created externally. Close " +
                      "this definition and load the existing file."
                    : "Create mode. A new Directions definition will " +
                      $"be written to:\n{jsonAssetPath}" +
                      unsavedMessage,
                fileExists ? MessageType.Error : MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                fileExists
                    ? "Edit mode. The existing Directions definition " +
                      $"was loaded from:\n{jsonAssetPath}" +
                      unsavedMessage
                    : "The Directions JSON being edited is no longer " +
                      "available on disk.",
                fileExists ? MessageType.Info : MessageType.Error
            );
        }
    }

    /// <summary>
    /// Starts a creation session for the selected POI and language.
    /// Existing JSON files cannot be overwritten from this mode.
    /// </summary>
    private void StartCreateSession()
    {
        if (!HasValidDefinitionSelection())
            return;

        if (SelectedDefinitionExists())
        {
            EditorUtility.DisplayDialog(
                "Definition Already Exists",
                "A Directions definition already exists for the " +
                "selected POI and language. Load it to make changes.",
                "OK"
            );

            return;
        }

        _mode = DirectionsEditorMode.Create;
        _loadedJsonAssetPath = null;
        _hasUnsavedChanges = false;

        ReplaceData(new DirectionsToPOIData());
    }

    /// <summary>
    /// Loads the selected Directions JSON and restores all editor-side asset
    /// references from their Addressables addresses.
    /// </summary>
    private void LoadSelectedDefinition()
    {
        if (!HasValidDefinitionSelection())
            return;

        string jsonAssetPath = GetSelectedJsonAssetPath();
        var selectedPoi =
            _projectContext.availablePois[_selectedPoiIndex];

        if (!DirectionsDefinitionLoadService.TryLoad(
                jsonAssetPath,
                selectedPoi.PoiId,
                out DirectionsToPOIData loadedData,
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

        _mode = DirectionsEditorMode.Edit;
        _loadedJsonAssetPath = jsonAssetPath;
        _hasUnsavedChanges = false;

        ReplaceData(loadedData);
    }

    private void DrawEditingActions()
    {
        bool canSave = CanSaveCurrentDefinition();

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(!canSave);

        string saveButtonText =
            _mode == DirectionsEditorMode.Create
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
    /// Clears the open definition while preserving project, POI, and language
    /// selections.
    /// </summary>
    private void EndEditingSession()
    {
        _mode = DirectionsEditorMode.None;
        _loadedJsonAssetPath = null;
        _hasUnsavedChanges = false;

        ReplaceData(new DirectionsToPOIData());
    }

    private void ReplaceData(DirectionsToPOIData replacement)
    {
        data = replacement ?? new DirectionsToPOIData();
        data.images ??= new List<Sprite>();
        data.description ??= string.Empty;

        InitializeSerializedState();
        Repaint();
    }

    /// <summary>
    /// Warns the user before an operation discards edited Directions values.
    /// </summary>
    private bool ConfirmDiscardUnsavedChanges()
    {
        if (!_hasUnsavedChanges)
            return true;

        return EditorUtility.DisplayDialog(
            "Unsaved Changes",
            "The current Directions definition contains unsaved " +
            "changes. Do you want to discard them?",
            "Discard",
            "Cancel"
        );
    }

    #endregion

    #region Directions Form

    private void DrawDescriptionField()
    {
        GUILayout.Label("Description", EditorStyles.boldLabel);

        _descriptionProperty.stringValue = EditorGUILayout.TextArea(
            _descriptionProperty.stringValue,
            GUILayout.Height(150)
        );
    }

    private void DrawImageList()
    {
        GUILayout.Label("Optional Images", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Images are optional. Selected sprites must be inside the " +
            "project Media folder.",
            MessageType.Info
        );

        _imageList.DoLayoutList();
    }

    private void DrawAudioDescriptionField()
    {
        GUILayout.Label(
            "Optional Audio Description",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "The audio description is optional. The selected audio " +
            "clip must be inside the project Media folder.",
            MessageType.Info
        );

        EditorGUILayout.PropertyField(
            _audioDescriptionProperty,
            new GUIContent("Audio Description")
        );
    }

    #endregion

    #region Save and Validation

    /// <summary>
    /// Creates or updates the selected Directions JSON through the central
    /// Directions export service.
    /// </summary>
    private void SaveCurrentDefinition()
    {
        if (!CanSaveCurrentDefinition())
        {
            EditorUtility.DisplayDialog(
                "Save Failed",
                "Cannot save Directions. Check the project, POI, " +
                "language, Directions folder, Media folder, and target " +
                "JSON file.",
                "OK"
            );

            return;
        }

        _serializedObject.ApplyModifiedProperties();

        string selectedJsonAssetPath = GetSelectedJsonAssetPath();
        bool fileExists = SelectedDefinitionExists();

        if (_mode == DirectionsEditorMode.Create && fileExists)
        {
            EditorUtility.DisplayDialog(
                "Creation Conflict",
                "The target JSON now exists. Close this definition and " +
                "load the existing file before making changes.",
                "OK"
            );

            return;
        }

        if (_mode == DirectionsEditorMode.Edit &&
            (!fileExists ||
             !string.Equals(
                 selectedJsonAssetPath,
                 _loadedJsonAssetPath,
                 StringComparison.Ordinal)))
        {
            EditorUtility.DisplayDialog(
                "Invalid Edit Target",
                "The original Directions JSON is no longer available " +
                "at the expected path. The file was not saved.",
                "OK"
            );

            return;
        }

        var selectedPoi =
            _projectContext.availablePois[_selectedPoiIndex];
        var selectedLanguage =
            _projectContext.availableLanguages[_selectedLanguageIndex];

        string poiId = selectedPoi.PoiId;
        string poiName = selectedPoi.DisplayName;
        LanguageList language = selectedLanguage.language;
        string projectId = _projectContext.projectId;

        data.poiId = poiId;
        data.poiName = poiName;

        _serializedObject.Update();

        string fileName =
            TheodenFileNaming.GetDirectionsJsonFileName(
                poiId,
                language
            );

        bool wasCreated = _mode == DirectionsEditorMode.Create;

        if (!DirectionsExportService.ExportDirections(
                data,
                projectId,
                poiId,
                language,
                _projectContext.directionsFolderPath,
                _projectContext.mediaFolderPath,
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

        _mode = DirectionsEditorMode.Edit;
        _loadedJsonAssetPath = selectedJsonAssetPath;
        _hasUnsavedChanges = false;

        string operation = wasCreated ? "created" : "updated";

        Debug.Log(
            $"Directions definition '{selectedJsonAssetPath}' " +
            $"{operation}."
        );

        EditorUtility.DisplayDialog(
            "Success",
            $"The Directions definition for '{poiName}' was " +
            $"successfully {operation}.",
            "OK"
        );
    }

    private bool CanSaveCurrentDefinition()
    {
        if (_mode == DirectionsEditorMode.None ||
            !HasValidDefinitionSelection())
        {
            return false;
        }

        if (!IsAssetPath(_projectContext.directionsFolderPath) ||
            !IsAssetPath(_projectContext.mediaFolderPath) ||
            string.IsNullOrWhiteSpace(_projectContext.projectId))
        {
            return false;
        }

        bool fileExists = SelectedDefinitionExists();

        return _mode switch
        {
            DirectionsEditorMode.Create => !fileExists,
            DirectionsEditorMode.Edit =>
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
            _selectedPoiIndex >= 0 &&
            _selectedPoiIndex <
                _projectContext.availablePois.Count &&
            _projectContext.availableLanguages != null &&
            _selectedLanguageIndex >= 0 &&
            _selectedLanguageIndex <
                _projectContext.availableLanguages.Count;
    }

    /// <summary>
    /// Builds the deterministic AssetDatabase path for the currently selected
    /// POI and language.
    /// </summary>
    private string GetSelectedJsonAssetPath()
    {
        if (!HasValidDefinitionSelection())
            return null;

        var selectedPoi =
            _projectContext.availablePois[_selectedPoiIndex];
        var selectedLanguage =
            _projectContext.availableLanguages[_selectedLanguageIndex];

        string folderPath =
            _projectContext.directionsFolderPath
                .Replace("\\", "/")
                .TrimEnd('/');

        string fileName =
            TheodenFileNaming.GetDirectionsJsonFileName(
                selectedPoi.PoiId,
                selectedLanguage.language
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