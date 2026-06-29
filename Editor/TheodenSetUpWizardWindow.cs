using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TheodenSetupWizardWindow : EditorWindow
{
    private string applicationName = "New Theoden App";

    private readonly List<LanguageSelectionData> languageSelections = new();
    private readonly List<POISetupData> poiSetupData = new();

    private Vector2 scrollPosition;

    [MenuItem("THEODEN/1.Setup Wizard")]
    public static void Open()
    {
        TheodenSetupWizardWindow window = GetWindow<TheodenSetupWizardWindow>();
        window.titleContent = new GUIContent("THEODEN Setup Wizard");
        window.minSize = new Vector2(560, 600);
        window.Show();
    }

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

    private void DrawApplicationSection()
    {
        EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);

        applicationName = EditorGUILayout.TextField("Application Name", applicationName);

        string sanitizedAppName = SanitizeFolderName(applicationName);
        EditorGUILayout.LabelField("Root Folder Preview", $"Assets/{sanitizedAppName}");

        EditorGUILayout.Space(10);
    }

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
            languageConfig
        );

        CreatePOIFoldersAndRegisterPOIs(poisPath, poiRegistry);

        EditorUtility.SetDirty(poiRegistry);
        EditorUtility.SetDirty(languageConfig);
        EditorUtility.SetDirty(projectConfig);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = projectConfig;

        EditorUtility.DisplayDialog(
            "THEODEN Setup Complete",
            $"Project structure created successfully at:\n{rootPath}",
            "OK"
        );
    }

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
        LanguageConfig languageConfig)
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

        projectConfig.configFolderPath = configPath;
        projectConfig.codexFolderPath = codexPath;
        projectConfig.directionsFolderPath = directionsPath;
        projectConfig.poisFolderPath = poisPath;
        projectConfig.mediaFolderPath = mediaPath;
        projectConfig.qrCodeFolderPath = qrCodesPath;

        projectConfig.useAddressables = true;
    }

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

    private void CreateFolderIfMissing(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";

        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private List<LanguageSelectionData> GetSelectedLanguages()
    {
        return languageSelections.FindAll(language => language.isSelected);
    }

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

    private class POISetupData
    {
        public string displayName;
        public string poiId;

        public POISetupData(string displayName)
        {
            this.displayName = displayName;
            this.poiId = GeneratePoiId(displayName);
        }
    }

    private class LanguageSelectionData
    {
        public LanguageList language;
        public string displayedName;
        public bool isSelected;
    }
}