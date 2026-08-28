using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/**
 * This class describes the template for a challenge that has one question to which the user might
 * respond with a single string that holds the answer.
 * e.g.: question = "What year was Napoleon born?", answer = "1769"
 */
[Serializable]
[PoiChallengeType("openAnswerType")]
public class OpenAnswerTemplate : POITemplate
{
    public ChallengeOpenAnswer challenge = new ChallengeOpenAnswer();

    [JsonIgnore]
    public override Challenge TemplateChallenge => challenge;

    [Serializable]
    public class ChallengeOpenAnswer : Challenge
    {
        [HideInInspector]
        public string type = "OpenAnswerType";
        [TextArea(2,4)]
        public string initialDescription;
        [TextArea(2,4)]
        public string question;

        public Answers answers = new Answers();

        [TextArea(2,4)]
        public string hint;
        [Header("Reward")]
        public Sprite poiBadge;
    }

    /// <summary>
    /// Dictionary of possible correct answers for the questions include synonyms for the same
    /// word to avoid misinterpreting the correct answers.
    /// 
    /// <remarks>NOTE on Unity ISerializationCallbackReceiver:
    /// Unity invokes OnAfterDeserialize after an object is deserialized.
    /// After Unity has written the data to your fields, use this callback to transform the deserialized
    /// data back into the form you want it to have at runtime.
    /// </remarks>
    /// 
    /// </summary>
    [Serializable]
    public class Answers : ISerializationCallbackReceiver
    {
        public List<string> correctAnswers;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            correctAnswers ??= new List<string>();
        }
    }
}