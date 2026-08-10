using System;
using Addressing;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Validates that every configured language contains all the JSON files
    /// required by the selected THEODEN project.
    /// </summary>
    public sealed class LocalizedContentValidationRule
        : ITheodenValidationRule
    {
        /// <inheritdoc/>
        public void Validate(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            /*
             * Missing context, configuration, languages, and POIs are handled
             * by ProjectConfigurationValidationRule.
             */
            if (context == null ||
                context.theodenProjectConfig == null ||
                context.availableLanguages == null ||
                context.availableLanguages.Count == 0 ||
                context.availablePois == null ||
                context.availablePois.Count == 0)
            {
                return;
            }

            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            bool codexFolderExists =
                AssetDatabase.IsValidFolder(
                    projectConfig.codexFolderPath
                );

            bool directionsFolderExists =
                AssetDatabase.IsValidFolder(
                    projectConfig.directionsFolderPath
                );

            bool poisFolderExists =
                AssetDatabase.IsValidFolder(
                    projectConfig.poisFolderPath
                );

            foreach (LanguageEntry languageEntry
                     in context.availableLanguages)
            {
                if (languageEntry == null)
                    continue;

                LanguageList language = languageEntry.language;

                if (codexFolderExists)
                {
                    ValidateCodexFile(
                        projectConfig.codexFolderPath,
                        language,
                        report
                    );
                }

                foreach (POIRegistryEntry poi
                         in context.availablePois)
                {
                    if (poi == null ||
                        string.IsNullOrWhiteSpace(poi.PoiId))
                    {
                        continue;
                    }

                    if (directionsFolderExists)
                    {
                        ValidateDirectionsFile(
                            projectConfig.directionsFolderPath,
                            poi.PoiId,
                            language,
                            report
                        );
                    }

                    if (poisFolderExists)
                    {
                        ValidatePoiFile(
                            projectConfig.poisFolderPath,
                            poi.PoiId,
                            language,
                            report
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Validates the Codex JSON associated with a language.
        /// </summary>
        private static void ValidateCodexFile(
            string codexFolderPath,
            LanguageList language,
            TheodenValidationReport report)
        {
            string fileName =
                TheodenFileNaming.GetCodexJsonFileName(language);

            string assetPath = CombineAssetPath(
                codexFolderPath,
                fileName
            );

            ValidateJsonFile(
                assetPath,
                "MISSING_CODEX_JSON",
                $"The Codex JSON for language '{language}' is missing.",
                report
            );
        }

        /// <summary>
        /// Validates the Directions JSON associated with a POI and language.
        /// </summary>
        private static void ValidateDirectionsFile(
            string directionsFolderPath,
            string poiId,
            LanguageList language,
            TheodenValidationReport report)
        {
            string fileName =
                TheodenFileNaming.GetDirectionsJsonFileName(
                    poiId,
                    language
                );

            string assetPath = CombineAssetPath(
                directionsFolderPath,
                fileName
            );

            ValidateJsonFile(
                assetPath,
                "MISSING_DIRECTIONS_JSON",
                $"The Directions JSON for POI '{poiId}' " +
                $"and language '{language}' is missing.",
                report
            );
        }

        /// <summary>
        /// Validates the POI JSON associated with a POI and language.
        /// </summary>
        private static void ValidatePoiFile(
            string poisFolderPath,
            string poiId,
            LanguageList language,
            TheodenValidationReport report)
        {
            string poiFolderPath = CombineAssetPath(
                poisFolderPath,
                poiId
            );

            string dataFolderPath = CombineAssetPath(
                poiFolderPath,
                "Data"
            );

            /*
             * A missing POI or Data folder is already reported by
             * ProjectStructureValidationRule.
             */
            if (!AssetDatabase.IsValidFolder(dataFolderPath))
                return;

            string fileName =
                TheodenFileNaming.GetPoiJsonFileName(
                    poiId,
                    language
                );

            string assetPath = CombineAssetPath(
                dataFolderPath,
                fileName
            );

            ValidateJsonFile(
                assetPath,
                "MISSING_POI_JSON",
                $"The POI JSON for POI '{poiId}' " +
                $"and language '{language}' is missing.",
                report
            );
        }

        /// <summary>
        /// Validates that a JSON file exists and is imported as a TextAsset.
        /// </summary>
        private static void ValidateJsonFile(
            string assetPath,
            string errorCode,
            string errorMessage,
            TheodenValidationReport report)
        {
            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            if (jsonAsset != null)
                return;

            report.AddError(
                errorCode,
                errorMessage,
                assetPath
            );
        }

        /// <summary>
        /// Combines two parts of a Unity project-relative asset path.
        /// </summary>
        private static string CombineAssetPath(
            string parentPath,
            string childPath)
        {
            return $"{parentPath.TrimEnd('/')}/{childPath.Trim('/')}";
        }
    }
}