using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Theoden.Editor.POIDefinitionClasses;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.Import
{
    public static class PoiDefinitionLoadService
    {
        public static bool TryLoad(
            string jsonAssetPath,
            string expectedPoiId,
            LanguageList expectedLanguage,
            out LevelTemplateBase template,
            out Type templateType,
            out string error)
        {
            template = null;
            templateType = null;
            error = null;

            if (string.IsNullOrWhiteSpace(jsonAssetPath))
            {
                error = "The POI JSON path is empty.";
                return false;
            }

            if (!jsonAssetPath.Equals(
                    "Assets",
                    StringComparison.Ordinal) &&
                !jsonAssetPath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal))
            {
                error =
                    $"The POI JSON must be inside Assets: " +
                    $"'{jsonAssetPath}'.";

                return false;
            }

            TextAsset jsonAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    jsonAssetPath
                );

            if (jsonAsset == null)
            {
                error =
                    $"POI JSON not found at '{jsonAssetPath}'.";

                return false;
            }

            try
            {
                JObject root = JObject.Parse(jsonAsset.text);

                string challengeTypeId =
                    root.SelectToken("challenge.type")?.Value<string>();

                if (!PoiTemplateTypeRegistry.TryGetTemplateType(
                        challengeTypeId,
                        out templateType,
                        out error))
                {
                    return false;
                }

                if (!ValidateIdentity(
                        root,
                        expectedPoiId,
                        expectedLanguage,
                        out error))
                {
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
                        new StringEnumConverter(),
                        new AddressableAssetReferenceJsonConverter()
                    }
                };

                template =
                    JsonConvert.DeserializeObject(
                        jsonAsset.text,
                        templateType,
                        settings
                    ) as LevelTemplateBase;

                if (template == null)
                {
                    error =
                        $"The JSON could not be loaded as " +
                        $"'{templateType.Name}'.";

                    return false;
                }

                return true;
            }
            catch (JsonException exception)
            {
                error =
                    $"Invalid POI JSON '{jsonAssetPath}':\n" +
                    exception.Message;

                template = null;
                templateType = null;
                return false;
            }
            catch (Exception exception)
            {
                error =
                    $"Failed to load POI JSON '{jsonAssetPath}':\n" +
                    exception.Message;

                template = null;
                templateType = null;
                return false;
            }
        }

        private static bool ValidateIdentity(
            JObject root,
            string expectedPoiId,
            LanguageList expectedLanguage,
            out string error)
        {
            error = null;

            string jsonPoiId =
                root.SelectToken("poi.poiId")?.Value<string>();

            if (string.IsNullOrWhiteSpace(jsonPoiId))
            {
                error =
                    "The POI JSON does not contain poi.poiId.";

                return false;
            }

            if (!string.Equals(
                    jsonPoiId,
                    expectedPoiId,
                    StringComparison.Ordinal))
            {
                error =
                    $"The selected POI is '{expectedPoiId}', but the " +
                    $"JSON belongs to '{jsonPoiId}'.";

                return false;
            }

            string jsonLanguage =
                root["language"]?.Value<string>();

            if (!Enum.TryParse(
                    jsonLanguage,
                    true,
                    out LanguageList parsedLanguage) ||
                !Enum.IsDefined(
                    typeof(LanguageList),
                    parsedLanguage))
            {
                error =
                    $"The POI JSON contains an invalid language: " +
                    $"'{jsonLanguage}'.";

                return false;
            }

            if (!parsedLanguage.Equals(expectedLanguage))
            {
                error =
                    $"The selected language is '{expectedLanguage}', " +
                    $"but the JSON language is '{parsedLanguage}'.";

                return false;
            }

            return true;
        }
    }
}