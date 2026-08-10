using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the initial POI presentation screen using UI Toolkit.
/// </summary>
public class CodexInitialView : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement imageContainer;
    private VisualElement poiImage;
    private Label levelTitleText;
    private Label descriptionText;
    private Button playButton;
    private Button backButton;

    // ============================================================
    // RUNTIME STATE
    // ============================================================
    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();
    private int currentIndex;
    private Vector2 touchStartPos;

    [SerializeField] private float swipeThreshold = 50f;

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
        SetupButtons();
        LoadData();
    }

    private void OnDisable()
    {
        if (playButton != null)
            playButton.clicked -= OnPlayClicked;

        if (backButton != null)
            backButton.clicked -= OnBackClicked;
    }

    private void Update()
    {
        HandleSwipe();
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        imageContainer = root.Q<VisualElement>("image_container");
        poiImage = root.Q<VisualElement>("poi_image");
        levelTitleText = root.Q<Label>("title_label");
        //descriptionScroll = root.Q<ScrollView>("description_scroll");
        descriptionText = root.Q<Label>("description_label");
        playButton = root.Q<Button>("play_button");
        backButton = root.Q<Button>("back_button");
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

        if (playButton != null)
        {
            playButton.clicked -= OnPlayClicked;
            playButton.clicked += OnPlayClicked;
        }
    }

    private void LoadData()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[CodexInitialView] DataManager is missing.");
            ShowFallback();
            return;
        }

        POIModel poi = DataManager.Instance.SelectedPOI;

        if (poi == null)
        {
            Debug.LogError("[CodexInitialView] SelectedPOI is null.");
            ShowFallback();
            return;
        }

        UpdateTextContent(poi);
        LoadImages(poi);
        UpdateVisuals();
    }

    private void UpdateTextContent(POIModel poi)
    {
        if (levelTitleText != null)
        {
            levelTitleText.text = string.IsNullOrWhiteSpace(poi.poiName)
                ? "Unknown POI"
                : poi.poiName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(poi.shortSummary)
                ? "No description available."
                : poi.shortSummary;
        }
    }

    private void ShowFallback()
    {
        if (levelTitleText != null)
            levelTitleText.text = "Unknown";

        if (descriptionText != null)
            descriptionText.text = "No data found.";

        if (poiImage != null)
            poiImage.style.display = DisplayStyle.None;

        if (playButton != null)
            playButton.SetEnabled(false);
    }

    // ============================================================
    // IMAGE LOADING
    // ============================================================
    private void LoadImages(POIModel poi)
    {
        loadedTextures.Clear();

        if (poi.images == null || poi.images.Count == 0)
        {
            Debug.LogWarning("[CodexInitialView] No images found in POI model.");
            return;
        }

        foreach (POIModel.ImageReference imageData in poi.images)
        {
            if (imageData == null)
                continue;

            if (imageData.sprite == null)
            {
                Debug.LogWarning(
                    "[CodexInitialView] Image sprite is null for address: " +
                    imageData.address
                );
                continue;
            }

            Texture2D texture = imageData.sprite.texture;
            if (texture != null)
                loadedTextures.Add(texture);
        }

        currentIndex = 0;
        Debug.Log("[CodexInitialView] Loaded textures: " + loadedTextures.Count);
    }

    // ============================================================
    // SWIPE
    // ============================================================
    private void HandleSwipe()
    {
        if (loadedTextures.Count <= 1) return;
    }

    private void HandleMouseSwipe()
    {
        if (Input.GetMouseButtonDown(0))
            touchStartPos = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 delta = (Vector2)Input.mousePosition - touchStartPos;
            ProcessSwipeDelta(delta);
        }
    }

    private void HandleTouchSwipe()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            touchStartPos = touch.position;
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            Vector2 delta = touch.position - touchStartPos;
            ProcessSwipeDelta(delta);
        }
    }

    private void ProcessSwipeDelta(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) <= swipeThreshold)
            return;

        if (delta.x > 0)
            PrevImage();
        else
            NextImage();
    }

    // ============================================================
    // IMAGE NAVIGATION
    // ============================================================
    private void NextImage()
    {
        if (loadedTextures.Count == 0)
            return;

        currentIndex = (currentIndex + 1) % loadedTextures.Count;
        UpdateVisuals();
    }

    private void PrevImage()
    {
        if (loadedTextures.Count == 0)
            return;

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = loadedTextures.Count - 1;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (poiImage == null)
            return;

        if (loadedTextures.Count == 0)
        {
            poiImage.style.display = DisplayStyle.None;
            Debug.LogWarning("[CodexInitialView] No textures to display.");
            return;
        }

        poiImage.style.display = DisplayStyle.Flex;
        //poiImage.image = loadedTextures[currentIndex];
        poiImage.style.backgroundImage = new StyleBackground(loadedTextures[currentIndex]);
    }

    // ============================================================
    // BUTTONS
    // ============================================================
    private void OnBackClicked()
    {
        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[CodexInitialView] NavigationManager missing.");
            return;
        }

        NavigationManager.Instance.GoBack();
    }

    private void OnPlayClicked()
    {
        if (DataManager.Instance == null || DataManager.Instance.SelectedPOI == null)
        {
            Debug.LogError("[CodexInitialView] Cannot open challenge. SelectedPOI is null.");
            return;
        }

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[CodexInitialView] NavigationManager missing.");
            return;
        }

        POIModel selectedPOI = DataManager.Instance.SelectedPOI;

        if (selectedPOI is OpenAnswerPOIModel)
        {
            NavigationManager.Instance.NavigateTo("OpenAnswerUIToolkit");
        }
        else if (selectedPOI is MultipleChoicePOIModel)
        {
            NavigationManager.Instance.NavigateTo("ChallengeUIToolkit");
        }
        else
        {
            Debug.LogError($"[CodexInitialView] Unknown POI type: {selectedPOI.GetType().Name}");
        }

        //NavigationManager.Instance.NavigateTo("ChallengeUIToolkit");
    }
}