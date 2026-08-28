using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.Models;

/// <summary>
/// Controls the multiple-choice challenge scene using UI Toolkit.
/// </summary>
public class ChallengeGameManager : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES (UI TOOLKIT)
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Label poiNameText;
    private Label questionText;
    private VisualElement answersParent;
    private Button hintButton;
    private Button backButton;
    private VisualElement resultContainer;
    private Label resultLabel;
    private Label progressLabel;

    // ============================================================
    // TEMPLATE
    // ============================================================
    [Header("Templates")]
    [SerializeField] private VisualTreeAsset answerButtonTemplate;

    // ============================================================
    // COLORS
    // ============================================================
    [Header("Colors")]
    //[SerializeField] private Color normalColor = new Color(0.217f, 0.217f, 0.217f);
    [SerializeField] private Color normalColor = Color.grey;
    [SerializeField] private Color correctColor = new Color(0.518f, 0.769f, 0.255f);
    [SerializeField] private Color wrongColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color correctTextColor = Color.white;
    [SerializeField] private Color wrongTextColor = Color.white;

    // ============================================================
    // RUNTIME STATE
    // ============================================================
    private MultipleChoicePOIModel currentPOI;
    private string correctAnswerKey;
    private bool answered;
    private bool isWaitingForRetry;
    private int wrongAttempts = 0;
    private const int MAX_ATTEMPTS = 3;

    private readonly List<VisualElement> answerContainers = new List<VisualElement>();
    private readonly List<Button> answerButtons = new List<Button>();
    private readonly Dictionary<string, Button> answerButtonsByKey = new Dictionary<string, Button>();

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[ChallengeGameManager] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        EnsureLocalizationManager();
        SetupButtons();
        LoadAndDisplay();
    }

    private void OnDisable()
    {
        if (hintButton != null)
            hintButton.clicked -= OnHintClicked;

        if (backButton != null)
            backButton.clicked -= OnBackClicked;
        ClearGeneratedAnswers();
    }

    private void EnsureLocalizationManager()
    {
        if (LocalizationManager.Instance == null)
        {
            GameObject go = new GameObject("LocalizationManager");
            LocalizationManager lm = go.AddComponent<LocalizationManager>();
            DontDestroyOnLoad(go);
            Debug.Log("[InstructionsPageManager] LocalizationManager created.");
        }

        LocalizationManager.Instance?.LoadLocalization();
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        poiNameText = root.Q<Label>("poi_progress_label");
        questionText = root.Q<Label>("question_label");
        answersParent = root.Q<VisualElement>("answer_container");
        hintButton = root.Q<Button>("hint_button");
        resultContainer = root.Q<VisualElement>("result_container");
        resultLabel = root.Q<Label>("result_label");
        backButton = root.Q<Button>("back_button");
        progressLabel = root.Q<Label>("progress_label");

        if (answersParent != null)
        {
            answersParent.style.flexDirection = FlexDirection.Column;
            answersParent.style.flexGrow = 1;
            answersParent.style.width = new Length(100, LengthUnit.Percent);
        }

        if (poiNameText == null)
            Debug.LogWarning("[ChallengeGameManager] 'poi_name_text' not found in UXML.");

        if (questionText == null)
            Debug.LogWarning("[ChallengeGameManager] 'question_text' not found in UXML.");

        if (answersParent == null)
            Debug.LogWarning("[ChallengeGameManager] 'answers_parent' not found in UXML.");

        if (hintButton == null)
            Debug.LogWarning("[ChallengeGameManager] 'hint_button' not found in UXML.");

        if (resultContainer == null)
            Debug.LogWarning("[ChallengeGameManager] 'result_container' not found in UXML.");

        if (resultLabel == null)
            Debug.LogWarning("[ChallengeGameManager] 'result_label' not found in UXML.");

        if (backButton == null)
            Debug.LogWarning("[ChallengeGameManager] 'back_button' not found in UXML.");

        if (progressLabel == null)
            Debug.LogWarning("[ChallengeGameManager] 'progress_label' not found in UXML.");
    }

    // ============================================================
    // SETUP
    // ============================================================
    private void SetupButtons()
    {
        if (hintButton != null)
        {
            hintButton.clicked -= OnHintClicked;
            hintButton.clicked += OnHintClicked;
            if (LocalizationManager.Instance != null)
            {
                hintButton.text = LocalizationManager.Instance.GetText("hint_button");
            }
        }

        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }
    }

    private void OnBackClicked()
    {
        Debug.Log("[ChallengeGameManager] Back button clicked.");

        StopAllCoroutines();

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.GoBack();
        }
        else
        {
            Debug.LogError("[ChallengeGameManager] NavigationManager is missing.");
        }
    }

    // ============================================================
    // LOADING
    // ============================================================
    private void LoadAndDisplay()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[ChallengeGameManager] DataManager is missing.");
            ShowFallback();
            return;
        }

        POIModel selectedPOI = DataManager.Instance.SelectedPOI;

        if (selectedPOI == null)
        {
            Debug.LogError("[ChallengeGameManager] SelectedPOI is null.");
            ShowFallback();
            return;
        }

        if (selectedPOI is not MultipleChoicePOIModel multipleChoicePOI)
        {
            Debug.LogError(
                "[ChallengeGameManager] SelectedPOI is not a MultipleChoicePOIModel. Actual type: " +
                selectedPOI.GetType().Name
            );
            ShowFallback();
            return;
        }

        currentPOI = multipleChoicePOI;

        Debug.Log("[ChallengeGameManager] Loading challenge for POI: " + currentPOI.poiName);

        DisplayChallenge();
    }

    // ============================================================
    // DISPLAY
    // ============================================================
    private void DisplayChallenge()
    {
        if (currentPOI == null)
        {
            Debug.LogError("[ChallengeGameManager] currentPOI is null.");
            return;
        }

        answered = false;
        isWaitingForRetry = false;
        wrongAttempts = 0;
        correctAnswerKey = NormalizeAnswerKey(currentPOI.correctAnswer);

        if (poiNameText != null)
            poiNameText.text = currentPOI.poiName;

        SetQuestionText();

        ClearGeneratedAnswers();
        CreateAnswerButtons();

        HideResult();

        UpdateProgressLabel();

        Debug.Log("[ChallengeGameManager] Question: " + currentPOI.question);
        Debug.Log("[ChallengeGameManager] Correct answer: " + correctAnswerKey);
    }

    private void SetQuestionText()
    {
        if (questionText == null || currentPOI == null)
            return;

        string initialDescription = string.IsNullOrWhiteSpace(currentPOI.initialDescription)
            ? ""
            : currentPOI.initialDescription + "\n\n";

        questionText.text = initialDescription + currentPOI.question;
    }

    // ============================================================
    // ANSWER BUTTON GENERATION
    // ============================================================
    private void CreateAnswerButtons()
    {
        if (currentPOI.answers == null || currentPOI.answers.Count == 0)
        {
            Debug.LogError("[ChallengeGameManager] No answers found in MultipleChoicePOIModel.");
            return;
        }

        if (answersParent == null)
        {
            Debug.LogError("[ChallengeGameManager] Answers Parent is not assigned.");
            return;
        }

        if (answerButtonTemplate == null)
        {
            Debug.LogError("[ChallengeGameManager] Answer Button Template is not assigned.");
            return;
        }

        foreach (MultipleChoicePOIModel.AnswerEntry answer in currentPOI.answers)
        {
            if (answer == null) continue;
            CreateAnswerButton(answer);
        }
    }

    private void CreateAnswerButton(MultipleChoicePOIModel.AnswerEntry answer)
    {
        VisualElement buttonElement = answerButtonTemplate.Instantiate();
        buttonElement.style.marginBottom = 200;
        buttonElement.style.marginTop = 0;

        Button button = buttonElement.Q<Button>("answer_button");
        VisualElement container = buttonElement.Q<VisualElement>("answer_container");

        if (container == null) container = buttonElement;

        string normalizedKey = NormalizeAnswerKey(answer.key);
        string displayText = BuildAnswerText(answer);

        if (button != null)
        {
            button.text = displayText;
            button.style.backgroundColor = normalColor;
            button.style.color = normalTextColor;
            // listener
            string capturedKey = normalizedKey;
            button.clicked += () => OnAnswerClicked(capturedKey, container, button);
        }

        answersParent.Add(buttonElement);

        answerContainers.Add(buttonElement);
        answerButtons.Add(button);

        if (!string.IsNullOrWhiteSpace(normalizedKey)) answerButtonsByKey[normalizedKey] = button;
    }

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

    private void ClearGeneratedAnswers()
    {
        foreach (var container in answerContainers)
        {
            if (container != null && container.parent != null)
                container.parent.Remove(container);
        }

        answerContainers.Clear();
        answerButtons.Clear();
        answerButtonsByKey.Clear();
    }

    // ============================================================
    // ANSWER SELECTION
    // ============================================================
    private void OnAnswerClicked(string answerKey, VisualElement container, Button button)
    {
        Debug.Log($"[ChallengeGameManager] OnAnswerClicked - answered: {answered}, isWaitingForRetry: {isWaitingForRetry}, attempts: {wrongAttempts}/{MAX_ATTEMPTS}");

        if (answered || isWaitingForRetry)
            return;

        // NON USARE ChallengeSessionService.RegisterAttempt qui
        // Gestisci tutto manualmente
        answered = true;

        Debug.Log("[ChallengeGameManager] Player selected: " + answerKey);

        bool isCorrect = string.Equals(answerKey, correctAnswerKey, System.StringComparison.Ordinal);

        if (isCorrect)
        {
            HandleCorrectAnswer(container, button);
        }
        else
        {
            HandleWrongAnswer(container, button);
        }
    }

    private void HandleCorrectAnswer(VisualElement container, Button button)
    {
        Debug.Log("[ChallengeGameManager] Correct answer!");

        SetAnswerStyle(container, button, correctColor, correctTextColor);
        SetAnswerButtonsInteractable(false);

        UpdateProgressLabel();

        ShowResult("Correct answer!");
        CompleteChallenge(solvedByPlayer:true);
    }

    private void HandleWrongAnswer(VisualElement container, Button button)
    {
        wrongAttempts++;
        Debug.Log($"[ChallengeGameManager] Wrong answer. Attempt {wrongAttempts}/{MAX_ATTEMPTS}");

        UpdateProgressLabel();

        SetAnswerStyle(container, button, wrongColor, wrongTextColor);
        SetAnswerButtonsInteractable(false);

        if (wrongAttempts >= MAX_ATTEMPTS)
        {
            Button correctButton = GetButtonForKey(correctAnswerKey);
            if (correctButton != null)
            {
                SetAnswerStyle(
                    correctButton.parent,
                    correctButton,
                    correctColor,
                    correctTextColor
                );
            }

            ShowResult($"No attempts remaining.\nCorrect answer: {correctAnswerKey}");
            CompleteChallenge(solvedByPlayer: false);
            return;
        }

        string hint = string.IsNullOrWhiteSpace(currentPOI.hint)
            ? $"Wrong answer. Try again ({wrongAttempts}/{MAX_ATTEMPTS})"
            : $"Wrong answer. Hint: {currentPOI.hint} ({wrongAttempts}/{MAX_ATTEMPTS})";

        ShowResult(hint);

        isWaitingForRetry = true;
        StartCoroutine(AllowRetry());
    }

    private IEnumerator AllowRetry()
    {
        yield return new WaitForSeconds(1.5f);

        ResetButtonColors();
        SetAnswerButtonsInteractable(true);
        SetQuestionText();
        HideResult();
        UpdateProgressLabel();
        answered = false;
        isWaitingForRetry = false;

        Debug.Log($"[ChallengeGameManager] Try again. Attempts left: {MAX_ATTEMPTS - wrongAttempts}");
    }

    // ============================================================
    // HINT
    // ============================================================
    private void OnHintClicked()
    {
        if (currentPOI == null || questionText == null)
            return;

        if (answered)
            return;

        string hint = string.IsNullOrWhiteSpace(currentPOI.hint)
            ? LocalizationManager.Instance.GetText("no_hint_label")
            : currentPOI.hint;

        questionText.text = currentPOI.question + "\n\n<b>Hint:</b> " + hint;
    }

    // ============================================================
    // VICTORY
    // ============================================================
    private IEnumerator ShowVictory()
    {
        yield return new WaitForSeconds(1.5f);

        if (DataManager.Instance != null)
            DataManager.Instance.MarkCurrentPOICompleted();

        if (currentPOI != null)
            currentPOI.isChallengeCompleted = true;

        Debug.Log("[ChallengeGameManager] Victory!");

        if (currentPOI != null)
        {
            Debug.Log("[ChallengeGameManager] Full narrative: " + currentPOI.fullNarrative);

            if (currentPOI.poiBadge != null)
                Debug.Log("[ChallengeGameManager] Badge earned: " + currentPOI.poiBadge.name);
            else
                Debug.Log("[ChallengeGameManager] No badge sprite loaded.");
        }

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo("CodexUIToolkit");
        }
        else
        {
            Debug.LogError("[ChallengeGameManager] NavigationManager is missing.");
        }
    }

    // ============================================================
    // UI UTILITIES
    // ============================================================
    private void HideResult()
    {
        if (resultContainer != null)
            resultContainer.style.display = DisplayStyle.None;

        if (resultLabel != null)
            resultLabel.text = "";
    }

    private void SetAnswerStyle(VisualElement container, Button button, Color bgColor, Color textColor)
    {
        if (container != null)
            container.style.backgroundColor = bgColor;

        if (button != null)
        {
            button.style.color = textColor;
            button.style.backgroundColor = bgColor;
        }
    }

    private void ResetButtonColors()
    {
        for (int i = 0; i < answerContainers.Count && i < answerButtons.Count; i++)
        {
            SetAnswerStyle(
                answerContainers[i],
                answerButtons[i],
                normalColor,
                normalTextColor
            );
        }
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (var button in answerButtons)
        {
            if (button != null)
            {
                button.SetEnabled(interactable);
                if (interactable)
                {
                    button.style.backgroundColor = normalColor;
                    button.style.color = normalTextColor;
                }
            }
        }
    }

    private Button GetButtonForKey(string key)
    {
        key = NormalizeAnswerKey(key);

        if (answerButtonsByKey.TryGetValue(key, out Button button))
            return button;

        return null;
    }

    private string NormalizeAnswerKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        return key
            .Trim()
            .Replace(".", "")
            .ToUpperInvariant();
    }

    private void ShowFallback()
    {
        if (questionText != null)
            questionText.text = "Challenge data not available.";

        if (hintButton != null)
            hintButton.SetEnabled(false);

        if (resultContainer != null)
            resultContainer.style.display = DisplayStyle.None;
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel == null)
            return;

        progressLabel.text = $"{LocalizationManager.Instance.GetText("attempts_progress")}: {wrongAttempts}/{MAX_ATTEMPTS}";
        progressLabel.style.color = correctColor;
    }

    private void CompleteChallenge(bool solvedByPlayer)
    {
        if (currentPOI == null)
        {
            Debug.LogError(
                "[ChallengeGameManager] Cannot complete a null POI."
            );

            return;
        }

        if (DataManager.Instance != null)
            DataManager.Instance.MarkCurrentPOICompleted();

        if (currentPOI != null)
            currentPOI.isChallengeCompleted = true;

        Debug.Log($"[ChallengeGameManager] Challenge completed. Solved: {solvedByPlayer}");

        if (backButton != null)
            backButton.SetEnabled(false);

        StartCoroutine(NavigateToBadgePage());
    }

    private IEnumerator NavigateToBadgePage()
    {
        yield return new WaitForSeconds(0.5f);

        if (NavigationManager.Instance == null)
        {
            Debug.LogError(
                "[ChallengeGameManager] NavigationManager is missing."
            );
            yield break;
        }

        NavigationManager.Instance.NavigateTo("BadgePageUIToolkit");
    }

    private void ShowResult(string message)
    {
        if (resultContainer != null)
            resultContainer.style.display = DisplayStyle.Flex;

        if (resultLabel != null)
            resultLabel.text = message ?? "";
    }
}