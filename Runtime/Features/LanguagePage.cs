using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LanguagePage : MonoBehaviour
{
    //public languageConfig languageConfig;
    public Transform buttonContainer;
    public GameObject buttonPrefab;

    void Start()
    {
        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (var lang in LanguageManager.Instance.GetAvailableLanguages())
        {
            GameObject obj = Instantiate(buttonPrefab, buttonContainer);

            var buttonUI = obj.GetComponent<LanguageButtonUiController>();

            buttonUI.Setup(lang, OnLanguageSelected);
        }
    }

    void OnLanguageSelected(LanguageEntry lang)
    {
        LanguageManager.Instance.SetLanguage(lang.language);

        Debug.Log($"[Language] Selected: {lang.language}");

        NavigationManager.Instance.NavigateTo("Nickname");
    }
}