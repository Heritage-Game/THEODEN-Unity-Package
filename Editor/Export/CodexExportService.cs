using System;
using System.IO;
using Addressing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor.Export
{
    /// <summary>
    /// Service responsible for exporting codex menu data to JSON
    /// and registering the exported JSON as an Addressable asset.
    /// </summary>
    public static class CodexExportService
    {
        /// <summary>
        /// Exports a codex menu JSON file and registers it in Addressables.
        /// </summary>
        /// <param name="menuData">The codex menu data to export.</param>
        /// <param name="language">The language of the codex file.</param>
        /// <param name="codexFolderPath">Unity project-relative Codex folder path.</param>
        /// <param name="fileName">JSON file name, with or without .json extension.</param>
        /// <param name="error">Output error message if export fails.</param>
        /// <returns>True if export succeeds; otherwise false.</returns>
        public static bool ExportCodex(
            CodexMenu menuData,
            LanguageList language,
            string codexFolderPath,
            string fileName,
            out string error)
        {
            error = null;

            if (menuData == null)
            {
                error = "Codex menu data is null.";
                return false;
            }

            codexFolderPath = NormalizeUnityPath(codexFolderPath);

            if (!WriteJsonFile(
                    menuData,
                    codexFolderPath,
                    fileName,
                    out string jsonAssetPath,
                    out error))
            {
                return false;
            }

            if (!SetupJsonAsAddressable(
                    jsonAssetPath,
                    language,
                    out error))
            {
                return false;
            }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            return true;
        }

        /// <summary>
        /// Serializes the codex menu and writes it to disk.
        /// </summary>
        private static bool WriteJsonFile(
            CodexMenu menuData,
            string assetFolderPath,
            string fileName,
            out string jsonAssetPath,
            out string error)
        {
            jsonAssetPath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                error = "Codex export folder is not set.";
                return false;
            }

            if (!assetFolderPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !assetFolderPath.Equals("Assets", StringComparison.Ordinal))
            {
                error = "Codex export path must be inside Assets/.";
                return false;
            }

            try
            {
                string absoluteFolderPath = ToAbsolutePath(assetFolderPath);

                if (!Directory.Exists(absoluteFolderPath))
                    Directory.CreateDirectory(absoluteFolderPath);

                fileName = SanitizeFileName(fileName);

                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    fileName += ".json";

                string absoluteFilePath = Path.Combine(absoluteFolderPath, fileName);

                string json = Serialize(menuData);

                File.WriteAllText(absoluteFilePath, json);

                jsonAssetPath = NormalizeUnityPath(Path.Combine(assetFolderPath, fileName));

                AssetDatabase.ImportAsset(jsonAssetPath);
                AssetDatabase.Refresh();

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Registers the exported codex JSON as an Addressable asset.
        /// </summary>
        private static bool SetupJsonAsAddressable(
            string jsonAssetPath,
            LanguageList language,
            out string error)
        {
            error = null;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                error = "Addressables settings not found.";
                return false;
            }

            string groupName = TheodenAddressablesNaming.GetCodexGroupName();
            string label = TheodenAddressablesNaming.GetCodexLabel();

            AddressableAssetGroup group = GetOrCreateGroup(settings, groupName);
            
            if (!AddressablesGroupPathConfigurator.ConfigureGroupPaths(settings, group))
            {
                error = $"Could not configure Addressables paths for group: {groupName}";
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(jsonAssetPath);

            if (string.IsNullOrWhiteSpace(guid))
            {
                error = $"Could not find GUID for exported codex JSON: {jsonAssetPath}";
                return false;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

            entry.address = TheodenAddressablesNaming.GetCodexJsonAddress(language);
            entry.SetLabel(label, true, true);

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                group,
                true
            );

            return true;
        }

        /// <summary>
        /// Gets an existing Addressables group or creates it if missing.
        /// </summary>
        private static AddressableAssetGroup GetOrCreateGroup(
            AddressableAssetSettings settings,
            string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);

            if (group != null)
                return group;

            return settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                settings.DefaultGroup.Schemas
            );
        }

        /// <summary>
        /// Serializes codex menu data using Json.NET.
        /// </summary>
        private static string Serialize(CodexMenu menuData)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters =
                {
                    new StringEnumConverter()
                }
            };

            return JsonConvert.SerializeObject(menuData, settings);
        }

        /// <summary>
        /// Converts a Unity project-relative path into an absolute file system path.
        /// </summary>
        private static string ToAbsolutePath(string assetFolderPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetFolderPath);
        }

        /// <summary>
        /// Normalizes a Unity path to forward-slash format.
        /// </summary>
        private static string NormalizeUnityPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }

        /// <summary>
        /// Sanitizes a file name by replacing invalid characters.
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "codex.json";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            return fileName.Trim();
        }
    }
}