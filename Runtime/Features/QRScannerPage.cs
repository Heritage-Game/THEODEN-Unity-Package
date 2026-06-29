using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using ZXing;
using ZXing.Common;
using System.Collections;

public class QRScannerPage : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public RawImage cameraFeedImage;
    public TMP_Text instructionText;
    public Button closeButton;
    public AspectRatioFitter cameraFitter;

    [Header("Mock Mode")]
    public bool useMockScanner = true;
    public string mockPOI_ID = "poi_001";

    // Camera
    private WebCamTexture webCamTexture;
    private bool isScanning = false;
    private bool mockReady = false;

    // ZXing QR reader
    private BarcodeReaderGeneric barcodeReader;

    // Scan interval
    private float scanInterval = 0.5f;
    private float scanTimer = 0f;

    void Start()
    {
        closeButton.onClick.AddListener(OnCloseClicked);

        barcodeReader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        if (useMockScanner)
        {
            SetupMockScanner();
        }
        else
        {
            StartCoroutine(SetupCamera());
        }
    }

    // ============================================================
    // MOCK SCANNER — No buttons needed! Uses Update() to detect clicks
    // ============================================================

    void SetupMockScanner()
    {
        instructionText.text = "MOCK MODE — Tap anywhere on screen to simulate scan";
        instructionText.color = new Color(0.18f, 0.5f, 0.25f);
        cameraFeedImage.color = new Color(0.9f, 0.9f, 0.9f);
        mockReady = true;
        Debug.Log("[MOCK] Ready! Tap anywhere or press SPACE to simulate QR scan");
    }

    void Update()
    {
        // ============================================================
        // MOCK: Detect tap/click or spacebar press — no button needed!
        // ============================================================
        if (mockReady)
        {
            // Spacebar press (easiest for testing in Editor)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                mockReady = false;
                Debug.Log("[MOCK] SPACE pressed → simulating QR scan");
                OnMockScanClicked();
                return;
            }

            // Mouse click or screen tap
            if (Input.GetMouseButtonDown(0))
            {
                // Make sure we're not clicking the Close/Back button
                if (EventSystem.current.currentSelectedGameObject != closeButton.gameObject)
                {
                    mockReady = false;
                    Debug.Log("[MOCK] Screen tapped → simulating QR scan");
                    OnMockScanClicked();
                    return;
                }
            }
        }

        // ============================================================
        // REAL CAMERA: QR scanning loop
        // ============================================================
        if (!isScanning || webCamTexture == null || !webCamTexture.isPlaying)
            return;

        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval)
            return;

        scanTimer = 0f;
        TryScanQRCode();
    }

    void OnMockScanClicked()
    {
        Debug.Log($"[MOCK] Simulated QR scan → {mockPOI_ID}");
        instructionText.text = "Code detected!";
        instructionText.color = new Color(0.29f, 0.6f, 0.37f);
        OnQRCodeDetected(mockPOI_ID);
    }

    // Needed for IPointerClickHandler (alternative click detection)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (mockReady)
        {
            mockReady = false;
            Debug.Log("[MOCK] Pointer click detected → simulating QR scan");
            OnMockScanClicked();
        }
    }

    // ============================================================
    // REAL CAMERA
    // ============================================================

    IEnumerator SetupCamera()
    {
        instructionText.text = "Starting camera...";

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            instructionText.text = "Camera permission denied!";
            yield break;
        }

        WebCamDevice[] devices = WebCamTexture.devices;
        string cameraName = null;

        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                cameraName = devices[i].name;
                break;
            }
        }

        if (cameraName == null && devices.Length > 0)
            cameraName = devices[0].name;

        if (cameraName == null)
        {
            instructionText.text = "No camera found!";
            yield break;
        }

        webCamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
        webCamTexture.Play();

        while (webCamTexture.width < 100)
            yield return null;

        cameraFeedImage.texture = webCamTexture;
        cameraFeedImage.color = Color.white;
        FixCameraRotation();

        instructionText.text = "Scan the code please";
        isScanning = true;
    }

    void FixCameraRotation()
    {
        int rotationAngle = -webCamTexture.videoRotationAngle;
        cameraFeedImage.rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);

        if (webCamTexture.videoVerticallyMirrored)
            cameraFeedImage.rectTransform.localScale = new Vector3(1, -1, 1);

        if (cameraFitter != null)
        {
            float aspectRatio = (float)webCamTexture.width / webCamTexture.height;
            cameraFitter.aspectRatio = aspectRatio;
        }
    }

    // ============================================================
    // QR SCANNING
    // ============================================================

    void TryScanQRCode()
    {
        try
        {
            Color32[] pixels = webCamTexture.GetPixels32();
            int width = webCamTexture.width;
            int height = webCamTexture.height;

            byte[] rgbaBytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                rgbaBytes[i * 4] = pixels[i].r;
                rgbaBytes[i * 4 + 1] = pixels[i].g;
                rgbaBytes[i * 4 + 2] = pixels[i].b;
                rgbaBytes[i * 4 + 3] = pixels[i].a;
            }

            var result = barcodeReader.Decode(
                rgbaBytes,
                width,
                height,
                RGBLuminanceSource.BitmapFormat.RGBA32
            );

            if (result != null)
            {
                isScanning = false;
                Debug.Log($"QR Code scanned: {result.Text}");
                OnQRCodeDetected(result.Text);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"QR scan error: {e.Message}");
        }
    }

    // ============================================================
    // QR DETECTED → NAVIGATE
    // ============================================================

    void OnQRCodeDetected(string qrContent)
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        instructionText.text = "Code detected!";
        instructionText.color = new Color(0.29f, 0.6f, 0.37f);

        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();

        PlayerPrefs.SetString("ScannedPOI_ID", qrContent);
        PlayerPrefs.Save();

        Debug.Log($"[QR] Saved POI ID: {qrContent} → Navigating to POISummary");
        StartCoroutine(NavigateAfterDelay("POISummary", 1.0f));
    }

    // ============================================================
    // SAFE SCENE LOADING
    // ============================================================

    void SafeLoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Transitions.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning(
                $"Scene '{sceneName}' is not in Build Settings!\n" +
                $"Go to File → Build Profiles → Add Open Scenes"
            );
            instructionText.text = $"Scene '{sceneName}' not found!\nAdd it to Build Settings.";
            instructionText.color = Color.red;
        }
    }

    IEnumerator NavigateAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SafeLoadScene(sceneName);
    }

    void OnCloseClicked()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();

        SafeLoadScene("MainMenu");
    }

    void OnDestroy()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();
    }
}