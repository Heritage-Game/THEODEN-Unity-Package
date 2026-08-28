using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class LanguagePage : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES (UI TOOLKIT)
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement buttonContainer;
    private Label titleLabel;
    private Label subtitleLabel;
    private Button backButton;

    // ============================================================
    // TEMPLATE
    // ============================================================
    [Header("Templates")]
    [SerializeField] private VisualTreeAsset languageButtonTemplate;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[LanguagePage] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        EnsureLocalizationManager();

        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }


        if (LanguageManager.Instance == null)
        {
            Debug.LogError("[LanguagePage] LanguageManager not found in scene. Please add it to the scene.");
            return;
        }

        GenerateButtons();
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.clicked -= OnBackClicked;
    }

    private void BindUIElements()
    {
        buttonContainer = root.Q<VisualElement>("button_container");
        titleLabel = root.Q<Label>("language_title");
        backButton = root.Q<Button>("back_button");

        if (buttonContainer == null) Debug.LogWarning("[LanguagePage] 'button_container' not found in UXML.");

        if (titleLabel == null) Debug.LogWarning("[LanguagePage] 'title_label' not found in UXML.");

        if (backButton == null) Debug.LogWarning("[LanguagePage] 'back_button' not found in UXML.");
    }

    private void EnsureLocalizationManager()
    {
        if (LocalizationManager.Instance == null)
        {
            GameObject go = new GameObject("LocalizationManager");
            LocalizationManager lm = go.AddComponent<LocalizationManager>();
            DontDestroyOnLoad(go);
            Debug.Log("[InstructionsPageManager] LocalizationManager created.");
        }

        LocalizationManager.Instance?.LoadLocalization();
    }

    // ============================================================
    // BUTTON GENERATION
    // ============================================================
    private void GenerateButtons()
    {
        if (buttonContainer == null)
        {
            Debug.LogError("[LanguagePage] Button container is null.");
            return;
        }

        if (languageButtonTemplate == null)
        {
            Debug.LogError("[LanguagePage] Language button template is not assigned.");
            return;
        }

        if (LanguageManager.Instance == null)
        {
            Debug.LogError("[LanguagePage] LanguageManager instance is null.");
            return;
        }

        if (titleLabel != null)
        {
            titleLabel.text = LocalizationManager.Instance.GetText("language_title");
        }

        // clean container
        buttonContainer.Clear();

        var availableLanguages = LanguageManager.Instance.GetAvailableLanguages();

        if(availableLanguages == null || availableLanguages.Count == 0)
        {
            Debug.LogError("[LanguagePage] No available languages found.");
            return;
        }

        foreach (var lang in availableLanguages)
        {
            CreateLanguageButton(lang);
        }
    }

    private void CreateLanguageButton(LanguageEntry lang)
    {
        VisualElement buttonElement = languageButtonTemplate.Instantiate();

        Button button = buttonElement.Q<Button>("lang_button");

        if (button != null)
        {
            button.text = lang.displayedName;
            LanguageEntry capturedLang = lang;
            button.clicked += () => OnLanguageSelected(capturedLang);
        }

        buttonContainer.Add(buttonElement);
    }

    // ============================================================
    // LANGUAGE SELECTION
    // ============================================================
    private void OnLanguageSelected(LanguageEntry lang)
    {
        if (LanguageManager.Instance == null)
        {
            Debug.LogError("[LanguagePage] LanguageManager instance is null.");
            return;
        }

        LanguageManager.Instance.SetLanguage(lang.language);

        Debug.Log($"[LanguagePage] Selected: {lang.language}");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[LanguagePage] NavigationManager is null.");
            return;
        }

        NavigationManager.Instance.NavigateTo("NicknameUIToolkit");
    }

    private void CreateLanguageManager()
    {
        LanguageManager existing = FindFirstObjectByType<LanguageManager>();
        if (existing != null)
        {
            Debug.Log("[LanguagePage] Found existing LanguageManager.");
            return;
        }

        GameObject go = new GameObject("LanguageManager");
        LanguageManager lm = go.AddComponent<LanguageManager>();

        DontDestroyOnLoad(go);
        Debug.Log("[LanguagePage] LanguageManager created.");
    }

    private void OnBackClicked()
    {
        Debug.Log("[LanguagePage] Back button clicked.");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[LanguagePage] NavigationManager is null.");
            return;
        }

        NavigationManager.Instance.GoBack();
    }
}