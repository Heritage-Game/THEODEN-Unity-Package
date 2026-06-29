using System;
using Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Codex
{
    /// <summary>
    /// Passive UI view responsible for displaying
    /// a single codex item.
    ///
    /// This class should NOT:
    /// - perform navigation
    /// - mutate runtime state
    /// - load data
    /// - coordinate application logic
    ///
    /// It only:
    /// - displays UI
    /// - emits UI interaction events
    /// 
    /// This class notifies on the event that the CodexItem is clicked.
    /// </summary>
    public class CodexItemView : MonoBehaviour
    {
        public event Action<CodexItemDefinition> OnClicked;

        [Header("UI")]
        [SerializeField] private Image statusIcon;
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI arrowText;
        [SerializeField] private Button itemButton;
        [SerializeField] private Image backgroundImage;

        private CodexItemDefinition item;
        private Color customBackgroundColor;

        public void Setup(CodexItemDefinition itemDefinition, Color targetColor)
        {
            item = itemDefinition;
            customBackgroundColor = targetColor;

            if (item == null)
            {
                Debug.LogError("[CodexItemView] Cannot setup view. Item is null.");
                return;
            }

            if (levelTitleText != null)
                levelTitleText.text = item.levelTitle;

            ApplyVisualState(item.state);

            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(HandleClick);
            }
        }

        private void ApplyVisualState(CodexItemState state)
        {
            bool isLocked = state == CodexItemState.Locked;

            Color activeTextColor = Color.white;
            Color lockedTextColor = new Color(0.65f, 0.65f, 0.65f);

            Color activeIconColor = Color.white;
            Color lockedIconColor = new Color(0.7f, 0.7f, 0.7f);

            if (statusIcon != null)
                statusIcon.color = isLocked ? lockedIconColor : activeIconColor;

            if (levelTitleText != null)
                levelTitleText.color = isLocked ? lockedTextColor : activeTextColor;

            if (arrowText != null)
            {
                arrowText.color = isLocked
                    ? new Color(0.8f, 0.8f, 0.8f)
                    : Color.white;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isLocked
                    ? new Color(0.94f, 0.94f, 0.94f, 0.6f)
                    : customBackgroundColor;
            }

            if (itemButton != null)
                itemButton.interactable = !isLocked;
        }

        private void HandleClick()
        {
            if (item == null)
            {
                Debug.LogError("[CodexItemView] Clicked item is null.");
                return;
            }

            OnClicked?.Invoke(item);
        }

        private void OnDestroy()
        {
            if (itemButton != null)
                itemButton.onClick.RemoveAllListeners();

            OnClicked = null;
        }
    }
}