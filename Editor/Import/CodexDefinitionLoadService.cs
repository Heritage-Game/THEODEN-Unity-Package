using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Import
{
    /// <summary>
    /// Loads an exported Codex JSON file back into its editor-side model.
    /// </summary>
    public static class CodexDefinitionLoadService
    {
        /// <summary>
        /// Loads a Codex definition and verifies that its stored language
        /// matches the language selected in the editor window.
        /// </summary>
        /// <param name="jsonAssetPath">
        /// Unity project-relative path of the Codex JSON file.
        /// </param>
        /// <param name="expectedLanguage">
        /// Language currently selected in the Codex editor.
        /// </param>
        /// <param name="menuData">
        /// Deserialized Codex menu when the operation succeeds.
        /// </param>
        /// <param name="error">
        /// Human-readable error when the operation fails.
        /// </param>
        /// <returns>True when the file was loaded and validated.</returns>
        public static bool TryLoad(
            string jsonAssetPath,
            LanguageList expectedLanguage,
            out CodexMenu menuData,
            out string error)
        {
            menuData = null;
            error = null;

            if (!IsAssetPath(jsonAssetPath))
            {
                error =
                    "The Codex JSON path must be inside Assets: " +
                    $"'{jsonAssetPath}'.";

                return false;
            }

            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(jsonAssetPath);

            if (jsonAsset == null)
            {
                error = $"Codex JSON not found at '{jsonAssetPath}'.";
                return false;
            }

            try
            {
                JObject root = JObject.Parse(jsonAsset.text);

                if (root["language"] == null)
                {
                    error =
                        "The Codex JSON does not contain a language.";

                    return false;
                }

                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.None,
                    ObjectCreationHandling =
                        ObjectCreationHandling.Replace,
                    MissingMemberHandling =
                        MissingMemberHandling.Ignore,
                    Converters =
                    {
                        new StringEnumConverter()
                    }
                };

                menuData = JsonConvert.DeserializeObject<CodexMenu>(
                    jsonAsset.text,
                    settings
                );

                if (menuData == null)
                {
                    error = "The Codex JSON could not be deserialized.";
                    return false;
                }

                if (!menuData.language.Equals(expectedLanguage))
                {
                    error =
                        $"The selected language is '{expectedLanguage}', " +
                        $"but the Codex JSON language is " +
                        $"'{menuData.language}'.";

                    menuData = null;
                    return false;
                }

                menuData.items ??= new List<CodexItem>();
                return true;
            }
            catch (JsonException exception)
            {
                error =
                    $"Invalid Codex JSON '{jsonAssetPath}':\n" +
                    exception.Message;

                menuData = null;
                return false;
            }
            catch (Exception exception)
            {
                error =
                    $"Failed to load Codex JSON " +
                    $"'{jsonAssetPath}':\n" + exception.Message;

                menuData = null;
                return false;
            }
        }

        private static bool IsAssetPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string normalizedPath = path.Replace("\\", "/");

            return normalizedPath.Equals(
                       "Assets",
                       StringComparison.Ordinal) ||
                   normalizedPath.StartsWith(
                       "Assets/",
                       StringComparison.Ordinal
                   );
        }
    }
}