
using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// This class describes the template for a challenge that is represented by a question, which the user might respond
/// to by choosing an option from the choices available. 
/// </summary>
[Serializable]
public class MultipleChoiceTemplate : POITemplate
{
    public ChallengeMC challenge = new ChallengeMC();

    [JsonIgnore]
    public override Challenge TemplateChallenge => challenge;

    [Serializable]
    public class ChallengeMC : Challenge
    {
        [HideInInspector]
        public string type = "multipleChoiceType";
        [TextArea(2,4)]
        public string initialDescription;
        [TextArea(2,4)]
        public string question;

        public Answers answers = new Answers();

        [Header("The key of the correct answer for the question")]
        [Tooltip("Insert here the key to the correct answer ( e.g. 'A', '1', ... ) for the question ")]
        public string correctAnswer;
        [TextArea(2,4)]
        public string hint;
        [Header("Reward")]
        public Sprite poiBadge;
    }

    /// <summary>
    /// Serializable replacement for Dictionary(string, string)
    /// </summary>
    [Serializable]
    public class Answers : ISerializationCallbackReceiver
    {
        public List<AnswerEntry> entries;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            entries ??= new List<AnswerEntry>();
        }
    }

    [Serializable]
    public class AnswerEntry
    {
        public string key;     // e.g., "A"
        public string value;   // e.g., "13th"
    }
}
