using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// This class contains the information that the Json associated with each POI (Point of interest) must have.
/// It is the blueprint for the information displayed. If anything needs to be added to the POI window it must
/// be added here.
/// </summary>
[Serializable]
public class Root
{
    [JsonProperty("gameData")]
    public GameData GameData { get; set; }
}

[Serializable]
public class GameData
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("lastUpdated")]
    public string LastUpdated { get; set; }

    [JsonProperty("pointOfInterest")]
    public List<PointOfInterest> PointsOfInterest { get; set; }
}

[Serializable]
public class PointOfInterest
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    // If category is a comma-separated string, use string.
    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("challenge")]
    public Challenge Challenge { get; set; }

    [JsonProperty("story")]
    public Story Story { get; set; }

    [JsonProperty("media")]
    public Media Media { get; set; }

    [JsonProperty("nextPOI")]
    public string NextPoi { get; set; }

    [JsonProperty("tags")]
    public List<string> Tags { get; set; }
}

[Serializable]
public class Challenge
{
    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("initialDescription")]
    public string InitialDescription { get; set; }

    [JsonProperty("question")]
    public string Question { get; set; }

    [JsonProperty("answers")]
    public Dictionary<string, string> Answers { get; set; } // e.g. { "A":"13th", "B":"12th" }

    [JsonProperty("correctAnswer")]
    public string CorrectAnswer { get; set; }

    [JsonProperty("hint")]
    public string Hint { get; set; }

    [JsonProperty("poiBadge")]
    public string PoiBadge { get; set; }
}

[Serializable]
public class Story
{
    [JsonProperty("shortSummary")]
    public string ShortSummary { get; set; }

    [JsonProperty("fullNarrative")]
    public string FullNarrative { get; set; }
}

[Serializable]
public class Media
{
    /// <summary>
    /// This section lists the media associated with the POI information, the images are saved and stored as
    /// GUIDs, NOT as paths (in case they change).
    /// </summary>
    [JsonProperty("images")]
    public List<string> Images { get; set; }
}
