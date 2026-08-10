using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This scriprableObject holds the languages that the App creator wants to include in the app configuration. The languages
/// are represented with an enum item (LanguageList) and a string that is the language nem displayed in the "language
/// selection" scene buttons.
/// <see cref="LanguageList"/>
/// </summary>
[CreateAssetMenu(fileName = "languageConfig", menuName = "THEODEN/Available languages")]
public class LanguageConfig : ScriptableObject
{
    public List<LanguageEntry> languages = new();
}

[Serializable]
public class LanguageEntry
{
    public LanguageList language;
    //public string displayedName;
    public string displayedName;        // "English", "Italiano"
    public Sprite flagSprite;          // Bandiera
    // oppure
    public string displayName;         // "English"
    public string code;               // "ENG"
    public Sprite flag;
    //optional to add eventually
    //public Sprite flag;
}