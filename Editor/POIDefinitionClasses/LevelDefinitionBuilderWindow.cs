using System;
using System.Collections.Generic;
using System.Linq;
using Addressing;
using Theoden.Editor.Export;
using Theoden.Editor.Import;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Theoden.Editor.POIDefinitionClasses
{
    /// <summary>
    /// Editor window used to create new localized POI definitions and edit
    /// definitions that were previously exported as JSON files.
    /// </summary>
    public class LevelDefinitionBuilderWindow : EditorWindow
    {
        private const string ToolVersion = "0.1.0";

        /// <summary>
        /// Represents the operation currently being performed by the window.
        /// </summary>
        private enum DefinitionEditorMode
        {
            None,
            Create,
            Edit
        }

        // UI roots
        private VisualElement _contentRoot;
        private VisualElement _staticRoot;
        private VisualElement _templateRoot;
        private VisualElement _footerRoot;

        // UI controls
        private ObjectField _projectFolderField;
        private PopupField<string> _templateDropdown;
        private PopupField<string> _poiDropdown;
        private PopupField<string> _languageDropdown;
        private HelpBox _definitionStatusBox;
        private Button _createNewButton;
        private Button _loadExistingButton;
        private Button _saveButton;
        private Button _discardButton;

        // Template data displayed through SerializedObject and PropertyField
        private LevelDefinitionTemplateSO _currentSo;
        private SerializedObject _serializedSo;
        private SerializedProperty _templateProperty;
        private Type[] _templateTypes;
        private Type _currentTemplateType;

        // Project selection
        private DefaultAsset _projectFolder;
        private TheodenProjectContext _projectContext;
        private int _selectedPoiIndex;
        private int _selectedLanguageIndex;

        // Editing session
        private DefinitionEditorMode _mode;
        private string _loadedJsonAssetPath;
        private bool _hasUnsavedChanges;
        private bool _suppressDirtyTracking;

        /// <summary>
        /// Opens the POI definition editor.
        /// </summary>
        [MenuItem("THEODEN/2.Create or Edit POI Definition")]
        public static void ShowWindow()
        {
            GetWindow<LevelDefinitionBuilderWindow>("POI Definition");
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1;

            _templateTypes =
                PoiTemplateTypeRegistry.GetRegisteredTemplateTypes();

            BuildLayout();

            if (_templateTypes.Length == 0)
            {
                _staticRoot.Add(new HelpBox(
                    "No registered POI templates were found. Add " +
                    "PoiChallengeTypeAttribute to at least one concrete " +
                    "POITemplate class.",
                    HelpBoxMessageType.Error
                ));

                return;
            }

            _currentTemplateType = _templateTypes[0];

            BuildStaticUI();
            BuildTemplateContainer();
            BuildFooterUI();
            RegisterCallbacks();
            UpdateEditorControls();
        }

        private void OnDisable()
        {
            ReleaseTemplateData();
        }

        #region UI Building

        private void BuildLayout()
        {
            var scroll = new ScrollView
            {
                style =
                {
                    flexGrow = 1
                }
            };

            _contentRoot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 8,
                    paddingBottom = 8
                }
            };

            _staticRoot = new VisualElement();
            _templateRoot = new VisualElement();
            _footerRoot = new VisualElement();

            _contentRoot.Add(_staticRoot);
            _contentRoot.Add(_templateRoot);
            _contentRoot.Add(_footerRoot);

            scroll.Add(_contentRoot);
            rootVisualElement.Add(scroll);
        }

        private void BuildStaticUI()
        {
            _projectFolderField = new ObjectField("Project Folder")
            {
                objectType = typeof(DefaultAsset),
                allowSceneObjects = false
            };

            var templateNames = _templateTypes
                .Select(type => type.Name)
                .ToList();

            _templateDropdown = new PopupField<string>(
                "Template",
                templateNames,
                templateNames[0]
            );

            _poiDropdown = new PopupField<string>(
                "Point of Interest",
                new List<string> { "Select a project folder first" },
                0
            );
            _poiDropdown.SetEnabled(false);

            _languageDropdown = new PopupField<string>(
                "Language",
                new List<string> { "Select a project folder first" },
                0
            );
            _languageDropdown.SetEnabled(false);

            _definitionStatusBox = new HelpBox(
                "Select a THEODEN project folder.",
                HelpBoxMessageType.Info
            );
            _definitionStatusBox.style.marginTop = 8;

            var definitionActions = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 6,
                    marginBottom = 8
                }
            };

            _createNewButton = new Button(OnCreateNewClicked)
            {
                text = "Create New"
            };
            _createNewButton.style.marginRight = 4;

            _loadExistingButton = new Button(OnLoadExistingClicked)
            {
                text = "Load Existing"
            };

            definitionActions.Add(_createNewButton);
            definitionActions.Add(_loadExistingButton);

            _staticRoot.Add(_projectFolderField);
            _staticRoot.Add(_templateDropdown);
            _staticRoot.Add(_poiDropdown);
            _staticRoot.Add(_languageDropdown);
            _staticRoot.Add(_definitionStatusBox);
            _staticRoot.Add(definitionActions);
        }

        private void BuildTemplateContainer()
        {
            _templateRoot.style.flexDirection = FlexDirection.Column;
            _templateRoot.style.marginTop = 6;
        }

        private void BuildFooterUI()
        {
            _footerRoot.style.flexDirection = FlexDirection.Row;
            _footerRoot.style.marginTop = 10;

            _saveButton = new Button(OnSaveClicked)
            {
                text = "Save"
            };
            _saveButton.style.marginRight = 4;

            _discardButton = new Button(OnDiscardClicked)
            {
                text = "Discard Changes"
            };

            _footerRoot.Add(_saveButton);
            _footerRoot.Add(_discardButton);
        }

        private void RegisterCallbacks()
        {
            _projectFolderField.RegisterValueChangedCallback(
                OnProjectFolderChanged
            );

            _templateDropdown.RegisterValueChangedCallback(
                OnTemplateSelectionChanged
            );

            _poiDropdown.RegisterValueChangedCallback(
                OnPoiSelectionChanged
            );

            _languageDropdown.RegisterValueChangedCallback(
                OnLanguageSelectionChanged
            );

            _templateRoot.RegisterCallback<SerializedPropertyChangeEvent>(
                OnTemplatePropertyChanged
            );
        }

        #endregion

        #region Selection Callbacks

        private void OnProjectFolderChanged(
            ChangeEvent<UnityEngine.Object> evt)
        {
            if (!ConfirmDiscardUnsavedChanges())
            {
                _projectFolderField.SetValueWithoutNotify(
                    evt.previousValue
                );
                return;
            }

            EndEditingSession();
            _projectFolder = evt.newValue as DefaultAsset;

            if (_projectFolder == null)
            {
                ClearProjectContextUI();
                return;
            }

            string path = AssetDatabase.GetAssetPath(_projectFolder);

            if (!AssetDatabase.IsValidFolder(path))
            {
                Debug.LogError(
                    $"Failed to load project folder: {path}"
                );
                ClearProjectContextUI();
                return;
            }

            if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                    path,
                    out _projectContext,
                    out string error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog(
                    "Invalid THEODEN Project",
                    error,
                    "OK"
                );
                ClearProjectContextUI();
                return;
            }

            RefreshProjectContextDropdowns();
        }

        private void OnTemplateSelectionChanged(
            ChangeEvent<string> evt)
        {
            Type newType = _templateTypes.FirstOrDefault(
                type => type.Name == evt.newValue
            );

            if (newType == null || newType == _currentTemplateType)
                return;

            if (_mode == DefinitionEditorMode.Edit)
            {
                _templateDropdown.SetValueWithoutNotify(
                    evt.previousValue
                );
                return;
            }

            if (_mode == DefinitionEditorMode.Create &&
                !ConfirmDiscardUnsavedChanges())
            {
                _templateDropdown.SetValueWithoutNotify(
                    evt.previousValue
                );
                return;
            }

            _currentTemplateType = newType;

            if (_mode == DefinitionEditorMode.Create)
            {
                RebuildTemplateData(newType);
                _hasUnsavedChanges = false;
                UpdateEditorControls();
            }
        }

        private void OnPoiSelectionChanged(ChangeEvent<string> evt)
        {
            int newIndex = _poiDropdown.choices.IndexOf(evt.newValue);

            if (newIndex < 0 || newIndex == _selectedPoiIndex)
                return;

            if (!ConfirmDiscardUnsavedChanges())
            {
                _poiDropdown.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            _selectedPoiIndex = newIndex;
            EndEditingSession();
            UpdateEditorControls();
        }

        private void OnLanguageSelectionChanged(ChangeEvent<string> evt)
        {
            int newIndex = _languageDropdown.choices.IndexOf(evt.newValue);

            if (newIndex < 0 || newIndex == _selectedLanguageIndex)
                return;

            if (!ConfirmDiscardUnsavedChanges())
            {
                _languageDropdown.SetValueWithoutNotify(
                    evt.previousValue
                );
                return;
            }

            _selectedLanguageIndex = newIndex;
            EndEditingSession();
            UpdateEditorControls();
        }

        private void OnTemplatePropertyChanged(
            SerializedPropertyChangeEvent evt)
        {
            if (_suppressDirtyTracking ||
                _mode == DefinitionEditorMode.None)
            {
                return;
            }

            _hasUnsavedChanges = true;
            UpdateEditorControls();
        }

        #endregion

        #region Project Context UI

        private void ClearProjectContextUI()
        {
            EndEditingSession();

            _projectContext = null;
            _projectFolder = null;
            _selectedPoiIndex = 0;
            _selectedLanguageIndex = 0;

            _projectFolderField?.SetValueWithoutNotify(null);

            if (_poiDropdown != null)
            {
                var choices = new List<string>
                {
                    "Select a project folder first"
                };

                _poiDropdown.choices = choices;
                _poiDropdown.SetValueWithoutNotify(choices[0]);
                _poiDropdown.SetEnabled(false);
            }

            if (_languageDropdown != null)
            {
                var choices = new List<string>
                {
                    "Select a project folder first"
                };

                _languageDropdown.choices = choices;
                _languageDropdown.SetValueWithoutNotify(choices[0]);
                _languageDropdown.SetEnabled(false);
            }

            UpdateEditorControls();
        }

        private void RefreshProjectContextDropdowns()
        {
            if (_projectContext == null || !_projectContext.IsValid)
            {
                ClearProjectContextUI();
                return;
            }

            RefreshPoiDropdown();
            RefreshLanguageDropdown();
            UpdateEditorControls();
        }

        private void RefreshPoiDropdown()
        {
            if (_projectContext.availablePois == null ||
                _projectContext.availablePois.Count == 0)
            {
                var choices = new List<string> { "No POIs found" };

                _poiDropdown.choices = choices;
                _poiDropdown.SetValueWithoutNotify(choices[0]);
                _poiDropdown.SetEnabled(false);
                return;
            }

            var poiOptions = _projectContext.availablePois
                .Select(poi => $"{poi.DisplayName} ({poi.PoiId})")
                .ToList();

            _selectedPoiIndex = 0;
            _poiDropdown.choices = poiOptions;
            _poiDropdown.SetValueWithoutNotify(poiOptions[0]);
            _poiDropdown.SetEnabled(true);
        }

        private void RefreshLanguageDropdown()
        {
            if (_projectContext.availableLanguages == null ||
                _projectContext.availableLanguages.Count == 0)
            {
                var choices = new List<string> { "No languages found" };

                _languageDropdown.choices = choices;
                _languageDropdown.SetValueWithoutNotify(choices[0]);
                _languageDropdown.SetEnabled(false);
                return;
            }

            var languageOptions = _projectContext.availableLanguages
                .Select(language => language.displayedName)
                .ToList();

            _selectedLanguageIndex = 0;
            _languageDropdown.choices = languageOptions;
            _languageDropdown.SetValueWithoutNotify(languageOptions[0]);
            _languageDropdown.SetEnabled(true);
        }

        #endregion

        #region Create and Edit Workflow

        /// <summary>
        /// Starts a creation session for the selected POI and language.
        /// Creation is blocked when the target JSON already exists.
        /// </summary>
        private void OnCreateNewClicked()
        {
            if (!HasValidDefinitionSelection())
                return;

            if (SelectedDefinitionExists())
            {
                EditorUtility.DisplayDialog(
                    "Definition Already Exists",
                    "A POI definition already exists for the selected " +
                    "POI and language. Load it to make changes.",
                    "OK"
                );
                return;
            }

            _mode = DefinitionEditorMode.Create;
            _loadedJsonAssetPath = null;
            _hasUnsavedChanges = false;

            RebuildTemplateData(_currentTemplateType);
            UpdateEditorControls();
        }

        private void OnLoadExistingClicked()
        {
            LoadSelectedDefinition();
        }

        /// <summary>
        /// Loads the JSON associated with the current POI and language,
        /// determines its concrete template type, and rebuilds the Inspector UI.
        /// </summary>
        private void LoadSelectedDefinition()
        {
            if (!HasValidDefinitionSelection())
                return;

            string jsonAssetPath = GetSelectedJsonAssetPath();
            var selectedPoi =
                _projectContext.availablePois[_selectedPoiIndex];
            var selectedLanguage =
                _projectContext.availableLanguages[_selectedLanguageIndex];

            if (!PoiDefinitionLoadService.TryLoad(
                    jsonAssetPath,
                    selectedPoi.PoiId,
                    selectedLanguage.language,
                    out LevelTemplateBase loadedTemplate,
                    out Type loadedTemplateType,
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

            if (!_templateTypes.Contains(loadedTemplateType))
            {
                EditorUtility.DisplayDialog(
                    "Unsupported Template",
                    $"The template '{loadedTemplateType.Name}' is not " +
                    "available in this window.",
                    "OK"
                );
                return;
            }

            _mode = DefinitionEditorMode.Edit;
            _loadedJsonAssetPath = jsonAssetPath;
            _currentTemplateType = loadedTemplateType;
            _hasUnsavedChanges = false;

            _templateDropdown.SetValueWithoutNotify(
                loadedTemplateType.Name
            );

            RebuildTemplateData(
                loadedTemplateType,
                loadedTemplate
            );

            UpdateEditorControls();
        }

        private void OnDiscardClicked()
        {
            if (!ConfirmDiscardUnsavedChanges())
                return;

            EndEditingSession();
            UpdateEditorControls();
        }

        /// <summary>
        /// Clears the transient template and returns the window to selection mode.
        /// Project, POI, language, and template selections are preserved.
        /// </summary>
        private void EndEditingSession()
        {
            _mode = DefinitionEditorMode.None;
            _loadedJsonAssetPath = null;
            _hasUnsavedChanges = false;

            _templateRoot?.Clear();
            ReleaseTemplateData();

            if (_templateDropdown != null)
                _templateDropdown.SetEnabled(true);
        }

        /// <summary>
        /// Warns the user before an operation discards edited template values.
        /// </summary>
        /// <returns>
        /// True when there are no unsaved changes or the user accepts the discard.
        /// </returns>
        private bool ConfirmDiscardUnsavedChanges()
        {
            if (!_hasUnsavedChanges)
                return true;

            return EditorUtility.DisplayDialog(
                "Unsaved Changes",
                "The current POI definition contains unsaved changes. " +
                "Do you want to discard them?",
                "Discard",
                "Cancel"
            );
        }

        #endregion

        #region Save

        /// <summary>
        /// Creates or updates the selected JSON through the central POI export
        /// service. In edit mode the original creation timestamp is retained
        /// because it was restored with the loaded template.
        /// </summary>
        private void OnSaveClicked()
        {
            if (_mode == DefinitionEditorMode.None ||
                _currentSo == null ||
                _templateProperty == null)
            {
                Debug.LogError("No POI definition is currently open.");
                return;
            }

            if (!HasValidDefinitionSelection())
            {
                Debug.LogError("No valid POI definition is selected.");
                return;
            }

            string selectedJsonAssetPath = GetSelectedJsonAssetPath();
            bool fileExists = SelectedDefinitionExists();

            if (_mode == DefinitionEditorMode.Create && fileExists)
            {
                EditorUtility.DisplayDialog(
                    "Creation Conflict",
                    "The target JSON now exists. Close this definition and " +
                    "load the existing file before making changes.",
                    "OK"
                );
                UpdateEditorControls();
                return;
            }

            if (_mode == DefinitionEditorMode.Edit &&
                (!fileExists ||
                 !string.Equals(
                     selectedJsonAssetPath,
                     _loadedJsonAssetPath,
                     StringComparison.Ordinal)))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Edit Target",
                    "The original JSON is no longer available at the " +
                    "expected path. The file was not saved.",
                    "OK"
                );
                UpdateEditorControls();
                return;
            }

            _serializedSo.ApplyModifiedProperties();

            var template =
                _templateProperty.managedReferenceValue
                    as LevelTemplateBase;

            if (template == null)
            {
                Debug.LogError("Template data is missing.");
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
            string projectRootFolderPath =
                _projectContext.projectFolderPath;
            string jsonExportFolderPath =
                TheodenProjectPaths.GetPoiDataFolder(
                    _projectContext.poisFolderPath,
                    poiId
                );

            if (string.IsNullOrWhiteSpace(projectId))
            {
                DisplaySaveError(
                    "The selected THEODEN project has no valid project ID."
                );
                return;
            }

            if (!IsAssetPath(projectRootFolderPath))
            {
                DisplaySaveError(
                    "Invalid THEODEN project root folder: " +
                    projectRootFolderPath
                );
                return;
            }

            if (!IsAssetPath(jsonExportFolderPath))
            {
                DisplaySaveError(
                    "Invalid POI export folder: " +
                    jsonExportFolderPath
                );
                return;
            }

            bool wasCreated = _mode == DefinitionEditorMode.Create;

            template.InjectForExport(
                poiId,
                poiName,
                language,
                ToolVersion
            );

            // Refresh the SerializedObject snapshot after metadata was
            // updated directly on the managed-reference instance.
            _serializedSo.Update();

            string fileName =
                TheodenFileNaming.GetPoiJsonFileName(
                    poiId,
                    language
                );

            if (!PoiExportService.ExportPoi(
                    template,
                    projectId,
                    poiId,
                    language,
                    projectRootFolderPath,
                    jsonExportFolderPath,
                    fileName,
                    out string error))
            {
                DisplaySaveError(error);
                return;
            }

            _mode = DefinitionEditorMode.Edit;
            _loadedJsonAssetPath = selectedJsonAssetPath;
            _hasUnsavedChanges = false;

            UpdateEditorControls();

            string operation = wasCreated ? "created" : "updated";

            Debug.Log(
                $"POI definition '{selectedJsonAssetPath}' {operation}."
            );

            EditorUtility.DisplayDialog(
                "Success",
                $"The POI definition for '{poiName}' was " +
                $"successfully {operation}.",
                "OK"
            );
        }

        private static void DisplaySaveError(string error)
        {
            Debug.LogError(error);
            EditorUtility.DisplayDialog(
                "Save Failed",
                error,
                "OK"
            );
        }

        #endregion

        #region Template Data

        /// <summary>
        /// Creates the temporary ScriptableObject used by UI Toolkit and
        /// assigns either a new concrete template or a deserialized one.
        /// </summary>
        /// <param name="templateType">Concrete POI template type.</param>
        /// <param name="templateData">
        /// Previously loaded data, or null to instantiate a new template.
        /// </param>
        private void RebuildTemplateData(
            Type templateType,
            LevelTemplateBase templateData = null)
        {
            _suppressDirtyTracking = true;

            try
            {
                _templateRoot.Clear();
                ReleaseTemplateData();

                _currentSo =
                    ScriptableObject.CreateInstance<
                        LevelDefinitionTemplateSO>();

                _serializedSo = new SerializedObject(_currentSo);
                _templateProperty = _serializedSo.FindProperty(
                    nameof(LevelDefinitionTemplateSO.template)
                );

                _templateProperty.managedReferenceValue =
                    templateData ??
                    Activator.CreateInstance(templateType)
                        as LevelTemplateBase;

                _serializedSo.ApplyModifiedProperties();
                _serializedSo.Update();

                TemplateDrawer.DrawTemplate(
                    _templateRoot,
                    _templateProperty
                );
            }
            finally
            {
                _suppressDirtyTracking = false;
            }
        }

        private void ReleaseTemplateData()
        {
            _templateProperty = null;
            _serializedSo = null;

            if (_currentSo == null)
                return;

            DestroyImmediate(_currentSo);
            _currentSo = null;
        }

        #endregion

        #region State and Validation

        private void UpdateEditorControls()
        {
            if (_definitionStatusBox == null)
                return;

            bool hasValidSelection =
                HasValidDefinitionSelection();
            bool fileExists =
                hasValidSelection && SelectedDefinitionExists();
            bool isEditing =
                _mode != DefinitionEditorMode.None;

            _createNewButton.SetEnabled(
                !isEditing && hasValidSelection && !fileExists
            );

            _loadExistingButton.SetEnabled(
                !isEditing && hasValidSelection && fileExists
            );

            bool hasExpectedFileState =
                _mode == DefinitionEditorMode.Create
                    ? !fileExists
                    : _mode == DefinitionEditorMode.Edit && fileExists;

            _saveButton.SetEnabled(
                isEditing &&
                hasValidSelection &&
                hasExpectedFileState &&
                _currentSo != null &&
                _templateProperty != null
            );

            _saveButton.text = _mode switch
            {
                DefinitionEditorMode.Create => "Create JSON",
                DefinitionEditorMode.Edit => "Save Changes",
                _ => "Save"
            };

            _discardButton.SetEnabled(isEditing);
            _discardButton.text = _hasUnsavedChanges
                ? "Discard Changes"
                : "Close Definition";

            _templateDropdown.SetEnabled(
                _mode != DefinitionEditorMode.Edit
            );

            _templateRoot.style.display = isEditing
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _footerRoot.style.display = isEditing
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            UpdateDefinitionStatus(
                hasValidSelection,
                fileExists
            );
        }

        private void UpdateDefinitionStatus(
            bool hasValidSelection,
            bool fileExists)
        {
            if (_projectContext == null || !_projectContext.IsValid)
            {
                SetDefinitionStatus(
                    "Select a valid THEODEN project folder.",
                    HelpBoxMessageType.Info
                );
                return;
            }

            if (!hasValidSelection)
            {
                SetDefinitionStatus(
                    "Select a valid POI and language.",
                    HelpBoxMessageType.Warning
                );
                return;
            }

            string jsonAssetPath = GetSelectedJsonAssetPath();
            string dirtyMessage = _hasUnsavedChanges
                ? "\nThere are unsaved changes."
                : string.Empty;

            switch (_mode)
            {
                case DefinitionEditorMode.Create:
                    SetDefinitionStatus(
                        fileExists
                            ? "The target JSON was created externally. " +
                              "Close this definition and load the " +
                              "existing file."
                            : "Create mode. A new definition will be " +
                              $"written to:\n{jsonAssetPath}" +
                              dirtyMessage,
                        fileExists
                            ? HelpBoxMessageType.Error
                            : HelpBoxMessageType.Info
                    );
                    break;

                case DefinitionEditorMode.Edit:
                    SetDefinitionStatus(
                        fileExists
                            ? "Edit mode. The existing definition was " +
                              $"loaded from:\n{jsonAssetPath}" +
                              dirtyMessage
                            : "The JSON being edited is no longer " +
                              "available on disk.",
                        fileExists
                            ? HelpBoxMessageType.Info
                            : HelpBoxMessageType.Error
                    );
                    break;

                default:
                    SetDefinitionStatus(
                        fileExists
                            ? "Existing definition found:\n" +
                              jsonAssetPath
                            : "No definition exists for the selected " +
                              "POI and language. A new one can be " +
                              "created at:\n" + jsonAssetPath,
                        HelpBoxMessageType.Info
                    );
                    break;
            }
        }

        private void SetDefinitionStatus(
            string message,
            HelpBoxMessageType messageType)
        {
            _definitionStatusBox.text = message;
            _definitionStatusBox.messageType = messageType;
        }

        private bool HasValidDefinitionSelection()
        {
            return
                _projectContext != null &&
                _projectContext.IsValid &&
                !string.IsNullOrWhiteSpace(
                    _projectContext.poisFolderPath
                ) &&
                IsAssetPath(_projectContext.poisFolderPath) &&
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
        /// Builds the deterministic AssetDatabase path for the JSON selected
        /// by the current project, POI, and language controls.
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
                TheodenProjectPaths.GetPoiDataFolder(
                    _projectContext.poisFolderPath,
                    selectedPoi.PoiId
                )
                .Replace("\\", "/")
                .TrimEnd('/');

            string fileName =
                TheodenFileNaming.GetPoiJsonFileName(
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
}