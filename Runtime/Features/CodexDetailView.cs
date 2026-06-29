using Core.Models;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CodexDetailView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image logoImage;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI directionsText;
    [SerializeField] private Button scanQRButton;
    [SerializeField] private Button backButton;

    private void Start()
    {
        SetupButtons();
        LoadSelectedDirectionsData();
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (scanQRButton != null)
        {
            scanQRButton.onClick.RemoveAllListeners();
            scanQRButton.onClick.AddListener(OnScanQRClicked);
        }
    }

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

        // ------------------------------------------------------
        //                  IGNORE IMAGES FOR NOW
        // ------------------------------------------------------
        /*
         * if (logoImage != null)
        {
            bool hasDirectionImage =
                directions.imageGUIDs != null &&
                directions.imageGUIDs.Count > 0 &&
                !string.IsNullOrEmpty(directions.imageGUIDs[0]);

            // For the demo, hide it unless you already have a resolver for direction images.
            logoImage.gameObject.SetActive(false);

            if (hasDirectionImage)
            {
                Debug.Log("[CodexDetailView] Direction image GUID found, but image loading is disabled for demo.");
            }
        }
         */
        
    }

    private void ShowFallbackText()
    {
        if (levelTitleText != null)
            levelTitleText.text = "Unknown level";

        if (directionsText != null)
            directionsText.text = "Directions data could not be loaded.";

        if (logoImage != null)
            logoImage.gameObject.SetActive(false);
    }

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

        NavigationManager.Instance.NavigateTo("QRScanner");
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        if (scanQRButton != null)
            scanQRButton.onClick.RemoveAllListeners();
    }
}