using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class MapPage : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    // Elementi UI Toolkit
    private VisualElement root;
    private VisualElement mapContainer;
    private Image mapImage;
    private Button btnExitMap;
    private Button btnZoomIn;
    private Button btnZoomOut;

    // ============================================================
    // MAP SETTINGS
    // ============================================================
    [Header("Map Settings")]
    [SerializeField] private float zoomFactor = 1.5f;
    private const float MAXZOOM = 8f;
    private float currentZoom = 1f;

    // ============================================================
    // POI REFERENCES
    // ============================================================
    private readonly List<MapLocation> pois = new List<MapLocation>();

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[MapPage] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        SetupButtons();
        SetupMap();
        GetPois();
    }

    private void OnDisable()
    {
        if (btnExitMap != null)
            btnExitMap.clicked -= OnExitMapClicked;

        if (btnZoomIn != null)
            btnZoomIn.clicked -= OnZoomInClicked;

        if (btnZoomOut != null)
            btnZoomOut.clicked -= OnZoomOutClicked;
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        mapContainer = root.Q<VisualElement>("map_container");
        mapImage = root.Q<Image>("map_image");
        btnExitMap = root.Q<Button>("exit_map_button");
        btnZoomIn = root.Q<Button>("zoom_in_button");
        btnZoomOut = root.Q<Button>("zoom_out_button");

        if (mapContainer == null)
            Debug.LogWarning("[MapPage] 'map_container' not found in UXML.");

        if (mapImage == null)
            Debug.LogWarning("[MapPage] 'map_image' not found in UXML.");

        if (btnExitMap == null)
            Debug.LogWarning("[MapPage] 'exit_map_button' not found in UXML.");

        if (btnZoomIn == null)
            Debug.LogWarning("[MapPage] 'zoom_in_button' not found in UXML.");

        if (btnZoomOut == null)
            Debug.LogWarning("[MapPage] 'zoom_out_button' not found in UXML.");
    }

    // ============================================================
    // SETUP
    // ============================================================
    private void SetupButtons()
    {
        if (btnExitMap != null)
        {
            btnExitMap.clicked -= OnExitMapClicked;
            btnExitMap.clicked += OnExitMapClicked;
        }

        if (btnZoomIn != null)
        {
            btnZoomIn.clicked -= OnZoomInClicked;
            btnZoomIn.clicked += OnZoomInClicked;
        }

        if (btnZoomOut != null)
        {
            btnZoomOut.clicked -= OnZoomOutClicked;
            btnZoomOut.clicked += OnZoomOutClicked;
        }
    }

    private void SetupMap()
    {
        if (mapImage == null || mapContainer == null)
            return;

        // reset zoom
        currentZoom = 1f;
        mapImage.style.scale = new Scale(Vector3.one);
        mapImage.style.transformOrigin = new TransformOrigin(0, 0, 0);

        Texture2D mapTexture = Resources.Load<Texture2D>("Images/archaeopark map");

        if (mapTexture != null)
        {
            mapImage.image = mapTexture;
            mapImage.scaleMode = ScaleMode.ScaleToFit;
            mapImage.style.width = new Length(100, LengthUnit.Percent);
            mapImage.style.height = new Length(100, LengthUnit.Percent);
            mapImage.style.alignSelf = Align.Center;
            Debug.Log("[MapPage] Map image loaded successfully.");
        }
        else
        {
            Debug.LogError("[MapPage] Failed to load map image from Resources/Images/archaeopark map");
        }

        Debug.Log("[MapPage] Map setup complete.");
    }

    private void GetPois()
    {
        pois.Clear();

        if (mapImage == null)
            return;

        var locationElements = mapImage.Query<VisualElement>(className: "map-location").ToList();

        foreach (var element in locationElements)
        {
            // Se hai un componente MapLocation associato al VisualElement
            // MapLocation mapLocation = element.userData as MapLocation;
            // if (mapLocation != null) pois.Add(mapLocation);
        }

        // Invece, se MapLocation è un MonoBehaviour su GameObject separati,
        // cerca nella scena i componenti MapLocation
        MapLocation[] foundLocations = FindObjectsOfType<MapLocation>();
        foreach (var location in foundLocations)
        {
            pois.Add(location);
        }

        Debug.Log($"[MapPage] Found {pois.Count} POIs.");
    }

    // ============================================================
    // ZOOM FUNCTIONS
    // ============================================================
    public void ZoomInMap()
    {
        if (currentZoom * zoomFactor > MAXZOOM)
        {
            Debug.Log("[MapPage] Max zoom reached.");
            return;
        }

        currentZoom *= zoomFactor;
        ApplyZoom();
        UpdateAllPois();
        Debug.Log($"[MapPage] Zoom in: {currentZoom}");
    }

    public void ZoomOutMap()
    {
        if (currentZoom / zoomFactor < 1f)
        {
            Debug.Log("[MapPage] Min zoom reached.");
            return;
        }

        currentZoom /= zoomFactor;
        ApplyZoom();
        UpdateAllPois();
        Debug.Log($"[MapPage] Zoom out: {currentZoom}");
    }

    private void ApplyZoom()
    {
        if (mapImage == null)
            return;

        mapImage.style.scale = new Scale(new Vector3(currentZoom, currentZoom, 1f));
        mapImage.style.transformOrigin = new TransformOrigin(0, 0, 0);
    }

    private void UpdateAllPois()
    {
        foreach (var poi in pois)
        {
            if (poi != null)
                poi.UpdateUi();
        }
    }

    // ============================================================
    // BUTTON HANDLERS
    // ============================================================
    private void OnExitMapClicked()
    {
        Debug.Log("[MapPage] Exit map clicked.");

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[MapPage] NavigationManager missing.");
            return;
        }

        NavigationManager.Instance.NavigateTo("MenuUIToolkit");
    }

    private void OnZoomInClicked()
    {
        ZoomInMap();
    }

    private void OnZoomOutClicked()
    {
        ZoomOutMap();
    }
}