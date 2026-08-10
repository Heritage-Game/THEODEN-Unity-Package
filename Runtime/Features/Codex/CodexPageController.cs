using System.Collections;
using System.Collections.Generic;
using Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

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
        // ============================================================
        // UI REFERENCES
        // ============================================================
        [Header("UI References")]
        [SerializeField] private UIDocument uiDocument;

        private VisualElement root;
        private VisualElement contentParent;
        private Button goBackButton;

        // ============================================================
        // TEMPLATE
        // ============================================================
        [Header("Templates")]
        [SerializeField] private VisualTreeAsset codexItemTemplate;

        // ============================================================
        // DESIGN
        // ============================================================
        [Header("Design")]
        [SerializeField] private Color[] itemColors;

        // ============================================================
        // RUNTIME STATE
        // ============================================================
        private CodexModel codexModel;
        private bool isOpeningDetail = false;
        private readonly List<VisualElement> itemElements = new List<VisualElement>();

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================
        private void OnEnable()
        {
            if (uiDocument == null)
            {
                Debug.LogError("[CodexPageController] UIDocument not assigned.");
                return;
            }

            root = uiDocument.rootVisualElement;
            BindUIElements();
            SetupButtons();

            if (DataManager.Instance == null)
            {
                Debug.LogError("[DataManager] DataManager not found in scene. Please add it to the scene.");
                return;
            }

            if (codexItemTemplate == null)
            {
                Debug.LogError("[CodexPageController] Codex item template is not assigned.");
                return;
            }

            StartCoroutine(Initialize());
        }

        private void OnDisable()
        {
            if (goBackButton != null)
                goBackButton.clicked -= OnBackClicked;

            ClearContent();
        }

        // ============================================================
        // UI BINDING
        // ============================================================
        private void BindUIElements()
        {
            contentParent = root.Q<VisualElement>("content_parent");
            goBackButton = root.Q<Button>("back_button");

            if (contentParent == null)
                Debug.LogWarning("[CodexPageController] 'content_parent' not found in UXML.");

            if (goBackButton == null)
                Debug.LogWarning("[CodexPageController] 'back_button' not found in UXML.");
        }

        // ============================================================
        // SETUP
        // ============================================================
        private void SetupButtons()
        {
            if (goBackButton != null)
            {
                goBackButton.clicked -= OnBackClicked;
                goBackButton.clicked += OnBackClicked;
            }
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================
        private IEnumerator Initialize()
        {
            while (DataManager.Instance == null)
            {
                yield return null;
            }

            while (!DataManager.Instance.IsDataLoaded)
            {
                Debug.Log("[CodexPageController] Waiting for DataManager to load...");
                yield return null; 
            }

            codexModel = DataManager.Instance.CodexMenu;

            if (codexModel == null)
            {
                Debug.LogError("[CodexPageController] CodexModel is null.");
                ShowError("Codex data not available.");
                yield break;
            }

            if (codexModel.items == null || codexModel.items.Count == 0)
            {
                Debug.LogError("[CodexPageController] CodexModel has no items.");
                ShowError("No items found in Codex.");
                yield break;
            }

            GenerateButtons();
        }

        // ============================================================
        // UI BUILDING
        // ============================================================
        private void GenerateButtons()
        {
            if (contentParent == null)
            {
                Debug.LogError("[CodexPageController] Content parent is not assigned.");
                return;
            }

            if (codexItemTemplate == null)
            {
                Debug.LogError("[CodexPageController] Codex item template is not assigned.");
                return;
            }

            ClearContent();

            for (int i = 0; i < codexModel.items.Count; i++)
            {
                CreateItem(codexModel.items[i], i);
            }
        }

        /// <summary>
        /// Creates a codex item using UI Toolkit template.
        /// </summary>
        /// <param name="item">The codex item to create</param>
        /// <param name="index">The position inside the index of the item</param>
        private void CreateItem(CodexItemDefinition item, int index)
        {
            // button template
            VisualElement itemElement = codexItemTemplate.Instantiate();
            itemElement.style.marginBottom = 200;
            Button itemButton = itemElement.Q<Button>("poi_button");

            if (itemButton != null)
            {
                itemButton.text = item.levelTitle;

                switch (item.state)
                {
                    case CodexItemState.Locked:
                        itemButton.SetEnabled(false);
                        itemButton.style.backgroundColor = new Color(0.4667f, 0.4667f, 0.4667f);
                        break;
                    case CodexItemState.Directions:
                        itemButton.text += " >";
                        break;
                    case CodexItemState.Unlocked:
                        itemButton.text += " ✅";
                        //itemButton.SetEnabled(false);
                        itemButton.style.backgroundColor = new Color(0.518f, 0.769f, 0.255f);
                        break;
                }

                CodexItemDefinition capturedItem = item;
                itemButton.clicked += () => HandleItemClicked(capturedItem);
            }

            contentParent.Add(itemElement);
            itemElements.Add(itemElement);
        }

        private void ClearContent()
        {
            if (contentParent == null)
                return;

            foreach (var item in itemElements)
            {
                if (item != null && item.parent != null)
                    item.parent.Remove(item);
            }

            itemElements.Clear();
        }

        // ============================================================
        // UI UTILITIES
        // ============================================================
        private void ShowError(string message)
        {
            if (contentParent != null)
            {
                Label errorLabel = new Label();
                errorLabel.text = $"{message}";
                errorLabel.style.color = Color.red;
                errorLabel.style.fontSize = 18;
                errorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                errorLabel.style.marginTop = 40;
                errorLabel.style.marginBottom = 40;
                contentParent.Add(errorLabel);
            }
        }

        // ============================================================
        // ITEM CLICK HANDLER
        // ============================================================
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

        // ============================================================
        // NAVIGATION
        // ============================================================
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

            isOpeningDetail = false;

            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.NavigateTo("CodexDetailUIToolkit");
            }
            else
            {
                Debug.LogError("[CodexPageController] NavigationManager missing.");
            }
        }

        private void OnBackClicked()
        {
            if (NavigationManager.Instance == null)
            {
                Debug.LogError("[CodexPageController] NavigationManager missing.");
                return;
            }

            NavigationManager.Instance.GoBack();
        }
    }
}