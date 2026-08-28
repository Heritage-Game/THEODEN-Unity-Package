using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }
    public bool isLoaded { get; private set; }

    private LocalizationModel localizationData;
    private LocalizationEntry currentLocalization;
    private Dictionary<string, string> textDictionary = new Dictionary<string, string>();

    private const string LOCALIZATION_FILE = "flavour_text";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadLocalization();
    }

    public void LoadLocalization()
    {
#if UNITY_EDITOR
            string fullPath = System.IO.Path.Combine(
                Application.dataPath, 
                "../Packages/it.unicam.theoden/Editor/Assets/flavour_text.json"
            );
    
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    string jsonContent = System.IO.File.ReadAllText(fullPath);
                    ParseLocalization(jsonContent);
                    isLoaded = true;
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[LocalizationManager] Failed to load from Packages: {ex.Message}");
                }
            }
#endif

        // f<llback: loads from resources
        TextAsset jsonFile = Resources.Load<TextAsset>(LOCALIZATION_FILE);
        if (jsonFile != null)
        {
            ParseLocalization(jsonFile.text);
            isLoaded = true;
        }
        else
        {
            Debug.LogError($"[LocalizationManager] Localization file not found.");
            isLoaded = false;
        }
    }

    private void ParseLocalization(string jsonContent)
    {
        try
        {
            localizationData = JsonUtility.FromJson<LocalizationModel>(jsonContent);

            if (localizationData == null || localizationData.languages == null)
            {
                Debug.LogError("[LocalizationManager] Failed to parse localization JSON.");
                return;
            }

            LanguageList currentLanguage = LanguageList.ENG;
            if (LanguageManager.Instance != null)
            {
                currentLanguage = LanguageManager.Instance.CurrentLanguage;
            }

            string languageCode = currentLanguage.ToString();

            foreach (var entry in localizationData.languages)
            {
                if (entry.language == languageCode)
                {
                    currentLocalization = entry;
                    BuildDictionary();
                    isLoaded = true;
                    return;
                }
            }

            // fallback to ENG
            Debug.LogWarning($"[LocalizationManager] Language not found: {languageCode}. Using ENG as fallback.");
            foreach (var entry in localizationData.languages)
            {
                if (entry.language == "ENG")
                {
                    currentLocalization = entry;
                    BuildDictionary();
                    isLoaded = true;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LocalizationManager] Failed to parse localization: {ex.Message}");
            isLoaded = false;
        }
    }

    private void BuildDictionary()
    {
        textDictionary.Clear();

        if (currentLocalization?.uiTexts == null)
        {
            Debug.LogError("[LocalizationManager] Localization data is null.");
            return;
        }

        var uiTexts = currentLocalization.uiTexts;
        var fields = typeof(UILocalization).GetFields();

        foreach (var field in fields)
        {
            string value = field.GetValue(uiTexts) as string;
            if (!string.IsNullOrEmpty(value))
            {
                textDictionary[field.Name] = value;
            }
        }
    }

    public string GetText(string key, string defaultValue = null)
    {
        if (!isLoaded)
        {
            LoadLocalization();
        }

        if (textDictionary.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"[LocalizationManager] Text key not found: {key}");
        return defaultValue ?? key;
    }

    public void SetUIText(VisualElement element, string key, string defaultValue = null)
    {
        if (element == null) return;

        string text = GetText(key, defaultValue);

        if (element is Label label)
        {
            label.text = text;
        }
        else if (element is Button button)
        {
            button.text = text;
        }
        else if (element is TextField textField)
        {
            textField.value = text;
        }
    }
}