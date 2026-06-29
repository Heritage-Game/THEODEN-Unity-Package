using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the Panel_CameraFrame to apply rounded corners.
/// Requires a "UI/RoundedCorners" shader (or use Mask component instead).
/// 
/// SIMPLE APPROACH: Just use this script to set up the Mask component
/// with a rounded rectangle sprite automatically.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Mask))]
public class RoundedCornersHelper : MonoBehaviour
{
    [Header("Assign your rounded rectangle sprite here")]
    public Sprite roundedRectSprite;

    void Awake()
    {
        var image = GetComponent<Image>();
        if (roundedRectSprite != null)
        {
            image.sprite = roundedRectSprite;
            image.type = Image.Type.Sliced; // So it scales with 9-slice
        }

        var mask = GetComponent<Mask>();
        mask.showMaskGraphic = true; // Shows the rounded border too
    }
}