using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using ZXing.Common;

/// <summary>
/// In pratica nella nuova logica la pipeline è:
/// <code>
/// QR legge ad es: roman_empire_ENG
///    ↓
///check che sia coerente con DataManager.Instance.SelectedPOI
///    ↓
///DataManager.LoadPOIFromQr("roman_empire_ENG")
///    ↓
///DataManager carica JSON POI + immagini
///    ↓
///NavigateTo("CodexInitial")
/// </code>
/// </summary>
public class QRScannerView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage cameraBackground;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform background;

    [Header("Demo")]
    [SerializeField] private Button debugLoadButton;

    private WebCamTexture webCamTexture;
    private BarcodeReaderGeneric barcodeReader;

    private bool isScanning = false;
    private bool isProcessingQr = false;

    private float scanInterval = 0.5f;
    private float scanTimer = 0f;

    private void Start()
    {
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

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (debugLoadButton != null)
        {
            debugLoadButton.onClick.RemoveAllListeners();
            debugLoadButton.onClick.AddListener(DebugLoadSelectedPOI);
        }
    }

    // ============================================================
    // CAMERA INITIALIZATION
    // ============================================================

    private IEnumerator InitCamera()
    {
        SetStatus("Requesting camera permission...", Color.white);

        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            SetStatus("Camera permission denied!", Color.red);
            yield break;
        }

        StartCamera();
    }

    private void StartCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices == null || devices.Length == 0)
        {
            SetStatus("No camera found!", Color.red);
            Debug.LogError("[QRScanner] No camera found.");
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

        if (cameraBackground != null)
            cameraBackground.texture = webCamTexture;

        webCamTexture.Play();

        StartCoroutine(WaitForCameraReady());
    }

    private IEnumerator WaitForCameraReady()
    {
        while (webCamTexture != null && webCamTexture.width < 100)
            yield return null;

        FixCameraRatio();
        FixCameraRotation();

        SetStatus("Scanning QR Code...", Color.white);
        isScanning = true;
    }

    //Used to set the camera ratio at maximum dimentions without stretching the image
    private void FixCameraRatio()
    {
        RectTransform rt = cameraBackground.rectTransform;

        float parentRatio = background.rect.width / background.rect.height;
        float webcamRatio = (float)webCamTexture.width / webCamTexture.height;

        if (webcamRatio > parentRatio)
        {
            // limit by width

            if (webCamTexture.videoRotationAngle == 0)
                rt.sizeDelta = new Vector2(background.rect.width, background.rect.width / webcamRatio);
            else
                rt.sizeDelta = new Vector2(background.rect.width * webcamRatio, background.rect.width);
        }
        else
        {
            // limit by height
            if (webCamTexture.videoRotationAngle == 0)
                rt.sizeDelta = new Vector2(background.rect.height * webcamRatio, background.rect.height);
            else
                rt.sizeDelta = new Vector2(background.rect.height, background.rect.height / webcamRatio);
        }
    }

    private void FixCameraRotation()
    {
        if (cameraBackground == null || webCamTexture == null)
            return;

        int rotation = -webCamTexture.videoRotationAngle;
        cameraBackground.rectTransform.localEulerAngles = new Vector3(0, 0, rotation);

        cameraBackground.rectTransform.localScale = webCamTexture.videoVerticallyMirrored
            ? new Vector3(1, -1, 1)
            : Vector3.one;
    }

    // ============================================================
    // SCANNING LOOP
    // ============================================================

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

                Debug.Log("[QRScanner] QR Code read: " + scannedCode);

                StartCoroutine(ValidateAndLoadQRCode(scannedCode));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[QRScanner] QR reading error: " + ex.Message);
        }
    }

    // ============================================================
    // VALIDATION + POI LOADING
    // ============================================================

    private IEnumerator ValidateAndLoadQRCode(string scannedCode)
    {
        if (DataManager.Instance == null)
        {
            SetStatus("DataManager missing!", Color.red);
            isProcessingQr = false;
            yield break;
        }

        if (DataManager.Instance.SelectedCodexItem == null)
        {
            SetStatus("No selected level!", Color.red);
            Debug.LogError("[QRScanner] SelectedCodexItem is null.");
            isProcessingQr = false;
            yield break;
        }

        string expectedQrCode = GetExpectedQrCodeFromSelectedItem();

        if (!string.IsNullOrEmpty(expectedQrCode) && scannedCode != expectedQrCode)
        {
            SetStatus("Wrong QR Code!", new Color(0.9f, 0.3f, 0.3f));

            Debug.LogWarning("[QRScanner] Wrong QR. Expected: " + expectedQrCode + " | Got: " + scannedCode);

            yield return ResetStatusDelayed();
            yield break;
        }

        SetStatus("Correct QR Code! Loading...", new Color(0.29f, 0.69f, 0.31f));

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        StopCamera();

        yield return DataManager.Instance.LoadPOIFromQr(scannedCode);

        if (DataManager.Instance.SelectedPOI == null)
        {
            SetStatus("POI data could not be loaded.", Color.red);
            Debug.LogError("[QRScanner] SelectedPOI is null after loading QR: " + scannedCode);

            isProcessingQr = false;
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        NavigationManager.Instance.NavigateTo("CodexInitial");
    }

    private string GetExpectedQrCodeFromSelectedItem()
    {
        var item = DataManager.Instance.SelectedCodexItem;

        if (item == null || string.IsNullOrEmpty(item.target))
            return "";

        // Example:
        // roman_empire_directions_ENG
        // becomes:
        // roman_empire

        string expected = item.target.Trim();

        expected = expected.Replace(".json", "");

        // Remove language suffix by removing everything after "_directions"
        int directionsIndex = expected.IndexOf("_directions");

        if (directionsIndex >= 0)
        {
            expected = expected.Substring(0, directionsIndex);
        }

        return expected.Trim();
    }

    private IEnumerator ResetStatusDelayed()
    {
        yield return new WaitForSeconds(2f);

        SetStatus("Scanning QR Code...", Color.white);

        isProcessingQr = false;
        isScanning = true;
    }

    // ============================================================
    // DEMO FALLBACK
    // ============================================================

    private void DebugLoadSelectedPOI()
    {
        if (isProcessingQr)
            return;

        string expectedQrCode = GetExpectedQrCodeFromSelectedItem();

        if (string.IsNullOrEmpty(expectedQrCode))
        {
            Debug.LogError("[QRScanner] Cannot debug-load POI. Expected QR code is empty.");
            SetStatus("Cannot load demo POI.", Color.red);
            return;
        }

        Debug.Log("[QRScanner] Debug loading QR: " + expectedQrCode);

        isScanning = false;
        isProcessingQr = true;

        StartCoroutine(ValidateAndLoadQRCode(expectedQrCode));
    }

    // ============================================================
    // UI
    // ============================================================

    private void OnBackClicked()
    {
        StopCamera();

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.GoBack();
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }

    private void StopCamera()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();
    }

    private void OnDestroy()
    {
        StopCamera();

        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        if (debugLoadButton != null)
            debugLoadButton.onClick.RemoveAllListeners();
    }
}