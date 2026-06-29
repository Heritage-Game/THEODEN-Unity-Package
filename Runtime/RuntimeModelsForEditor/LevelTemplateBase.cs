using System;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// This abstract class represents the Base class that every template inherits from.
/// It has two parameters that are expected to be implemented by every template class, since they are
/// fundamental general data that is needed to hold information of the time od creation and the
/// version of the implementation of the file created based on the template. 
/// </summary>
[Serializable]
public abstract class LevelTemplateBase
{
    [JsonProperty("metadata")]
    [SerializeField, HideInInspector]
    private Metadata metadata = new Metadata();

    [JsonProperty("poi")]
    [SerializeField, HideInInspector]
    private PoiReference poi = new PoiReference();

    [JsonProperty("language")]
    [SerializeField, HideInInspector]
    private LanguageList language;

    [Serializable]
    public class Metadata
    {
        public string createdAt;
        public string updatedAt;
        public int schemaVersion = 1;
        public string toolVersion;
    }

    [Serializable]
    public class PoiReference
    {
        public string poiName;
        public string poiId;
    }

    [JsonIgnore]
    public Metadata FileMetadata => metadata;

    [JsonIgnore]
    public string PoiName => poi.poiName;

    [JsonIgnore]
    public string PoiId => poi.poiId;

    [JsonIgnore]
    public LanguageList Language => language;

    public void InjectForExport(
        string poiId,
        string poiName,
        LanguageList selectedLanguage,
        string toolVersion)
    {
        poi.poiId = poiId;
        poi.poiName = poiName;
        language = selectedLanguage;

        string now = DateTime.UtcNow.ToString("O");

        if (string.IsNullOrEmpty(metadata.createdAt))
            metadata.createdAt = now;

        metadata.updatedAt = now;
        metadata.schemaVersion = 1;
        metadata.toolVersion = toolVersion;
    }

    public void PreserveCreatedAt(Metadata existingMetadata)
    {
        if (existingMetadata == null)
            return;

        metadata.createdAt = existingMetadata.createdAt;
    }
}
