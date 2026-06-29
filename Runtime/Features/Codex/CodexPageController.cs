using System.Collections;
using Core.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Codex
{
    /// <summary>
    /// Coordinates the Codex page feature.
    ///
    /// Responsibilities: Build codex UI, Create item views, Subscribe to view events, Coordinate navigation
    /// and runtime behavior.
    ///
    /// This class acts as the feature controller between:
    /// <code>
    /// runtime models
    ///   ↓
    /// views
    ///   ↓
    /// application services
    /// </code>
    /// </summary>
    public class CodexPageController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private CodexItemView codexItemPrefab;
        [SerializeField] private Button goBackButton;

        [Header("Design")]
        [SerializeField] private Color[] itemColors;

        private CodexModel codexModel;
        private bool isOpeningDetail = false;

        private void Start()
        {
            SetupButtons();
            StartCoroutine(Initialize());
        }

        private void SetupButtons()
        {
            if (goBackButton != null)
            {
                goBackButton.onClick.RemoveAllListeners();
                goBackButton.onClick.AddListener(OnBackClicked);
            }
        }

        private IEnumerator Initialize()
        {
            while (DataManager.Instance == null)
                yield return null;

            while (!DataManager.Instance.IsDataLoaded)
                yield return null;

            codexModel = DataManager.Instance.CodexMenu;

            if (codexModel == null)
            {
                Debug.LogError("[CodexPageController] CodexModel is null.");
                yield break;
            }

            if (codexModel.items == null || codexModel.items.Count == 0)
            {
                Debug.LogError("[CodexPageController] CodexModel has no items.");
                yield break;
            }

            BuildUI();
        }

        private void BuildUI()
        {
            if (contentParent == null)
            {
                Debug.LogError("[CodexPageController] Content parent is not assigned.");
                return;
            }

            if (codexItemPrefab == null)
            {
                Debug.LogError("[CodexPageController] Codex item prefab is not assigned.");
                return;
            }

            ClearContent();

            for (int i = 0; i < codexModel.items.Count; i++)
            {
                CreateItem(codexModel.items[i], i);
            }
        }

        /// <summary>
        /// This method creates the items inside the codex menu. Each item is a CodexItemPrefab. The colors of the
        /// elements inside the menu is given by the list of colors that is listes inside the Codex Manager gameObject
        /// in the Design section
        /// </summary>
        /// <param name="item">The codex item to create </param>
        /// <param name="index">the position inside the index of the item</param>
        private void CreateItem(CodexItemDefinition item, int index)
        {
            CodexItemView view = Instantiate(codexItemPrefab, contentParent);

            //fallback color yellow
            Color assignedColor = Color.yellow; 
            if (itemColors != null && itemColors.Length > 0)
            {
                //repeats the colors that are inside the design color list cyclically 
                assignedColor = itemColors[index % itemColors.Length];
                assignedColor.a = 1f;
            }

            view.Setup(item, assignedColor);
            view.OnClicked += HandleItemClicked;
        }
        private void ClearContent()
        {
            if (contentParent == null)
                return;

            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }
        }
        private void HandleItemClicked(CodexItemDefinition item)
        {
            if (isOpeningDetail)
                return;

            if (item == null)
            {
                Debug.LogError("[CodexPageController] Clicked item is null.");
                return;
            }

            Debug.Log($"[CodexPageController] Clicked: {item.levelTitle} | State: {item.state} | Target: {item.target}");

            if (item.state == CodexItemState.Locked)
            {
                Debug.Log("[CodexPageController] Item is locked.");
                return;
            }

            if (string.IsNullOrEmpty(item.target))
            {
                Debug.LogError("[CodexPageController] Item target is empty. Cannot load directions.");
                return;
            }

            StartCoroutine(OpenCodexDetail(item));
        }

        private IEnumerator OpenCodexDetail(CodexItemDefinition item)
        {
            isOpeningDetail = true;

            yield return DataManager.Instance.SelectCodexItemAndLoadDirections(item);

            if (DataManager.Instance.SelectedDirections == null)
            {
                Debug.LogError("[CodexPageController] Directions were not loaded. Cannot open CodexDetail.");
                isOpeningDetail = false;
                yield break;
            }

            Debug.Log("[CodexPageController] Directions loaded. Opening CodexDetail.");

            NavigationManager.Instance.NavigateTo("CodexDetail");
        }

        private void OnBackClicked()
        {
            if (NavigationManager.Instance == null)
            {
                Debug.LogError("[CodexPageController] NavigationManager missing.");
                return;
            }

            NavigationManager.Instance.NavigateTo("Menu");
        }

        private void OnDestroy()
        {
            if (goBackButton != null)
                goBackButton.onClick.RemoveAllListeners();
        }
    }
}