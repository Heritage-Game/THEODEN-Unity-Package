using UnityEngine;
using UnityEngine.UIElements;
using TMPro;
using System.Collections;

public class MenuPage : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;
    private VisualElement root;
    private Label welcomeText;
    private Button btnDiscover;
    private Button btnShowMap;
    private Button btnCodex;
    private Button backButton;
    private Button btnLeaderboards;

    // menu foldout
    private Foldout menuFoldout;
    private Button menuLanguage;
    private Button menuInstructions;

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[CodexInitialView] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        LoadData();
        SetupButtons();
        SetupMenu();
    }

    private void OnDisable()
    {
        if (backButton != null) backButton.clicked -= OnBackClicked;

        if (btnDiscover != null) btnDiscover.clicked -= OnDiscoverClicked;

        if (btnShowMap != null) btnShowMap.clicked -= OnShowMapClicked;

        if (btnCodex != null) btnCodex.clicked -= OnCodexClicked;

        if (btnLeaderboards != null) btnLeaderboards.clicked -= OnLeaderboardsClicked;

        if(menuLanguage != null) menuLanguage.clicked -= OnMenuLanguageClicked;

        if (menuInstructions != null) menuInstructions.clicked -= OnMenuInstructionsClicked;

        if (menuLanguage != null) menuLanguage.clicked -= OnMenuLanguageClicked;

        if (menuInstructions != null) menuInstructions.clicked -= OnMenuInstructionsClicked;
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        welcomeText = root.Q<Label>("hello_label");
        btnDiscover = root.Q<Button>("discover_button");
        btnShowMap = root.Q<Button>("show_map_button");
        btnCodex = root.Q<Button>("codex_button");
        btnLeaderboards = root.Q<Button>("leaderboard_button");
        backButton = root.Q<Button>("back_button");
        menuFoldout = root.Q<Foldout>("menu_foldout");
        menuLanguage = root.Q<Button>("menu_language");
        menuInstructions = root.Q<Button>("menu_instructions");
    }

    // ============================================================
    // SETUP
    // ============================================================
    private void LoadData()
    {
        UpdateWelcomeText();
    }

    private void UpdateWelcomeText()
    {
        string nickname = PlayerPrefs.GetString("NICKNAME", "Explorer");

        if (welcomeText != null)
        {
            welcomeText.text = $"Hello, {nickname}!";
        }
    }

    // ============================================================
    // MENU SETUP
    // ============================================================
    private void SetupMenu()
    {
        if (menuFoldout != null)
        {
            menuFoldout.value = false;
        }

        if (menuLanguage != null)
        {
            menuLanguage.clicked -= OnMenuLanguageClicked;
            menuLanguage.clicked += OnMenuLanguageClicked;
        }

        if (menuInstructions != null)
        {
            menuInstructions.clicked -= OnMenuInstructionsClicked;
            menuInstructions.clicked += OnMenuInstructionsClicked;
        }
    }

    // ============================================================
    // MENU HANDLERS
    // ============================================================
    private void OnMenuLanguageClicked()
    {
        Debug.Log("[MenuPage] Language selected from menu");
        if (menuFoldout != null)
            menuFoldout.value = false;

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo("LanguageUIToolkit");
        }
        else
        {
            Debug.LogError("[MenuPage] NavigationManager missing.");
        }
    }

    private void OnMenuInstructionsClicked()
    {
        Debug.Log("[MenuPage] Instructions selected from menu");
        if (menuFoldout != null)
            menuFoldout.value = false;

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo("InstructionsUIToolkit");
        }
        else
        {
            Debug.LogError("[MenuPage] NavigationManager missing.");
        }
    }

    // ============================================================
    // ON START SCRIPT
    // ============================================================
    void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        UpdateWelcomeText();
        // Load and display nickname
        string nickname = PlayerPrefs.GetString("NICKNAME", "Explorer");
        if (welcomeText != null)
        {
            welcomeText.text = $"Hello, {nickname}!";
        }
        //buttons
        if (btnDiscover != null)
        {
            btnDiscover.clicked -= OnDiscoverClicked;
            btnDiscover.clicked += OnDiscoverClicked;
        }

        if (btnShowMap != null)
        {
            btnShowMap.clicked -= OnShowMapClicked;
            btnShowMap.clicked += OnShowMapClicked;
        }

        if (btnCodex != null)
        {
            btnCodex.clicked -= OnCodexClicked;
            btnCodex.clicked += OnCodexClicked;
        }

        if (btnLeaderboards != null)
        {
            btnLeaderboards.clicked -= OnLeaderboardsClicked;
            btnLeaderboards.clicked += OnLeaderboardsClicked;
        }

        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }
    }

    // ============================================================
    // BUTTON HANDLERS
    // ============================================================
    private void OnDiscoverClicked()
    {
        if (NavigationManager.Instance != null) NavigationManager.Instance.NavigateTo("QRScannerUIToolkit");
        else Debug.LogError("[MenuPage] NavigationManager missing.");
    }

    private void OnShowMapClicked()
    {
        if (NavigationManager.Instance != null) NavigationManager.Instance.NavigateTo("MapScene");
        else Debug.LogError("[MenuPage] NavigationManager missing.");
    }

    private void OnCodexClicked()
    {
        if (NavigationManager.Instance != null) NavigationManager.Instance.NavigateTo("CodexUIToolkit");
        else Debug.LogError("[MenuPage] NavigationManager missing.");
    }

    private void OnLeaderboardsClicked()
    {
        if (NavigationManager.Instance != null) NavigationManager.Instance.NavigateTo("LeaderboardUIToolkit");
        else Debug.LogError("[MenuPage] NavigationManager missing.");
    }

    private void OnBackClicked()
    {
        if (NavigationManager.Instance != null) NavigationManager.Instance.GoBack();
        else Debug.LogError("[MenuPage] NavigationManager missing.");
    }
}