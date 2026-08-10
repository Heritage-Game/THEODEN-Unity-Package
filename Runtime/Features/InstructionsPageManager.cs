using UnityEngine;
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
            titleLabel.text = "Instructions";
        }

        if (instructionsText != null)
        {
            instructionsText.text =
                "Heritage Game was created to make visiting and learning about cultural sites more exciting and interactive.\n\n" +
                "1) Read the map\n" +
                "2) Find the points of interest listed in the codex\n" +
                "3) scan the QR code for the point of interest you found\n" +
                "4) Take the quiz and verify your knowledge\n\n" +
                "Have fun and contribute in keeping cultural heritage alive!";
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