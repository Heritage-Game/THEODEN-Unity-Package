using System;
using System.Collections.Generic;

/// <summary>
/// Runtime model for a POI that uses a multiple-choice challenge.
/// </summary>
[Serializable]
public class MultipleChoicePOIModel : POIModel
{
    /// <summary>
    /// Available answers for the multiple-choice challenge.
    /// </summary>
    public List<AnswerEntry> answers = new List<AnswerEntry>();

    /// <summary>
    /// Key of the correct answer.
    /// </summary>
    public string correctAnswer;

    /// <summary>
    /// Represents one answer option in a multiple-choice challenge.
    /// </summary>
    [Serializable]
    public class AnswerEntry
    {
        /// <summary>
        /// Internal answer key.
        /// </summary>
        public string key;

        /// <summary>
        /// Text displayed to the player.
        /// </summary>
        public string value;
    }
}