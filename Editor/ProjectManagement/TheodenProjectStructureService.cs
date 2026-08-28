using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Addressing;
using Theoden.Editor.Export;
using Theoden.Editor.Import;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.ProjectManagement
{
    /// <summary>
    /// Describes one language that should remain active in a THEODEN project.
    /// </summary>
    public sealed class ProjectLanguageUpdate
    {
        public LanguageList Language { get; }
        public string DisplayedName { get; }

        public ProjectLanguageUpdate(
            LanguageList language,
            string displayedName)
        {
            Language = language;
            DisplayedName = displayedName;
        }
    }

    /// <summary>
    /// Describes a POI folder that still exists on disk but is no longer
    /// registered as part of the active THEODEN project.
    /// </summary>
    public sealed class InactivePoiFolder
    {
        /// <summary>
        /// Gets the stable POI identifier derived from the existing folder
        /// name.
        /// </summary>
        public string PoiId { get; }

        /// <summary>
        /// Gets the Unity project-relative path of the preserved POI folder.
        /// </summary>
        public string FolderPath { get; }

        /// <summary>
        /// Creates a description of an inactive POI folder.
        /// </summary>
        /// <param name="poiId">Stable ID read from the folder name.</param>
        /// <param name="folderPath">
        /// Unity project-relative path of the preserved folder.
        /// </param>
        public InactivePoiFolder(
            string poiId,
            string folderPath)
        {
            PoiId = poiId;
            FolderPath = folderPath;
        }
    }

    /// <summary>
    /// Applies structural changes to an existing THEODEN project.
    /// </summary>
    /// <remarks>
    /// This service updates configuration assets and creates missing POI
    /// folders. Removing a POI removes it from the active registry and from
    /// existing Codex files, while preserving its local POI and Directions
    /// content for recovery. Addressables cleanup is intentionally handled by
    /// a separate maintenance operation.
    /// </remarks>
    public static class TheodenProjectStructureService
    {
        /// <summary>
        /// Finds preserved POI folders that are not present in the active POI
        /// registry.
        /// </summary>
        /// <param name="projectConfig">
        /// Configuration of the THEODEN project to inspect.
        /// </param>
        /// <param name="inactiveFolders">
        /// Direct children of the configured POIs folder whose folder name is
        /// not currently registered as a POI ID.
        /// </param>
        /// <param name="error">Error message when discovery fails.</param>
        /// <returns>
        /// <c>true</c> when the POIs folder was inspected successfully;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool TryFindInactivePoiFolders(
            TheodenProjectConfig projectConfig,
            out List<InactivePoiFolder> inactiveFolders,
            out string error)
        {
            inactiveFolders = new List<InactivePoiFolder>();
            error = null;

            if (!TryValidateProjectConfiguration(
                    projectConfig,
                    out error))
            {
                return false;
            }

            string poisFolderPath = NormalizeUnityPath(
                projectConfig.poisFolderPath
            ).TrimEnd('/');

            if (!IsAssetFolder(poisFolderPath))
            {
                error =
                    "The project POIs folder is missing or invalid: " +
                    poisFolderPath;

                return false;
            }

            var registeredPoiIds = new HashSet<string>(
                projectConfig.poiRegistry.Pois
                    .Where(poi => poi != null)
                    .Select(poi => poi.PoiId),
                StringComparer.Ordinal
            );

            foreach (string childFolderPath in
                     AssetDatabase.GetSubFolders(poisFolderPath))
            {
                string normalizedFolderPath = NormalizeUnityPath(
                    childFolderPath
                ).TrimEnd('/');

                string poiId = Path.GetFileName(
                    normalizedFolderPath
                );

                if (string.IsNullOrWhiteSpace(poiId) ||
                    registeredPoiIds.Contains(poiId))
                {
                    continue;
                }

                inactiveFolders.Add(new InactivePoiFolder(
                    poiId,
                    normalizedFolderPath
                ));
            }

            inactiveFolders.Sort((left, right) =>
                string.Compare(
                    left.PoiId,
                    right.PoiId,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            return true;
        }

        /// <summary>
        /// Restores a preserved POI folder to the active project registry.
        /// Existing JSON, media, and other files are left untouched.
        /// </summary>
        /// <remarks>
        /// The existing folder name remains the stable POI ID. Missing Data
        /// and Media subfolders are recreated. A restored registry entry is
        /// initially unconfigured and must be added back to the desired Codex
        /// definitions explicitly.
        /// </remarks>
        /// <param name="projectConfig">
        /// Configuration of the THEODEN project to update.
        /// </param>
        /// <param name="poiId">
        /// Stable ID read from the preserved folder name.
        /// </param>
        /// <param name="displayName">
        /// User-facing name to store in the restored registry entry.
        /// </param>
        /// <param name="error">Error message when restoration fails.</param>
        /// <returns>
        /// <c>true</c> when the POI is restored successfully; otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool TryRestorePoi(
            TheodenProjectConfig projectConfig,
            string poiId,
            string displayName,
            out string error)
        {
            error = null;

            if (!TryValidateProjectConfiguration(
                    projectConfig,
                    out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(poiId))
            {
                error = "The preserved POI folder has an empty ID.";
                return false;
            }

            if (poiId.Equals(".", StringComparison.Ordinal) ||
                poiId.Equals("..", StringComparison.Ordinal) ||
                poiId.IndexOf('/') >= 0 ||
                poiId.IndexOf('\\') >= 0)
            {
                error =
                    $"POI ID '{poiId}' is not a valid folder name.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = "The POI display name cannot be empty.";
                return false;
            }

            if (projectConfig.poiRegistry.ContainsId(poiId))
            {
                error = $"POI '{poiId}' is already active.";
                return false;
            }

            string poisFolderPath = NormalizeUnityPath(
                projectConfig.poisFolderPath
            ).TrimEnd('/');

            if (!IsAssetFolder(poisFolderPath))
            {
                error =
                    "The project POIs folder is missing or invalid: " +
                    poisFolderPath;

                return false;
            }

            string poiFolderPath =
                $"{poisFolderPath}/{poiId}";

            if (!AssetDatabase.IsValidFolder(poiFolderPath))
            {
                error =
                    $"The preserved POI folder does not exist: " +
                    $"'{poiFolderPath}'.";

                return false;
            }

            if (!TryCreateFolderIfMissing(
                    poiFolderPath,
                    "Data",
                    out error) ||
                !TryCreateFolderIfMissing(
                    poiFolderPath,
                    "Media",
                    out error))
            {
                return false;
            }

            if (!projectConfig.poiRegistry.AddPoi(
                    poiId,
                    displayName.Trim(),
                    poiFolderPath))
            {
                error =
                    $"POI '{poiId}' could not be restored to the " +
                    "project registry.";

                return false;
            }

            EditorUtility.SetDirty(projectConfig.poiRegistry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        /// <summary>
        /// Adds a POI to the project registry and creates its Data and Media
        /// folders. Existing folders are reused without deleting their content.
        /// </summary>
        public static bool TryAddPoi(
            TheodenProjectConfig projectConfig,
            string displayName,
            out string createdPoiId,
            out string error)
        {
            createdPoiId = GeneratePoiId(displayName);
            error = null;

            if (!TryValidateProjectConfiguration(
                    projectConfig,
                    out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = "The POI display name cannot be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(createdPoiId))
            {
                error =
                    "The POI display name does not generate a valid ID.";

                return false;
            }

            if (projectConfig.poiRegistry.ContainsId(createdPoiId))
            {
                error =
                    $"A POI with ID '{createdPoiId}' already exists. " +
                    "Choose a different display name.";

                return false;
            }

            string poisFolderPath = NormalizeUnityPath(
                projectConfig.poisFolderPath
            ).TrimEnd('/');

            if (!IsAssetFolder(poisFolderPath))
            {
                error =
                    "The project POIs folder is missing or invalid: " +
                    poisFolderPath;

                return false;
            }

            string poiFolderPath =
                $"{poisFolderPath}/{createdPoiId}";

            if (!TryCreateFolderIfMissing(
                    poisFolderPath,
                    createdPoiId,
                    out error) ||
                !TryCreateFolderIfMissing(
                    poiFolderPath,
                    "Data",
                    out error) ||
                !TryCreateFolderIfMissing(
                    poiFolderPath,
                    "Media",
                    out error))
            {
                return false;
            }

            if (!projectConfig.poiRegistry.AddPoi(
                    createdPoiId,
                    displayName.Trim(),
                    poiFolderPath))
            {
                error =
                    $"The POI '{createdPoiId}' could not be added to " +
                    "the project registry.";

                return false;
            }

            EditorUtility.SetDirty(projectConfig.poiRegistry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        /// <summary>
        /// Updates only the user-facing POI name. The stable POI ID and folder
        /// paths are never renamed by this operation.
        /// </summary>
        public static bool TryUpdatePoiDisplayName(
            TheodenProjectConfig projectConfig,
            string poiId,
            string newDisplayName,
            out string error)
        {
            error = null;

            if (!TryValidateProjectConfiguration(
                    projectConfig,
                    out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newDisplayName))
            {
                error = "The POI display name cannot be empty.";
                return false;
            }

            if (!projectConfig.poiRegistry.UpdateDiplayedName(
                    poiId,
                    newDisplayName.Trim()))
            {
                error = $"POI '{poiId}' was not found in the registry.";
                return false;
            }

            EditorUtility.SetDirty(projectConfig.poiRegistry);
            AssetDatabase.SaveAssets();

            return true;
        }

        /// <summary>
        /// Removes a POI from the active registry and from every existing Codex
        /// definition. Local POI, Directions, media, and QR files are preserved.
        /// </summary>
        /// <remarks>
        /// Removal is blocked if it would leave the project without POIs or if
        /// one of the existing Codex files would become empty. This keeps the
        /// project in a state that can still pass structural validation.
        /// </remarks>
        public static bool TryRemovePoiFromActiveProject(
            TheodenProjectConfig projectConfig,
            string poiId,
            out string error)
        {
            error = null;

            if (!TryValidateProjectConfiguration(
                    projectConfig,
                    out error))
            {
                return false;
            }

            if (!projectConfig.poiRegistry.TryGetById(
                    poiId,
                    out POIRegistryEntry entry))
            {
                error = $"POI '{poiId}' was not found in the registry.";
                return false;
            }

            if (projectConfig.poiRegistry.Pois.Count <= 1)
            {
                error =
                    "A THEODEN project must contain at least one POI.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(projectConfig.projectId))
            {
                error = "The THEODEN project ID is missing.";
                return false;
            }

            if (!TryPrepareCodexUpdates(
                    projectConfig,
                    poiId,
                    out List<CodexUpdate> codexUpdates,
                    out error))
            {
                return false;
            }

            foreach (CodexUpdate update in codexUpdates)
            {
                string fileName =
                    TheodenFileNaming.GetCodexJsonFileName(
                        update.Language
                    );

                if (!CodexExportService.ExportCodex(
                        update.Menu,
                        projectConfig.projectId,
                        update.Language,
                        projectConfig.codexFolderPath,
                        fileName,
                        out error))
                {
                    error =
                        $"Could not update Codex '{fileName}' while " +
                        $"removing POI '{poiId}':\n{error}";

                    return false;
                }
            }

            if (!projectConfig.poiRegistry.RemovePoi(entry.PoiId))
            {
                error =
                    $"POI '{poiId}' could not be removed from the " +
                    "registry.";

                return false;
            }

            EditorUtility.SetDirty(projectConfig.poiRegistry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        /// <summary>
        /// Replaces the active language configuration while preserving the
        /// auxiliary data of LanguageEntry objects that remain active.
        /// </summary>
        public static bool TryApplyLanguages(
            TheodenProjectConfig projectConfig,
            IReadOnlyList<ProjectLanguageUpdate> selectedLanguages,
            out string error)
        {
            error = null;

            if (!TryValidateProjectConfiguration(
                    projectConfig,
                    out error))
            {
                return false;
            }

            if (projectConfig.languageConfig == null)
            {
                error = "The project LanguageConfig asset is missing.";
                return false;
            }

            if (selectedLanguages == null ||
                selectedLanguages.Count == 0)
            {
                error =
                    "A THEODEN project must contain at least one " +
                    "active language.";

                return false;
            }

            var languageIds = new HashSet<LanguageList>();

            foreach (ProjectLanguageUpdate update in selectedLanguages)
            {
                if (!languageIds.Add(update.Language))
                {
                    error =
                        $"Language '{update.Language}' appears more " +
                        "than once.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(update.DisplayedName))
                {
                    error =
                        $"Language '{update.Language}' has an empty " +
                        "display name.";

                    return false;
                }
            }

            projectConfig.languageConfig.languages ??=
                new List<LanguageEntry>();

            var existingEntries = new Dictionary<
                LanguageList,
                LanguageEntry>();

            foreach (LanguageEntry entry in
                     projectConfig.languageConfig.languages)
            {
                if (entry == null ||
                    existingEntries.ContainsKey(entry.language))
                {
                    continue;
                }

                existingEntries.Add(entry.language, entry);
            }

            var updatedEntries = new List<LanguageEntry>();

            foreach (ProjectLanguageUpdate update in selectedLanguages)
            {
                if (!existingEntries.TryGetValue(
                        update.Language,
                        out LanguageEntry entry))
                {
                    entry = new LanguageEntry
                    {
                        language = update.Language
                    };
                }

                string displayName = update.DisplayedName.Trim();

                entry.displayedName = displayName;
                entry.displayName = displayName;
                entry.code = update.Language.ToString();

                updatedEntries.Add(entry);
            }

            projectConfig.languageConfig.languages = updatedEntries;

            projectConfig.languages ??= new List<LanguageList>();
            projectConfig.languages.Clear();

            foreach (ProjectLanguageUpdate update in selectedLanguages)
                projectConfig.languages.Add(update.Language);

            EditorUtility.SetDirty(projectConfig.languageConfig);
            EditorUtility.SetDirty(projectConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        /// <summary>
        /// Generates the stable snake_case identifier used by the Setup Wizard
        /// and project-management tools for new POIs.
        /// </summary>
        public static string GeneratePoiId(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            string id = displayName.Trim().ToLowerInvariant();

            id = Regex.Replace(id, @"\s+", "_");
            id = Regex.Replace(id, @"[^a-z0-9_]", string.Empty);
            id = Regex.Replace(id, @"_+", "_");

            return id.Trim('_');
        }

        private static bool TryPrepareCodexUpdates(
            TheodenProjectConfig projectConfig,
            string removedPoiId,
            out List<CodexUpdate> updates,
            out string error)
        {
            updates = new List<CodexUpdate>();
            error = null;

            string codexFolderPath = NormalizeUnityPath(
                projectConfig.codexFolderPath
            ).TrimEnd('/');

            if (!IsAssetFolder(codexFolderPath))
            {
                error =
                    "The project Codex folder is missing or invalid: " +
                    codexFolderPath;

                return false;
            }

            foreach (LanguageList language in
                     Enum.GetValues(typeof(LanguageList)))
            {
                string fileName =
                    TheodenFileNaming.GetCodexJsonFileName(language);

                string jsonAssetPath =
                    $"{codexFolderPath}/{fileName}";

                if (AssetDatabase.LoadAssetAtPath<TextAsset>(
                        jsonAssetPath) == null)
                {
                    continue;
                }

                if (!CodexDefinitionLoadService.TryLoad(
                        jsonAssetPath,
                        language,
                        out CodexMenu menu,
                        out error))
                {
                    return false;
                }

                int removedCount = menu.items.RemoveAll(
                    item =>
                        item != null &&
                        string.Equals(
                            item.poiId,
                            removedPoiId,
                            StringComparison.Ordinal
                        )
                );

                if (removedCount == 0)
                    continue;

                if (menu.items.Count == 0)
                {
                    error =
                        $"Removing POI '{removedPoiId}' would leave " +
                        $"Codex '{fileName}' empty. Add another POI to " +
                        "that Codex before removing this one.";

                    return false;
                }

                updates.Add(new CodexUpdate(language, menu));
            }

            return true;
        }

        private static bool TryValidateProjectConfiguration(
            TheodenProjectConfig projectConfig,
            out string error)
        {
            error = null;

            if (projectConfig == null)
            {
                error = "The THEODEN project configuration is missing.";
                return false;
            }

            if (projectConfig.poiRegistry == null)
            {
                error = "The project POIRegistry asset is missing.";
                return false;
            }

            return true;
        }

        private static bool TryCreateFolderIfMissing(
            string parentPath,
            string folderName,
            out string error)
        {
            error = null;

            string fullPath = NormalizeUnityPath(
                $"{parentPath}/{folderName}"
            );

            if (AssetDatabase.IsValidFolder(fullPath))
                return true;

            if (!AssetDatabase.IsValidFolder(parentPath))
            {
                error =
                    $"Parent folder does not exist: '{parentPath}'.";

                return false;
            }

            string guid = AssetDatabase.CreateFolder(
                parentPath,
                folderName
            );

            if (string.IsNullOrWhiteSpace(guid) ||
                !AssetDatabase.IsValidFolder(fullPath))
            {
                error = $"Could not create folder '{fullPath}'.";
                return false;
            }

            return true;
        }

        private static bool IsAssetFolder(string path)
        {
            return
                IsAssetPath(path) &&
                AssetDatabase.IsValidFolder(path);
        }

        private static bool IsAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string normalizedPath = NormalizeUnityPath(path);

            return normalizedPath.Equals(
                       "Assets",
                       StringComparison.Ordinal) ||
                   normalizedPath.StartsWith(
                       "Assets/",
                       StringComparison.Ordinal
                   );
        }

        private static string NormalizeUnityPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }

        private sealed class CodexUpdate
        {
            public LanguageList Language { get; }
            public CodexMenu Menu { get; }

            public CodexUpdate(
                LanguageList language,
                CodexMenu menu)
            {
                Language = language;
                Menu = menu;
            }
        }
    }
}
