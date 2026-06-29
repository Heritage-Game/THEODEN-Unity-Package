using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the initial POI presentation screen.
/// </summary>
/// <remarks>
/// This view displays the currently loaded POI before the challenge starts.
/// It shows the POI title, a short narrative description, an optional image carousel,
/// and exposes navigation buttons for going back or starting the challenge.
/// 
/// The POI data is expected to be already loaded in <see cref="DataManager.SelectedPOI"/>.
/// Media assets are expected to have already been resolved by the runtime loading pipeline,
/// so this view reads loaded <see cref="Sprite"/> references directly from the runtime model.
/// </remarks>
public class CodexInitialView : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================

    /// <summary>
    /// Image component used to display the current POI image.
    /// </summary>
    [Header("UI References")]
    [SerializeField] private Image poiImage;

    /// <summary>
    /// Text component used to display the POI title.
    /// </summary>
    [SerializeField] private TextMeshProUGUI levelTitleText;

    /// <summary>
    /// Text component used to display the POI short description.
    /// </summary>
    [SerializeField] private TextMeshProUGUI descriptionText;

    /// <summary>
    /// Button used to enter the challenge scene.
    /// </summary>
    [SerializeField] private Button playButton;

    /// <summary>
    /// Button used to navigate back to the previous scene.
    /// </summary>
    [SerializeField] private Button backButton;

    /// <summary>
    /// Parent transform where carousel dots are instantiated.
    /// </summary>
    [Header("Dots")]
    [SerializeField] private Transform dotsParent;

    /// <summary>
    /// Prefab used to represent one carousel dot.
    /// </summary>
    [SerializeField] private GameObject dotPrefab;

    // ============================================================
    // RUNTIME STATE
    // ============================================================

    /// <summary>
    /// Sprites loaded from the current POI model and shown in the carousel.
    /// </summary>
    private readonly List<Sprite> loadedSprites = new List<Sprite>();

    /// <summary>
    /// Runtime instances of carousel dot images.
    /// </summary>
    private readonly List<Image> dots = new List<Image>();

    /// <summary>
    /// Index of the currently displayed image.
    /// </summary>
    private int currentIndex;

    /// <summary>
    /// Position where the current mouse/touch swipe started.
    /// </summary>
    private Vector2 touchStartPos;

    /// <summary>
    /// Minimum horizontal swipe distance required to change image.
    /// </summary>
    [SerializeField] private float swipeThreshold = 50f;

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    /// <summary>
    /// Initializes button listeners and loads data from the DataManager.
    /// </summary>
    private void Start()
    {
        SetupButtons();
        LoadData();
    }

    /// <summary>
    /// Checks for swipe gestures used to navigate the image carousel.
    /// </summary>
    private void Update()
    {
        HandleSwipe();
    }

    /// <summary>
    /// Removes button listeners when the view is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        if (playButton != null)
            playButton.onClick.RemoveAllListeners();
    }

    // ============================================================
    // SETUP
    // ============================================================

    /// <summary>
    /// Registers UI button callbacks.
    /// </summary>
    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClicked);
        }
    }

    /// <summary>
    /// Loads the selected POI from the DataManager and updates the UI.
    /// </summary>
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
        CreateDots();
        UpdateVisuals();
    }

    /// <summary>
    /// Updates title and description text using the selected POI data.
    /// </summary>
    /// <param name="poi">
    /// Currently selected POI model.
    /// </param>
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

    /// <summary>
    /// Displays fallback UI when no valid POI data is available.
    /// </summary>
    private void ShowFallback()
    {
        if (levelTitleText != null)
            levelTitleText.text = "Unknown";

        if (descriptionText != null)
            descriptionText.text = "No data found.";

        if (poiImage != null)
            poiImage.gameObject.SetActive(false);

        if (playButton != null)
            playButton.interactable = false;
    }

    // ============================================================
    // IMAGE LOADING
    // ============================================================

    /// <summary>
    /// Reads already-resolved POI image sprites from the runtime model.
    /// </summary>
    /// <param name="poi">
    /// Currently selected POI model.
    /// </param>
    /// <remarks>
    /// This method does not load assets from Addressables directly.
    /// The DataManager is responsible for loading the POI and resolving its media assets
    /// before this scene is shown.
    /// </remarks>
    private void LoadImages(POIModel poi)
    {
        loadedSprites.Clear();

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

            loadedSprites.Add(imageData.sprite);
        }

        currentIndex = 0;

        Debug.Log("[CodexInitialView] Loaded sprites: " + loadedSprites.Count);
    }

    // ============================================================
    // DOTS
    // ============================================================

    /// <summary>
    /// Creates carousel dot UI elements based on the number of loaded sprites.
    /// </summary>
    private void CreateDots()
    {
        if (dotsParent == null)
        {
            Debug.LogWarning("[CodexInitialView] Dots parent not assigned.");
            return;
        }

        foreach (Transform child in dotsParent)
            Destroy(child.gameObject);

        dots.Clear();

        if (dotPrefab == null)
        {
            Debug.LogWarning("[CodexInitialView] Dot prefab not assigned.");
            return;
        }

        for (int i = 0; i < loadedSprites.Count; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsParent);
            Image dotImage = dot.GetComponent<Image>();

            if (dotImage != null)
                dots.Add(dotImage);
        }
    }

    /// <summary>
    /// Updates the visual state of carousel dots according to the current image index.
    /// </summary>
    private void UpdateDots()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i] == null)
                continue;

            dots[i].color = i == currentIndex
                ? Color.white
                : new Color(1f, 1f, 1f, 0.3f);
        }
    }

    // ============================================================
    // SWIPE
    // ============================================================

    /// <summary>
    /// Handles mouse or touch swipe input used to navigate between POI images.
    /// </summary>
    private void HandleSwipe()
    {
        if (loadedSprites.Count <= 1)
            return;

#if UNITY_EDITOR
        HandleMouseSwipe();
#else
        HandleTouchSwipe();
#endif
    }

    /// <summary>
    /// Handles swipe navigation using mouse input in the Unity Editor.
    /// </summary>
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

    /// <summary>
    /// Handles swipe navigation using touch input on device.
    /// </summary>
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

    /// <summary>
    /// Processes a swipe delta and changes image if the swipe is large enough.
    /// </summary>
    /// <param name="delta">
    /// Difference between swipe end and start position.
    /// </param>
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

    /// <summary>
    /// Shows the next image in the carousel.
    /// </summary>
    private void NextImage()
    {
        if (loadedSprites.Count == 0)
            return;

        currentIndex = (currentIndex + 1) % loadedSprites.Count;
        UpdateVisuals();
    }

    /// <summary>
    /// Shows the previous image in the carousel.
    /// </summary>
    private void PrevImage()
    {
        if (loadedSprites.Count == 0)
            return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = loadedSprites.Count - 1;

        UpdateVisuals();
    }

    /// <summary>
    /// Updates the displayed image and the carousel dots.
    /// </summary>
    private void UpdateVisuals()
    {
        if (poiImage == null)
            return;

        if (loadedSprites.Count == 0)
        {
            poiImage.gameObject.SetActive(false);
            Debug.LogWarning("[CodexInitialView] No sprites to display.");
            return;
        }

        poiImage.gameObject.SetActive(true);
        poiImage.sprite = loadedSprites[currentIndex];

        UpdateDots();
    }

    // ============================================================
    // BUTTONS
    // ============================================================

    /// <summary>
    /// Handles the Back button click.
    /// </summary>
    private void OnBackClicked()
    {
        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[CodexInitialView] NavigationManager missing.");
            return;
        }

        NavigationManager.Instance.GoBack();
    }

    /// <summary>
    /// Handles the Play button click and navigates to the challenge scene.
    /// </summary>
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

        NavigationManager.Instance.NavigateTo("ChallengeGame");
    }
}