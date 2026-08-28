using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Theoden.Editor.Import
{
    /// <summary>
    /// Restores Unity asset references from the Addressables addresses
    /// stored in exported POI JSON files.
    /// </summary>
    public sealed class AddressableAssetReferenceJsonConverter
        : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(UnityEngine.Object)
                .IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"Expected an Addressables address for " +
                    $"'{objectType.Name}', but found {reader.TokenType}."
                );
            }

            string address = reader.Value as string;

            if (string.IsNullOrWhiteSpace(address))
                return null;

            var settings =
                AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                throw new JsonSerializationException(
                    "Addressables settings were not found."
                );
            }

            var matchingEntries = settings.groups
                .Where(group => group != null)
                .SelectMany(group => group.entries)
                .Where(entry =>
                    entry != null &&
                    string.Equals(
                        entry.address,
                        address,
                        StringComparison.Ordinal
                    ))
                .Take(2)
                .ToList();

            if (matchingEntries.Count == 0)
            {
                throw new JsonSerializationException(
                    $"No Addressable asset was found for address " +
                    $"'{address}'."
                );
            }

            if (matchingEntries.Count > 1)
            {
                throw new JsonSerializationException(
                    $"Multiple Addressable assets use address '{address}'."
                );
            }

            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    matchingEntries[0].guid
                );

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new JsonSerializationException(
                    $"The Addressable asset '{address}' has no valid path."
                );
            }

            UnityEngine.Object asset =
                AssetDatabase.LoadAssetAtPath(
                    assetPath,
                    objectType
                );

            if (asset == null)
            {
                asset = AssetDatabase
                    .LoadAllAssetsAtPath(assetPath)
                    .FirstOrDefault(objectType.IsInstanceOfType);
            }

            if (asset == null)
            {
                throw new JsonSerializationException(
                    $"Asset '{address}' exists at '{assetPath}', but " +
                    $"cannot be loaded as {objectType.Name}."
                );
            }

            return asset;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            throw new NotSupportedException(
                "This converter is only used for editor import."
            );
        }
    }
}