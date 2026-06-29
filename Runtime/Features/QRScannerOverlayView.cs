using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QRScannerOverlayView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform topShade;
    [SerializeField] private RectTransform bottomShade;
    [SerializeField] private RectTransform leftShade;
    [SerializeField] private RectTransform rightShade;
    [SerializeField] private RectTransform scanFrame;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TMP_Text statusText;

    [Header("Scan Area")]
    [SerializeField] private float scanAreaSize = 280f;
    [SerializeField] private float frameYOffset = 0f;

    private void Start()
    {
        BuildOverlay();
    }

    private void OnRectTransformDimensionsChange()
    {
        BuildOverlay();
    }

    private void BuildOverlay()
    {
        if (canvasRect == null || scanFrame == null)
            return;

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float frameSize = Mathf.Min(scanAreaSize, canvasWidth - 60f);
        float frameX = 0f;
        float frameY = frameYOffset;

        scanFrame.anchorMin = new Vector2(0.5f, 0.5f);
        scanFrame.anchorMax = new Vector2(0.5f, 0.5f);
        scanFrame.pivot = new Vector2(0.5f, 0.5f);
        scanFrame.sizeDelta = new Vector2(frameSize, frameSize);
        scanFrame.anchoredPosition = new Vector2(frameX, frameY);

        float left = frameX - frameSize / 2f;
        float right = frameX + frameSize / 2f;
        float top = frameY + frameSize / 2f;
        float bottom = frameY - frameSize / 2f;

        SetStretch(topShade, 0, 0, 1, 1, 0, top, 0, 0);
        SetStretch(bottomShade, 0, 0, 0, 0, 0, 0, 0, -bottom);
        SetStretch(leftShade, 0, 0.5f, 0, 0.5f, 0, left + canvasWidth / 2f, -frameSize / 2f + frameY, frameSize / 2f + frameY);
        SetStretch(rightShade, 1, 0.5f, 1, 0.5f, -(canvasWidth / 2f - right), 0, -frameSize / 2f + frameY, frameSize / 2f + frameY);

        if (statusText != null)
        {
            statusText.text = "QR kodu çerçeve içine tutun";
        }
    }

    private void SetStretch(
        RectTransform rt,
        float anchorMinX,
        float anchorMinY,
        float anchorMaxX,
        float anchorMaxY,
        float left,
        float right,
        float bottom,
        float top)
    {
        if (rt == null) return;

        rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, top);
    }
}