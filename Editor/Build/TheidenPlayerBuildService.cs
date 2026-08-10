using System;
using System.Collections.Generic;
using System.IO;
using Theoden.Editor.Validation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Theoden.Editor.Build
{
    /// <summary>
    /// Configures and builds a THEODEN Android APK.
    /// </summary>
    public static class TheodenPlayerBuildService
    {
        /// <summary>
        /// Validates and builds the selected THEODEN project.
        /// </summary>
        public static bool TryBuildAndroidApk(
            TheodenProjectContext context,
            string outputPath,
            bool developmentBuild,
            out BuildReport buildReport,
            out string error)
        {
            buildReport = null;
            error = null;

            if (!ValidateBuildRequest(
                    context,
                    outputPath,
                    out error))
            {
                return false;
            }

            TheodenProjectConfig config =
                context.theodenProjectConfig;

            string fullOutputPath =
                Path.GetFullPath(outputPath);

            string outputDirectory =
                Path.GetDirectoryName(fullOutputPath);

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                error = "The APK output directory is invalid.";
                return false;
            }

            Directory.CreateDirectory(outputDirectory);
            
            // Create runtime configuration
            if (!TheodenRuntimeConfigGenerator.CreateOrUpdate(
                    context,
                    out string runtimeConfigError))
            {
                error =
                    "Could not prepare the THEODEN runtime " +
                    "configuration:\n" +
                    runtimeConfigError;

                return false;
            }

            // Store the current global Unity settings so that building one
            // THEODEN project does not permanently modify another one.
            string previousProductName =
                PlayerSettings.productName;

            string previousApplicationIdentifier =
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Android
                );

            string previousBundleVersion =
                PlayerSettings.bundleVersion;

            int previousVersionCode =
                PlayerSettings.Android.bundleVersionCode;

            bool previousBuildAppBundle =
                EditorUserBuildSettings.buildAppBundle;

            bool previousUseCustomKeystore =
                PlayerSettings.Android.useCustomKeystore;

            try
            {
                ApplyAndroidSettings(config);

                BuildOptions options =
                    BuildOptions.DetailedBuildReport;

                if (developmentBuild)
                    options |= BuildOptions.Development;

                BuildPlayerOptions buildPlayerOptions =
                    new BuildPlayerOptions
                    {
                        scenes =
                            TheodenBuildSceneProvider.GetScenePaths(),

                        locationPathName =
                            fullOutputPath,

                        target =
                            BuildTarget.Android,

                        options =
                            options
                    };

                Debug.Log(
                    "[TheodenPlayerBuildService] Starting Android build: " +
                    fullOutputPath
                );
                
                buildReport =
                    BuildPipeline.BuildPlayer(buildPlayerOptions);

                if (buildReport == null)
                {
                    error = "Unity did not return a build report.";
                    return false;
                }

                if (buildReport.summary.result !=
                    BuildResult.Succeeded)
                {
                    error =
                        "Android build failed.\n" +
                        buildReport.SummarizeErrors();

                    return false;
                }

                Debug.Log(
                    "[TheodenPlayerBuildService] APK built successfully: " +
                    fullOutputPath +
                    " | Size: " +
                    buildReport.summary.totalSize +
                    " bytes"
                );

                return true;
            }
            catch (Exception exception)
            {
                error =
                    "An unexpected error occurred while building the APK: " +
                    exception.Message;

                Debug.LogException(exception);
                return false;
            }
            finally
            {
                RestorePreviousSettings(
                    previousProductName,
                    previousApplicationIdentifier,
                    previousBundleVersion,
                    previousVersionCode,
                    previousBuildAppBundle,
                    previousUseCustomKeystore
                );
            }
        }

        /// <summary>
        /// Checks every requirement before starting the build.
        /// </summary>
        private static bool ValidateBuildRequest(
            TheodenProjectContext context,
            string outputPath,
            out string error)
        {
            error = null;

            if (context == null || !context.IsValid)
            {
                error =
                    "The selected THEODEN project context is invalid.";

                return false;
            }

            TheodenValidationReport validationReport =
                TheodenProjectValidator.Validate(context);

            if (!validationReport.IsValid)
            {
                error =
                    "The project contains blocking validation errors. " +
                    "Run THEODEN project validation for details.";

                return false;
            }

            IReadOnlyList<string> missingScenes =
                TheodenBuildSceneProvider.GetMissingScenePaths();

            if (missingScenes.Count > 0)
            {
                error =
                    "The following runtime scenes could not be found:\n" +
                    string.Join("\n", missingScenes);

                return false;
            }

            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                error =
                    "Android Build Support is not installed for this " +
                    "Unity Editor version.";

                return false;
            }

            if (EditorUserBuildSettings.activeBuildTarget !=
                BuildTarget.Android)
            {
                error =
                    "Android is not the active build target. " +
                    "Switch the project to Android before building.";

                return false;
            }

            TheodenProjectConfig config =
                context.theodenProjectConfig;

            if (string.IsNullOrWhiteSpace(config.applicationName))
            {
                error = "The application name is empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    config.applicationIdentifier))
            {
                error = "The application identifier is empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    config.applicationVersion))
            {
                error = "The application version is empty.";
                return false;
            }

            if (config.androidVersionCode < 1)
            {
                error =
                    "The Android version code must be at least 1.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                error = "The APK output path is empty.";
                return false;
            }

            if (!string.Equals(
                    Path.GetExtension(outputPath),
                    ".apk",
                    StringComparison.OrdinalIgnoreCase))
            {
                error =
                    "The Android build output must have the .apk extension.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the selected project settings for the duration
        /// of the build.
        /// </summary>
        private static void ApplyAndroidSettings(
            TheodenProjectConfig config)
        {
            PlayerSettings.productName =
                config.applicationName;

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                config.applicationIdentifier
            );

            PlayerSettings.bundleVersion =
                config.applicationVersion;

            PlayerSettings.Android.bundleVersionCode =
                config.androidVersionCode;

            // Generate an APK rather than an Android App Bundle.
            EditorUserBuildSettings.buildAppBundle = false;

            // Demo builds use Unity's debug signing.
            PlayerSettings.Android.useCustomKeystore = false;
        }

        /// <summary>
        /// Restores the Unity settings that existed before the build.
        /// </summary>
        private static void RestorePreviousSettings(
            string productName,
            string applicationIdentifier,
            string bundleVersion,
            int versionCode,
            bool buildAppBundle,
            bool useCustomKeystore)
        {
            PlayerSettings.productName =
                productName;

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                applicationIdentifier
            );

            PlayerSettings.bundleVersion =
                bundleVersion;

            PlayerSettings.Android.bundleVersionCode =
                versionCode;

            EditorUserBuildSettings.buildAppBundle =
                buildAppBundle;

            PlayerSettings.Android.useCustomKeystore =
                useCustomKeystore;
        }
    }
}