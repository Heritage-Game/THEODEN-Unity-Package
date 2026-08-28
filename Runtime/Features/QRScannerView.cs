using System.Collections;
using Core.Models;
using UnityEngine;
using UnityEngine.UI; //for rawimage
using UnityEngine.UIElements;
using ZXing;
using ZXing.Common;

/// <summary>
/// Runtime QR loading flow:
/// <code>
/// QR reads: roman_empire
///     ↓
/// Checks against DataManager.Instance.SelectedCodexItem.poiId
///     ↓
/// DataManager.LoadPOIFromQr("roman_empire")
///     ↓
/// DataManager loads POI JSON and media assets
///     ↓
/// NavigateTo("CodexInitialUIToolkit")
/// </code>
/// </summary>
public class QRScannerView : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI Toolkit References")] [SerializeField]
    private UIDocument uiDocument;

    [Header("Camera Overlay (Canvas)")] [SerializeField]
    private RawImage cameraRawImage;

    private VisualElement root;
    private VisualElement cameraContainer;
    private VisualElement cameraBackground;
    private Label statusText;
    private UnityEngine.UIElements.Button backButton;

    // ============================================================
    // CAMERA
    // ============================================================
    private WebCamTexture webCamTexture;
    private BarcodeReaderGeneric barcodeReader;

    private bool isScanning = false;
    private bool isProcessingQr = false;

    private float scanInterval = 0.5f;
    private float scanTimer = 0f;

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[QRScannerView] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        EnsureLocalizationManager();
        SetupButtons();

        barcodeReader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        StartCoroutine(InitCamera());
    }

    private void OnDisable()
    {
        StopCamera();

        if (cameraRawImage != null)
            cameraRawImage.gameObject.SetActive(false);

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

    private void Update()
    {
        if (!isScanning || isProcessingQr || webCamTexture == null || !webCamTexture.isPlaying)
            return;

        scanTimer += Time.deltaTime;

        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanQRCode();
        }
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        cameraContainer = root.Q<VisualElement>("camera_container");
        cameraBackground = root.Q<VisualElement>("camera_background");
        statusText = root.Q<Label>("status_text");
        backButton = root.Q<UnityEngine.UIElements.Button>("back_button");

        if (cameraContainer == null)
            Debug.LogWarning("[QRScannerView] 'camera_container' not found in UXML.");

        if (cameraBackground == null)
            Debug.LogWarning("[QRScannerView] 'camera_background' not found in UXML.");

        if (statusText == null)
            Debug.LogWarning("[QRScannerView] 'status_text' not found in UXML.");

        if (backButton == null)
            Debug.LogWarning("[QRScannerView] 'back_button' not found in UXML.");
    }

    // ============================================================
    // CAMERA INITIALIZATION
    // ============================================================

    private IEnumerator InitCamera()
    {
        SetStatus(LocalizationManager.Instance.GetText("status_requesting"), Color.white);

        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            SetStatus(LocalizationManager.Instance.GetText("status_denied"), Color.red);
            yield break;
        }

        StartCamera();
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
    }

    private void StartCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices == null || devices.Length == 0)
        {
            SetStatus(LocalizationManager.Instance.GetText("status_not_found"), Color.red);
            Debug.LogError("[QRScannerView] No camera found.");
            return;
        }

        string cameraName = null;

        foreach (var device in devices)
        {
            if (!device.isFrontFacing)
            {
                cameraName = device.name;
                break;
            }
        }

        if (cameraName == null)
            cameraName = devices[0].name;

        webCamTexture = new WebCamTexture(cameraName, 1280, 720, 30);

        if (cameraRawImage != null)
        {
            cameraRawImage.texture = webCamTexture;
            cameraRawImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[QRScannerView] cameraRawImage not assigned!");
        }

        webCamTexture.Play();

        StartCoroutine(WaitForCameraReady());
    }

    private IEnumerator WaitForCameraReady()
    {
        while (webCamTexture != null && webCamTexture.width < 100) yield return null;

        if (cameraRawImage != null)
        {
            FixCameraRatioRawImage();
            FixCameraRotationRawImage();
        }

        SetStatus(LocalizationManager.Instance.GetText("status_scanning"), Color.black);
        isScanning = true;
    }

    private void FixCameraRatioRawImage()
    {
        if (cameraRawImage == null || webCamTexture == null)
            return;

        RectTransform rt = cameraRawImage.rectTransform;
        RectTransform parentRt = cameraRawImage.transform.parent as RectTransform;

        if (parentRt == null) return;

        float parentRatio = parentRt.rect.width / parentRt.rect.height;
        float webcamRatio = (float)webCamTexture.width / webCamTexture.height;

        if (webcamRatio > parentRatio)
        {
            if (webCamTexture.videoRotationAngle == 0)
                rt.sizeDelta = new Vector2(parentRt.rect.width, parentRt.rect.width / webcamRatio);
            else
                rt.sizeDelta = new Vector2(parentRt.rect.width * webcamRatio, parentRt.rect.width);
        }
        else
        {
            if (webCamTexture.videoRotationAngle == 0)
                rt.sizeDelta = new Vector2(parentRt.rect.height * webcamRatio, parentRt.rect.height);
            else
                rt.sizeDelta = new Vector2(parentRt.rect.height, parentRt.rect.height / webcamRatio);
        }
    }

    /// <summary>
    /// Returns the POI ID associated with the selected Codex item.
    /// </summary>
    private string GetExpectedPoiIdFromSelectedItem()
    {
        CodexItemDefinition selectedItem =
            DataManager.Instance.SelectedCodexItem;

        if (selectedItem == null ||
            string.IsNullOrWhiteSpace(selectedItem.poiId))
        {
            return string.Empty;
        }

        return selectedItem.poiId.Trim();
    }

    private void FixCameraRotationRawImage()
    {
        if (cameraRawImage == null || webCamTexture == null)
            return;

        int rotation = -webCamTexture.videoRotationAngle;
        cameraRawImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);

        cameraRawImage.rectTransform.localScale = webCamTexture.videoVerticallyMirrored
            ? new Vector3(1, -1, 1)
            : Vector3.one;
    }

    // ============================================================
    // DEMO FALLBACK
    // ============================================================

    private void DebugLoadSelectedPOI()
    {
        if (isProcessingQr)
            return;

        string expectedPoiId = GetExpectedPoiIdFromSelectedItem();

        if (string.IsNullOrEmpty(expectedPoiId))
        {
            Debug.LogError("[QRScanner] Cannot debug-load POI. Expected QR code is empty.");
            SetStatus(LocalizationManager.Instance.GetText("status_cannot_load"), Color.red);
            return;
        }

        Debug.Log("[QRScanner] Debug loading QR: " + expectedPoiId);

        isScanning = false;
        isProcessingQr = true;

        StartCoroutine(ValidateAndLoadQRCode(expectedPoiId));
    }

    private void FixCameraRatio()
    {
        if (cameraBackground == null || webCamTexture == null)
            return;

        float parentWidth = cameraContainer != null ? cameraContainer.resolvedStyle.width : Screen.width;
        float parentHeight = cameraContainer != null ? cameraContainer.resolvedStyle.height : Screen.height;

        if (parentWidth <= 0 || parentHeight <= 0)
        {
            parentWidth = Screen.width;
            parentHeight = Screen.height;
        }

        float parentRatio = parentWidth / parentHeight;
        float webcamRatio = (float)webCamTexture.width / webCamTexture.height;

        if (cameraBackground != null)
        {
            if (webcamRatio > parentRatio)
            {
                // limit by width
                cameraBackground.style.width = parentWidth;
                cameraBackground.style.height = parentWidth / webcamRatio;
            }
            else
            {
                // limit by height
                cameraBackground.style.width = parentHeight * webcamRatio;
                cameraBackground.style.height = parentHeight;
            }
        }
    }

    // ============================================================
    // UI UTILITIES
    // ============================================================
    private void OnBackClicked()
    {
        ChallengeSessionService.CancelSession();

        StopCamera();

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.GoBack();
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.style.color = color;
        }
    }

    private void StopCamera()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();

        if (cameraRawImage != null)
        {
            cameraRawImage.texture = null;
            cameraRawImage.gameObject.SetActive(false);
        }
    }

    private void FixCameraRotation()
    {
        if (cameraBackground == null || webCamTexture == null)
            return;

        int rotation = -webCamTexture.videoRotationAngle;
        cameraBackground.style.rotate = new Rotate(Angle.Degrees(rotation));

        if (webCamTexture.videoVerticallyMirrored)
        {
            cameraBackground.style.scale = new Scale(new Vector3(1, -1, 1));
        }
        else
        {
            cameraBackground.style.scale = new Scale(Vector3.one);
        }
    }

    // ============================================================
    // CAMERA FRAME UPDATE
    // ============================================================
    private void UpdateCameraFrame()
    {
        if (cameraBackground == null || webCamTexture == null || !webCamTexture.isPlaying)
            return;
    }

    // ============================================================
    // SCANNING LOOP
    // ============================================================
    private void ScanQRCode()
    {
        try
        {
            Color32[] pixels = webCamTexture.GetPixels32();
            int width = webCamTexture.width;
            int height = webCamTexture.height;

            byte[] byteArray = new byte[pixels.Length * 4];

            for (int i = 0; i < pixels.Length; i++)
            {
                byteArray[i * 4] = pixels[i].r;
                byteArray[i * 4 + 1] = pixels[i].g;
                byteArray[i * 4 + 2] = pixels[i].b;
                byteArray[i * 4 + 3] = pixels[i].a;
            }

            var result = barcodeReader.Decode(
                byteArray,
                width,
                height,
                RGBLuminanceSource.BitmapFormat.RGBA32
            );

            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                isScanning = false;
                isProcessingQr = true;

                string scannedCode = result.Text.Trim();

                Debug.Log("[QRScannerView] QR Code read: " + scannedCode);

                StartCoroutine(ValidateAndLoadQRCode(scannedCode));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[QRScannerView] QR reading error: " + ex.Message);
        }
    }

    // ============================================================
    // VALIDATION + POI LOADING
    // ============================================================
    private IEnumerator ValidateAndLoadQRCode(string scannedCode)
    {
        if (DataManager.Instance == null)
        {
            SetStatus(LocalizationManager.Instance.GetText("status_datamanager_missing"), Color.red);
            isProcessingQr = false;
            yield break;
        }

        // TODO:
        // This check will need to change if QR scanning can also
        // be started directly from the Main Menu.
        if (DataManager.Instance.SelectedCodexItem == null)
        {
            SetStatus(LocalizationManager.Instance.GetText("status_no_selected_level"), Color.red);

            Debug.LogError(
                "[QRScanner] SelectedCodexItem is null."
            );

            isProcessingQr = false;
            yield break;
        }

        string expectedPoiId =
            GetExpectedPoiIdFromSelectedItem();

        if (string.IsNullOrWhiteSpace(expectedPoiId))
        {
            SetStatus(
                LocalizationManager.Instance.GetText("status_no_valid_id"),
                Color.red
            );

            Debug.LogError(
                "[QRScanner] SelectedCodexItem has no valid POI ID."
            );

            yield return ResetStatusDelayed();
            yield break;
        }

        bool isCorrectQrCode = string.Equals(
            scannedCode,
            expectedPoiId,
            System.StringComparison.Ordinal);

        if (!isCorrectQrCode)
        {
            SetStatus(
                LocalizationManager.Instance.GetText("status_wrong_qr_code"),
                new Color(0.9f, 0.3f, 0.3f)
            );

            Debug.LogWarning(
                "[QRScanner] Wrong QR. Expected: " +
                expectedPoiId +
                " | Got: " +
                scannedCode
            );

            yield return ResetStatusDelayed();
            yield break;
        }

        /*
         * Start the session only if another matching session
         * is not already active.
         *
         * This prevents the timer from being restarted if the
         * same QR is processed more than once.
         */
        bool hasMatchingActiveSession =
            ChallengeSessionService.IsActive &&
            string.Equals(
                ChallengeSessionService.ActivePoiId,
                expectedPoiId,
                System.StringComparison.Ordinal
            );

        bool sessionReady =
            hasMatchingActiveSession ||
            ChallengeSessionService.StartSession(expectedPoiId);

        if (!sessionReady)
        {
            SetStatus(
                LocalizationManager.Instance.GetText("status_no_session"),
                Color.red
            );

            Debug.LogError(
                "[QRScanner] Could not start session for POI: " +
                scannedCode
            );

            isProcessingQr = false;
            isScanning = true;
            yield break;
        }

        SetStatus(
            LocalizationManager.Instance.GetText("status_correct"),
            new Color(0.29f, 0.69f, 0.31f)
        );

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        if (backButton != null)
            backButton.SetEnabled(false);


        yield return DataManager.Instance.LoadPOIFromQr(
            expectedPoiId
        );

        if (DataManager.Instance.SelectedPOI == null)
        {
            ChallengeSessionService.CancelSession();

            SetStatus(
                LocalizationManager.Instance.GetText("status_no_poi"),
                Color.red
            );

            Debug.LogError(
                "[QRScanner] SelectedPOI is null after loading QR: " +
                scannedCode
            );

            if (backButton != null)
                backButton.SetEnabled(true);

            yield return ResetStatusDelayed();
            yield break;
        }

        // POI successfully loaded 
        StopCamera();

        yield return new WaitForSeconds(0.5f);

        if (NavigationManager.Instance == null)
        {
            ChallengeSessionService.CancelSession();

            SetStatus(
                LocalizationManager.Instance.GetText("status_no_navigation_manager"),
                Color.red
            );

            if (backButton != null)
                backButton.SetEnabled(true);

            isProcessingQr = false;
            yield break;
        }

        NavigationManager.Instance.NavigateTo(
            "CodexInitialUIToolkit"
        );
    }


    private IEnumerator ResetStatusDelayed()
    {
        yield return new WaitForSeconds(2f);

        SetStatus(LocalizationManager.Instance.GetText("status_scanning"), Color.white);

        isProcessingQr = false;
        isScanning = true;
    }
}