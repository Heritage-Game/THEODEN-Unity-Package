using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class InstructionsPageManager : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Label titleLabel;
    private Label instructionsText;
    private Button continueButton;
    private Button backButton;

    // flavour text
    private LocalizationEntry currentLocalization;
    private Dictionary<string, string> textDictionary = new Dictionary<string, string>();

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[InstructionsPageManager] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        EnsureLocalizationManager();
        SetupButtons();
        LoadInstructions();
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
        titleLabel = root.Q<Label>("title_label");
        instructionsText = root.Q<Label>("instructions_text");
        continueButton = root.Q<Button>("continue_button");
        backButton = root.Q<Button>("back_button");

        if (titleLabel == null)
            Debug.LogWarning("[InstructionsPageManager] 'title_label' not found in UXML.");

        if (instructionsText == null)
            Debug.LogWarning("[InstructionsPageManager] 'instructions_text' not found in UXML.");

        if (continueButton == null)
            Debug.LogWarning("[InstructionsPageManager] 'continue_button' not found in UXML.");

        if (backButton == null)
            Debug.LogWarning("[InstructionsPageManager] 'back_button' not found in UXML.");
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
            if (LocalizationManager.Instance != null)
            {
                continueButton.text = LocalizationManager.Instance.GetText("continue_button");
            }
        }

        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }
    }

    // ============================================================
    // LOAD INSTRUCTIONS
    // ============================================================
    private void LoadInstructions()
    {
        if (titleLabel != null)
        {
            titleLabel.text = LocalizationManager.Instance.GetText("instructions_title");
        }

        if (instructionsText != null)
        {
            instructionsText.text = LocalizationManager.Instance.GetText("instructions_text");
        }
    }

    // ============================================================
    // BUTTON HANDLERS
    // ============================================================
    private void OnContinueClicked()
    {
        Debug.Log("[InstructionsPageManager] Continue button clicked.");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[InstructionsPageManager] NavigationManager is missing.");
            return;
        }

        NavigationManager.Instance.NavigateTo("MenuUIToolkit");
    }

    private void OnBackClicked()
    {
        Debug.Log("[InstructionsPageManager] Back button clicked.");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[InstructionsPageManager] NavigationManager is missing.");
            return;
        }

        NavigationManager.Instance.GoBack();
    }
}