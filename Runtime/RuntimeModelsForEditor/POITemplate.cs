
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public abstract class POITemplate : LevelTemplateBase
{
    public GameData gameData = new GameData();

    [JsonIgnore]
    public abstract Challenge TemplateChallenge { get; }

    [Serializable]
    public class GameData
    {
        public POI pointOfInterest = new POI();
    }

    [Serializable]
    public class POI
    {
        [Tooltip("General category of the point of interest, e.g. Roman history, architecture, daily life.")]
        public string category;

        [Header("Story of the POI")]
        public Story story = new Story();
        
        [Header("Media")]
        public Media media = new Media();

        [Header("Tags")]
        public Tags tags = new Tags();
    }

    [Serializable]
    public class Story
    {
        [TextArea(2,4)]
        //[Tooltip("Short summary shown in previews, cards, or introductory UI.")]
        [HelpBox("Short summary shown in cards and previews. Keep it concise.", HelpBoxType.Info)]
        public string shortIntroductorySummary;
        [TextArea(4,12)]
        [Tooltip("The full explanation of the story of the Point of interest, shown after the challenge is completed.")]
        public string fullNarrative;
    }

    [Serializable]
    public class Media : ISerializationCallbackReceiver
    {
        public List<Sprite> images;
        public Audio audio = new Audio();

        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// If a List is placed inside a managed reference - as it is in this case -
        /// it must be initialized in OnAfterDeserialize() method. Never in a constructor or field initializer. 
        /// </summary>
        public void OnAfterDeserialize()
        {
            images ??= new List<Sprite>();
            audio ??= new Audio();
        }
    }

    [Serializable]
    public class Audio
    {
        public AudioClip music;
        public AudioClip audioDescription;
    }

    [Serializable]
    public class Tags : ISerializationCallbackReceiver
    {
        public List<string> tags;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            tags ??= new List<string>();
        }
    }

    /// <summary>
    /// This class differentiates the types/structures for the Challenge.
    /// To extend behaviour, create a new class that inherits from this one.
    /// </summary>
    [Serializable]
    public abstract class Challenge
    {
        [Min(1)]
        [Tooltip("Points awarded when the challenge is completed correctly." +
                 " If this value is invalid the default value will be 100 points.")]
        public int points = TheodenScoringRules.DefaultChallengePoints;
    }
}
