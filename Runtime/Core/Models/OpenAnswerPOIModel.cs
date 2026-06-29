using System;
using System.Collections.Generic;

/// <summary>
/// Runtime model for a POI that uses an open-answer challenge.
/// </summary>
/// <remarks>
/// Open-answer challenges can contain multiple valid answers, such as synonyms,
/// spelling variants, or equivalent expressions.
/// </remarks>
[Serializable]
public class OpenAnswerPOIModel : POIModel
{
    /// <summary>
    /// List of valid answers accepted for the open-answer challenge.
    /// </summary>
    public List<string> correctAnswers = new List<string>();
}