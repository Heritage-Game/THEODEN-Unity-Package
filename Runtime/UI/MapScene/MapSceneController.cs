using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ContentLoading;
using Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Runtime controller for the THEODEN map scene.
/// Loads the project MapDefinition, creates the POI pins and manages
/// selection, zoom, pan, responsive layout and scene navigation.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public sealed class MapSceneController : MonoBehaviour
{
    private const float MinimumZoom = 1f;
    private const float MaximumZoom = 4f;
    private const float ZoomStep = 0.35f;

    [Header("Scene names")]
    [SerializeField] private string menuSceneName = "MenuUIToolkit";
    [SerializeField] private string discoverSceneName = "QRScannerUIToolkit";
    [SerializeField] private string codexSceneName = "CodexUIToolkit";
    [SerializeField] private string scoreSceneName = "LeaderboardUIToolkit";
    [SerializeField] private string directionsSceneName = "CodexDetailUIToolkit";
    [SerializeField] private string completedPoiSceneName = "POIRecapUIToolkit";

    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement safeArea;
    private VisualElement mapViewport;
    private VisualElement mapContent;
    private VisualElement mapImage;
    private VisualElement mapPlaceholder;
    private VisualElement pinsLayer;
    private VisualElement statusOverlay;
    private Label statusLabel;
    private Label progressLabel;
    private VisualElement selectedPoiCard;
    private VisualElement selectedPoiCardShadow;
    private VisualElement selectedPoiThumbnail;
    private Label selectedPoiTitle;
    private Label selectedPoiDistance;
    private Label selectedPoiStatus;
    private Button viewDirectionsButton;

    private Button backButton;
    private Button menuButton;
    private Button zoomInButton;
    private Button zoomOutButton;
    private Button recenterButton;
    private Button discoverButton;
    private Button mapButton;
    private Button codexButton;
    private Button scoreButton;

    private readonly Dictionary<string, Button> pinButtons = new();
    private readonly Dictionary<int, Vector2> activePointers = new();

    private MapDefinition mapDefinition;
    private MapPinDefinition selectedPin;
    private CodexItemDefinition selectedCodexItem;

    private float zoom = MinimumZoom;
    private Vector2 panOffset;
    private Vector2 fittedMapSize;
    private Vector2 previousSinglePointerPosition;
    private float previousPinchDistance;
    private Vector2 previousPinchCenter;
    private bool isDestroyed;
    private bool isOpeningPoi;

    private async void Start()
    {
        try
        {
            BindVisualElements();
            RegisterCallbacks();
            ApplySafeArea();
            SetLoadingState("Loading map…");

            await EnsureCodexIsLoadedAsync();
            mapDefinition =
                await TheodenRuntimeContentLoader.LoadMapDefinitionAsync();

            if (isDestroyed)
            {
                if (mapDefinition != null)
                    TheodenRuntimeContentLoader.ReleaseAsset(mapDefinition);

                mapDefinition = null;
                return;
            }

            if (mapDefinition == null || mapDefinition.MapImage == null)
            {
                SetErrorState(
                    "The map configuration is missing or has no image."
                );
                return;
            }

            ConfigureMapImage();
            CreatePins();
            UpdateProgressLabel();
            RecalculateMapLayout();
            SelectInitialPin();
            HideStatusOverlay();
        }
        catch (Exception exception)
        {
            if (!isDestroyed)
                SetErrorState("The project map could not be loaded.");

            Debug.LogError(
                "[MapSceneController] Failed to initialize the map."
            );
            Debug.LogException(exception);
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        UnregisterCallbacks();

        if (mapDefinition != null)
        {
            TheodenRuntimeContentLoader.ReleaseAsset(mapDefinition);
            mapDefinition = null;
        }
    }

    private void BindVisualElements()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        safeArea = RequireElement<VisualElement>("safe_area");
        mapViewport = RequireElement<VisualElement>("map_viewport");
        mapContent = RequireElement<VisualElement>("map_content");
        mapImage = RequireElement<VisualElement>("map_image");
        mapPlaceholder = RequireElement<VisualElement>("map_placeholder");
        pinsLayer = RequireElement<VisualElement>("pins_layer");
        statusOverlay = RequireElement<VisualElement>("map_status_overlay");
        statusLabel = RequireElement<Label>("map_status_label");
        progressLabel = RequireElement<Label>("map_progress_label");

        selectedPoiCard = RequireElement<VisualElement>("selected_poi_card");
        selectedPoiCardShadow =
            RequireElement<VisualElement>("selected_poi_card_shadow");
        selectedPoiThumbnail =
            RequireElement<VisualElement>("selected_poi_thumbnail");
        selectedPoiTitle = RequireElement<Label>("selected_poi_title");
        selectedPoiDistance = RequireElement<Label>("selected_poi_distance");
        selectedPoiStatus = RequireElement<Label>("selected_poi_status");
        viewDirectionsButton =
            RequireElement<Button>("view_directions_button");

        backButton = RequireElement<Button>("back_button");
        menuButton = RequireElement<Button>("menu_button");
        zoomInButton = RequireElement<Button>("zoom_in_button");
        zoomOutButton = RequireElement<Button>("zoom_out_button");
        recenterButton = RequireElement<Button>("recenter_button");
        discoverButton = RequireElement<Button>("discover_nav_button");
        mapButton = RequireElement<Button>("map_nav_button");
        codexButton = RequireElement<Button>("codex_nav_button");
        scoreButton = RequireElement<Button>("score_nav_button");

        selectedPoiDistance.style.display = DisplayStyle.None;
        SetPoiCardVisible(false);
    }

    private TElement RequireElement<TElement>(string elementName)
        where TElement : VisualElement
    {
        TElement element = root.Q<TElement>(elementName);

        if (element == null)
        {
            throw new InvalidOperationException(
                $"MapScene UXML element '{elementName}' was not found."
            );
        }

        return element;
    }

    private void RegisterCallbacks()
    {
        backButton.clicked += OnBackClicked;
        menuButton.clicked += OnMenuClicked;
        zoomInButton.clicked += OnZoomInClicked;
        zoomOutButton.clicked += OnZoomOutClicked;
        recenterButton.clicked += RecenterMap;
        discoverButton.clicked += OnDiscoverClicked;
        mapButton.clicked += RecenterMap;
        codexButton.clicked += OnCodexClicked;
        scoreButton.clicked += OnScoreClicked;
        viewDirectionsButton.clicked += OnViewPoiClicked;

        root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        mapViewport.RegisterCallback<GeometryChangedEvent>(
            OnViewportGeometryChanged
        );
        mapViewport.RegisterCallback<WheelEvent>(OnWheel);
        mapViewport.RegisterCallback<PointerDownEvent>(OnPointerDown);
        mapViewport.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        mapViewport.RegisterCallback<PointerUpEvent>(OnPointerUp);
        mapViewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
    }

    private void UnregisterCallbacks()
    {
        if (root == null)
            return;

        backButton.clicked -= OnBackClicked;
        menuButton.clicked -= OnMenuClicked;
        zoomInButton.clicked -= OnZoomInClicked;
        zoomOutButton.clicked -= OnZoomOutClicked;
        recenterButton.clicked -= RecenterMap;
        discoverButton.clicked -= OnDiscoverClicked;
        mapButton.clicked -= RecenterMap;
        codexButton.clicked -= OnCodexClicked;
        scoreButton.clicked -= OnScoreClicked;
        viewDirectionsButton.clicked -= OnViewPoiClicked;

        root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        mapViewport.UnregisterCallback<GeometryChangedEvent>(
            OnViewportGeometryChanged
        );
        mapViewport.UnregisterCallback<WheelEvent>(OnWheel);
        mapViewport.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        mapViewport.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        mapViewport.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        mapViewport.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
    }

    private async Task EnsureCodexIsLoadedAsync()
    {
        if (DataManager.Instance == null)
        {
            throw new InvalidOperationException(
                "DataManager is missing from the runtime application."
            );
        }

        if (!DataManager.Instance.IsDataLoaded)
            await DataManager.Instance.LoadCodexMenuAsync();

        if (!DataManager.Instance.IsDataLoaded)
        {
            throw new InvalidOperationException(
                "The Codex could not be loaded for the map scene."
            );
        }
    }

    private void ConfigureMapImage()
    {
        mapPlaceholder.style.display = DisplayStyle.None;
        mapImage.style.backgroundImage =
            new StyleBackground(mapDefinition.MapImage);
    }

    private void CreatePins()
    {
        pinsLayer.Clear();
        pinButtons.Clear();

        foreach (MapPinDefinition pinDefinition in mapDefinition.Pins)
        {
            if (pinDefinition == null ||
                string.IsNullOrWhiteSpace(pinDefinition.PoiId))
            {
                continue;
            }

            CodexItemDefinition codexItem =
                DataManager.Instance.GetCodexItemByPoiId(
                    pinDefinition.PoiId
                );

            Button pinButton = new Button
            {
                name = "map_pin_" + pinDefinition.PoiId,
                tooltip = ResolvePoiTitle(codexItem, pinDefinition.PoiId)
            };

            pinButton.AddToClassList("map-pin");
            ApplyStateClass(pinButton, codexItem);

            pinButton.style.left = Length.Percent(
                pinDefinition.NormalizedPosition.x * 100f
            );
            pinButton.style.top = Length.Percent(
                pinDefinition.NormalizedPosition.y * 100f
            );

            VisualElement icon = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            icon.AddToClassList("map-pin-icon");
            icon.AddToClassList(GetPinIconClass(codexItem));
            pinButton.Add(icon);

            MapPinDefinition capturedPin = pinDefinition;
            pinButton.clicked += () => SelectPin(capturedPin);

            pinsLayer.Add(pinButton);
            pinButtons[pinDefinition.PoiId] = pinButton;
        }

        UpdatePinScale();
    }

    private static void ApplyStateClass(
        VisualElement pin,
        CodexItemDefinition item)
    {
        CodexItemState state = item?.state ?? CodexItemState.Locked;

        pin.AddToClassList(state switch
        {
            CodexItemState.Unlocked => "map-pin--completed",
            CodexItemState.Directions => "map-pin--directions",
            _ => "map-pin--locked"
        });
    }

    private static string GetPinIconClass(CodexItemDefinition item)
    {
        CodexItemState state = item?.state ?? CodexItemState.Locked;

        return state switch
        {
            CodexItemState.Unlocked => "map-pin-icon--completed",
            CodexItemState.Directions => "map-pin-icon--directions",
            _ => "map-pin-icon--locked"
        };
    }

    private void SelectInitialPin()
    {
        MapPinDefinition initialPin = null;

        foreach (MapPinDefinition pin in mapDefinition.Pins)
        {
            CodexItemDefinition item =
                DataManager.Instance.GetCodexItemByPoiId(pin.PoiId);

            if (item != null && item.state == CodexItemState.Directions)
            {
                initialPin = pin;
                break;
            }
        }

        if (initialPin == null && mapDefinition.Pins.Count > 0)
            initialPin = mapDefinition.Pins[0];

        if (initialPin != null)
            SelectPin(initialPin);
    }

    private void SelectPin(MapPinDefinition pinDefinition)
    {
        if (pinDefinition == null)
            return;

        if (selectedPin != null &&
            pinButtons.TryGetValue(
                selectedPin.PoiId,
                out Button previousButton))
        {
            previousButton.RemoveFromClassList("map-pin--selected");
        }

        selectedPin = pinDefinition;
        selectedCodexItem =
            DataManager.Instance.GetCodexItemByPoiId(
                pinDefinition.PoiId
            );

        if (pinButtons.TryGetValue(pinDefinition.PoiId, out Button button))
            button.AddToClassList("map-pin--selected");

        selectedPoiTitle.text =
            ResolvePoiTitle(selectedCodexItem, pinDefinition.PoiId);

        CodexItemState state =
            selectedCodexItem?.state ?? CodexItemState.Locked;

        switch (state)
        {
            case CodexItemState.Directions:
                selectedPoiStatus.text = "Next stop";
                viewDirectionsButton.text = "View directions  ›";
                viewDirectionsButton.SetEnabled(true);
                break;

            case CodexItemState.Unlocked:
                selectedPoiStatus.text = "Completed";
                viewDirectionsButton.text = "View location  ›";
                viewDirectionsButton.SetEnabled(
                    !string.IsNullOrWhiteSpace(completedPoiSceneName)
                );
                break;

            default:
                selectedPoiStatus.text = "Locked";
                viewDirectionsButton.text = "Locked";
                viewDirectionsButton.SetEnabled(false);
                break;
        }

        selectedPoiThumbnail.EnableInClassList(
            "poi-thumbnail--completed",
            state == CodexItemState.Unlocked
        );
        SetPoiCardVisible(true);
    }

    private async void OnViewPoiClicked()
    {
        if (selectedCodexItem == null || isOpeningPoi)
            return;

        isOpeningPoi = true;
        viewDirectionsButton.SetEnabled(false);

        try
        {
            CodexItemState state = selectedCodexItem.state;

            await DataManager.Instance
                .SelectCodexItemAndLoadDirectionsAsync(selectedCodexItem);

            if (state == CodexItemState.Directions &&
                DataManager.Instance.IsDirectionsLoaded)
            {
                NavigationManager.Instance.NavigateTo(directionsSceneName);
                return;
            }

            if (state == CodexItemState.Unlocked &&
                DataManager.Instance.IsPOILoaded &&
                !string.IsNullOrWhiteSpace(completedPoiSceneName))
            {
                NavigationManager.Instance.NavigateTo(completedPoiSceneName);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[MapSceneController] Could not open POI: " +
                selectedCodexItem.poiId
            );
            Debug.LogException(exception);
        }
        finally
        {
            isOpeningPoi = false;

            if (!isDestroyed && selectedCodexItem != null)
            {
                viewDirectionsButton.SetEnabled(
                    selectedCodexItem.state != CodexItemState.Locked
                );
            }
        }
    }

    private static string ResolvePoiTitle(
        CodexItemDefinition item,
        string poiId)
    {
        if (item != null && !string.IsNullOrWhiteSpace(item.levelTitle))
            return item.levelTitle;

        if (string.IsNullOrWhiteSpace(poiId))
            return "Point of interest";

        string readableName = poiId.Replace('_', ' ').Trim();

        return char.ToUpperInvariant(readableName[0]) +
               readableName.Substring(1);
    }

    private void UpdateProgressLabel()
    {
        int completedCount = 0;

        foreach (MapPinDefinition pin in mapDefinition.Pins)
        {
            if (PlayerProgressService.IsPoiCompleted(pin.PoiId))
                completedCount++;
        }

        progressLabel.text =
            $"{completedCount} of {mapDefinition.Pins.Count} " +
            "locations discovered";
    }

    private void SetPoiCardVisible(bool visible)
    {
        selectedPoiCard.style.display =
            visible ? DisplayStyle.Flex : DisplayStyle.None;
        selectedPoiCardShadow.style.display =
            visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnRootGeometryChanged(GeometryChangedEvent geometryEvent)
    {
        ApplySafeArea();

        float width = geometryEvent.newRect.width;
        float height = geometryEvent.newRect.height;
        bool compact = width > height || height < 900f || width < 620f;

        root.EnableInClassList("map-screen--compact", compact);
    }

    private void ApplySafeArea()
    {
        if (root == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect screenSafeArea = Screen.safeArea;
        float panelWidth = root.resolvedStyle.width;
        float panelHeight = root.resolvedStyle.height;

        if (float.IsNaN(panelWidth) || float.IsNaN(panelHeight) ||
            panelWidth <= 0f || panelHeight <= 0f)
        {
            return;
        }

        float horizontalScale = panelWidth / Screen.width;
        float verticalScale = panelHeight / Screen.height;

        safeArea.style.paddingLeft =
            screenSafeArea.xMin * horizontalScale;
        safeArea.style.paddingRight =
            (Screen.width - screenSafeArea.xMax) * horizontalScale;
        safeArea.style.paddingTop =
            (Screen.height - screenSafeArea.yMax) * verticalScale;
        safeArea.style.paddingBottom =
            screenSafeArea.yMin * verticalScale;
    }

    private void OnViewportGeometryChanged(
        GeometryChangedEvent geometryEvent)
    {
        RecalculateMapLayout();
    }

    private void RecalculateMapLayout()
    {
        if (mapDefinition == null || mapDefinition.MapImage == null)
            return;

        float viewportWidth = mapViewport.resolvedStyle.width;
        float viewportHeight = mapViewport.resolvedStyle.height;

        if (float.IsNaN(viewportWidth) || float.IsNaN(viewportHeight) ||
            viewportWidth <= 0f || viewportHeight <= 0f)
        {
            return;
        }

        Rect spriteRect = mapDefinition.MapImage.rect;
        float spriteAspect = spriteRect.width / spriteRect.height;
        float viewportAspect = viewportWidth / viewportHeight;

        float contentWidth;
        float contentHeight;

        if (viewportAspect > spriteAspect)
        {
            contentHeight = viewportHeight;
            contentWidth = contentHeight * spriteAspect;
        }
        else
        {
            contentWidth = viewportWidth;
            contentHeight = contentWidth / spriteAspect;
        }

        fittedMapSize = new Vector2(contentWidth, contentHeight);

        mapContent.style.width = contentWidth;
        mapContent.style.height = contentHeight;
        mapContent.style.left = (viewportWidth - contentWidth) * 0.5f;
        mapContent.style.top = (viewportHeight - contentHeight) * 0.5f;

        ClampPanOffset();
        ApplyMapTransform();
    }

    private void OnZoomInClicked()
    {
        ZoomAtViewportPoint(
            GetViewportCenter(),
            zoom + ZoomStep
        );
    }

    private void OnZoomOutClicked()
    {
        ZoomAtViewportPoint(
            GetViewportCenter(),
            zoom - ZoomStep
        );
    }

    private void OnWheel(WheelEvent wheelEvent)
    {
        float direction = wheelEvent.delta.y < 0f ? 1f : -1f;
        Vector2 viewportPoint =
            mapViewport.WorldToLocal(wheelEvent.mousePosition);

        ZoomAtViewportPoint(
            viewportPoint,
            zoom + direction * ZoomStep
        );

        wheelEvent.StopPropagation();
    }

    private void ZoomAtViewportPoint(
        Vector2 viewportPoint,
        float requestedZoom)
    {
        float newZoom = Mathf.Clamp(
            requestedZoom,
            MinimumZoom,
            MaximumZoom
        );

        if (Mathf.Approximately(newZoom, zoom))
            return;

        Vector2 focusFromCenter = viewportPoint - GetViewportCenter();
        float zoomRatio = newZoom / zoom;

        panOffset = focusFromCenter -
                    (focusFromCenter - panOffset) * zoomRatio;
        zoom = newZoom;

        ClampPanOffset();
        ApplyMapTransform();
    }

    private void RecenterMap()
    {
        zoom = MinimumZoom;
        panOffset = Vector2.zero;
        ApplyMapTransform();
    }

    private void ClampPanOffset()
    {
        float contentWidth = fittedMapSize.x;
        float contentHeight = fittedMapSize.y;
        float viewportWidth = mapViewport.resolvedStyle.width;
        float viewportHeight = mapViewport.resolvedStyle.height;

        if (float.IsNaN(contentWidth) || float.IsNaN(contentHeight) ||
            float.IsNaN(viewportWidth) || float.IsNaN(viewportHeight))
        {
            return;
        }

        float maximumX = Mathf.Max(
            0f,
            (contentWidth * zoom - viewportWidth) * 0.5f
        );
        float maximumY = Mathf.Max(
            0f,
            (contentHeight * zoom - viewportHeight) * 0.5f
        );

        panOffset = new Vector2(
            Mathf.Clamp(panOffset.x, -maximumX, maximumX),
            Mathf.Clamp(panOffset.y, -maximumY, maximumY)
        );
    }

    private void ApplyMapTransform()
    {
        mapContent.transform.position =
            new Vector3(panOffset.x, panOffset.y, 0f);
        mapContent.transform.scale =
            new Vector3(zoom, zoom, 1f);

        UpdatePinScale();
        zoomInButton.SetEnabled(zoom < MaximumZoom);
        zoomOutButton.SetEnabled(zoom > MinimumZoom);
        recenterButton.SetEnabled(
            zoom > MinimumZoom || panOffset.sqrMagnitude > 0.01f
        );
    }

    private void UpdatePinScale()
    {
        float inverseZoom = 1f / Mathf.Max(zoom, MinimumZoom);

        foreach (Button pinButton in pinButtons.Values)
        {
            pinButton.transform.scale =
                new Vector3(inverseZoom, inverseZoom, 1f);
        }
    }

    private void OnPointerDown(PointerDownEvent pointerEvent)
    {
        if (pointerEvent.button != 0 ||
            IsPointerOverButton(pointerEvent.target as VisualElement))
        {
            return;
        }

        Vector2 localPosition =
            mapViewport.WorldToLocal(
                new Vector2(
                    pointerEvent.position.x,
                    pointerEvent.position.y
                )
            );

        activePointers[pointerEvent.pointerId] = localPosition;
        mapViewport.CapturePointer(pointerEvent.pointerId);

        if (activePointers.Count == 1)
        {
            previousSinglePointerPosition = localPosition;
        }
        else if (TryGetFirstTwoPointers(out Vector2 first, out Vector2 second))
        {
            previousPinchDistance = Vector2.Distance(first, second);
            previousPinchCenter = (first + second) * 0.5f;
        }

        pointerEvent.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent pointerEvent)
    {
        if (!activePointers.ContainsKey(pointerEvent.pointerId))
            return;

        Vector2 localPosition =
            mapViewport.WorldToLocal(
                new Vector2(
                    pointerEvent.position.x,
                    pointerEvent.position.y
                )
            );
        activePointers[pointerEvent.pointerId] = localPosition;

        if (activePointers.Count >= 2 &&
            TryGetFirstTwoPointers(out Vector2 first, out Vector2 second))
        {
            float currentDistance = Vector2.Distance(first, second);
            Vector2 currentCenter = (first + second) * 0.5f;

            panOffset += currentCenter - previousPinchCenter;

            if (previousPinchDistance > 0.01f)
            {
                float pinchRatio = currentDistance / previousPinchDistance;
                ZoomAtViewportPoint(currentCenter, zoom * pinchRatio);
            }
            else
            {
                ClampPanOffset();
                ApplyMapTransform();
            }

            previousPinchDistance = currentDistance;
            previousPinchCenter = currentCenter;
        }
        else
        {
            panOffset += localPosition - previousSinglePointerPosition;
            previousSinglePointerPosition = localPosition;
            ClampPanOffset();
            ApplyMapTransform();
        }

        pointerEvent.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent pointerEvent)
    {
        ReleaseTrackedPointer(pointerEvent.pointerId);
        pointerEvent.StopPropagation();
    }

    private void OnPointerCancel(PointerCancelEvent pointerEvent)
    {
        ReleaseTrackedPointer(pointerEvent.pointerId);
        pointerEvent.StopPropagation();
    }

    private void ReleaseTrackedPointer(int pointerId)
    {
        activePointers.Remove(pointerId);

        if (mapViewport.HasPointerCapture(pointerId))
            mapViewport.ReleasePointer(pointerId);

        if (activePointers.Count == 1)
        {
            foreach (Vector2 position in activePointers.Values)
            {
                previousSinglePointerPosition = position;
                break;
            }
        }
    }

    private bool TryGetFirstTwoPointers(
        out Vector2 first,
        out Vector2 second)
    {
        first = Vector2.zero;
        second = Vector2.zero;

        if (activePointers.Count < 2)
            return false;

        int index = 0;

        foreach (Vector2 position in activePointers.Values)
        {
            if (index == 0)
                first = position;
            else
            {
                second = position;
                return true;
            }

            index++;
        }

        return false;
    }

    private static bool IsPointerOverButton(VisualElement target)
    {
        if (target == null)
            return false;

        return target is Button ||
               target.GetFirstAncestorOfType<Button>() != null;
    }

    private Vector2 GetViewportCenter()
    {
        return new Vector2(
            mapViewport.resolvedStyle.width * 0.5f,
            mapViewport.resolvedStyle.height * 0.5f
        );
    }

    private void SetLoadingState(string message)
    {
        statusLabel.text = message;
        statusOverlay.RemoveFromClassList("map-status-overlay--error");
        statusOverlay.style.display = DisplayStyle.Flex;
    }

    private void SetErrorState(string message)
    {
        statusLabel.text = message;
        statusOverlay.AddToClassList("map-status-overlay--error");
        statusOverlay.style.display = DisplayStyle.Flex;
    }

    private void HideStatusOverlay()
    {
        statusOverlay.style.display = DisplayStyle.None;
    }

    private void OnBackClicked()
    {
        NavigationManager.Instance.GoBack();
    }

    private void OnMenuClicked()
    {
        NavigationManager.Instance.NavigateTo(menuSceneName);
    }

    private void OnDiscoverClicked()
    {
        NavigationManager.Instance.NavigateTo(discoverSceneName);
    }

    private void OnCodexClicked()
    {
        NavigationManager.Instance.NavigateTo(codexSceneName);
    }

    private void OnScoreClicked()
    {
        NavigationManager.Instance.NavigateTo(scoreSceneName);
    }
}