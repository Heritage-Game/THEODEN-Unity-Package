using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Models;

public class CodexItemViewOLD : MonoBehaviour
{
    [SerializeField] private Image statusIcon;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI arrowText;
    [SerializeField] private Button itemButton;
    [SerializeField] private Image backgroundImage;

    private LevelData levelData;

    public void Setup(LevelData data)
    {
        levelData = data;
        levelTitleText.text = data.title;

        bool isLocked = !data.IsUnlocked();

        Color activeTextColor = new Color(0.2f, 0.2f, 0.2f);
        Color lockedTextColor = new Color(0.65f, 0.65f, 0.65f);
        Color activeIconColor = new Color(0.29f, 0.55f, 0.16f);
        Color lockedIconColor = new Color(0.7f, 0.7f, 0.7f);

        statusIcon.color = isLocked ? lockedIconColor : activeIconColor;
        levelTitleText.color = isLocked ? lockedTextColor : activeTextColor;
        arrowText.color = isLocked ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.5f, 0.5f, 0.5f);
        backgroundImage.color = isLocked
            ? new Color(0.94f, 0.94f, 0.94f, 0.6f)
            : new Color(0.94f, 0.94f, 0.94f, 1f);

        itemButton.interactable = !isLocked;
        itemButton.onClick.AddListener(OnItemClicked);
    }

    private void OnItemClicked()
    {
        Debug.Log("Tiklandi: " + levelData.title);
        DataManager.Instance.SelectedLevel = levelData;
        Transitions.LoadScene("CodexDetail");                  
    }

    private void OnDestroy()
    {
        if (itemButton != null)
            itemButton.onClick.RemoveAllListeners();
    }
}