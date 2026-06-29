using System;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// Centralized application input manager responsible for:
/// <list type="bullet">
/// <item><description>Reading raw input from Unity's Input System.</description></item>
/// <item><description>Converting low-level hardware input into high-level application events.</description></item>
/// <item><description>Providing a single access point for input across the entire application.</description></item>
/// <item><description>Detecting and dispatching gestures such as taps and swipes.</description></item>
/// </list>
/// 
/// <para>
/// This service acts as an abstraction layer between:
/// </para>
/// 
/// <code>
/// Physical Devices
///     ↓
/// Unity Input System
///     ↓
/// InputService
///     ↓
/// UI / Gameplay / Features
/// </code>
/// 
/// <para>
/// The goal of this architecture is to avoid direct hardware polling
/// (e.g. Input.GetTouch, Input.GetMouseButton, Input.mousePosition)
/// inside gameplay systems, pages, views, or UI components.
/// </para>
/// 
/// <para>
/// Instead of individual systems reading input independently,
/// all input is centralized here and exposed through events.
/// </para>
/// 
/// <para>
/// Supported platforms/devices include:
/// </para>
/// 
/// <list type="bullet">
/// <item><description>Mouse.</description></item>
/// <item><description>Touchscreen.</description></item>
/// <item><description>Keyboard.</description></item>
/// </list>
/// 
/// <para>
/// The service uses the generated <c>AppInputActions</c> wrapper class
/// produced by Unity's Input System package.
/// </para>
/// 
/// <para>
/// IMPORTANT:
/// This class should be the ONLY place in the runtime codebase
/// that directly interacts with hardware input.
/// </para>
/// 
/// <para>
/// Systems should SUBSCRIBE to events exposed by this service,
/// and should NEVER poll input directly.
/// </para>
/// 
/// <example>
/// Example usage:
/// <code>
/// private void OnEnable()
/// {
///     InputService.Instance.OnSwipe += HandleSwipe;
/// }
/// 
/// private void OnDisable()
/// {
///     InputService.Instance.OnSwipe -= HandleSwipe;
/// }
/// 
/// private void HandleSwipe(Vector2 direction)
/// {
///     Debug.Log(direction);
/// }
/// </code>
/// </example>
/// 
/// <para>
/// CURRENT RESPONSIBILITIES:
/// </para>
/// 
/// <list type="bullet">
/// <item><description>Pointer position tracking.</description></item>
/// <item><description>Tap event dispatching.</description></item>
/// <item><description>Swipe detection.</description></item>
/// <item><description>Scroll event dispatching.</description></item>
/// <item><description>Back action dispatching.</description></item>
/// </list>
/// 
/// <para>
/// FUTURE EXTENSIONS MAY INCLUDE:
/// </para>
/// 
/// <list type="bullet">
/// <item><description>Pinch gestures. --> TO DO</description></item>
/// <item><description>Long press detection.</description></item>
/// <item><description>Drag gestures.</description></item>
/// <item><description>Input contexts and ownership. --> TO DO</description></item> 
/// <item><description>UI input blocking. --> TO DO</description></item>
/// <item><description>Gamepad support.</description></item>
/// </list>
/// </summary>
public class InputService : MonoBehaviour
{
    public static InputService Instance { get; private set; }

    // =====================================================
    // EVENTS
    // =====================================================

    /// <summary>
    /// Invoked when a tap/click interaction is completed.
    /// The provided Vector2 represents the screen position
    /// of the pointer/finger at the moment of the tap.
    /// </summary>
    public event Action<Vector2> OnTap;
    /// <summary>
    /// Invoked when a swipe gesture is detected.
    /// The provided Vector2 is the normalized swipe direction.
    /// 
    /// Examples:
    /// (1,0)   → Right
    /// (-1,0)  → Left
    /// (0,1)   → Up
    /// (0,-1)  → Down
    /// </summary>
    public event Action<Vector2> OnSwipe;
    /// <summary>
    /// Invoked when a scroll input is received.
    /// The provided Vector2 contains the scroll delta.
    /// </summary>
    public event Action<Vector2> OnScroll;
    /// <summary>
    /// Invoked when the back action is triggered.
    /// Mapped to:
    /// - Escape key on desktop.
    /// - Android back button on mobile.
    /// </summary>
    public event Action OnBack;

    // =====================================================
    // INPUT ACTIONS
    // =====================================================

    private AppInputActions inputActions;

    // =====================================================
    // POINTER
    // =====================================================

    /// <summary>
    /// Current pointer position in screen coordinates. 
    /// Represents:
    /// - Mouse position on desktop.
    /// - Primary finger position on touch devices.
    /// </summary>
    public Vector2 PointerPosition { get; private set; }

    // =====================================================
    // PRESS / SWIPE
    // =====================================================

    private bool isPressing;

    private Vector2 pressStartPosition;

    /// <summary>
    /// Minimum distance in screen pixels required
    /// for a movement to be considered a swipe gesture.
    /// </summary>
    [Header("Swipe Settings")]
    [SerializeField] private float minimumSwipeDistance = 100f;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        // Singleton

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        inputActions = new AppInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        // POINT

        inputActions.UI.Point.performed += HandlePoint;

        // PRESS

        inputActions.UI.Press.started += HandlePressStarted;
        inputActions.UI.Press.canceled += HandlePressCanceled;

        // TAP

        inputActions.UI.Tap.performed += HandleTap;

        // SCROLL

        inputActions.UI.Scroll.performed += HandleScroll;

        // BACK

        inputActions.UI.Back.performed += HandleBack;
    }

    private void OnDisable()
    {
        // POINT

        inputActions.UI.Point.performed -= HandlePoint;

        // PRESS

        inputActions.UI.Press.started -= HandlePressStarted;
        inputActions.UI.Press.canceled -= HandlePressCanceled;

        // TAP

        inputActions.UI.Tap.performed -= HandleTap;

        // SCROLL

        inputActions.UI.Scroll.performed -= HandleScroll;

        // BACK

        inputActions.UI.Back.performed -= HandleBack;

        inputActions.Disable();
    }

    // =====================================================
    // POINT
    // =====================================================

    private void HandlePoint(InputAction.CallbackContext context)
    {
        PointerPosition = context.ReadValue<Vector2>();
    }

    // =====================================================
    // PRESS START
    // =====================================================

    private void HandlePressStarted(InputAction.CallbackContext context)
    {
        isPressing = true;

        pressStartPosition = PointerPosition;
    }

    // =====================================================
    // PRESS END
    // =====================================================

    private void HandlePressCanceled(InputAction.CallbackContext context)
    {
        if (!isPressing)
            return;

        isPressing = false;

        Vector2 endPosition = PointerPosition;

        Vector2 delta = endPosition - pressStartPosition;

        // SWIPE DETECTION

        if (delta.magnitude >= minimumSwipeDistance)
        {
            Vector2 direction = delta.normalized;

            OnSwipe?.Invoke(direction);
        }
    }

    // =====================================================
    // TAP
    // =====================================================

    private void HandleTap(InputAction.CallbackContext context)
    {
        OnTap?.Invoke(PointerPosition);
    }

    // =====================================================
    // SCROLL
    // =====================================================

    private void HandleScroll(InputAction.CallbackContext context)
    {
        Vector2 scroll = context.ReadValue<Vector2>();

        OnScroll?.Invoke(scroll);
    }

    // =====================================================
    // BACK
    // =====================================================

    private void HandleBack(InputAction.CallbackContext context)
    {
        OnBack?.Invoke();
    }
}