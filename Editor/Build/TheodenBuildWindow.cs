using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Theoden.Editor.Build
{
    /// <summary>
    /// Configures and builds an Android APK for a THEODEN project.
    /// </summary>
    public sealed class TheodenBuildWindow : EditorWindow
    {
        // ============================================================
        // PROJECT
        // ============================================================

        [SerializeField]
        private DefaultAsset selectedProjectFolder;

        private TheodenProjectContext projectContext;
        private SerializedObject serializedProjectConfig;

        private SerializedProperty applicationNameProperty;
        private SerializedProperty applicationIdentifierProperty;
        private SerializedProperty applicationVersionProperty;
        private SerializedProperty androidVersionCodeProperty;

        private string projectLoadError;

        // ============================================================
        // BUILD CONFIGURATION
        // ============================================================

        [SerializeField]
        private string outputPath;

        [SerializeField]
        private bool developmentBuild;

        // ============================================================
        // BUILD RESULT
        // ============================================================

        private string statusMessage;
        private MessageType statusMessageType = MessageType.None;
        private BuildReport lastBuildReport;
        
        // ============================================================
        // REMOTE SERVICES AND URLS
        // ============================================================

        [SerializeField]
        private bool useRemoteAddressables;

        [SerializeField]
        private string remoteContentBaseUrl;

        [SerializeField]
        private bool useLeaderboard;

        [SerializeField]
        private string leaderboardBaseUrl = "http://localhost:8000";

        private Vector2 scrollPosition;

        // ============================================================
        // WINDOW OPENING
        // ============================================================

        /// <summary>
        /// Opens the build window without a preselected project.
        /// </summary>
        [MenuItem("THEODEN/Build Project")]
        public static void ShowWindow()
        {
            TheodenBuildWindow window =
                GetWindow<TheodenBuildWindow>();

            window.titleContent =
                new GUIContent("THEODEN Build");

            window.minSize =
                new Vector2(600f, 600f);

            window.Show();
        }

        /// <summary>
        /// Opens the build window for an already selected project.
        /// </summary>
        public static void OpenForProject(
            DefaultAsset projectFolder)
        {
            ShowWindow();

            TheodenBuildWindow window =
                GetWindow<TheodenBuildWindow>();

            window.SetProjectFolder(projectFolder);
            window.Focus();
        }

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        private void OnEnable()
        {
            if (selectedProjectFolder != null)
                LoadSelectedProject();
        }

        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();

            GUILayout.Space(12);

            DrawProjectSelection();

            if (projectContext != null &&
                serializedProjectConfig != null)
            {
                GUILayout.Space(15);

                serializedProjectConfig.Update();

                DrawApplicationSettings();

                GUILayout.Space(15);

                DrawAndroidPlatformStatus();

                GUILayout.Space(15);

                DrawBuildSettings();

                serializedProjectConfig.ApplyModifiedProperties();

                GUILayout.Space(20);

                DrawBuildButton();
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUILayout.Space(15);

                EditorGUILayout.HelpBox(
                    statusMessage,
                    statusMessageType
                );
            }

            DrawBuildReport();

            EditorGUILayout.EndScrollView();
        }

        // ============================================================
        // HEADER
        // ============================================================

        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "THEODEN Android Build",
                EditorStyles.boldLabel
            );

            EditorGUILayout.HelpBox(
                "Configure and generate an Android APK for the " +
                "selected THEODEN project.",
                MessageType.Info
            );
        }

        // ============================================================
        // PROJECT SELECTION
        // ============================================================

        private void DrawProjectSelection()
        {
            EditorGUILayout.LabelField(
                "Project",
                EditorStyles.boldLabel
            );

            EditorGUI.BeginChangeCheck();

            DefaultAsset newProjectFolder =
                (DefaultAsset)EditorGUILayout.ObjectField(
                    "Project Folder",
                    selectedProjectFolder,
                    typeof(DefaultAsset),
                    false
                );

            if (EditorGUI.EndChangeCheck())
                SetProjectFolder(newProjectFolder);

            if (!string.IsNullOrWhiteSpace(projectLoadError))
            {
                EditorGUILayout.HelpBox(
                    projectLoadError,
                    MessageType.Error
                );

                return;
            }

            if (projectContext != null)
            {
                EditorGUILayout.HelpBox(
                    "Project loaded successfully:\n" +
                    projectContext.projectFolderPath,
                    MessageType.None
                );
            }
        }

        private void SetProjectFolder(
            DefaultAsset projectFolder)
        {
            selectedProjectFolder = projectFolder;

            projectContext = null;
            serializedProjectConfig = null;
            projectLoadError = null;

            statusMessage = null;
            lastBuildReport = null;

            if (selectedProjectFolder == null)
            {
                outputPath = null;
                Repaint();
                return;
            }

            LoadSelectedProject();
            Repaint();
        }

        private void LoadSelectedProject()
        {
            string folderPath =
                AssetDatabase.GetAssetPath(
                    selectedProjectFolder
                );

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                projectLoadError =
                    "The selected asset is not a valid folder.";

                return;
            }

            if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                    folderPath,
                    out projectContext,
                    out string error))
            {
                projectLoadError = error;
                projectContext = null;
                return;
            }

            BindSerializedConfiguration();

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = CreateDefaultOutputPath();
        }

        private void BindSerializedConfiguration()
        {
            serializedProjectConfig =
                new SerializedObject(
                    projectContext.theodenProjectConfig
                );

            applicationNameProperty =
                serializedProjectConfig.FindProperty(
                    "applicationName"
                );

            applicationIdentifierProperty =
                serializedProjectConfig.FindProperty(
                    "applicationIdentifier"
                );

            applicationVersionProperty =
                serializedProjectConfig.FindProperty(
                    "applicationVersion"
                );

            androidVersionCodeProperty =
                serializedProjectConfig.FindProperty(
                    "androidVersionCode"
                );
        }

        // ============================================================
        // APPLICATION SETTINGS
        // ============================================================

        private void DrawApplicationSettings()
        {
            EditorGUILayout.LabelField(
                "Application",
                EditorStyles.boldLabel
            );

            if (applicationNameProperty == null ||
                applicationIdentifierProperty == null ||
                applicationVersionProperty == null ||
                androidVersionCodeProperty == null)
            {
                EditorGUILayout.HelpBox(
                    "One or more build fields are missing from " +
                    "TheodenProjectConfig.",
                    MessageType.Error
                );

                return;
            }

            EditorGUILayout.PropertyField(
                applicationNameProperty,
                new GUIContent("Application Name")
            );

            EditorGUILayout.PropertyField(
                applicationIdentifierProperty,
                new GUIContent(
                    "Application Identifier",
                    "For example: it.unicam.theoden.romanborder"
                )
            );

            EditorGUILayout.PropertyField(
                applicationVersionProperty,
                new GUIContent(
                    "Application Version",
                    "Version displayed to the user, for example 1.0.0."
                )
            );

            EditorGUILayout.PropertyField(
                androidVersionCodeProperty,
                new GUIContent(
                    "Android Version Code",
                    "Increase this integer for every distributed update."
                )
            );

            EditorGUILayout.HelpBox(
                "Once the application has been distributed, do not " +
                "change its application identifier. Increase the Android " +
                "version code for every new release.",
                MessageType.Info
            );
        }

        // ============================================================
        // PLATFORM
        // ============================================================

        private void DrawAndroidPlatformStatus()
        {
            EditorGUILayout.LabelField(
                "Target Platform",
                EditorStyles.boldLabel
            );

            bool androidSupported =
                BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.Android,
                    BuildTarget.Android
                );

            bool androidActive =
                EditorUserBuildSettings.activeBuildTarget ==
                BuildTarget.Android;

            if (!androidSupported)
            {
                EditorGUILayout.HelpBox(
                    "Android Build Support is not installed for this " +
                    "Unity Editor version.",
                    MessageType.Error
                );

                return;
            }

            if (androidActive)
            {
                EditorGUILayout.HelpBox(
                    "Android is the active build target.",
                    MessageType.Info
                );

                return;
            }

            EditorGUILayout.HelpBox(
                "Android is installed but is not currently the active " +
                "build target.",
                MessageType.Warning
            );

            if (GUILayout.Button(
                    "Switch to Android",
                    GUILayout.Height(30)))
            {
                SwitchToAndroid();
            }
        }

        private void SwitchToAndroid()
        {
            bool confirmed =
                EditorUtility.DisplayDialog(
                    "Switch Build Target",
                    "Unity will switch the active build target to " +
                    "Android. This may trigger asset reimporting and " +
                    "script recompilation.",
                    "Switch",
                    "Cancel"
                );

            if (!confirmed)
                return;

            bool switched =
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    NamedBuildTarget.Android,
                    BuildTarget.Android
                );

            if (switched)
            {
                SetStatus(
                    "The active build target is now Android. " +
                    "Wait for Unity to finish compiling before building.",
                    MessageType.Info
                );
            }
            else
            {
                SetStatus(
                    "Unity could not switch the active build target " +
                    "to Android.",
                    MessageType.Error
                );
            }
        }

        // ============================================================
        // BUILD SETTINGS
        // ============================================================

        private void DrawBuildSettings()
        {
            EditorGUILayout.LabelField(
                "Build Configuration",
                EditorStyles.boldLabel
            );

            EditorGUILayout.BeginHorizontal();

            outputPath =
                EditorGUILayout.TextField(
                    "APK Output",
                    outputPath
                );

            if (GUILayout.Button(
                    "Browse",
                    GUILayout.Width(80)))
            {
                SelectOutputPath();
            }

            EditorGUILayout.EndHorizontal();

            developmentBuild =
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Development Build",
                        "Include debugging and development features."
                    ),
                    developmentBuild
                );

            EditorGUILayout.HelpBox(
                "This version uses debug signing and is intended for " +
                "testing. Release keystore support will be added later.",
                MessageType.Warning
            );

            if (GUILayout.Button(
                    "Prepare Runtime for Play Mode",
                    GUILayout.Height(30)))
            {
                ApplyRuntimeConfiguration();
            }

            EditorGUILayout.HelpBox(
                "Apply the selected project configuration before testing " +
                "the application in Play Mode. It is applied automatically " +
                "when building the APK.",
                MessageType.Info
            );
            
            string[] scenes =
                TheodenBuildSceneProvider.GetScenePaths();

            EditorGUILayout.LabelField(
                "Included Scenes",
                scenes.Length.ToString()
            );

            if (GUILayout.Button("Show Scene List"))
                ShowSceneList();
        }

        private void SelectOutputPath()
        {
            string defaultDirectory =
                string.IsNullOrWhiteSpace(outputPath)
                    ? GetDefaultBuildDirectory()
                    : Path.GetDirectoryName(outputPath);

            string defaultFileName =
                GetSafeApplicationFileName() + ".apk";

            string selectedPath =
                EditorUtility.SaveFilePanel(
                    "Select APK Output",
                    defaultDirectory,
                    defaultFileName,
                    "apk"
                );

            if (!string.IsNullOrWhiteSpace(selectedPath))
                outputPath = selectedPath;
        }

        private static void ShowSceneList()
        {
            string[] scenes =
                TheodenBuildSceneProvider.GetScenePaths();

            EditorUtility.DisplayDialog(
                "THEODEN Build Scenes",
                string.Join("\n", scenes),
                "OK"
            );
        }
        
        private void ApplyRuntimeConfiguration()
        {
            if (!TryPrepareRuntimeConfiguration(
                    out string error))
            {
                SetStatus(
                    error,
                    MessageType.Error
                );

                return;
            }

            SetStatus(
                "Runtime configuration updated successfully.\n" +
                TheodenRuntimeConfigGenerator
                    .RuntimeConfigAssetPath,
                MessageType.Info
            );
        }

        private bool TryPrepareRuntimeConfiguration(
            out string error)
        {
            error = null;

            if (projectContext == null)
            {
                error =
                    "Select a valid THEODEN project before generating " +
                    "the runtime configuration.";

                return false;
            }

            if (serializedProjectConfig != null)
                serializedProjectConfig.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();

            return TheodenRuntimeConfigGenerator.CreateOrUpdate(
                projectContext,
                out error
            );
        }
        
        // ============================================================
        // REMOTE SERVICE SETTINGS
        // ============================================================
        
        private void DrawRemoteServicesSettings()
        {
            EditorGUILayout.LabelField(
                "Remote Services",
                EditorStyles.boldLabel
            );

            useRemoteAddressables =
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Remote Addressables",
                        "Load Addressables content from a remote server."
                    ),
                    useRemoteAddressables
                );

            if (useRemoteAddressables)
            {
                remoteContentBaseUrl =
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Remote Content URL",
                            "Base URL containing the remote Addressables content."
                        ),
                        remoteContentBaseUrl
                    );
            }

            useLeaderboard =
                EditorGUILayout.Toggle(
                    new GUIContent(
                        "Leaderboard",
                        "Enable communication with the leaderboard service."
                    ),
                    useLeaderboard
                );

            if (useLeaderboard)
            {
                leaderboardBaseUrl =
                    EditorGUILayout.TextField(
                        new GUIContent(
                            "Leaderboard API URL",
                            "Base URL of the leaderboard API."
                        ),
                        leaderboardBaseUrl
                    );
            }
        }
        // ============================================================
        // BUILD
        // ============================================================

        private void DrawBuildButton()
        {
            bool androidActive =
                EditorUserBuildSettings.activeBuildTarget ==
                BuildTarget.Android;

            bool unityBusy =
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode;

            bool canBuild =
                projectContext != null &&
                androidActive &&
                !unityBusy &&
                !string.IsNullOrWhiteSpace(outputPath);

            if (unityBusy)
            {
                EditorGUILayout.HelpBox(
                    "Wait for Unity to finish compiling, importing, " +
                    "or leaving Play Mode.",
                    MessageType.Warning
                );
            }

            using (new EditorGUI.DisabledScope(!canBuild))
            {
                if (GUILayout.Button(
                        "Build Android APK",
                        GUILayout.Height(42)))
                {
                    BuildAndroidApk();
                }
            }
        }

        private void BuildAndroidApk()
        {
            serializedProjectConfig.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            statusMessage = null;
            lastBuildReport = null;

            bool succeeded =
                TheodenPlayerBuildService.TryBuildAndroidApk(
                    projectContext,
                    outputPath,
                    developmentBuild,
                    out lastBuildReport,
                    out string error
                );

            if (!succeeded)
            {
                SetStatus(
                    error,
                    MessageType.Error
                );

                return;
            }

            SetStatus(
                "APK built successfully:\n" +
                Path.GetFullPath(outputPath),
                MessageType.Info
            );
        }

        // ============================================================
        // RESULT
        // ============================================================

        private void DrawBuildReport()
        {
            if (lastBuildReport == null)
                return;

            GUILayout.Space(10);

            EditorGUILayout.LabelField(
                "Last Build",
                EditorStyles.boldLabel
            );

            BuildSummary summary =
                lastBuildReport.summary;

            double sizeMegabytes =
                summary.totalSize /
                (1024d * 1024d);

            EditorGUILayout.LabelField(
                "Result",
                summary.result.ToString()
            );

            EditorGUILayout.LabelField(
                "Duration",
                summary.totalTime.ToString()
            );

            EditorGUILayout.LabelField(
                "Size",
                $"{sizeMegabytes:F2} MB"
            );

            if (summary.result == BuildResult.Succeeded &&
                File.Exists(outputPath))
            {
                if (GUILayout.Button("Show APK in Folder"))
                    EditorUtility.RevealInFinder(outputPath);
            }
        }

        private void SetStatus(
            string message,
            MessageType messageType)
        {
            statusMessage = message;
            statusMessageType = messageType;
            Repaint();
        }

        // ============================================================
        // PATH HELPERS
        // ============================================================

        private string CreateDefaultOutputPath()
        {
            return Path.Combine(
                GetDefaultBuildDirectory(),
                GetSafeApplicationFileName() + ".apk"
            );
        }

        private static string GetDefaultBuildDirectory()
        {
            string unityProjectRoot =
                Directory.GetParent(Application.dataPath)?.FullName;

            if (string.IsNullOrWhiteSpace(unityProjectRoot))
                unityProjectRoot = Application.dataPath;

            return Path.Combine(
                unityProjectRoot,
                "Builds"
            );
        }

        private string GetSafeApplicationFileName()
        {
            string applicationName =
                projectContext?.theodenProjectConfig?.applicationName;

            if (string.IsNullOrWhiteSpace(applicationName))
                applicationName = "TheodenApp";

            foreach (char invalidCharacter
                     in Path.GetInvalidFileNameChars())
            {
                applicationName =
                    applicationName.Replace(
                        invalidCharacter,
                        '_'
                    );
            }

            return applicationName.Trim();
        }
    }
}