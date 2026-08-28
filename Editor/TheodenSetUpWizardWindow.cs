using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides the initial setup workflow for a THEODEN application.
/// The wizard creates the project folder structure, configuration assets,
/// language settings, and POI registry entries required by the authoring tools.
/// </summary>
public class TheodenSetupWizardWindow : EditorWindow
{
    private string applicationName = "New Theoden App";

    private readonly List<LanguageSelectionData> languageSelections = new();
    private readonly List<POISetupData> poiSetupData = new();

    private Vector2 scrollPosition;

    /// <summary>
    /// Opens the THEODEN Setup Wizard window and applies its initial editor settings.
    /// </summary>
    [MenuItem("THEODEN/1.Setup Wizard")]
    public static void Open()
    {
        TheodenSetupWizardWindow window = GetWindow<TheodenSetupWizardWindow>();
        window.titleContent = new GUIContent("THEODEN Setup Wizard");
        window.minSize = new Vector2(560, 600);
        window.Show();
    }

    /// <summary>
    /// Initializes the selectable languages and default POI entries when the window is enabled.
    /// Existing in-memory selections are preserved.
    /// </summary>
    private void OnEnable()
    {
        InitializeLanguages();

        if (poiSetupData.Count == 0)
        {
            poiSetupData.Add(new POISetupData("Roman Empire"));
            poiSetupData.Add(new POISetupData("Germanic House"));
            poiSetupData.Add(new POISetupData("Farmstead"));
        }
    }

    /// <summary>
    /// Draws the complete scrollable interface of the setup wizard.
    /// </summary>
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawApplicationSection();
        DrawLanguagesSection();
        DrawPOISection();
        DrawSummarySection();
        DrawActionButtons();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draws the wizard title and introductory information.
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("THEODEN Project Setup", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "This wizard creates the initial folder structure and configuration assets for a THEODEN application.",
            MessageType.Info
        );

        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// Draws the application settings and previews the sanitized root folder path.
    /// </summary>
    private void DrawApplicationSection()
    {
        EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);

        applicationName = EditorGUILayout.TextField("Application Name", applicationName);

        string sanitizedAppName = SanitizeFolderName(applicationName);
        EditorGUILayout.LabelField("Root Folder Preview", $"Assets/{sanitizedAppName}");

        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// Draws the available language options and their editable display names.
    /// </summary>
    private void DrawLanguagesSection()
    {
        EditorGUILayout.LabelField("Languages", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Select the languages that the application will support.",
            MessageType.None
        );

        foreach (LanguageSelectionData languageSelection in languageSelections)
        {
            EditorGUILayout.BeginHorizontal();

            languageSelection.isSelected = EditorGUILayout.Toggle(languageSelection.isSelected, GUILayout.Width(20));

            EditorGUILayout.LabelField(languageSelection.language.ToString(), GUILayout.Width(80));

            using (new EditorGUI.DisabledScope(!languageSelection.isSelected))
            {
                languageSelection.displayedName = EditorGUILayout.TextField(languageSelection.displayedName);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// Draws the editable POI list and previews the identifier generated for each POI.
    /// </summary>
    private void DrawPOISection()
    {
        EditorGUILayout.LabelField("POIs", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Each POI will be registered in the POIRegistry and will receive its own folder.",
            MessageType.None
        );

        for (int i = 0; i < poiSetupData.Count; i++)
        {
            POISetupData poi = poiSetupData[i];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            poi.displayName = EditorGUILayout.TextField("Display Name", poi.displayName);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                poiSetupData.RemoveAt(i);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            poi.poiId = GeneratePoiId(poi.displayName);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Generated POI ID", poi.poiId);
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add POI"))
        {
            poiSetupData.Add(new POISetupData("New POI"));
        }

        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// Draws a preview of the folder structure that will be generated.
    /// </summary>
    private void DrawSummarySection()
    {
        EditorGUILayout.LabelField("Project Structure Preview", EditorStyles.boldLabel);

        string sanitizedAppName = SanitizeFolderName(applicationName);

        EditorGUILayout.HelpBox(
            $"Assets/{sanitizedAppName}/\n" +
            $"  Config/\n" +
            $"  Codex/\n" +
            $"  Directions/\n" +
            $"  POIs/\n" +
            $"  Media/\n" +
            $"  QRCodes/",
            MessageType.None
        );

        EditorGUILayout.Space(10);
    }

    /// <summary>
    /// Draws the action used to validate the current settings and create the project structure.
    /// </summary>
    private void DrawActionButtons()
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 36,
            fontStyle = FontStyle.Bold
        };

        if (GUILayout.Button("Create Project Structure", buttonStyle))
        {
            CreateProjectStructure();
        }
    }

    /// <summary>
    /// Populates the language selection list from the languages supported by THEODEN.
    /// English is selected by default.
    /// </summary>
    private void InitializeLanguages()
    {
        if (languageSelections.Count > 0)
        {
            return;
        }

        foreach (LanguageList language in Enum.GetValues(typeof(LanguageList)))
        {
            languageSelections.Add(new LanguageSelectionData
            {
                language = language,
                displayedName = language.ToString(),
                isSelected = language.ToString() == "ENG"
            });
        }
    }

    /// <summary>
    /// Creates or reuses the folders and configuration assets required by the project,
    /// applies the selected settings, and registers the configured POIs.
    /// </summary>
    private void CreateProjectStructure()
    {
        if (!ValidateInput())
        {
            return;
        }

        string sanitizedAppName = SanitizeFolderName(applicationName);

        string rootPath = $"Assets/{sanitizedAppName}";
        string configPath = $"{rootPath}/Config";
        string codexPath = $"{rootPath}/Codex";
        string directionsPath = $"{rootPath}/Directions";
        string poisPath = $"{rootPath}/POIs";
        string mediaPath = $"{rootPath}/Media";
        string qrCodesPath = $"{rootPath}/QRCodes";

        CreateFolderIfMissing("Assets", sanitizedAppName);
        CreateFolderIfMissing(rootPath, "Config");
        CreateFolderIfMissing(rootPath, "Codex");
        CreateFolderIfMissing(rootPath, "Directions");
        CreateFolderIfMissing(rootPath, "POIs");
        CreateFolderIfMissing(rootPath, "Media");
        CreateFolderIfMissing(rootPath, "QRCodes");

        POIRegistry poiRegistry = CreateOrLoadPOIRegistry(configPath);
        LanguageConfig languageConfig = CreateOrLoadLanguageConfig(configPath);
        MapDefinition mapDefinition = CreateOrLoadMapDefinition(configPath);
        TheodenProjectConfig projectConfig = CreateOrLoadProjectConfig(configPath);

        ConfigureLanguageConfig(languageConfig);
        ConfigureProjectConfig(
            projectConfig,
            rootPath,
            configPath,
            codexPath,
            directionsPath,
            poisPath,
            mediaPath,
            qrCodesPath,
            poiRegistry,
            languageConfig,
            mapDefinition
        );

        CreatePOIFoldersAndRegisterPOIs(poisPath, poiRegistry);

        EditorUtility.SetDirty(poiRegistry);
        EditorUtility.SetDirty(languageConfig);
        EditorUtility.SetDirty(mapDefinition);
        EditorUtility.SetDirty(projectConfig);
        

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //Selects the projectConfig as the current object
        //Shows its fields in the inspector when the wizard is closed 
        //the user can check the created project easily this way
        Selection.activeObject = projectConfig;

        EditorUtility.DisplayDialog(
            "THEODEN Setup Complete",
            $"Project structure created successfully at:\n{rootPath}",
            "OK"
        );
    }

    /// <summary>
    /// Validates the application name, selected languages, and generated POI identifiers.
    /// If the target project folder already exists, asks the user whether it should be reused.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the current input can be used to create the project;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            EditorUtility.DisplayDialog(
                "Invalid Application Name",
                "Application name cannot be empty.",
                "OK"
            );

            return false;
        }

        if (GetSelectedLanguages().Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Invalid Languages",
                "Select at least one language.",
                "OK"
            );

            return false;
        }

        if (poiSetupData.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Invalid POIs",
                "Add at least one POI.",
                "OK"
            );

            return false;
        }

        HashSet<string> generatedIds = new();

        foreach (POISetupData poi in poiSetupData)
        {
            string generatedId = GeneratePoiId(poi.displayName);

            if (string.IsNullOrWhiteSpace(generatedId))
            {
                EditorUtility.DisplayDialog(
                    "Invalid POI",
                    $"The POI '{poi.displayName}' generates an empty id.",
                    "OK"
                );

                return false;
            }

            if (!generatedIds.Add(generatedId))
            {
                EditorUtility.DisplayDialog(
                    "Duplicate POI ID",
                    $"The generated POI id '{generatedId}' is duplicated. Please use different display names.",
                    "OK"
                );

                return false;
            }
        }

        string sanitizedAppName = SanitizeFolderName(applicationName);
        string rootPath = $"Assets/{sanitizedAppName}";

        if (AssetDatabase.IsValidFolder(rootPath))
        {
            bool continueAnyway = EditorUtility.DisplayDialog(
                "Project Folder Already Exists",
                $"The folder '{rootPath}' already exists.\n\nThe wizard will reuse existing assets if possible and add missing data.\n\nContinue?",
                "Continue",
                "Cancel"
            );

            if (!continueAnyway)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Loads the project's POI registry or creates it when it does not yet exist.
    /// </summary>
    /// <param name="configPath">The Unity project-relative path of the configuration folder.</param>
    /// <returns>The existing or newly created POI registry.</returns>
    private POIRegistry CreateOrLoadPOIRegistry(string configPath)
    {
        string assetPath = $"{configPath}/POIRegistry.asset";

        POIRegistry existingRegistry = AssetDatabase.LoadAssetAtPath<POIRegistry>(assetPath);

        if (existingRegistry != null)
        {
            return existingRegistry;
        }

        POIRegistry registry = CreateInstance<POIRegistry>();
        AssetDatabase.CreateAsset(registry, assetPath);

        return registry;
    }

    /// <summary>
    /// Loads the project's language configuration or creates it when it does not yet exist.
    /// </summary>
    /// <param name="configPath">The Unity project-relative path of the configuration folder.</param>
    /// <returns>The existing or newly created language configuration.</returns>
    private LanguageConfig CreateOrLoadLanguageConfig(string configPath)
    {
        string assetPath = $"{configPath}/languageConfig.asset";

        LanguageConfig existingLanguageConfig = AssetDatabase.LoadAssetAtPath<LanguageConfig>(assetPath);

        if (existingLanguageConfig != null)
        {
            return existingLanguageConfig;
        }

        LanguageConfig languageConfig = CreateInstance<LanguageConfig>();
        languageConfig.languages = new List<LanguageEntry>();

        AssetDatabase.CreateAsset(languageConfig, assetPath);

        return languageConfig;
    }

    /// <summary>
    /// Loads the project's map definition or creates it when it does not yet exist.
    /// </summary>
    /// <param name="configPath">
    /// The Unity project-relative path of the configuration folder.
    /// </param>
    /// <returns>
    /// The existing or newly created map definition.
    /// </returns>
    private MapDefinition CreateOrLoadMapDefinition(string configPath)
    {
        string assetPath = $"{configPath}/MapDefinition.asset";

        MapDefinition existingMapDefinition =
            AssetDatabase.LoadAssetAtPath<MapDefinition>(assetPath);

        if (existingMapDefinition != null)
        {
            return existingMapDefinition;
        }

        MapDefinition mapDefinition = CreateInstance<MapDefinition>();
        AssetDatabase.CreateAsset(mapDefinition, assetPath);

        return mapDefinition;
    }
    
    /// <summary>
    /// Loads the main THEODEN project configuration or creates it when it does not yet exist.
    /// </summary>
    /// <param name="configPath">The Unity project-relative path of the configuration folder.</param>
    /// <returns>The existing or newly created project configuration.</returns>
    private TheodenProjectConfig CreateOrLoadProjectConfig(string configPath)
    {
        string assetPath = $"{configPath}/TheodenProjectConfig.asset";

        TheodenProjectConfig existingProjectConfig = AssetDatabase.LoadAssetAtPath<TheodenProjectConfig>(assetPath);

        if (existingProjectConfig != null)
        {
            return existingProjectConfig;
        }

        TheodenProjectConfig projectConfig = CreateInstance<TheodenProjectConfig>();
        AssetDatabase.CreateAsset(projectConfig, assetPath);

        return projectConfig;
    }

    /// <summary>
    /// Replaces the language configuration contents with the languages currently selected in the wizard.
    /// </summary>
    /// <param name="languageConfig">The language configuration asset to update.</param>
    private void ConfigureLanguageConfig(LanguageConfig languageConfig)
    {
        languageConfig.languages ??= new List<LanguageEntry>();
        languageConfig.languages.Clear();

        foreach (LanguageSelectionData selectedLanguage in GetSelectedLanguages())
        {
            languageConfig.languages.Add(new LanguageEntry
            {
                language = selectedLanguage.language,
                displayedName = selectedLanguage.displayedName
            });
        }
    }

    /// <summary>
    /// Applies the current wizard settings, project paths, and configuration asset references
    /// to the main THEODEN project configuration.
    /// </summary>
    /// <param name="projectConfig">The project configuration asset to update.</param>
    /// <param name="rootPath">The Unity project-relative root path of the application.</param>
    /// <param name="configPath">The path containing the project's configuration assets.</param>
    /// <param name="codexPath">The path containing Codex data.</param>
    /// <param name="directionsPath">The path containing directions data.</param>
    /// <param name="poisPath">The root path containing POI folders.</param>
    /// <param name="mediaPath">The path containing application-level media.</param>
    /// <param name="qrCodesPath">The path containing generated QR codes.</param>
    /// <param name="poiRegistry">The POI registry associated with the project.</param>
    /// <param name="languageConfig">The language configuration associated with the project.</param>
    /// <param name="mapDefinition"> The map definition associated with the project.</param>
    private void ConfigureProjectConfig(
        TheodenProjectConfig projectConfig,
        string rootPath,
        string configPath,
        string codexPath,
        string directionsPath,
        string poisPath,
        string mediaPath,
        string qrCodesPath,
        POIRegistry poiRegistry,
        LanguageConfig languageConfig,
        MapDefinition mapDefinition)
    {
        projectConfig.applicationName = applicationName;
        projectConfig.folderPath = rootPath;

        projectConfig.languages ??= new List<LanguageList>();
        projectConfig.languages.Clear();

        foreach (LanguageSelectionData selectedLanguage in GetSelectedLanguages())
        {
            projectConfig.languages.Add(selectedLanguage.language);
        }

        projectConfig.languageConfig = languageConfig;
        projectConfig.poiRegistry = poiRegistry;
        projectConfig.mapDefinition =  mapDefinition;

        projectConfig.configFolderPath = configPath;
        projectConfig.codexFolderPath = codexPath;
        projectConfig.directionsFolderPath = directionsPath;
        projectConfig.poisFolderPath = poisPath;
        projectConfig.mediaFolderPath = mediaPath;
        projectConfig.qrCodeFolderPath = qrCodesPath;

        projectConfig.useAddressables = true;
    }

    /// <summary>
    /// Creates the data and media folders for each configured POI and adds the POI
    /// to the project registry.
    /// </summary>
    /// <param name="poisPath">The Unity project-relative root path containing all POI folders.</param>
    /// <param name="registry">The registry in which the configured POIs are recorded.</param>
    private void CreatePOIFoldersAndRegisterPOIs(string poisPath, POIRegistry registry)
    {
        foreach (POISetupData poiData in poiSetupData)
        {
            string poiId = GeneratePoiId(poiData.displayName);
            string poiFolderPath = $"{poisPath}/{poiId}";

            CreateFolderIfMissing(poisPath, poiId);
            CreateFolderIfMissing(poiFolderPath, "Data");
            CreateFolderIfMissing(poiFolderPath, "Media");

            registry.AddPoi(poiId, poiData.displayName, poiFolderPath);
        }
    }

    /// <summary>
    /// Creates a direct child folder when it is not already present.
    /// </summary>
    /// <param name="parentPath">The Unity project-relative path of the parent folder.</param>
    /// <param name="folderName">The name of the child folder to create.</param>
    private void CreateFolderIfMissing(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";

        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    /// <summary>
    /// Gets the languages currently enabled in the wizard.
    /// </summary>
    /// <returns>A new list containing the selected language entries.</returns>
    private List<LanguageSelectionData> GetSelectedLanguages()
    {
        return languageSelections.FindAll(language => language.isSelected);
    }

    /// <summary>
    /// Converts an application name into a folder name containing only letters,
    /// digits, and underscores.
    /// </summary>
    /// <param name="rawName">The application name entered by the user.</param>
    /// <returns>
    /// A valid folder name, or <c>NewTheodenApp</c> when the input does not contain
    /// any supported characters.
    /// </returns>
    private static string SanitizeFolderName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "NewTheodenApp";
        }

        string result = rawName.Trim();

        result = Regex.Replace(result, @"\s+", "");
        result = Regex.Replace(result, @"[^a-zA-Z0-9_]", "");

        if (string.IsNullOrWhiteSpace(result))
        {
            result = "NewTheodenApp";
        }

        return result;
    }

    /// <summary>
    /// Generates the canonical POI identifier associated with a display name.
    /// </summary>
    /// <param name="displayName">The human-readable POI name.</param>
    /// <returns>
    /// A lowercase snake_case identifier, or an empty string when the display name is empty.
    /// </returns>
    private static string GeneratePoiId(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "";
        }

        string id = displayName.Trim().ToLowerInvariant();

        id = Regex.Replace(id, @"\s+", "_");
        id = Regex.Replace(id, @"[^a-z0-9_]", "");
        id = Regex.Replace(id, @"_+", "_");
        id = id.Trim('_');

        return id;
    }

    /// <summary>
    /// Stores the editable setup data for a POI displayed by the wizard.
    /// </summary>
    private class POISetupData
    {
        public string displayName;
        public string poiId;

        /// <summary>
        /// Initializes a POI setup entry and generates its initial identifier.
        /// </summary>
        /// <param name="displayName">The initial human-readable POI name.</param>
        public POISetupData(string displayName)
        {
            this.displayName = displayName;
            this.poiId = GeneratePoiId(displayName);
        }
    }

    /// <summary>
    /// Stores the selection state and display name of a supported language.
    /// </summary>
    private class LanguageSelectionData
    {
        public LanguageList language;
        public string displayedName;
        public bool isSelected;
    }
}
