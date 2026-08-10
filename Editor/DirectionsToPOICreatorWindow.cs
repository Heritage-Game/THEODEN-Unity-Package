using System.Linq;
using Addressing;
using Theoden.Editor.Export;
using RuntimeModelsForEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class DirectionsToPOICreatorWindow : EditorWindow
{
    [SerializeField]
    private DirectionsToPOIData data = new DirectionsToPOIData();

    private SerializedObject _serializedObject;
    private SerializedProperty _imageListProperty;
    private SerializedProperty _audioDescriptionProperty;

    private ReorderableList _imageList;

    private DefaultAsset _projectFolder;
    private TheodenProjectContext _projectContext;

    private int _selectedPoiIndex;
    private int _selectedLanguageIndex;

    [MenuItem("THEODEN/3.Create Directions To POI")]
    public static void ShowWindow()
    {
        GetWindow<DirectionsToPOICreatorWindow>("Create Directions To POI");
    }

    private void OnEnable()
    {
        if (data == null)
            data = new DirectionsToPOIData();

        _serializedObject = new SerializedObject(this);

        _imageListProperty = _serializedObject.FindProperty("data.images");
        _audioDescriptionProperty = _serializedObject.FindProperty("data.audioDescription");

        BuildImageList();
    }

    private void OnGUI()
    {
        if (_serializedObject == null)
            return;

        _serializedObject.Update();

        GUILayout.Label("Directions To POI", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawProjectFolderField();
        EditorGUILayout.Space();

        DrawProjectContextFields();
        EditorGUILayout.Space();

        DrawSelectedPoiInfo();
        EditorGUILayout.Space();

        DrawDescriptionField();
        EditorGUILayout.Space();

        DrawImageList();
        EditorGUILayout.Space();

        DrawAudioDescriptionField();
        EditorGUILayout.Space();

        DrawExportButton();

        _serializedObject.ApplyModifiedProperties();
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

        _imageList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            DrawImageListElement(rect, index);
        };
    }

    private void DrawImageListElement(Rect rect, int index)
    {
        SerializedProperty element = _imageListProperty.GetArrayElementAtIndex(index);

        rect.y += 5;
        float previewSize = 60;

        Sprite sprite = element.objectReferenceValue as Sprite;

        if (sprite != null && sprite.texture != null)
        {
            DrawSpritePreview(
                new Rect(rect.x, rect.y, previewSize, previewSize),
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

    private void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;

        Rect uv = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height
        );

        GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
    }

    private void DrawProjectFolderField()
    {
        EditorGUI.BeginChangeCheck();

        _projectFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Project Folder",
            _projectFolder,
            typeof(DefaultAsset),
            false
        );

        if (EditorGUI.EndChangeCheck())
            LoadProjectContext();
    }

    private void LoadProjectContext()
    {
        _projectContext = null;
        _selectedPoiIndex = 0;
        _selectedLanguageIndex = 0;

        if (_projectFolder == null)
            return;

        string projectFolderPath = AssetDatabase.GetAssetPath(_projectFolder);

        if (!AssetDatabase.IsValidFolder(projectFolderPath))
        {
            Debug.LogError($"Selected asset is not a valid folder: {projectFolderPath}");
            _projectFolder = null;
            return;
        }

        if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                projectFolderPath,
                out _projectContext,
                out string error))
        {
            Debug.LogError(error);
            _projectContext = null;
        }
    }

    private void DrawProjectContextFields()
    {
        if (_projectContext == null || !_projectContext.IsValid)
        {
            EditorGUILayout.HelpBox(
                "Select a valid THEODEN project folder to choose POI and language.",
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
                "No POIs found in the selected project configuration.",
                MessageType.Warning
            );
            return;
        }

        string[] poiOptions = _projectContext.availablePois
            .Select(poi => $"{poi.DisplayName} ({poi.PoiId})")
            .ToArray();

        _selectedPoiIndex = EditorGUILayout.Popup(
            "Point of Interest",
            _selectedPoiIndex,
            poiOptions
        );
    }

    private void DrawLanguageDropdown()
    {
        if (_projectContext.availableLanguages == null ||
            _projectContext.availableLanguages.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No languages found in the selected project configuration.",
                MessageType.Warning
            );
            return;
        }

        string[] languageOptions = _projectContext.availableLanguages
            .Select(language => language.displayedName)
            .ToArray();

        _selectedLanguageIndex = EditorGUILayout.Popup(
            "Language",
            _selectedLanguageIndex,
            languageOptions
        );
    }

    private void DrawSelectedPoiInfo()
    {
        if (_projectContext == null ||
            _projectContext.availablePois == null ||
            _selectedPoiIndex < 0 ||
            _selectedPoiIndex >= _projectContext.availablePois.Count)
        {
            return;
        }

        var selectedPoi = _projectContext.availablePois[_selectedPoiIndex];

        EditorGUILayout.HelpBox(
            $"Selected POI:\nName: {selectedPoi.DisplayName}\nID: {selectedPoi.PoiId}",
            MessageType.None
        );
    }

    private void DrawDescriptionField()
    {
        GUILayout.Label("Description", EditorStyles.boldLabel);

        data.description = EditorGUILayout.TextArea(
            data.description,
            GUILayout.Height(150)
        );
    }

    private void DrawImageList()
    {
        GUILayout.Label("Optional Images", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Images are optional. Selected sprites must be inside the project Media folder.",
            MessageType.Info
        );

        _imageList.DoLayoutList();
    }

    private void DrawAudioDescriptionField()
    {
        GUILayout.Label("Optional Audio Description", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Audio description is optional. The selected audio clip must be inside the project Media folder.",
            MessageType.Info
        );

        EditorGUILayout.PropertyField(
            _audioDescriptionProperty,
            new GUIContent("Audio Description")
        );
    }

    private void DrawExportButton()
    {
        EditorGUI.BeginDisabledGroup(!CanExport());

        if (GUILayout.Button("Export JSON"))
            ExportJson();

        EditorGUI.EndDisabledGroup();
    }

    private bool CanExport()
    {
        if (_projectContext == null || !_projectContext.IsValid)
            return false;

        if (_projectContext.availablePois == null ||
            _selectedPoiIndex < 0 ||
            _selectedPoiIndex >= _projectContext.availablePois.Count)
            return false;

        if (_projectContext.availableLanguages == null ||
            _selectedLanguageIndex < 0 ||
            _selectedLanguageIndex >= _projectContext.availableLanguages.Count)
            return false;

        if (string.IsNullOrWhiteSpace(_projectContext.directionsFolderPath) ||
            !_projectContext.directionsFolderPath.StartsWith("Assets"))
            return false;

        if (string.IsNullOrWhiteSpace(_projectContext.mediaFolderPath) ||
            !_projectContext.mediaFolderPath.StartsWith("Assets"))
            return false;

        return true;
    }

    private void ExportJson()
    {
        _serializedObject.ApplyModifiedProperties();

        if (!CanExport())
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "Cannot export directions. Check project folder, POI, language, Directions folder and Media folder.",
                "OK"
            );
            return;
        }

        var selectedPoi = _projectContext.availablePois[_selectedPoiIndex];
        var selectedLanguageData = _projectContext.availableLanguages[_selectedLanguageIndex];

        string poiId = selectedPoi.PoiId;
        string poiName = selectedPoi.DisplayName;
        LanguageList language = selectedLanguageData.language;
        string projectId =
            _projectContext.projectId;
        
        if (string.IsNullOrWhiteSpace(projectId))
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "The selected THEODEN project has no valid project id.",
                "OK"
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(poiId))
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "Selected POI has no valid ID.",
                "OK"
            );
            return;
        }

        data.poiId = poiId;
        data.poiName = poiName;

        string fileName = TheodenFileNaming.GetDirectionsJsonFileName(
            poiId,
            language
        );

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
                "Export Failed",
                error,
                "OK"
            );

            return;
        }

        Debug.Log($"Directions exported successfully for POI '{poiId}'.");

        EditorUtility.DisplayDialog(
            "Success",
            $"Directions JSON exported successfully for POI '{poiName}'.",
            "OK"
        );
    }
}