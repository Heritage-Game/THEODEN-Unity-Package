using UnityEngine;
using System.Collections;

public class SplashPage : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string languageSceneName = "Language";
    [SerializeField] private string nicknameSceneName = "Nickname";
    [SerializeField] private string mainMenuSceneName = "Menu";

    [Header("Timing")]
    [SerializeField] private float splashDuration = 2.5f;

    private const string NICKNAME_KEY = "NICKNAME";

    private void Start()
    {
        StartCoroutine(RouteAfterSplash());
    }

    private IEnumerator RouteAfterSplash()
    {
        yield return new WaitForSeconds(splashDuration);

        bool hasLanguage = LanguageManager.HasLanguagePreference();
        bool hasNickname = PlayerPrefs.HasKey(NICKNAME_KEY);

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[SplashPage] NavigationManager missing.");
            yield break;
        }

        if (!hasLanguage)
        {
            NavigationManager.Instance.NavigateTo(languageSceneName);
            yield break;
        }

        if (!hasNickname)
        {
            NavigationManager.Instance.NavigateTo(nicknameSceneName);
            yield break;
        }

        NavigationManager.Instance.NavigateTo(mainMenuSceneName);
    }
}