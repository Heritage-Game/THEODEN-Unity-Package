using System.Collections;
using Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapLocation : MonoBehaviour
{
    [Header("POI Data")]
    [SerializeField] private int orderNumber;
    [SerializeField] private string displayedName;
    [SerializeField] private string poiId;

    [Header("Navigation")]
    [SerializeField] private string unlockedSceneToOpen = "CodexInitial";
    [SerializeField] private string codexSceneToOpen = "Codex";

    //set this to Datamanager.instance.CodexMenu
    [Header("Temporary Unlock State")]
    [SerializeField] private bool isUnlocked;

    [Header("Popup UI")]
    [SerializeField] private GameObject popup;
    [SerializeField] private TextMeshProUGUI poiNameText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private GameObject codexLock;

    private RectTransform local;
    private RectTransform parent;
    private Vector2 originalScale;

    private void Start()
    {
        local = GetComponent<RectTransform>();
        originalScale = local.localScale;

        parent = transform.parent.GetComponent<RectTransform>();

        InitUi();
    }

    private void InitUi()
    {
        popup.SetActive(false);

        poiNameText.text = orderNumber + ". " + displayedName;

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnActionButtonClicked);

        UpdateCodexUI();
    }

    public void OpenPopup()
    {
        popup.SetActive(!popup.activeSelf);

        if (popup.activeSelf)
        {
            UpdateCodexUI();
        }
    }

    public void UpdateUi()
    {
        if (local == null || parent == null)
            return;

        local.localScale = originalScale / parent.localScale.x;
    }

    private void UpdateCodexUI()
    {
        if (isUnlocked)
        {
            codexLock.SetActive(false);
            actionButton.gameObject.SetActive(true);

            if (actionButtonText != null)
                actionButtonText.text = "Open POI";
        }
        else
        {
            codexLock.SetActive(true);
            actionButton.gameObject.SetActive(true);

            if (actionButtonText != null)
                actionButtonText.text = "Go to Codex";
        }
    }

    private void OnActionButtonClicked()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[MapLocation] DataManager missing.");
            return;
        }
        if (!DataManager.Instance.IsDataLoaded)
        {
            Debug.LogWarning("[MapLocation] DataManager exists, but Codex is not loaded yet.");
            return;
        }

        CodexItemState state = DataManager.Instance.GetCodexItemStateByPoiId(poiId);

        switch (state)
        {
            case CodexItemState.Unlocked:
                OpenUnlockedPOI();
                break;

            case CodexItemState.Directions:
                OpenDirectionsPage();
                break;

            case CodexItemState.Locked:
                GoToCodex();
                break;
        }
    }

    private void OpenUnlockedPOI()
    {
        StartCoroutine(OpenUnlockedPOIRoutine());
    }

    /// <summary>
    /// Opens POI screen if it's unlocked
    /// </summary>
    /// <returns></returns>
    private IEnumerator OpenUnlockedPOIRoutine()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[MapLocation] DataManager missing.");
            yield break;
        }

        yield return DataManager.Instance.LoadPOIFromQr(poiId);

        if (!DataManager.Instance.IsPOILoaded)
        {
            Debug.LogError("[MapLocation] POI could not be loaded: " + poiId);
            yield break;
        }

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo(unlockedSceneToOpen);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(unlockedSceneToOpen);
        }
    }

    private void OpenDirectionsPage()
    {
        StartCoroutine(OpenDirectionsPageRoutine());
    }

    private IEnumerator OpenDirectionsPageRoutine()
    {
        CodexItemDefinition item = DataManager.Instance.GetCodexItemByPoiId(poiId);

        if (item == null)
        {
            Debug.LogError("[MapLocation] Cannot open directions. Codex item not found for POI: " + poiId);
            yield break;
        }

        yield return DataManager.Instance.SelectCodexItemAndLoadDirections(item);

        if (!DataManager.Instance.IsDirectionsLoaded)
        {
            Debug.LogError("[MapLocation] Directions could not be loaded for POI: " + poiId);
            yield break;
        }

        NavigationManager.Instance.NavigateTo("CodexDetail");
    }
    private void GoToCodex()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo(codexSceneToOpen);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(codexSceneToOpen);
        }
    }
}