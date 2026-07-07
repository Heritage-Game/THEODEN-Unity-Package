using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapPage : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private RectTransform scrollView;
    public RectTransform mapImage;
    [SerializeField] float zoomFactor = 1.5f;
    private const float MAXZOOM = 8;
    private MapLocation[] pois;

    [Header("Navigation Buttons")]
    public Button btnExitMap; // Görseldeki "Exit navigation" butonunu buraya sürükle

    void Start()
    {
        SetupMap();
        GetPois();

        // Geri dön butonunu baðlama
        if (btnExitMap != null)
        {
            btnExitMap.onClick.AddListener(() =>
            {
                NavigationManager.Instance.NavigateTo("Menu"); // Ana menü sahnenizin adý
            });
        }
    }

    private void SetupMap()
    {
        mapImage.GetComponent<Image>().SetNativeSize();

        float imageRatio = mapImage.rect.width / mapImage.rect.height;
        float scrollViewRatio = scrollView.rect.width / scrollView.rect.height;

        mapImage.sizeDelta =
            imageRatio > scrollViewRatio ?
                new Vector2(scrollView.rect.width, scrollView.rect.width / imageRatio)
                : new Vector2(scrollView.rect.height * imageRatio, scrollView.rect.height);

        mapImage.anchoredPosition = Vector2.zero;
    }

    private void GetPois()
    {
        pois = mapImage.GetComponentsInChildren<MapLocation>();
    }

    public void ZoomInMap()
    {
        if (mapImage.localScale.x * zoomFactor > MAXZOOM) return;

        mapImage.localScale *= zoomFactor;
        UpdateAllPois();
    }

    public void ZoomOutMap()
    {
        if (mapImage.localScale.x / zoomFactor < 1) return;

        mapImage.localScale /= zoomFactor;
        UpdateAllPois();
    }

    private void UpdateAllPois()
    {
        for (int i = 0; i < pois.Length; i++)
        {
            pois[i].UpdateUi();
        }
    }
}