using System;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Theoden.Editor.POIDefinitionClasses
{
    public static class LevelDefinitionJsonExporter
    {
        public static bool Export(
            LevelDefinitionTemplateSO so,
            string assetFolderPath,
            string fileName,
            out string error)
        {
            error = null;

            if (so == null)
            {
                error = "Level Definition is null.";
                return false;
            }

            if (so.template == null)
            {
                error = "Level template data is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(assetFolderPath))
            {
                error = "Export folder is not set.";
                return false;
            }

            if (!assetFolderPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                error = "Export path must be inside Assets/.";
                return false;
            }

            try
            {
                string fullFolderPath = ToAbsolutePath(assetFolderPath);

                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    fileName += ".json";

                string fullPath = Path.Combine(fullFolderPath, fileName);

                //Serialize the template inside the scriptable object
                string json = Serialize(so.template);

                Debug.Log("FULL EXPORT PATH: " + fullPath);

                File.WriteAllText(fullPath, json);
                AssetDatabase.Refresh();

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string ToAbsolutePath(string assetFolderPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetFolderPath);
        }

        private static string Serialize(LevelTemplateBase template)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return JsonConvert.SerializeObject(template, settings);
        }
    }
}