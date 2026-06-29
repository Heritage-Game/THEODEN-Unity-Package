using System;
using System.Collections.Generic;
using System.Linq;
using Addressing;
using Editor.Export;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.POIDefinitionClasses
{
    public class LevelDefinitionBuilderWindow : EditorWindow
    {
        // UI roots
        private VisualElement _contentRoot;
        private VisualElement _staticRoot;
        private VisualElement _templateRoot;
        private VisualElement _footerRoot;

        // UI controls
        private PopupField<string> _templateDropdown;
        private Button _exportButton;

        //Warning Box UI
        private HelpBox _poiIdWarningBox;
        
        // Data
        private LevelDefinitionTemplateSO _currentSo;
        private SerializedObject _serializedSo;
        private SerializedProperty _templateProperty;

        private Type[] _templateTypes;
        private Type _currentTemplateType;
        
        // Project config selection
        private ObjectField _projectFolderField;
        private DefaultAsset _projectFolder;

        // Config-driven dropdowns
        private PopupField<string> _poiDropdown;
        private PopupField<string> _languageDropdown;

        private int _selectedPoiIndex;
        private int _selectedLanguageIndex;

        // Loaded project context
        private TheodenProjectContext _projectContext;

        [MenuItem("THEODEN/2.Create POI Definition")]
        public static void ShowWindow()
        {
            GetWindow<LevelDefinitionBuilderWindow>("Level Builder");
        }

        private void CreateGUI()
        {
            rootVisualElement.style.flexGrow = 1;

            _templateTypes = GetTemplateTypes();

            BuildLayout();
            BuildStaticUI();
            BuildTemplateContainer();
            BuildFooterUI();
            RegisterCallbacks();

            // Initial build (delayed to avoid UITK timing issues)
            EditorApplication.delayCall += () =>
            {
                if (this != null && _templateDropdown != null)
                    BuildTemplate(_templateDropdown.value);
            };
        }

        

        #region UIBuilding
       
        
        private void BuildLayout()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            _contentRoot = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column }
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
            //----------- ADD PROJECT FOLDER SELECTOR --------------------
            _projectFolderField = new ObjectField("Project Folder")
            {
                objectType = typeof(DefaultAsset),
                allowSceneObjects = false
            };
            _staticRoot.Add(_projectFolderField);
            
            
            //---------------- ADD TEMPLATE SELECTOR ----------------------
            var templateNames = _templateTypes.Select(t => t.Name).ToList();

            if (templateNames.Count == 0)
            {
                _staticRoot.Add(new HelpBox(
                    "No level templates found. Create at least one concrete LevelTemplateBase class.",
                    HelpBoxMessageType.Error
                ));
                return;
            }
            
            _templateDropdown = new PopupField<string>(
                "Template",
                templateNames,
                templateNames.First()
            );

            _staticRoot.Add(_templateDropdown);
            
            //---------------- ADD POI SELECTION -----------------
            _poiDropdown = new PopupField<string>(
                "Select the Point of Interest",
                new List<string> { "Select a project folder first" }, 0);
           _poiDropdown.SetEnabled(false);
           _staticRoot.Add(_poiDropdown);
           
           //---------------- LANGUAGE SELECTION -----------------
           _languageDropdown = new PopupField<string>(
               "Language",
               new List<string> { "Select a project folder first" },
               0
           );
           _languageDropdown.SetEnabled(false);
           _staticRoot.Add(_languageDropdown);
           
           //---------------------- WARNING BOX
            _poiIdWarningBox = new HelpBox(
                "POI IDs are now loaded from the configuration asset. " +
                "Select the folder of your project to select your POI",
                HelpBoxMessageType.Info
            );

            _poiIdWarningBox.style.marginTop = 8;

            _staticRoot.Add(_poiIdWarningBox);
        }

        private void BuildTemplateContainer()
        {
            _templateRoot.style.flexDirection = FlexDirection.Column;
        }

        private void BuildFooterUI()
        {

            _exportButton = new Button
            {
                text = "Export JSON"
            };
            _exportButton.SetEnabled(false);


            _footerRoot.Add(_exportButton);
        }

        private void RegisterCallbacks()
        {
            _projectFolderField.RegisterValueChangedCallback(evt =>
            {
                _projectFolder = evt.newValue as DefaultAsset;
                if (_projectFolder == null)
                {
                    ClearProjectContextUI();
                    return;
                }

                var path = AssetDatabase.GetAssetPath(_projectFolder);

                if (!AssetDatabase.IsValidFolder(path))
                {
                    Debug.LogError($"Failed to load project folder: {path}");
                    _projectFolder = null;
                    //avoid triggering callbacks inside an error 
                    _projectFolderField.SetValueWithoutNotify(null);
                    ClearProjectContextUI();
                    return;
                }

                if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                        path,
                        out _projectContext,
                        out string error))
                {
                    Debug.LogError(error);
                    ClearProjectContextUI();
                    return;
                }

                RefreshProjectContextDropdowns();
            });
            //--------------------------------------------------------------
            //                  TEMPLATE CALLBACK ONLY IF TEMPLATE !NULL
            //--------------------------------------------------------------
            if (_templateDropdown != null)
            {
                _templateDropdown.RegisterValueChangedCallback(evt =>
                {
                    BuildTemplate(evt.newValue);
                });
            }

            _poiDropdown.RegisterValueChangedCallback(evt =>
            {
                _selectedPoiIndex = _poiDropdown.index;
                UpdateExportButtonState();
            });

            _languageDropdown.RegisterValueChangedCallback(evt =>
            {
                _selectedLanguageIndex = _languageDropdown.index;
                UpdateExportButtonState();
            });
            

            _exportButton.clicked += OnExportClicked;
        }

        

        #endregion
        
        #region ProjectContextUI
        
        private void ClearProjectContextUI()
        {
            _projectContext = null;
            _selectedPoiIndex = 0;
            _selectedLanguageIndex = 0;

            if (_poiDropdown != null)
            {
                _poiDropdown.choices = new List<string> { "Select a project folder first" };
                _poiDropdown.index = 0;
                _poiDropdown.SetEnabled(false);
            }

            if (_languageDropdown != null)
            {
                _languageDropdown.choices = new List<string> { "Select a project folder first" };
                _languageDropdown.index = 0;
                _languageDropdown.SetEnabled(false);
            }

            UpdateExportButtonState();
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

            UpdateExportButtonState();
        }
        
        private void RefreshPoiDropdown()
        {
            if (_projectContext.availablePois == null || _projectContext.availablePois.Count == 0)
            {
                _poiDropdown.choices = new List<string> { "No POIs found" };
                _poiDropdown.index = 0;
                _poiDropdown.SetEnabled(false);
                return;
            }

            var poiOptions = _projectContext.availablePois
                .Select(poi => $"{poi.DisplayName} ({poi.PoiId})")
                .ToList();

            _selectedPoiIndex = 0;
            _poiDropdown.choices = poiOptions;
            _poiDropdown.index = 0;
            _poiDropdown.SetEnabled(true);
        }
        
        private void RefreshLanguageDropdown()
        {
            if (_projectContext.availableLanguages == null || _projectContext.availableLanguages.Count == 0)
            {
                _languageDropdown.choices = new List<string> { "No languages found" };
                _languageDropdown.index = 0;
                _languageDropdown.SetEnabled(false);
                return;
            }

            var languageOptions = _projectContext.availableLanguages
                .Select(language => language.displayedName)
                .ToList();

            _selectedLanguageIndex = 0;
            _languageDropdown.choices = languageOptions;
            _languageDropdown.index = 0;
            _languageDropdown.SetEnabled(true);
        }
        
        #endregion

        #region ExportButtonState

        private void UpdateExportButtonState()
        {
            bool hasValidContext =
                _projectContext != null &&
                _projectContext.IsValid;
            
            bool hasValidPoiFolder =
                hasValidContext &&
                !string.IsNullOrWhiteSpace(_projectContext.poisFolderPath) &&
                _projectContext.poisFolderPath.StartsWith("Assets");

            bool hasValidPoiSelection =
                hasValidContext &&
                _projectContext.availablePois != null &&
                _selectedPoiIndex >= 0 &&
                _selectedPoiIndex < _projectContext.availablePois.Count;

            bool hasValidLanguageSelection =
                hasValidContext &&
                _projectContext.availableLanguages != null &&
                _selectedLanguageIndex >= 0 &&
                _selectedLanguageIndex < _projectContext.availableLanguages.Count;

            bool canExport =
                _currentSo != null &&
                _templateProperty != null &&
                hasValidPoiFolder &&
                hasValidPoiSelection &&
                hasValidLanguageSelection;

            _exportButton?.SetEnabled(canExport);
        }

        #endregion
        
        #region Export and Validation
        private void OnExportClicked()
        {
            if (_currentSo == null)
            {
                Debug.LogError("No template selected.");
                return;
            }

            if (_projectContext == null || !_projectContext.IsValid)
            {
                Debug.LogError("No valid THEODEN project selected.");
                return;
            }

            if (_selectedPoiIndex < 0 || _selectedPoiIndex >= _projectContext.availablePois.Count)
            {
                Debug.LogError("No valid POI selected.");
                return;
            }

            if (_selectedLanguageIndex < 0 || _selectedLanguageIndex >= _projectContext.availableLanguages.Count)
            {
                Debug.LogError("No valid language selected.");
                return;
            }

            _serializedSo.ApplyModifiedProperties();
            var template = _templateProperty.managedReferenceValue as LevelTemplateBase;

            if (template == null)
            {
                Debug.LogError("Template data is missing.");
                return;
            }

            var selectedPoi = _projectContext.availablePois[_selectedPoiIndex];
            var selectedLanguageData = _projectContext.availableLanguages[_selectedLanguageIndex];

            string poiId = selectedPoi.PoiId;
            string poiName = selectedPoi.DisplayName;
            LanguageList language = selectedLanguageData.language;

            if (string.IsNullOrWhiteSpace(poiId))
            {
                Debug.LogError("Selected POI has no valid ID.");
                return;
            }

            //--------------------------------------------------------
            //              USING THE PROJECT CONTEXT TO RESOLVE PATHS
            //---------------------------------------------------------
            string poiRootFolderPath = TheodenProjectPaths.GetPoiRootFolder(
                _projectContext.poisFolderPath,
                poiId
            );

            string jsonExportFolderPath = TheodenProjectPaths.GetPoiDataFolder(
                _projectContext.poisFolderPath,
                poiId
            );

            if (string.IsNullOrWhiteSpace(poiRootFolderPath) ||
                !poiRootFolderPath.StartsWith("Assets"))
            {
                Debug.LogError($"Invalid POI root folder: {poiRootFolderPath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(jsonExportFolderPath) ||
                !jsonExportFolderPath.StartsWith("Assets"))
            {
                Debug.LogError($"Invalid POI export folder: {jsonExportFolderPath}");
                return;
            }

            template.InjectForExport(
                poiId,
                poiName,
                language,
                "0.1.0"
            );

            //--------------------------------------------------------------
            //          USING NAMING CONVENTION TO ASSIGN THE CORRECT NAME
            //---------------------------------------------------------------
            string fileName = TheodenFileNaming.GetPoiJsonFileName(poiId, language);

            Debug.Log($"Exporting POI '{poiId}' as {fileName}");
            Debug.Log($"POI root folder: {poiRootFolderPath}");
            Debug.Log($"JSON export folder: {jsonExportFolderPath}");

            if (!PoiExportService.ExportPoi(
                    template,
                    poiId,
                    language,
                    poiRootFolderPath,
                    jsonExportFolderPath,
                    fileName,
                    out var error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog(
                    "Export Failed",
                    error,
                    "OK"
                );
                return;
            }

            Debug.Log("POI export successful.");
            EditorUtility.DisplayDialog(
                "Success",
                $"POI JSON exported successfully for '{poiName}'.",
                "OK"
            );
        }
        
        #endregion

        #region TemplateBuilding

        

       
        private void BuildTemplate(string templateName)
        {
            var newType = _templateTypes.FirstOrDefault(t => t.Name == templateName);
            if (newType == null || newType == _currentTemplateType)
                return;

            _currentTemplateType = newType;
            RebuildTemplateData(newType);
        }

        private void RebuildTemplateData(Type templateType)
        {
            // Create data
            _currentSo = ScriptableObject.CreateInstance<LevelDefinitionTemplateSO>();

            _serializedSo = new SerializedObject(_currentSo);

            _templateProperty = _serializedSo.FindProperty("template");
            _templateProperty.managedReferenceValue = Activator.CreateInstance(templateType);

            _serializedSo.ApplyModifiedProperties();
            _serializedSo.Update();

            // Rebuild template UI
            _templateRoot.Clear();
            TemplateDrawer.DrawTemplate(_templateRoot, _templateProperty);

            
            //Update Export Button state --- enable if every necessary field is filled
            UpdateExportButtonState();
        }

        private static Type[] GetTemplateTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(LevelTemplateBase).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    t.IsClass)
                .ToArray();
        }
        #endregion
    }
}
