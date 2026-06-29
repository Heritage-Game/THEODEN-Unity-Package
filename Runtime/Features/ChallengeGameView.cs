using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Models;

public class ChallengeGameView : MonoBehaviour
{
    [Header("UI Referanslari")]
    [SerializeField] private Image logoImage;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI explanationText;
    [SerializeField] private GameObject continueButton;

    private ChallengeData challengeData;
    private bool answered = false;
    private bool answeredCorrectly = false;

    // Renkler
    private Color defaultColor = new Color(0.94f, 0.94f, 0.94f);
    private Color correctColor = new Color(0.29f, 0.69f, 0.31f);
    private Color wrongColor = new Color(0.9f, 0.3f, 0.3f);
    private Color defaultTextColor = new Color(0.2f, 0.2f, 0.2f);
    private Color whiteTextColor = Color.white;

    private void Start()
    {
        continueButton.GetComponent<Button>().onClick.AddListener(OnContinueClicked);
        LoadChallenge();
    }

    private void LoadChallenge()
    {
        var selectedLevel = DataManager.Instance.SelectedLevel;

        if (selectedLevel != null && selectedLevel.challenge != null)
        {
            challengeData = selectedLevel.challenge;
        }
        else
        {
            Debug.LogError("Challenge verisi bulunamadi!");
            return;
        }

        questionText.text = challengeData.question;

        string[] prefixes = { "A) ", "B) ", "C) ", "D) " };

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;

            TextMeshProUGUI btnText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = prefixes[i] + challengeData.options[i];
            btnText.color = defaultTextColor;

            optionButtons[i].GetComponent<Image>().color = defaultColor;
            optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
        }

        resultText.text = "";
        explanationText.text = "";
        continueButton.SetActive(false);
    }

    private void OnOptionClicked(int selectedIndex)
    {
        if (answered) return;
        answered = true;

        answeredCorrectly = (selectedIndex == challengeData.correctIndex);

        Image selectedImage = optionButtons[selectedIndex].GetComponent<Image>();
        TextMeshProUGUI selectedText = optionButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();

        if (answeredCorrectly)
        {
            selectedImage.color = correctColor;
            selectedText.color = whiteTextColor;
            resultText.text = "CORRECT!";
            resultText.color = correctColor;
            Debug.Log("Dogru cevap!");
        }
        else
        {
            selectedImage.color = wrongColor;
            selectedText.color = whiteTextColor;

            Image correctImage = optionButtons[challengeData.correctIndex].GetComponent<Image>();
            TextMeshProUGUI correctText = optionButtons[challengeData.correctIndex].GetComponentInChildren<TextMeshProUGUI>();
            correctImage.color = correctColor;
            correctText.color = whiteTextColor;

            resultText.text = "WRONG!";
            resultText.color = wrongColor;
            Debug.Log("Yanlis cevap!");
        }

        explanationText.text = challengeData.explanation;

        foreach (var btn in optionButtons)
        {
            btn.interactable = false;
        }

        continueButton.SetActive(true);
    }

    private void OnContinueClicked()
    {
        var selectedLevel = DataManager.Instance.SelectedLevel;

        if (answeredCorrectly)
        {
            DataManager.Instance.UnlockNextLevel(selectedLevel.id);
            Debug.Log("Sonraki level acildi!");
        }

        DataManager.Instance.SelectedLevel = null;
        NavigationManager.Instance.NavigateTo("Codex");
    }

    private void OnDestroy()
    {
        foreach (var btn in optionButtons)
        {
            if (btn != null) btn.onClick.RemoveAllListeners();
        }

        Button contBtn = continueButton.GetComponent<Button>();
        if (contBtn != null) contBtn.onClick.RemoveAllListeners();
    }
}