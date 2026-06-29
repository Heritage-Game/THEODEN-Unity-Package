using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json;
using System.Collections.Generic;

public class POISummaryPage : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text poiNameText;
    public TMP_Text summaryText;
    public Image poiImage;
    public Button playGameButton;
    public Button closeButton;

    [SerializeField] GameObject caroselPanel;
    [SerializeField] GameObject imagesButton;

    [Header("Test Images (drag placeholder sprites here)")]
    public List<Sprite> placeholderImages;

    // The current POI data
    private PointOfInterest currentPOI;
    private Root loadedData;

    void Start()
    {
        closeButton.onClick.AddListener(OnCloseClicked);
        playGameButton.onClick.AddListener(OnPlayGameClicked);

        // Get the POI ID that was saved by the QR scanner
        string scannedPOI_ID = PlayerPrefs.GetString("ScannedPOI_ID", "");
        Debug.Log($"[POISummary] Loading POI with ID: {scannedPOI_ID}");

        if (string.IsNullOrEmpty(scannedPOI_ID))
        {
            Debug.LogError("[POISummary] No POI ID found! Did you come from QR scanner?");
            summaryText.text = "Error: No POI data found.";
            return;
        }

        // Load mock data from Resources
        LoadMockData();

        // Find the scanned POI
        currentPOI = FindPOIById(scannedPOI_ID);

        if (currentPOI != null)
        {
            DisplayPOI(currentPOI);
        }
        else
        {
            Debug.LogError($"[POISummary] POI with ID '{scannedPOI_ID}' not found in data!");
            summaryText.text = $"POI '{scannedPOI_ID}' not found in mock data.";
        }
    }

    /// <summary>
    /// Load mock JSON data from Resources folder.
    /// When universities connect their real server, this will be replaced
    /// with data fetched via CommonVariables.URL
    /// </summary>
    void LoadMockData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("MockData/test_poi_data");

        if (jsonFile == null)
        {
            Debug.LogError("[POISummary] Could not find MockData/test_poi_data.json in Resources!");
            return;
        }

        loadedData = JsonConvert.DeserializeObject<Root>(jsonFile.text);
        Debug.Log($"[POISummary] Loaded {loadedData.GameData.PointsOfInterest.Count} POIs from mock data");
    }

    /// <summary>
    /// Find a specific POI by its ID from the loaded data.
    /// </summary>
    PointOfInterest FindPOIById(string poiId)
    {
        if (loadedData?.GameData?.PointsOfInterest == null)
            return null;

        foreach (var poi in loadedData.GameData.PointsOfInterest)
        {
            if (poi.Id == poiId)
                return poi;
        }
        return null;
    }

    /// <summary>
    /// Display the POI data on the UI.
    /// Maps POiDefinition.cs fields to UI elements.
    /// 
    /// POiDefinition.cs field    →    UI element
    /// ─────────────────────────────────────────
    /// poi.Name                  →    poiNameText      ("Ancient Column")
    /// poi.Category              →    categoryText     ("Historical Monument")
    /// poi.Story.ShortSummary    →    summaryText      (short description)
    /// poi.Media.Images[0]       →    poiImage         (placeholder for now)
    /// </summary>
    void DisplayPOI(PointOfInterest poi)
    {
        Debug.Log($"[POISummary] Displaying: {poi.Name}");

        if (poiNameText != null)
            poiNameText.text = "<size=40><b>" + poi.Name + "</b></size>" +
                System.Environment.NewLine + "<size=30><b>" + poi.Category + "</b></size>";

        if (summaryText != null)
            summaryText.text = poi.Story.ShortSummary;

        // Load placeholder image
        if (poiImage != null && placeholderImages != null && placeholderImages.Count > 0)
        {
            int index = loadedData.GameData.PointsOfInterest.IndexOf(poi);
            int imageIndex = index % placeholderImages.Count;
            Sprite sprite = placeholderImages[imageIndex];

            poiImage.sprite = sprite;
            poiImage.color = Color.white;

            // Update Aspect Ratio Fitter if it exists
            AspectRatioFitter fitter = poiImage.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                float ratio = sprite.rect.width / sprite.rect.height;
                fitter.aspectRatio = ratio;
                Debug.Log($"[POISummary] Image aspect ratio set to: {ratio}");
            }

            Debug.Log($"[POISummary] Image loaded: {sprite.name}");
        }
        else if (poiImage != null)
        {
            poiImage.color = new Color(0.85f, 0.92f, 0.98f);
            Debug.LogWarning("[POISummary] No placeholder images! Drag sprites into the list on POISummaryManager.");
        }

        Debug.Log($"[POISummary] ✅ Displayed: {poi.Name} / {poi.Category}");
    }
    void OnPlayGameClicked()
    {
        if (currentPOI == null) return;

        Debug.Log($"[POISummary] Play Game clicked! Loading challenge for: {currentPOI.Name}");

        // Save current POI ID for the challenge scene
        PlayerPrefs.SetString("CurrentPOI_ID", currentPOI.Id);
        PlayerPrefs.Save();

        SafeLoadScene("ChallangeGame");
    }

    void OnCloseClicked()
    {
        SafeLoadScene("Discover");
    }

    void SafeLoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Transitions.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning(
                $"Scene '{sceneName}' not in Build Settings!\n" +
                $"Go to File → Build Profiles → Add Open Scenes"
            );
        }
    }

    //Methods to activate and deactivate the carosel panel, called by buttons
    public void OpenCarosel()
    {
        caroselPanel.SetActive(true);
    }
    public void CloseCarosel()
    {
        caroselPanel.SetActive(false);
    }
    //If there are no other images to display, disable "Images" button
    private void DisableImagesButton()
    {
        //TODO
        imagesButton.SetActive(false);
    }
}