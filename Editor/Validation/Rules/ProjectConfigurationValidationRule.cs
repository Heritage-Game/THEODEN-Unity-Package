using System;
using System.Collections.Generic;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Validates the configuration required to identify and process
    /// a THEODEN project.
    /// </summary>
    public sealed class ProjectConfigurationValidationRule
        : ITheodenValidationRule
    {
        /// <inheritdoc/>
        public void Validate(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            if (context == null)
            {
                report.AddError(
                    "MISSING_PROJECT_CONTEXT",
                    "The THEODEN project context could not be loaded."
                );

                return;
            }

            int initialErrorCount = report.ErrorCount;

            ValidateConfigurationAssets(context, report);
            ValidateRequiredPaths(context, report);
            ValidateLanguages(context, report);
            ValidatePois(context, report);

            /*
             * IsValid is an aggregate property. Normally, the checks above
             * should already explain why the context is invalid.
             *
             * This fallback prevents an invalid context from passing without
             * producing a meaningful validation issue.
             */
            if (!context.IsValid &&
                report.ErrorCount == initialErrorCount)
            {
                report.AddError(
                    "INVALID_PROJECT_CONTEXT",
                    "The selected THEODEN project context is not valid.",
                    context.projectFolderPath
                );
            }
        }

        /// <summary>
        /// Validates the configuration assets referenced by the project context.
        /// </summary>
        private static void ValidateConfigurationAssets(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (context.theodenProjectConfig == null)
            {
                report.AddError(
                    "MISSING_PROJECT_CONFIG",
                    "The selected project does not contain a TheodenProjectConfig.",
                    context.projectFolderPath
                );
            }

            if (context.languageConfig == null)
            {
                report.AddError(
                    "MISSING_LANGUAGE_CONFIG",
                    "The selected project does not contain a LanguageConfig.",
                    context.projectFolderPath
                );
            }

            if (context.poiRegistry == null)
            {
                report.AddError(
                    "MISSING_POI_REGISTRY",
                    "The selected project does not contain a POIRegistry.",
                    context.projectFolderPath
                );
            }
        }

        /// <summary>
        /// Validates that all paths required by the THEODEN authoring
        /// and build workflow are configured.
        /// </summary>
        private static void ValidateRequiredPaths(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            ValidateRequiredPath(
                context.projectFolderPath,
                nameof(context.projectFolderPath),
                context.projectFolderPath,
                report
            );

            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            if (projectConfig == null)
                return;

            ValidateRequiredPath(
                projectConfig.folderPath,
                nameof(projectConfig.folderPath),
                context.projectFolderPath,
                report
            );

            ValidateRequiredPath(
                projectConfig.configFolderPath,
                nameof(projectConfig.configFolderPath),
                context.projectFolderPath,
                report
            );

            ValidateRequiredPath(
                projectConfig.codexFolderPath,
                nameof(projectConfig.codexFolderPath),
                context.projectFolderPath,
                report
            );

            ValidateRequiredPath(
                projectConfig.directionsFolderPath,
                nameof(projectConfig.directionsFolderPath),
                context.projectFolderPath,
                report
            );

            ValidateRequiredPath(
                projectConfig.poisFolderPath,
                nameof(projectConfig.poisFolderPath),
                context.projectFolderPath,
                report
            );

            ValidateRequiredPath(
                projectConfig.mediaFolderPath,
                nameof(projectConfig.mediaFolderPath),
                context.projectFolderPath,
                report
            );

            ValidateRequiredPath(
                projectConfig.qrCodeFolderPath,
                nameof(projectConfig.qrCodeFolderPath),
                context.projectFolderPath,
                report
            );
        }

        /// <summary>
        /// Adds an error when a required project path is empty.
        /// </summary>
        private static void ValidateRequiredPath(
            string path,
            string fieldName,
            string projectFolderPath,
            TheodenValidationReport report)
        {
            if (!string.IsNullOrWhiteSpace(path))
                return;

            report.AddError(
                "MISSING_REQUIRED_PATH",
                $"The required project path '{fieldName}' is empty.",
                projectFolderPath
            );
        }

        /// <summary>
        /// Validates that the project defines at least one language.
        /// </summary>
        private static void ValidateLanguages(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (context.languageConfig == null)
                return;

            if (context.languageConfig.languages == null ||
                context.languageConfig.languages.Count == 0)
            {
                report.AddError(
                    "NO_CONFIGURED_LANGUAGES",
                    "The LanguageConfig does not contain any configured languages.",
                    context.projectFolderPath
                );
            }
        }

        /// <summary>
        /// Validates the registered POIs and their identifiers.
        /// </summary>
        private static void ValidatePois(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (context.poiRegistry == null)
                return;

            if (context.availablePois == null ||
                context.availablePois.Count == 0)
            {
                report.AddError(
                    "NO_REGISTERED_POIS",
                    "The POIRegistry does not contain any registered POIs.",
                    context.projectFolderPath
                );

                return;
            }

            HashSet<string> registeredIds =
                new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < context.availablePois.Count; i++)
            {
                POIRegistryEntry poi = context.availablePois[i];

                if (poi == null)
                {
                    report.AddError(
                        "NULL_POI_ENTRY",
                        $"The POIRegistry entry at index {i} is null.",
                        context.projectFolderPath
                    );

                    continue;
                }

                string poiId = poi.PoiId;

                if (string.IsNullOrWhiteSpace(poiId))
                {
                    report.AddError(
                        "EMPTY_POI_ID",
                        $"The POIRegistry entry at index {i} has an empty POI id.",
                        context.projectFolderPath
                    );

                    continue;
                }

                if (!registeredIds.Add(poiId))
                {
                    report.AddError(
                        "DUPLICATE_POI_ID",
                        $"The POI id '{poiId}' is registered more than once.",
                        context.projectFolderPath
                    );
                }
            }
        }
    }
}