using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class SplashPage : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement blackScreen;

    // ============================================================
    // SCENE NAMES
    // ============================================================
    [Header("Scene Names")]
    [SerializeField] private string languageSceneName = "LanguageUIToolkit";
    [SerializeField] private string nicknameSceneName = "NicknameUIToolkit";
    [SerializeField] private string mainMenuSceneName = "InstructionsUIToolkit";

    // ============================================================
    // TIMING
    // ============================================================
    [Header("Timing")]
    [SerializeField] private float splashDuration = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    // ============================================================
    // CONSTANTS
    // ============================================================
    private const string NICKNAME_KEY = "NICKNAME";

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[SplashPage] UIDocument not assigned.");
            StartCoroutine(RouteAfterSplash());
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        SetupSplashUI();
        StartCoroutine(RouteAfterSplash());
    }

    private void OnDisable()
    {
        
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        blackScreen = root.Q<VisualElement>("black_screen");

        if (blackScreen == null)
        {
            Debug.LogWarning("[SplashPage] 'black_screen' not found in UXML.");
            CreateBlackScreen();
        }
    }

    private void CreateBlackScreen()
    {
        if (root == null) return;

        blackScreen.style.width = new Length(100, LengthUnit.Percent);
        blackScreen.style.height = new Length(100, LengthUnit.Percent);
        blackScreen.style.backgroundColor = Color.black;
        blackScreen.style.opacity = 0;
        blackScreen.style.display = DisplayStyle.None;
        blackScreen.pickingMode = PickingMode.Ignore;

        root.Add(blackScreen);
        Debug.Log("[SplashPage] Black screen created.");
    }

    // ============================================================
    // UI SETUP
    // ============================================================
    private void SetupSplashUI()
    {
        if (blackScreen != null)
        {
            blackScreen.style.opacity = 0;
            blackScreen.style.display = DisplayStyle.None;
        }
    }

    // ============================================================
    // TRANSITION
    // ============================================================
    /// <summary>
    /// Mostra lo schermo nero con fade
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        if (blackScreen == null)
        {
            Debug.LogWarning("[SplashPage] Black screen not available.");
            yield break;
        }
        
        // show black screen
        blackScreen.style.display = DisplayStyle.Flex;
        blackScreen.style.opacity = 0;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            blackScreen.style.opacity = alpha;
            yield return null;
        }

        blackScreen.style.opacity = 1;

        Debug.Log("[SplashPage] Screen is now black.");
    }

    // ============================================================
    // NAVIGATION ROUTING
    // ============================================================
    private IEnumerator RouteAfterSplash()
    {
        yield return new WaitForSeconds(splashDuration);
        // fade to black
        yield return StartCoroutine(FadeToBlack());
        yield return new WaitForSeconds(0.1f);

        bool hasLanguage = LanguageManager.HasLanguagePreference();
        bool hasNickname = PlayerPrefs.HasKey(NICKNAME_KEY);

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[SplashPage] NavigationManager missing.");
            yield break;
        }

        // go to language page if there's no preference
        if (!hasLanguage)
        {
            Debug.Log("[SplashPage] No language preference. Navigating to: " + languageSceneName);
            NavigationManager.Instance.NavigateTo(languageSceneName);
            yield break;
        }

        // go to nickname if there's no nickname
        if (!hasNickname)
        {
            Debug.Log("[SplashPage] No nickname. Navigating to: " + nicknameSceneName);
            NavigationManager.Instance.NavigateTo(nicknameSceneName);
            yield break;
        }

        // go to menu
        Debug.Log("[SplashPage] All preferences found. Navigating to: " + mainMenuSceneName);
        NavigationManager.Instance.NavigateTo(mainMenuSceneName);
    }

    // ============================================================
    // PUBLIC METHODS (per test/debug)
    // ============================================================
    /// <summary>
    /// Salta la splash e va direttamente alla scena successiva (per debug)
    /// </summary>
    public void SkipSplash()
    {
        StopAllCoroutines();
        StartCoroutine(RouteAfterSplash());
    }
}