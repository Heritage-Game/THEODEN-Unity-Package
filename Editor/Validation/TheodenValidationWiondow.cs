using System;
using UnityEditor;
using UnityEngine;
using Theoden.Editor.Build;
namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Editor window used to validate a THEODEN project and display
    /// all detected errors and warnings.
    /// </summary>
    public sealed class TheodenValidationWindow : EditorWindow
    {
        private DefaultAsset _selectedProjectFolder;
        private TheodenProjectContext _projectContext;

        private TheodenValidationReport _validationReport;

        private string _projectLoadError;
        private bool _hasValidated;

        private Vector2 _reportScrollPosition;

        /// <summary>
        /// Opens the THEODEN Project Validation window.
        /// </summary>
        [MenuItem("THEODEN/Validate Project")]
        public static void ShowWindow()
        {
            TheodenValidationWindow window =
                GetWindow<TheodenValidationWindow>();

            window.titleContent =
                new GUIContent("THEODEN Validation");

            window.minSize =
                new Vector2(650, 600);

            window.Show();
        }

        /// <summary>
        /// Opens the validation window with a project folder
        /// already selected.
        /// </summary>
        /// <param name="projectFolder">
        /// The THEODEN project folder to load.
        /// </param>
        public static void OpenForProject(
            DefaultAsset projectFolder)
        {
            ShowWindow();

            TheodenValidationWindow window =
                GetWindow<TheodenValidationWindow>();

            window.SetProjectFolder(projectFolder);
            window.Focus();
        }

        /// <summary>
        /// Draws the validation window interface.
        /// </summary>
        private void OnGUI()
        {
            DrawHeader();

            GUILayout.Space(10);

            DrawProjectSelection();

            GUILayout.Space(10);

            DrawValidationButton();

            if (_hasValidated && _validationReport != null)
            {
                GUILayout.Space(15);

                DrawValidationReport();

                GUILayout.Space(15);

                DrawBuildConfigurationShortcut();
            }
        }

        /// <summary>
        /// Draws the title and introductory information.
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.LabelField(
                "THEODEN Project Validation",
                EditorStyles.boldLabel
            );

            EditorGUILayout.HelpBox(
                "Select a THEODEN project folder and validate " +
                "its configuration, localized content, JSON references, " +
                "and Addressables setup.",
                MessageType.Info
            );
        }

        /// <summary>
        /// Draws the project folder selector and its current loading state.
        /// </summary>
        private void DrawProjectSelection()
        {
            EditorGUILayout.LabelField(
                "Project",
                EditorStyles.boldLabel
            );

            EditorGUI.BeginChangeCheck();

            DefaultAsset selectedFolder =
                (DefaultAsset)EditorGUILayout.ObjectField(
                    "Project Folder",
                    _selectedProjectFolder,
                    typeof(DefaultAsset),
                    false
                );

            if (EditorGUI.EndChangeCheck())
                SetProjectFolder(selectedFolder);

            if (_selectedProjectFolder == null)
            {
                EditorGUILayout.HelpBox(
                    "Select the root folder of the THEODEN project " +
                    "that you want to validate.",
                    MessageType.Info
                );

                return;
            }

            string folderPath =
                AssetDatabase.GetAssetPath(
                    _selectedProjectFolder
                );

            if (!string.IsNullOrWhiteSpace(_projectLoadError))
            {
                EditorGUILayout.HelpBox(
                    _projectLoadError,
                    MessageType.Error
                );

                return;
            }

            if (_projectContext != null)
            {
                EditorGUILayout.HelpBox(
                    $"Loaded project: {folderPath}",
                    MessageType.None
                );
            }
        }

        /// <summary>
        /// Draws the button used to start or repeat validation.
        /// </summary>
        private void DrawValidationButton()
        {
            using (new EditorGUI.DisabledScope(
                       _selectedProjectFolder == null))
            {
                string buttonText =
                    _hasValidated
                        ? "Validate Again"
                        : "Validate Project";

                if (GUILayout.Button(
                        buttonText,
                        GUILayout.Height(34)))
                {
                    RunValidation();
                }
            }
        }

        /// <summary>
        /// Draws the validation summary and complete issue list.
        /// </summary>
        private void DrawValidationReport()
        {
            EditorGUILayout.LabelField(
                "Validation Report",
                EditorStyles.boldLabel
            );

            DrawReportSummary();

            if (_validationReport.Issues.Count == 0)
                return;

            GUILayout.Space(8);

            _reportScrollPosition =
                EditorGUILayout.BeginScrollView(
                    _reportScrollPosition,
                    GUILayout.MinHeight(250),
                    GUILayout.ExpandHeight(true)
                );

            DrawIssuesOfSeverity(
                ValidationSeverity.Error,
                "Errors"
            );

            DrawIssuesOfSeverity(
                ValidationSeverity.Warning,
                "Warnings"
            );

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draws the total number of errors and warnings.
        /// </summary>
        private void DrawReportSummary()
        {
            if (_validationReport.IsValid &&
                _validationReport.WarningCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "Validation completed successfully. " +
                    "No errors or warnings were found.",
                    MessageType.Info
                );

                return;
            }

            if (_validationReport.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Validation completed with no blocking errors.\n" +
                    $"Warnings: {_validationReport.WarningCount}",
                    MessageType.Warning
                );

                return;
            }

            EditorGUILayout.HelpBox(
                "Validation failed. Resolve all blocking errors " +
                "before building the project.\n" +
                $"Errors: {_validationReport.ErrorCount}\n" +
                $"Warnings: {_validationReport.WarningCount}",
                MessageType.Error
            );
        }

        /// <summary>
        /// Draws every report issue matching a specific severity.
        /// </summary>
        private void DrawIssuesOfSeverity(
            ValidationSeverity severity,
            string sectionTitle)
        {
            bool containsIssues = false;

            foreach (ValidationIssue issue
                     in _validationReport.Issues)
            {
                if (issue.Severity != severity)
                    continue;

                containsIssues = true;
                break;
            }

            if (!containsIssues)
                return;

            EditorGUILayout.LabelField(
                sectionTitle,
                EditorStyles.boldLabel
            );

            foreach (ValidationIssue issue
                     in _validationReport.Issues)
            {
                if (issue.Severity != severity)
                    continue;

                DrawValidationIssue(issue);
            }

            GUILayout.Space(8);
        }

        /// <summary>
        /// Draws a single validation issue.
        /// </summary>
        private static void DrawValidationIssue(
            ValidationIssue issue)
        {
            EditorGUILayout.BeginVertical(
                EditorStyles.helpBox
            );

            MessageType messageType =
                issue.Severity == ValidationSeverity.Error
                    ? MessageType.Error
                    : MessageType.Warning;

            EditorGUILayout.HelpBox(
                $"[{issue.Code}]\n{issue.Message}",
                messageType
            );

            if (!string.IsNullOrWhiteSpace(issue.AssetPath))
            {
                EditorGUILayout.LabelField(
                    "Related path:",
                    EditorStyles.miniBoldLabel
                );

                EditorGUILayout.SelectableLabel(
                    issue.AssetPath,
                    EditorStyles.textField,
                    GUILayout.Height(
                        EditorGUIUtility.singleLineHeight
                    )
                );

                if (GUILayout.Button("Show Related Asset"))
                    PingRelatedAsset(issue.AssetPath);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        /// <summary>
        /// Draws the shortcut to the build configuration workflow.
        /// </summary>
        private void DrawBuildConfigurationShortcut()
        {
            if (!_validationReport.IsValid)
            {
                EditorGUILayout.HelpBox(
                    "Build configuration is unavailable until all " +
                    "blocking validation errors have been resolved.",
                    MessageType.Error
                );

                return;
            }

            EditorGUILayout.HelpBox(
                "The project has no blocking validation errors. " +
                "You can proceed to the build configuration.",
                MessageType.Info
            );

            if (GUILayout.Button(
                    "Open Build Configuration",
                    GUILayout.Height(34)))
            {
                OpenBuildConfiguration();
            }
        }

        /// <summary>
        /// Loads the selected THEODEN project folder and clears
        /// the previous validation state.
        /// </summary>
        private void SetProjectFolder(
            DefaultAsset projectFolder)
        {
            _selectedProjectFolder = projectFolder;
            _projectContext = null;
            _validationReport = null;
            _projectLoadError = null;
            _hasValidated = false;
            _reportScrollPosition = Vector2.zero;

            if (projectFolder == null)
            {
                Repaint();
                return;
            }

            string folderPath =
                AssetDatabase.GetAssetPath(projectFolder);

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                _projectLoadError =
                    "The selected asset is not a valid project folder.";

                Repaint();
                return;
            }

            if (!TheodenProjectConfigLoader.TryLoadProjectContext(
                    folderPath,
                    out _projectContext,
                    out string error))
            {
                _projectLoadError = error;
                _projectContext = null;
            }

            Repaint();
        }

        /// <summary>
        /// Runs project validation and stores the generated report.
        /// </summary>
        private void RunValidation()
        {
            _hasValidated = true;
            _reportScrollPosition = Vector2.zero;

            if (_projectContext == null)
            {
                _validationReport =
                    new TheodenValidationReport();

                string folderPath =
                    _selectedProjectFolder != null
                        ? AssetDatabase.GetAssetPath(
                            _selectedProjectFolder
                        )
                        : "";

                string errorMessage =
                    !string.IsNullOrWhiteSpace(_projectLoadError)
                        ? _projectLoadError
                        : "The selected project context could not be loaded.";

                _validationReport.AddError(
                    "PROJECT_CONTEXT_LOAD_FAILED",
                    errorMessage,
                    folderPath
                );

                Repaint();
                return;
            }

            _validationReport =
                TheodenProjectValidator.Validate(
                    _projectContext
                );

            Repaint();
        }

        /// <summary>
        /// Selects the related asset or the nearest existing parent folder.
        /// </summary>
        private static void PingRelatedAsset(
            string assetPath)
        {
            UnityEngine.Object relatedAsset =
                FindNearestExistingAsset(assetPath);

            if (relatedAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Asset Not Found",
                    "Neither the related asset nor one of its " +
                    "project folders could be found.",
                    "OK"
                );

                return;
            }

            Selection.activeObject = relatedAsset;
            EditorGUIUtility.PingObject(relatedAsset);
        }

        /// <summary>
        /// Finds an asset or the nearest existing parent folder.
        /// </summary>
        private static UnityEngine.Object
            FindNearestExistingAsset(
                string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            string currentPath =
                assetPath
                    .Replace("\\", "/")
                    .TrimEnd('/');

            while (!string.IsNullOrWhiteSpace(currentPath))
            {
                UnityEngine.Object asset =
                    AssetDatabase.LoadAssetAtPath<
                        UnityEngine.Object
                    >(currentPath);

                if (asset != null)
                    return asset;

                int lastSeparatorIndex =
                    currentPath.LastIndexOf(
                        "/",
                        StringComparison.Ordinal
                    );

                if (lastSeparatorIndex < 0)
                    break;

                currentPath =
                    currentPath.Substring(
                        0,
                        lastSeparatorIndex
                    );
            }

            return null;
        }

        /// <summary>
        /// Opens the build configuration workflow.
        /// </summary>
        private void OpenBuildConfiguration()
        {

            EditorUtility.DisplayDialog(
                "Build Configuration",
                "The project passed validation. " +
                "Build your project in the Build Window.",
                "OK"
            );
            
            TheodenBuildWindow.OpenForProject(
                _selectedProjectFolder
            );
        }
    }
}