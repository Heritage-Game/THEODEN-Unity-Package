using Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

public class CodexDetailView : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES (UI TOOLKIT)
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement logoContainer;
    private Image logoImage;
    private Label levelTitleText;
    private Label directionsText;
    private Button scanQRButton;
    private Button backButton;

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[CodexDetailView] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        EnsureLocalizationManager();
        SetupButtons();
        LoadSelectedDirectionsData();
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.clicked -= OnBackClicked;

        if (scanQRButton != null)
            scanQRButton.clicked -= OnScanQRClicked;
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
        logoContainer = root.Q<VisualElement>("logo_container");
        logoImage = root.Q<Image>("poi_image");
        levelTitleText = root.Q<Label>("title_label");
        directionsText = root.Q<Label>("directions_text");
        scanQRButton = root.Q<Button>("scan_qr_button");
        backButton = root.Q<Button>("back_button");

        if (logoContainer == null)
            Debug.LogWarning("[CodexDetailView] 'logo_container' not found in UXML.");

        if (logoImage == null)
            Debug.LogWarning("[CodexDetailView] 'logo_image' not found in UXML.");

        if (levelTitleText == null)
            Debug.LogWarning("[CodexDetailView] 'level_title_text' not found in UXML.");

        if (directionsText == null)
            Debug.LogWarning("[CodexDetailView] 'directions_text' not found in UXML.");

        if (scanQRButton == null)
            Debug.LogWarning("[CodexDetailView] 'scan_qr_button' not found in UXML.");

        if (backButton == null)
            Debug.LogWarning("[CodexDetailView] 'back_button' not found in UXML.");
    }

    // ============================================================
    // SETUP
    // ============================================================
    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }

        if (scanQRButton != null)
        {
            scanQRButton.clicked -= OnScanQRClicked;
            scanQRButton.clicked += OnScanQRClicked;
            if (LocalizationManager.Instance != null)
            {
                scanQRButton.text = LocalizationManager.Instance.GetText("scan_qr_button");
            }
        }
    }

    // ============================================================
    // LOAD DATA
    // ============================================================
    private void LoadSelectedDirectionsData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[CodexDetailView] DataManager is missing.");
            ShowFallbackText();
            return;
        }

        DirectionsToNextPOIModel directions = DataManager.Instance.SelectedDirections;

        if (directions == null)
        {
            Debug.LogError("[CodexDetailView] SelectedDirections is null.");
            ShowFallbackText();
            return;
        }

        if (levelTitleText != null)
        {
            levelTitleText.text = string.IsNullOrEmpty(directions.poiName)
                ? "Directions"
                : directions.poiName;
        }

        if (directionsText != null)
        {
            directionsText.text = string.IsNullOrEmpty(directions.description)
                ? "No directions available."
                : directions.description;
        }

        LoadLogoImage(directions);
    }

    // ============================================================
    // LOGO IMAGE LOADING
    // ============================================================
    private void LoadLogoImage(DirectionsToNextPOIModel directions)
    {
        if (logoImage == null) return;

        /*bool hasDirectionImage = directions.imageGUIDs != null &&
                                 directions.imageGUIDs.Count > 0 &&
                                 !string.IsNullOrEmpty(directions.imageGUIDs[0]);*/
        bool hasDirectionImage = directions.images != null &&
                             directions.images.Count > 0 &&
                             directions.images[0] != null &&
                             !string.IsNullOrEmpty(directions.images[0].address);

        logoImage.style.display = DisplayStyle.None;
        logoContainer.style.display = DisplayStyle.None;

        if (hasDirectionImage)
        {
            Debug.Log("[CodexDetailView] Direction image GUID found, but image loading is disabled for demo.");
        }
    }

    // ============================================================
    // UI UTILITIES
    // ============================================================
    private void ShowFallbackText()
    {
        if (levelTitleText != null)
            levelTitleText.text = "Unknown level";

        if (directionsText != null)
            directionsText.text = "Directions data could not be loaded.";

        if (logoImage != null)
            logoImage.style.display = DisplayStyle.None;

        if (logoContainer != null)
            logoContainer.style.display = DisplayStyle.None;
    }

    // ============================================================
    // BUTTON HANDLERS
    // ============================================================
    private void OnBackClicked()
    {
        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[CodexDetailView] NavigationManager is missing.");
            return;
        }

        NavigationManager.Instance.GoBack();
    }

    private void OnScanQRClicked()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[CodexDetailView] DataManager is missing.");
            return;
        }

        if (DataManager.Instance.SelectedCodexItem == null)
        {
            Debug.LogError("[CodexDetailView] No selected codex item. Cannot scan QR.");
            return;
        }

        Debug.Log("[CodexDetailView] Opening QRScanner for: " + DataManager.Instance.SelectedCodexItem.levelTitle);

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[CodexDetailView] NavigationManager is missing.");
            return;
        }

        NavigationManager.Instance.NavigateTo("QRScannerUIToolkit");
    }
}