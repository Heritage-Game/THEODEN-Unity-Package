using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This class works a singleton that is conceived to be attached to an empty gameObject inside the Language selection scene.
/// The main function that this script provides is to store the language selected by the user. That selection is then
/// used to identify the correct JSON files for the Codex and the POIs so that the information can be displayed in the
/// correct language.
/// </summary>
public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    [SerializeField] private LanguageConfig config;

    public LanguageList CurrentLanguage { get; private set; }

    private const string PREF_KEY = "LANGUAGE";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();
    }

    public void SetLanguage(LanguageList language)
    {
        CurrentLanguage = language;
        PlayerPrefs.SetInt(PREF_KEY, (int)language);
        PlayerPrefs.Save();
    }

    private void LoadLanguage()
    {
        if (PlayerPrefs.HasKey(PREF_KEY))
        {
            CurrentLanguage = (LanguageList)PlayerPrefs.GetInt(PREF_KEY);
        }
        else
        {
            CurrentLanguage = config.languages[0].language; // default
        }
    }

    /// <summary>
    /// Selects the right file for the Codex menu creation. The codex json file is saved as "menu+language.json"
    /// by configuration.
    /// </summary>
    /// <returns>The Codex file for the selected language</returns>
    public string GetCodexFileName()
    {
        //return $"Codex{CurrentLanguage}.json";
        return $"codex_{CurrentLanguage}.json";
    }

    public List<LanguageEntry> GetAvailableLanguages()
    {
        return config.languages;
    }
    public bool HasSavedLanguage()
    {
        return PlayerPrefs.HasKey(PREF_KEY);
    }
    public static bool HasLanguagePreference()
    {
        return PlayerPrefs.HasKey(PREF_KEY);
    }
}
