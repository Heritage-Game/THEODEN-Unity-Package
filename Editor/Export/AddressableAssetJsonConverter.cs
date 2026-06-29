using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Editor.Export
{
    /// <summary>
    /// Json.NET converter used during the POI export pipeline to serialize Unity asset references
    /// as Addressables addresses.
    /// </summary>
    /// <remarks>
    /// Unity assets such as <see cref="Sprite"/> and <see cref="AudioClip"/> cannot be meaningfully
    /// serialized directly into the exported runtime JSON. The runtime does not need the Unity object
    /// itself; it needs the Addressables address that can be used to load that asset.
    ///
    /// This converter intercepts every value deriving from <see cref="UnityEngine.Object"/> during
    /// JSON serialization. For each Unity asset, it looks up the corresponding Addressables entry
    /// using the asset GUID and writes the entry address as a JSON string.
    ///
    /// This converter does not create Addressables groups, does not mark assets as Addressable, and
    /// does not assign addresses. That work must be performed before serialization by another service,
    /// such as an Addressables setup service.
    ///
    /// Example:
    /// A template field like:
    /// <code>
    /// public Sprite poiBadge;
    /// </code>
    ///
    /// can be exported as:
    /// <code>
    /// "poiBadge": "poi/roman_empire/badges/roman_badge"
    /// </code>
    ///
    /// This keeps the JSON runtime-friendly and allows the game to load assets using Unity's
    /// Addressables system.
    /// </remarks>
    public class AddressableAssetJsonConverter : JsonConverter
    {
        /// <summary>
        /// Determines whether this converter can handle the given object type.
        /// </summary>
        /// <param name="objectType">
        /// The type currently being serialized by Json.NET.
        /// </param>
        /// <returns>
        /// <c>true</c> if the type derives from <see cref="UnityEngine.Object"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This makes the converter apply to Unity asset references such as <see cref="Sprite"/>,
        /// <see cref="AudioClip"/>, textures, materials, prefabs, and any other Unity object type.
        /// </remarks>
        public override bool CanConvert(Type objectType)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Writes a Unity asset reference as an Addressables address string in the exported JSON.
        /// </summary>
        /// <param name="writer">
        /// The JSON writer used by Json.NET.
        /// </param>
        /// <param name="value">
        /// The Unity asset reference being serialized.
        /// </param>
        /// <param name="serializer">
        /// The active Json.NET serializer instance.
        /// </param>
        /// <remarks>
        /// The method follows these steps:
        ///
        /// 1. If the value is null, it writes JSON null.
        /// 2. If the value is not a valid Unity object, it writes JSON null.
        /// 3. It retrieves the asset path using <see cref="AssetDatabase.GetAssetPath(UnityEngine.Object)"/>.
        /// 4. It converts the asset path to a Unity GUID.
        /// 5. It searches the Addressables settings for an entry with that GUID.
        /// 6. If the entry exists, it writes <c>entry.address</c> as a JSON string.
        ///
        /// If the asset is not Addressable, the method writes JSON null and logs a warning. This usually
        /// means the Addressables setup step was not executed before serialization.
        /// </remarks>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var unityObject = value as UnityEngine.Object;

            if (unityObject == null)
            {
                writer.WriteNull();
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(unityObject);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                writer.WriteNull();
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                writer.WriteNull();
                Debug.LogError("Addressables settings not found during JSON export.");
                return;
            }

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry == null)
            {
                writer.WriteNull();
                Debug.LogWarning(
                    $"Asset '{unityObject.name}' is not Addressable. " +
                    "Run Addressables setup before JSON serialization."
                );
                return;
            }

            writer.WriteValue(entry.address);
        }

        /// <summary>
        /// Reads JSON back into a Unity asset reference. Always throws an exception because this converter IS NOT MEANT
        /// FOR DESERIALIZATION.
        /// </summary>
        /// <param name="reader">
        /// The JSON reader used by Json.NET.
        /// </param>
        /// <param name="objectType">
        /// The target Unity object type requested by Json.NET.
        /// </param>
        /// <param name="existingValue">
        /// The existing value, if any.
        /// </param>
        /// <param name="serializer">
        /// The active Json.NET serializer instance.
        /// </param>
        /// <returns>
        /// This method never returns a value because deserialization is not supported.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Always thrown because this converter is intended only for editor-side export.
        /// </exception>
        /// <remarks>
        /// The exported JSON contains Addressables address strings, not direct Unity object references.
        /// At runtime, those strings should be read by runtime DTOs and resolved using the Addressables
        /// loading API. Therefore, this converter should not be used to deserialize JSON back into
        /// editor template objects.
        /// </remarks>
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            throw new NotSupportedException("This converter is only used for editor export.");
        }
    }
}