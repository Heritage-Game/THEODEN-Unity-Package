using System;
using UnityEditor;
using UnityEngine;

namespace Editor.POIDefinitionClasses
{
    public static class MetaDataHandler
    {

        public static string TryInsertMetadata(SerializedProperty templateProperty)
        {
            var dateProperty = FindDateProperty(templateProperty);
            var versionProperty = FindAndSetVersionProperty(templateProperty);
            if (dateProperty == null)
            {
                Debug.Log("No Date property found for " + templateProperty.name);
                return null;
            }
            
            dateProperty.stringValue = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return dateProperty.stringValue;
            
        }

        private static SerializedProperty FindAndSetVersionProperty(SerializedProperty templateProperty)
        {
            var versionProperty = templateProperty.FindPropertyRelative("metadata")
                .FindPropertyRelative("version");
            if (versionProperty == null)
            {
                Debug.Log("No version property found for " + templateProperty.name);
                return null;
            }
            versionProperty.intValue = 1;
            return versionProperty;
        }

        private static SerializedProperty FindDateProperty(SerializedProperty templateProperty)
        {
            var metaDataProp =  templateProperty.FindPropertyRelative("metadata");
            var dateProperty = metaDataProp?.FindPropertyRelative("date");
            if (dateProperty == null || dateProperty.propertyType != SerializedPropertyType.String)
                return null;
            return dateProperty;
        }
    }
}
