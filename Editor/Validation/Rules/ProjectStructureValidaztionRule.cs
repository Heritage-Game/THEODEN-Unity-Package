using System;
using UnityEditor;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Validates the folder structure of a THEODEN project.
    /// </summary>
    public sealed class ProjectStructureValidationRule
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
             * Missing context and configuration assets are handled by
             * ProjectConfigurationValidationRule.
             */
            if (context == null)
                return;

            ValidateFolder(
                context.projectFolderPath,
                "project root",
                "MISSING_PROJECT_FOLDER",
                report
            );

            TheodenProjectConfig projectConfig =
                context.theodenProjectConfig;

            if (projectConfig == null)
                return;

            ValidateFolder(
                projectConfig.configFolderPath,
                "configuration",
                "MISSING_CONFIG_FOLDER",
                report
            );

            ValidateFolder(
                projectConfig.codexFolderPath,
                "Codex",
                "MISSING_CODEX_FOLDER",
                report
            );

            ValidateFolder(
                projectConfig.directionsFolderPath,
                "Directions",
                "MISSING_DIRECTIONS_FOLDER",
                report
            );

            bool poisFolderExists = ValidateFolder(
                projectConfig.poisFolderPath,
                "POIs",
                "MISSING_POIS_FOLDER",
                report
            );

            ValidateFolder(
                projectConfig.mediaFolderPath,
                "application media",
                "MISSING_MEDIA_FOLDER",
                report
            );

            ValidateFolder(
                projectConfig.qrCodeFolderPath,
                "QR codes",
                "MISSING_QR_CODES_FOLDER",
                report
            );

            /*
             * Child POI folders cannot be validated reliably when their
             * parent folder does not exist.
             */
            if (poisFolderExists)
                ValidatePoiFolders(context, report);
        }

        /// <summary>
        /// Validates the folder structure associated with every registered POI.
        /// </summary>
        private static void ValidatePoiFolders(
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (context.availablePois == null)
                return;

            string poisRootPath =
                context.theodenProjectConfig.poisFolderPath;

            foreach (POIRegistryEntry poi in context.availablePois)
            {
                /*
                 * Null entries and empty POI identifiers are handled by
                 * ProjectConfigurationValidationRule.
                 */
                if (poi == null ||
                    string.IsNullOrWhiteSpace(poi.PoiId))
                {
                    continue;
                }

                string poiFolderPath = CombineAssetPath(
                    poisRootPath,
                    poi.PoiId
                );

                bool poiFolderExists = ValidateFolder(
                    poiFolderPath,
                    $"POI '{poi.PoiId}'",
                    "MISSING_POI_FOLDER",
                    report
                );

                /*
                 * Avoid reporting missing Data and Media folders when the
                 * entire POI folder is already missing.
                 */
                if (!poiFolderExists)
                    continue;

                string dataFolderPath = CombineAssetPath(
                    poiFolderPath,
                    "Data"
                );

                string mediaFolderPath = CombineAssetPath(
                    poiFolderPath,
                    "Media"
                );

                ValidateFolder(
                    dataFolderPath,
                    $"data folder for POI '{poi.PoiId}'",
                    "MISSING_POI_DATA_FOLDER",
                    report
                );

                ValidateFolder(
                    mediaFolderPath,
                    $"media folder for POI '{poi.PoiId}'",
                    "MISSING_POI_MEDIA_FOLDER",
                    report
                );
            }
        }

        /// <summary>
        /// Checks whether a Unity project-relative folder exists.
        /// </summary>
        /// <param name="folderPath">The folder path to validate.</param>
        /// <param name="folderDescription">
        /// A user-facing description of the folder.
        /// </param>
        /// <param name="errorCode">
        /// The validation code used when the folder is missing.
        /// </param>
        /// <param name="report">
        /// The report to which the error is added.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the folder exists;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        private static bool ValidateFolder(
            string folderPath,
            string folderDescription,
            string errorCode,
            TheodenValidationReport report)
        {
            /*
             * Empty paths are already reported by
             * ProjectConfigurationValidationRule.
             */
            if (string.IsNullOrWhiteSpace(folderPath))
                return false;

            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            report.AddError(
                errorCode,
                $"The {folderDescription} folder does not exist at the expected path.",
                folderPath
            );

            return false;
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