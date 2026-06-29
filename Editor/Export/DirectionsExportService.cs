using System;
using System.IO;
using Addressing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RuntimeModelsForEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor.Export
{
    /// <summary>
    /// Central service responsible for exporting directions data into a runtime JSON file
    /// and registering both referenced media assets and the exported JSON in Addressables.
    /// </summary>
    /// <remarks>
    /// This service coordinates the complete directions export pipeline.
    ///
    /// The export process follows these steps:
    ///
    /// 1. Register all media assets referenced by the directions data as Addressables.
    /// 2. Serialize the directions data into a JSON file.
    /// 3. Convert Unity asset references, such as Sprite and AudioClip fields, into Addressables
    ///    address strings during JSON serialization.
    /// 4. Register the exported JSON file itself as an Addressable asset.
    ///
    /// The resulting JSON is intended to be loaded at runtime through Addressables.
    /// The media references inside the JSON are also Addressables addresses.
    /// </remarks>
    public static class DirectionsExportService
    {
        /// <summary>
        /// Exports directions data to JSON and registers all related assets in Addressables.
        /// </summary>
        /// <param name="directionsData">
        /// The directions export data containing text, selected sprites, and optional audio.
        /// </param>
        /// <param name="poiId">
        /// The unique identifier of the target Point of Interest.
        /// </param>
        /// <param name="language">
        /// The selected language of the exported directions JSON.
        /// </param>
        /// <param name="directionsFolderPath">
        /// Unity project-relative folder where the directions JSON file should be written.
        /// Expected format: Assets/&lt;ProjectName&gt;/Directions.
        /// </param>
        /// <param name="mediaFolderPath">
        /// Unity project-relative folder where selected media assets must be located.
        /// Expected format: Assets/&lt;ProjectName&gt;/Media.
        /// </param>
        /// <param name="fileName">
        /// Name of the JSON file to export, with or without the .json extension.
        /// </param>
        /// <param name="error">
        /// Output error message if export fails.
        /// </param>
        /// <returns>
        /// True if export completed successfully; otherwise false.
        /// </returns>
        public static bool ExportDirections(
            DirectionsToPOIData directionsData,
            string poiId,
            LanguageList language,
            string directionsFolderPath,
            string mediaFolderPath,
            string fileName,
            out string error)
        {
            error = null;

            if (directionsData == null)
            {
                error = "Directions data is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(poiId))
            {
                error = "POI id is missing.";
                return false;
            }

            directionsFolderPath = NormalizeUnityPath(directionsFolderPath);
            mediaFolderPath = NormalizeUnityPath(mediaFolderPath);

            if (!DirectionsAddressablesSetupService.SetupAddressablesForDirections(
                    directionsData,
                    poiId,
                    mediaFolderPath,
                    out error))
            {
                return false;
            }

            if (!WriteJsonFile(
                    directionsData,
                    directionsFolderPath,
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
        /// Serializes the directions data and writes the resulting JSON file to disk.
        /// </summary>
        /// <param name="directionsData">
        /// The directions data to serialize.
        /// </param>
        /// <param name="assetFolderPath">
        /// Unity project-relative folder where the JSON file should be written.
        /// </param>
        /// <param name="fileName">
        /// JSON file name, with or without the .json extension.
        /// </param>
        /// <param name="jsonAssetPath">
        /// Output Unity project-relative path of the exported JSON asset.
        /// </param>
        /// <param name="error">
        /// Output error message if writing fails.
        /// </param>
        /// <returns>
        /// True if the JSON file was written successfully; otherwise false.
        /// </returns>
        private static bool WriteJsonFile(
            DirectionsToPOIData directionsData,
            string assetFolderPath,
            string fileName,
            out string jsonAssetPath,
            out string error)
        {
            jsonAssetPath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                error = "Directions export folder is not set.";
                return false;
            }

            if (!assetFolderPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                !assetFolderPath.Equals("Assets", StringComparison.Ordinal))
            {
                error = "Directions export path must be inside Assets/.";
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

                string json = Serialize(directionsData);

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
        /// Registers the exported directions JSON file as an Addressable asset.
        /// </summary>
        /// <param name="jsonAssetPath">
        /// Unity project-relative path of the exported JSON file.
        /// </param>
        /// <param name="poiId">
        /// The unique identifier of the target Point of Interest.
        /// </param>
        /// <param name="language">
        /// The selected language of the exported directions JSON.
        /// </param>
        /// <param name="error">
        /// Output error message if setup fails.
        /// </param>
        /// <returns>
        /// True if the JSON was successfully registered as Addressable; otherwise false.
        /// </returns>
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

            string groupName = TheodenAddressablesNaming.GetDirectionsGroupName(poiId);
            string label = TheodenAddressablesNaming.GetDirectionsLabel(poiId);

            AddressableAssetGroup group = settings.FindGroup(groupName);

            if (group == null)
            {
                error = $"Addressables group '{groupName}' not found.";
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(jsonAssetPath);

            if (string.IsNullOrWhiteSpace(guid))
            {
                error = $"Could not find GUID for exported directions JSON: {jsonAssetPath}";
                return false;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);

            entry.address = TheodenAddressablesNaming.GetDirectionsJsonAddress(poiId, language);
            entry.SetLabel(label, true, true);

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.EntryMoved,
                group,
                true
            );

            return true;
        }

        /// <summary>
        /// Serializes directions data using Json.NET and converts Unity asset references
        /// into Addressables addresses.
        /// </summary>
        /// <param name="directionsData">
        /// Directions data to serialize.
        /// </param>
        /// <returns>
        /// A formatted JSON string representing the exported directions data.
        /// </returns>
        private static string Serialize(DirectionsToPOIData directionsData)
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

            return JsonConvert.SerializeObject(directionsData, settings);
        }

        /// <summary>
        /// Converts a Unity project-relative asset path into an absolute file system path.
        /// </summary>
        private static string ToAbsolutePath(string assetFolderPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetFolderPath);
        }

        /// <summary>
        /// Normalizes a path to Unity's preferred forward-slash format.
        /// </summary>
        private static string NormalizeUnityPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace("\\", "/");
        }

        /// <summary>
        /// Removes invalid file name characters from a file name.
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "directions.json";

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(invalidChar, '_');

            return fileName.Trim();
        }
    }
}