using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the multiple-choice challenge scene.
/// </summary>
/// <remarks>
/// This manager reads the currently loaded POI from <see cref="DataManager.SelectedPOI"/>
/// and expects it to be a <see cref="MultipleChoicePOIModel"/>.
/// 
/// Answer buttons are generated dynamically from the runtime model, so the challenge
/// is no longer limited to exactly four answers.
/// 
/// This class currently supports only multiple-choice POIs. Open-answer challenges
/// should use a different manager or a higher-level challenge controller that chooses
/// the correct view based on the concrete POI model type.
/// </remarks>
public class ChallengeGameManager : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================

    /// <summary>
    /// Text component used to display the POI name.
    /// </summary>
    [Header("UI References")]
    [SerializeField] private TMP_Text poiNameText;

    /// <summary>
    /// Text component used to display the initial description, question and hints.
    /// </summary>
    [SerializeField] private TMP_Text questionText;

    /// <summary>
    /// Parent transform where answer buttons are instantiated.
    /// </summary>
    [SerializeField] private Transform answersParent;

    /// <summary>
    /// Prefab used to create one answer button.
    /// </summary>
    /// <remarks>
    /// The prefab should contain a <see cref="Button"/> component and a child
    /// <see cref="TMP_Text"/> component.
    /// </remarks>
    [SerializeField] private Button answerButtonPrefab;

    /// <summary>
    /// Button used to show the challenge hint.
    /// </summary>
    [SerializeField] private Button hintButton;

    // ============================================================
    // COLORS
    // ============================================================

    /// <summary>
    /// Normal answer button color.
    /// </summary>
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.176f, 0.353f, 0.153f);

    /// <summary>
    /// Color used for the correct answer.
    /// </summary>
    [SerializeField] private Color correctColor = new Color(0.2f, 0.7f, 0.2f);

    /// <summary>
    /// Color used for the wrong selected answer.
    /// </summary>
    [SerializeField] private Color wrongColor = new Color(0.8f, 0.2f, 0.2f);

    // ============================================================
    // RUNTIME STATE
    // ============================================================

    /// <summary>
    /// Currently loaded multiple-choice POI.
    /// </summary>
    private MultipleChoicePOIModel currentPOI;

    /// <summary>
    /// Normalized key of the correct answer.
    /// </summary>
    private string correctAnswerKey;

    /// <summary>
    /// True after the player has selected an answer and before retry is allowed.
    /// </summary>
    private bool answered;

    /// <summary>
    /// Runtime answer buttons generated from the POI model.
    /// </summary>
    private readonly List<Button> answerButtons = new List<Button>();

    /// <summary>
    /// Maps normalized answer keys to their generated buttons.
    /// </summary>
    private readonly Dictionary<string, Button> answerButtonsByKey = new Dictionary<string, Button>();

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    /// <summary>
    /// Loads the current POI and displays its challenge.
    /// </summary>
    private void Start()
    {
        LoadAndDisplay();
    }

    /// <summary>
    /// Cleans generated answer buttons and button listeners.
    /// </summary>
    private void OnDestroy()
    {
        ClearGeneratedAnswers();

        if (hintButton != null)
            hintButton.onClick.RemoveAllListeners();
    }

    // ============================================================
    // LOADING
    // ============================================================

    /// <summary>
    /// Loads the selected POI from the DataManager and validates that it is a multiple-choice challenge.
    /// </summary>
    private void LoadAndDisplay()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[Challenge] DataManager is missing.");
            return;
        }

        POIModel selectedPOI = DataManager.Instance.SelectedPOI;

        if (selectedPOI == null)
        {
            Debug.LogError("[Challenge] SelectedPOI is null.");
            return;
        }

        if (selectedPOI is not MultipleChoicePOIModel multipleChoicePOI)
        {
            Debug.LogError(
                "[Challenge] SelectedPOI is not a MultipleChoicePOIModel. Actual type: " +
                selectedPOI.GetType().Name
            );

            return;
        }

        currentPOI = multipleChoicePOI;

        Debug.Log("[Challenge] Loading challenge for POI: " + currentPOI.poiName);

        DisplayChallenge();
    }

    // ============================================================
    // DISPLAY
    // ============================================================

    /// <summary>
    /// Displays the multiple-choice challenge UI.
    /// </summary>
    private void DisplayChallenge()
    {
        if (currentPOI == null)
        {
            Debug.LogError("[Challenge] currentPOI is null.");
            return;
        }

        answered = false;
        correctAnswerKey = NormalizeAnswerKey(currentPOI.correctAnswer);

        if (poiNameText != null)
            poiNameText.text = currentPOI.poiName;

        SetQuestionText();

        ClearGeneratedAnswers();
        CreateAnswerButtons();

        SetupHintButton();

        Debug.Log("[Challenge] Question: " + currentPOI.question);
        Debug.Log("[Challenge] Correct answer: " + correctAnswerKey);
    }

    /// <summary>
    /// Updates the question text using the initial description and question fields.
    /// </summary>
    private void SetQuestionText()
    {
        if (questionText == null || currentPOI == null)
            return;

        string initialDescription = string.IsNullOrWhiteSpace(currentPOI.initialDescription)
            ? ""
            : currentPOI.initialDescription + "\n\n";

        questionText.text = initialDescription + currentPOI.question;
    }

    /// <summary>
    /// Configures the hint button listener.
    /// </summary>
    private void SetupHintButton()
    {
        if (hintButton == null)
            return;

        hintButton.onClick.RemoveAllListeners();
        hintButton.onClick.AddListener(OnHintClicked);
    }

    // ============================================================
    // ANSWER BUTTON GENERATION
    // ============================================================

    /// <summary>
    /// Creates one answer button for each answer available in the current POI model.
    /// </summary>
    private void CreateAnswerButtons()
    {
        if (currentPOI.answers == null || currentPOI.answers.Count == 0)
        {
            Debug.LogError("[Challenge] No answers found in MultipleChoicePOIModel.");
            return;
        }

        if (answersParent == null)
        {
            Debug.LogError("[Challenge] Answers Parent is not assigned.");
            return;
        }

        if (answerButtonPrefab == null)
        {
            Debug.LogError("[Challenge] Answer Button Prefab is not assigned.");
            return;
        }

        foreach (MultipleChoicePOIModel.AnswerEntry answer in currentPOI.answers)
        {
            if (answer == null)
                continue;

            CreateAnswerButton(answer);
        }
    }

    /// <summary>
    /// Creates and initializes one answer button.
    /// </summary>
    /// <param name="answer">
    /// Answer data used to populate the button.
    /// </param>
    private void CreateAnswerButton(MultipleChoicePOIModel.AnswerEntry answer)
    {
        Button button = Instantiate(answerButtonPrefab, answersParent);

        string normalizedKey = NormalizeAnswerKey(answer.key);

        TMP_Text answerText = button.GetComponentInChildren<TMP_Text>();

        if (answerText != null)
            answerText.text = BuildAnswerText(answer);

        button.interactable = true;
        SetButtonColor(button, normalColor);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnAnswerClicked(normalizedKey, button));

        answerButtons.Add(button);

        if (!string.IsNullOrWhiteSpace(normalizedKey))
            answerButtonsByKey[normalizedKey] = button;
    }

    /// <summary>
    /// Builds the displayed answer text.
    /// </summary>
    /// <param name="answer">
    /// Answer entry.
    /// </param>
    /// <returns>
    /// Formatted answer text.
    /// </returns>
    private string BuildAnswerText(MultipleChoicePOIModel.AnswerEntry answer)
    {
        string key = string.IsNullOrWhiteSpace(answer.key)
            ? ""
            : answer.key.Trim();

        string value = string.IsNullOrWhiteSpace(answer.value)
            ? ""
            : answer.value.Trim();

        if (string.IsNullOrWhiteSpace(key))
            return value;

        return key + " " + value;
    }

    /// <summary>
    /// Removes all generated answer buttons and clears runtime mappings.
    /// </summary>
    private void ClearGeneratedAnswers()
    {
        foreach (Button button in answerButtons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            Destroy(button.gameObject);
        }

        answerButtons.Clear();
        answerButtonsByKey.Clear();
    }

    // ============================================================
    // ANSWER SELECTION
    // ============================================================

    /// <summary>
    /// Handles answer button selection.
    /// </summary>
    /// <param name="answerKey">
    /// Normalized selected answer key.
    /// </param>
    /// <param name="clickedButton">
    /// Button clicked by the player.
    /// </param>
    private void OnAnswerClicked(string answerKey, Button clickedButton)
    {
        if (answered)
            return;

        answered = true;

        Debug.Log("[Challenge] Player selected: " + answerKey);

        if (answerKey == correctAnswerKey)
        {
            HandleCorrectAnswer(clickedButton);
            return;
        }

        HandleWrongAnswer(clickedButton);
    }

    /// <summary>
    /// Handles the correct answer case.
    /// </summary>
    /// <param name="clickedButton">
    /// Button clicked by the player.
    /// </param>
    private void HandleCorrectAnswer(Button clickedButton)
    {
        Debug.Log("[Challenge] Correct answer!");

        SetButtonColor(clickedButton, correctColor);
        SetAnswerButtonsInteractable(false);

        StartCoroutine(ShowVictory());
    }

    /// <summary>
    /// Handles the wrong answer case.
    /// </summary>
    /// <param name="clickedButton">
    /// Button clicked by the player.
    /// </param>
    private void HandleWrongAnswer(Button clickedButton)
    {
        Debug.Log("[Challenge] Wrong answer.");

        SetButtonColor(clickedButton, wrongColor);

        Button correctButton = GetButtonForKey(correctAnswerKey);

        if (correctButton != null)
            SetButtonColor(correctButton, correctColor);

        SetAnswerButtonsInteractable(false);

        if (questionText != null)
        {
            string hint = string.IsNullOrWhiteSpace(currentPOI.hint)
                ? "Try again."
                : currentPOI.hint;

            questionText.text = "<color=#CC0000>Wrong!</color>\n\n<b>Hint:</b> " + hint;
        }

        StartCoroutine(AllowRetry());
    }

    /// <summary>
    /// Allows the player to try again after a wrong answer.
    /// </summary>
    private IEnumerator AllowRetry()
    {
        yield return new WaitForSeconds(3f);

        ResetButtonColors();
        SetAnswerButtonsInteractable(true);
        SetQuestionText();

        answered = false;

        Debug.Log("[Challenge] Try again.");
    }

    /// <summary>
    /// Shows the current POI hint.
    /// </summary>
    private void OnHintClicked()
    {
        if (currentPOI == null || questionText == null)
            return;

        string hint = string.IsNullOrWhiteSpace(currentPOI.hint)
            ? "No hint available."
            : currentPOI.hint;

        questionText.text =
            currentPOI.question +
            "\n\n<b>Hint:</b> " +
            hint;
    }

    // ============================================================
    // VICTORY
    // ============================================================

    /// <summary>
    /// Handles challenge completion and returns to the Codex scene.
    /// </summary>
    private IEnumerator ShowVictory()
    {
        yield return new WaitForSeconds(1.5f);

        if (DataManager.Instance != null)
            DataManager.Instance.MarkCurrentPOICompleted();

        if (currentPOI != null)
            currentPOI.isChallengeCompleted = true;

        Debug.Log("[Challenge] Victory!");

        if (currentPOI != null)
        {
            Debug.Log("[Challenge] Full narrative: " + currentPOI.fullNarrative);

            if (currentPOI.poiBadge != null)
                Debug.Log("[Challenge] Badge earned: " + currentPOI.poiBadge.name);
            else
                Debug.Log("[Challenge] No badge sprite loaded.");
        }

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo("Codex");
        }
        else
        {
            Debug.LogError("[Challenge] NavigationManager is missing.");
        }
    }

    // ============================================================
    // BUTTON UTILITIES
    // ============================================================

    /// <summary>
    /// Gets an answer button by normalized key.
    /// </summary>
    /// <param name="key">
    /// Answer key.
    /// </param>
    /// <returns>
    /// Matching button, or null if not found.
    /// </returns>
    private Button GetButtonForKey(string key)
    {
        key = NormalizeAnswerKey(key);

        if (answerButtonsByKey.TryGetValue(key, out Button button))
            return button;

        return null;
    }

    /// <summary>
    /// Normalizes an answer key for comparison.
    /// </summary>
    /// <param name="key">
    /// Raw answer key.
    /// </param>
    /// <returns>
    /// Normalized answer key.
    /// </returns>
    private string NormalizeAnswerKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        return key
            .Trim()
            .Replace(".", "")
            .ToUpperInvariant();
    }

    /// <summary>
    /// Resets all generated answer button colors to the normal color.
    /// </summary>
    private void ResetButtonColors()
    {
        foreach (Button button in answerButtons)
            SetButtonColor(button, normalColor);
    }

    /// <summary>
    /// Sets all generated answer buttons interactable or non-interactable.
    /// </summary>
    /// <param name="interactable">
    /// Whether the buttons should be interactable.
    /// </param>
    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    /// <summary>
    /// Changes the visual color of a button image.
    /// </summary>
    /// <param name="button">
    /// Button to update.
    /// </param>
    /// <param name="color">
    /// Color to assign.
    /// </param>
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();

        if (image != null)
            image.color = color;
    }
}