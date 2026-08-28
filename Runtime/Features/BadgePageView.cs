using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class BadgePageView : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Image badgeImage;
    private VisualElement badgeContainer;
    private Label scoreLabel;
    private Label titleLabel;
    private Label badgeLabel;
    private Button continueButton;
    private Button backButton;

    // flavour text
    private LocalizationEntry currentLocalization;
    private Dictionary<string, string> textDictionary = new Dictionary<string, string>();

    // ============================================================
    // RUNTIME STATE
    // ============================================================
    private POIModel currentPOI;

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[BadgePageView] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        EnsureLocalizationManager();
        SetupButtons();
        LoadData();
        LoadLocalizedTexts();
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.clicked -= OnContinueClicked;

        if (backButton != null)
            backButton.clicked -= OnBackClicked;
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
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        badgeImage = root.Q<Image>("badge_image");
        scoreLabel = root.Q<Label>("score_label");
        badgeLabel = root.Q<Label>("badge_label");
        titleLabel = root.Q<Label>("challenge_completed_label");
        badgeContainer = root.Q<VisualElement>("badge_container");
        continueButton = root.Q<Button>("continue_button");
        backButton = root.Q<Button>("back_button");

        if (badgeImage == null)
            Debug.LogWarning("[BadgePageView] 'badge_image' not found in UXML.");

        if (scoreLabel == null)
            Debug.LogWarning("[BadgePageView] 'score_label' not found in UXML.");

        if (continueButton == null)
            Debug.LogWarning("[BadgePageView] 'continue_button' not found in UXML.");

        if (backButton == null)
            Debug.LogWarning("[BadgePageView] 'back_button' not found in UXML.");
    }

    // ============================================================
    // SETUP
    // ============================================================
    private void SetupButtons()
    {
        if (continueButton != null)
        {
            continueButton.clicked -= OnContinueClicked;
            continueButton.clicked += OnContinueClicked;
        }

        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }
    }

    // ============================================================
    // LOAD DATA
    // ============================================================
    private void LoadData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[BadgePageView] DataManager is missing.");
            ShowFallback();
            return;
        }

        currentPOI = DataManager.Instance.SelectedPOI;

        if (currentPOI == null)
        {
            Debug.LogError("[BadgePageView] SelectedPOI is null.");
            ShowFallback();
            return;
        }

        DisplayBadge();
    }

    // ============================================================
    // DISPLAY BADGE
    // ============================================================
    private void DisplayBadge()
    {
        // show score
        if (scoreLabel != null)
        {
            int points = currentPOI.points > 0 ? currentPOI.points : 100;

            scoreLabel.text = $"⭐ {points} points earned!";
            scoreLabel.style.color = new Color(1f, 0.8f, 0f);
            Debug.Log($"[BadgePageView] Displaying score: {points} points");
        }

        // show badge
        bool hasBadge = currentPOI.poiBadge != null;

        if (badgeContainer != null)
        {
            badgeContainer.style.display = hasBadge ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (badgeImage != null && hasBadge)
        {
            badgeImage.sprite = currentPOI.poiBadge;
            //badgeImage.scaleMode = ScaleMode.ScaleToFit;
            badgeImage.style.display = DisplayStyle.Flex;
            //badgeImage.style.width = 200;
            //badgeImage.style.height = 200;
            badgeImage.style.alignSelf = Align.Center;
            //badgeImage.style.marginTop = 20;
            Debug.Log("[BadgePageView] Badge image loaded.");
        }
        else
        {
            Debug.LogWarning("[BadgePageView] No badge sprite found.");
            ShowDefaultBadge();
        }

        Debug.Log($"[BadgePageView] Displaying badge for POI: {currentPOI.poiName}");
    }

    // ============================================================
    // UI UTILITIES
    // ============================================================
    private void ShowDefaultBadge()
    {
        if (badgeImage != null)
        {
            badgeImage.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f);
            badgeImage.style.width = 200;
            badgeImage.style.height = 200;
            badgeImage.style.alignSelf = Align.Center;
            badgeImage.style.marginTop = 20;
            badgeImage.style.display = DisplayStyle.Flex;
        }
    }

    private void ShowFallback()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = "No points available.";
            scoreLabel.style.color = Color.red;
        }

        if (badgeImage != null)
            badgeImage.style.display = DisplayStyle.None;

        if (continueButton != null)
            continueButton.SetEnabled(false);
    }

    private void LoadLocalizedTexts()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.LoadLocalization();

        if (titleLabel != null)
        {
            titleLabel.text = LocalizationManager.Instance.GetText("challenge_completed_label");
        }

        if (badgeLabel != null)
        {
            badgeLabel.text = LocalizationManager.Instance.GetText("badge_label");
        }

        if (continueButton != null)
        {
            continueButton.text = LocalizationManager.Instance.GetText("continue_button");
        }
    }

    // ============================================================
    // BUTTON HANDLERS
    // ============================================================
    private void OnContinueClicked()
    {
        Debug.Log("[BadgePageView] Continue button clicked.");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[BadgePageView] NavigationManager is missing.");
            return;
        }

        NavigationManager.Instance.NavigateTo("POIRecapUIToolkit");
    }

    private void OnBackClicked()
    {
        Debug.Log("[BadgePageView] Back button clicked.");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[BadgePageView] NavigationManager is missing.");
            return;
        }

        NavigationManager.Instance.GoBack();
    }
}