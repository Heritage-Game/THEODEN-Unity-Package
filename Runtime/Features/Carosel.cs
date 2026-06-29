using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Carosel : MonoBehaviour
{
    [SerializeField] RectTransform content;

    public void Setup(List<Sprite> images)
    {
        float width = GetComponent<RectTransform>().rect.width;

        //Generates the images in the carosel adapting them depending on the width of the screen
        foreach (Sprite s in images)
        {
            Image im = Instantiate(new GameObject().AddComponent<Image>(), content);
            im.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            im.sprite = s;
            AspectRatioFitter f = im.gameObject.AddComponent<AspectRatioFitter>();
            f.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;

            float aspect = im.sprite.rect.width / im.sprite.rect.height;
            f.aspectRatio = aspect;
        }
        //Sets the correct width for the content
        float sum = GetComponentInChildren<HorizontalLayoutGroup>().spacing * (images.Count - 1)
            + images.Count * width;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sum);
    }

}
