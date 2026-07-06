using System;
using System.IO;
using Addressing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Theoden.Editor.Export
{
    /// <summary>
    /// Central service responsible for exporting a POI template into a runtime JSON file
    /// and registering both media assets and the exported JSON inside the Addressables system.
    /// </summary>
    /// <remarks>
    /// This service coordinates the complete POI export pipeline.
    ///
    /// It does not represent UI logic and should not be called directly from low-level drawers
    /// or template classes. The editor window should collect the required user selections
    /// and then call this service to perform the actual export.
    ///
    /// The export process follows these main steps:
    ///
    /// 1. Prepare all media assets referenced by the template as Addressables.
    /// 2. Serialize the template into a JSON file.
    /// 3. Convert Unity asset references, such as Sprite and AudioClip fields, into Addressables
    ///    address strings during JSON serialization.
    /// 4. Register the exported JSON file itself as an Addressable asset.
    ///
    /// The resulting JSON file is intended to be loaded at runtime through Addressables.
    /// Media references inside the JSON are also stored as Addressables addresses, so runtime
    /// systems can load them using Addressables.LoadAssetAsync.
    /// </remarks>
    public static class PoiExportService
    {
        /// <summary>
        /// Exports a POI template to JSON and registers the required assets in Addressables.
        /// </summary>
        /// <param name="template">
        /// The level template instance to export. This is usually the concrete object stored
        /// inside a LevelDefinitionTemplateSO managed reference.
        /// </param>
        /// <param name="poiId">
        /// The unique identifier of the point of interest being exported.
        /// This value is used for Addressables group naming, labels, and address generation.
        /// </param>
        /// <param name="language">
        /// The selected language for the exported POI JSON.
        /// This value is used to build the JSON Addressables address.
        /// </param>
        /// <param name="poiRootFolderPath">
        /// The Unity project-relative root folder of the POI.
        /// For example: Assets/MyProject/POIs/roman_empire.
        /// </param>
        /// <param name="jsonExportFolderPath">
        /// The Unity project-relative folder where the JSON file should be written.
        /// For example: Assets/MyProject/POIs/roman_empire/Data.
        /// </param>
        /// <param name="fileName">
        /// The exported JSON file name, with or without the .json extension.
        /// For example: roman_empire_ENG.
        /// </param>
        /// <param name="error">
        /// Output error message if the export fails.
        /// </param>
        /// <returns>
        /// True if the export completed successfully; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This is the main entry point of the export pipeline.
        ///
        /// The method first prepares Addressables entries for all Unity assets referenced by the template.
        /// This must happen before JSON serialization because the JSON converter expects those assets
        /// to already have valid Addressables entries.
        ///
        /// After the media setup, the method writes the JSON file to disk and then registers that JSON
        /// file itself as an Addressable asset. This allows the runtime to load the POI data using
        /// a predictable address such as:
        ///
        /// poi/{poiId}/json/{poiId}_{language}
        /// </remarks>
        public static bool ExportPoi(
            LevelTemplateBase template,
            string poiId,
            LanguageList language,
            string poiRootFolderPath,
            string jsonExportFolderPath,
            string fileName,
            out string error)
        {
            error = null;

            if (template == null)
            {
                error = "Template is null.";
                return false;
            }

            poiRootFolderPath = NormalizeUnityPath(poiRootFolderPath);
            jsonExportFolderPath = NormalizeUnityPath(jsonExportFolderPath);

            if (!PoiAddressablesSetupService.SetupAddressablesForTemplate(
                    template,
                    poiId,
                    poiRootFolderPath,
                    out error))
            {
                return false;
            }

            if (!WriteJsonFile(
                    template,
                    jsonExportFolderPath,
                    fileName,
                    out string jsonAssetPath,
                    out error))
            {
                return false;
            }

            if (!SetupJsonAsAddressable(
                    jsonAssetPath,
                    poiId,
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
        /// Serializes the template and writes the resulting JSON file to disk.
        /// </summary>
        /// <param name="template">
        /// The template object to serialize.
        /// </param>
        /// <param name="assetFolderPath">
        /// Unity project-relative folder where the JSON file should be written.
        /// The path must be inside the Assets folder.
        /// </param>
        /// <param name="fileName">
        /// Name of the JSON file, with or without the .json extension.
        /// </param>
        /// <param name="jsonAssetPath">
        /// Output Unity project-relative path of the exported JSON asset.
        /// For example: Assets/MyProject/POIs/roman_empire/Data/roman_empire_ENG.json.
        /// </param>
        /// <param name="error">
        /// Output error message if writing the JSON file fails.
        /// </param>
        /// <returns>
        /// True if the JSON file was written successfully; otherwise, false.
        /// </returns>
        /// <remarks>
        /// This method only writes the JSON file to disk.
        /// It does not mark the JSON file as Addressable. That responsibility belongs to
        /// SetupJsonAsAddressable.
        ///
        /// During serialization, Unity asset references are converted into Addressables address strings
        /// by AddressableAssetJsonConverter.
        /// </remarks>
        private static bool WriteJsonFile(
            LevelTemplateBase template,
            string assetFolderPath,
            string fileName,
            out string jsonAssetPath,
            out string error)
        {
            jsonAssetPath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                error = "Export folder is not set.";
                return false;
            }

            if (!assetFolderPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !assetFolderPath.Equals("Assets", StringComparison.Ordinal))
            {
                error = "Export path must be inside Assets/.";
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

                string json = Serialize(template);

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
        /// Registers the exported JSON file as an Addressable asset.
        /// </summary>
        /// <param name="jsonAssetPath">
        /// Unity project-relative path of the exported JSON file.
        /// </param>
        /// <param name="poiId">
        /// Unique identifier of the POI. Used to find the correct Addressables group and assign labels.
        /// </param>
        /// <param name="language">
        /// Language of the exported JSON. Used to generate the JSON Addressables address.
        /// </param>
        /// <param name="error">
        /// Output error message if the JSON file cannot be registered as Addressable.
        /// </param>
        /// <returns>
        /// True if the JSON was successfully registered as Addressable; otherwise, false.
        /// </returns>
        /// <remarks>
        /// The JSON file is added to the same POI-specific Addressables group used for the media assets.
        ///
        ///The assigned address follows the shared naming convention defined by <see cref="TheodenAddressablesNaming"/>.
        /// For example:
        ///
        /// poi/roman_empire/json/roman_empire_ENG
        ///
        /// This address can later be reconstructed by the runtime after scanning a QR code containing
        /// only the POI id.
        /// </remarks>
        private static bool SetupJsonAsAddressable(
            string jsonAssetPath,
            string poiId,
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

            string groupName = TheodenAddressablesNaming.GetPoiGroupName(poiId);
            string label = TheodenAddressablesNaming.GetPoiLabel(poiId);

            AddressableAssetGroup group = settings.FindGroup(groupName);

            if (group == null)
            {
                error = $"Addressables group '{groupName}' not found.";
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(jsonAssetPath);

            if (string.IsNullOrWhiteSpace(guid))
            {
                error = $"Could not find GUID for exported JSON: {jsonAssetPath}";
                return false;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

            entry.address = TheodenAddressablesNaming.GetPoiJsonAddress(poiId, language);
            entry.SetLabel(label, true, true);

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                group,
                true
            );

            return true;
        }

        /// <summary>
        /// Serializes a level template into a formatted JSON string.
        /// </summary>
        /// <param name="template">
        /// The template to serialize.
        /// </param>
        /// <returns>
        /// A formatted JSON string representing the exported template.
        /// </returns>
        /// <remarks>
        /// This method uses Json.NET instead of Unity's JsonUtility because the template structure
        /// can contain inheritance, managed references, nested classes, and custom conversion logic.
        ///
        /// The AddressableAssetJsonConverter is especially important here. It converts UnityEngine.Object
        /// references, such as Sprite and AudioClip, into Addressables address strings.
        ///
        /// The StringEnumConverter makes enum values, such as LanguageList, appear as readable strings
        /// instead of integer values.
        /// </remarks>
        private static string Serialize(LevelTemplateBase template)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters =
                {
                    new StringEnumConverter(),
                    new AddressableAssetJsonConverter()
                }
            };

            return JsonConvert.SerializeObject(template, settings);
        }

        /// <summary>
        /// Converts a Unity project-relative asset path into an absolute file system path.
        /// </summary>
        /// <param name="assetFolderPath">
        /// Unity project-relative path starting with Assets.
        /// </param>
        /// <returns>
        /// Absolute file system path corresponding to the given Unity asset path.
        /// </returns>
        /// <remarks>
        /// Unity APIs usually work with paths relative to the project folder, such as:
        ///
        /// Assets/MyProject/POIs/roman_empire/Data
        ///
        /// System.IO APIs require absolute file system paths instead.
        /// This method bridges the two path formats.
        /// </remarks>
        private static string ToAbsolutePath(string assetFolderPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetFolderPath);
        }

        /// <summary>
        /// Normalizes a path to Unity's preferred forward-slash format.
        /// </summary>
        /// <param name="path">
        /// Path to normalize.
        /// </param>
        /// <returns>
        /// The normalized path using forward slashes.
        /// </returns>
        /// <remarks>
        /// On Windows, Path.Combine can generate backslashes.
        /// Unity asset paths are more reliable when written with forward slashes.
        /// </remarks>
        private static string NormalizeUnityPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }

        /// <summary>
        /// Removes invalid file name characters from a file name.
        /// </summary>
        /// <param name="fileName">
        /// File name to sanitize.
        /// </param>
        /// <returns>
        /// A safe file name that can be used on the file system.
        /// </returns>
        /// <remarks>
        /// This method only sanitizes the file name, not a full path.
        /// It replaces invalid file name characters with underscores.
        /// </remarks>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "poi.json";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            return fileName.Trim();
        }
    }
}