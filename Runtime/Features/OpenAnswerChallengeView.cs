using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class OpenAnswerChallengeView : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Label poiNameText;
    private Label questionText;
    private TextField answerInput;
    private Button submitButton;
    private Button backButton;
    private Button hintButton;
    private Label hintLabel;
    private Label progressLabel;
    private VisualElement resultContainer;
    private Label resultLabel;

    // ============================================================
    // RUNTIME STATE
    // ============================================================
    private OpenAnswerPOIModel currentPOI;
    private string correctAnswerKey;
    private bool answered;
    private bool isWaitingForRetry;
    private int wrongAttempts = 0;
    private const int MAX_ATTEMPTS = 3;

    // ============================================================
    // COLORS
    // ============================================================
    [Header("Colors")]
    [SerializeField] private Color correctColor = new Color(0.518f, 0.769f, 0.255f);
    [SerializeField] private Color wrongColor = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color correctTextColor = Color.white;
    [SerializeField] private Color wrongTextColor = Color.white;

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] UIDocument not assigned.");
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
        if (submitButton != null)
            submitButton.clicked -= OnSubmitClicked;

        if (backButton != null)
            backButton.clicked -= OnBackClicked;

        if (hintButton != null)
            hintButton.clicked -= OnHintClicked;
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
        poiNameText = root.Q<Label>("poi_label");
        questionText = root.Q<Label>("question_label");
        answerInput = root.Q<TextField>("answer_form");
        submitButton = root.Q<Button>("submit_button");
        backButton = root.Q<Button>("back_button");
        hintButton = root.Q<Button>("hint_button");
        hintLabel = root.Q<Label>("hint_label");
        progressLabel = root.Q<Label>("progress_label");
        resultLabel = root.Q<Label>("result_label");
        resultContainer = root.Q<VisualElement>("result_container");

        if (poiNameText == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'poi_progress_label' not found in UXML.");

        if (questionText == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'question_label' not found in UXML.");

        if (answerInput == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'answer_input' not found in UXML.");

        if (submitButton == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'submit_button' not found in UXML.");

        if (backButton == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'back_button' not found in UXML.");

        if (hintButton == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'hint_button' not found in UXML.");

        if (resultContainer == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'result_container' not found in UXML.");

        if (resultLabel == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'result_label' not found in UXML.");

        if (hintLabel == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'hint_label' not found in UXML.");

        if (progressLabel == null)
            Debug.LogWarning("[OpenAnswerChallengeView] 'progress_label' not found in UXML.");
    }

    // ============================================================
    // SETUP
    // ============================================================
    private void SetupButtons()
    {
        if (submitButton != null)
        {
            submitButton.clicked -= OnSubmitClicked;
            submitButton.clicked += OnSubmitClicked;
            if (LocalizationManager.Instance != null)
            {
                submitButton.text = LocalizationManager.Instance.GetText("submit_button");
            }
        }

        if (backButton != null)
        {
            backButton.clicked -= OnBackClicked;
            backButton.clicked += OnBackClicked;
        }

        if (hintButton != null)
        {
            hintButton.clicked -= OnHintClicked;
            hintButton.clicked += OnHintClicked;
            if (LocalizationManager.Instance != null)
            {
                hintButton.text = LocalizationManager.Instance.GetText("hint_button");
            }
        }

        if (answerInput != null)
        {
            answerInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnSubmitClicked();
                }
            });
        }
    }

    private void OnBackClicked()
    {
        Debug.Log("[OpenAnswerChallengeView] Back button clicked.");
        StopAllCoroutines();

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.GoBack();
        }
        else
        {
            Debug.LogError("[OpenAnswerChallengeView] NavigationManager is missing.");
        }
    }

    // ============================================================
    // LOADING
    // ============================================================
    private void LoadAndDisplay()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] DataManager is missing.");
            ShowFallback();
            return;
        }

        POIModel selectedPOI = DataManager.Instance.SelectedPOI;

        if (selectedPOI == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] SelectedPOI is null.");
            ShowFallback();
            return;
        }

        if (selectedPOI is not OpenAnswerPOIModel openAnswerPOI)
        {
            Debug.LogError(
                "[OpenAnswerChallengeView] SelectedPOI is not an OpenAnswerPOIModel. Actual type: " +
                selectedPOI.GetType().Name
            );
            ShowFallback();
            return;
        }

        currentPOI = openAnswerPOI;

        Debug.Log("[OpenAnswerChallengeView] Loading open answer challenge for POI: " + currentPOI.poiName);

        DisplayChallenge();
    }

    // ============================================================
    // DISPLAY
    // ============================================================
    private void DisplayChallenge()
    {
        if (currentPOI == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] currentPOI is null.");
            return;
        }

        answered = false;
        isWaitingForRetry = false;
        wrongAttempts = 0;
        //correctAnswerKey = NormalizeAnswerKey(currentPOI.correctAnswers[0]);

        if (poiNameText != null) poiNameText.text = currentPOI.poiName;

        SetQuestionText();

        if (answerInput != null)
        {
            answerInput.value = "";
            answerInput.SetEnabled(true);
            answerInput.Focus();
            SetInputBorderColor(new Color(0.85f, 0.85f, 0.85f), 7);
            answerInput.style.backgroundColor = new Color(0, 0, 0, 0);
        }

        if (submitButton != null)
            submitButton.SetEnabled(true);

        HideResult();

        UpdateProgressLabel();

        Debug.Log("[OpenAnswerChallengeView] Question: " + currentPOI.question);
        Debug.Log("[OpenAnswerChallengeView] Correct answer: " + correctAnswerKey);
    }

    private void SetQuestionText()
    {
        if (questionText == null || currentPOI == null)
            return;

        string initialDescription = string.IsNullOrWhiteSpace(currentPOI.initialDescription)
            ? ""
            : currentPOI.initialDescription + "\n\n";

        //questionText.text = initialDescription + currentPOI.question;
        questionText.text = currentPOI.question;
    }

    // ============================================================
    // SUBMIT ANSWER
    // ============================================================
    private void OnSubmitClicked()
    {
        if (answered || isWaitingForRetry)
            return;

        if (answerInput == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] Answer input is null.");
            return;
        }

        string userAnswer = answerInput.value.Trim();

        if (string.IsNullOrEmpty(userAnswer))
        {
            ShowResult("Please enter an answer.", false);
            return;
        }

        answered = true;

        if (submitButton != null)
            submitButton.SetEnabled(false);

        Debug.Log("[OpenAnswerChallengeView] Player submitted: " + userAnswer);

        bool isCorrect = IsAnswerCorrect(userAnswer);

        if (isCorrect)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleWrongAnswer(userAnswer);
        }
    }

    private bool IsAnswerCorrect(string userAnswer)
    {
        string normalizedUserAnswer = NormalizeAnswerKey(userAnswer);

        foreach (string correctAnswer in currentPOI.correctAnswers)
        {
            string normalizedCorrect = NormalizeAnswerKey(correctAnswer);
            if (string.Equals(normalizedUserAnswer, normalizedCorrect, System.StringComparison.Ordinal))
            {
                Debug.Log($"[OpenAnswerChallengeView] Match found: '{userAnswer}' == '{correctAnswer}'");
                return true;
            }
        }

        Debug.Log($"[OpenAnswerChallengeView] No match for: '{userAnswer}'");
        return false;
    }

    // ============================================================
    // ANSWER HANDLERS
    // ============================================================
    private void HandleCorrectAnswer()
    {
        Debug.Log("[OpenAnswerChallengeView] Correct answer!");

        if (answerInput != null)
        {
            SetInputBorderColor(correctColor, 3);
            answerInput.SetEnabled(false);
            answerInput.style.backgroundColor = new Color(0, 0, 0, 0);
        }

        UpdateProgressLabel();

        ShowResult("Correct! Well done!", true);
        CompleteChallenge(solvedByPlayer: true);
    }

    private void HandleWrongAnswer(string userAnswer)
    {
        wrongAttempts++;
        Debug.Log($"[OpenAnswerChallengeView] Wrong answer. Attempt {wrongAttempts}/{MAX_ATTEMPTS}");

        UpdateProgressLabel();

        if (answerInput != null)
        {
            SetInputBorderColor(wrongColor, 3);
            answerInput.style.backgroundColor = new Color(0, 0, 0, 0);
        }

        if (wrongAttempts >= MAX_ATTEMPTS)
        {
            string correctAnswersText = string.Join("\n", currentPOI.correctAnswers);
            ShowResult($"No attempts remaining.\nCorrect answers:\n{correctAnswersText}", false);

            if (answerInput != null)
                answerInput.SetEnabled(false);

            CompleteChallenge(solvedByPlayer: false);
            return;
        }

        string hint = string.IsNullOrWhiteSpace(currentPOI.hint)
            ? $"Wrong answer. Try again ({wrongAttempts}/{MAX_ATTEMPTS})"
            : $"Wrong answer. Hint: {currentPOI.hint} ({wrongAttempts}/{MAX_ATTEMPTS})";

        ShowResult(hint, false);

        isWaitingForRetry = true;
        StartCoroutine(AllowRetry());
    }

    private IEnumerator AllowRetry()
    {
        yield return new WaitForSeconds(3f);

        if (answerInput != null)
        {
            answerInput.value = "";
            answerInput.SetEnabled(true);
            SetInputBorderColor(new Color(0.85f, 0.85f, 0.85f), 7);
            answerInput.style.backgroundColor = new Color(0, 0, 0, 0);
            answerInput.Focus();
        }

        if (submitButton != null)
            submitButton.SetEnabled(true);

        UpdateProgressLabel();

        HideResult();

        answered = false;
        isWaitingForRetry = false;

        Debug.Log("[OpenAnswerChallengeView] Try again.");
    }

    // ============================================================
    // HINT
    // ============================================================
    private void OnHintClicked()
    {
        Debug.Log("[OpenAnswerChallengeView] Hint button clicked!");
        if (currentPOI == null) return;
        if (hintLabel == null)
        {
            Debug.LogWarning("[OpenAnswerChallengeView] hintLabel is null!");
            return;
        }

        if (answered) return;

        string hint = string.IsNullOrWhiteSpace(currentPOI.hint)
            ? "No hint available."
            : currentPOI.hint;

        hintLabel.text = "\n\n<b>Hint:</b> " + hint;
        hintLabel.style.display = DisplayStyle.Flex;
    }

    // ============================================================
    // COMPLETION
    // ============================================================
    private void CompleteChallenge(bool solvedByPlayer)
    {
        if (currentPOI == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] Cannot complete a null POI.");
            return;
        }

        if (DataManager.Instance != null)
            DataManager.Instance.MarkCurrentPOICompleted();

        if (currentPOI != null)
            currentPOI.isChallengeCompleted = true;

        Debug.Log($"[OpenAnswerChallengeView] Challenge completed. Solved: {solvedByPlayer}");

        if (backButton != null)
            backButton.SetEnabled(false);

        StartCoroutine(NavigateToBadgePage());
    }

    private IEnumerator NavigateToBadgePage()
    {
        yield return new WaitForSeconds(1.5f);

        if (NavigationManager.Instance == null)
        {
            Debug.LogError("[OpenAnswerChallengeView] NavigationManager is missing.");
            yield break;
        }

        NavigationManager.Instance.NavigateTo("BadgePageUIToolkit");
    }

    // ============================================================
    // UI UTILITIES
    // ============================================================
    private void ShowResult(string message, bool isCorrect)
    {
        if (resultLabel != null)
        {
            resultLabel.text = message;
            resultLabel.style.color = Color.white;
            resultLabel.style.fontSize = 18;
            resultLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        }
    }

    private void HideResult()
    {
        if (resultContainer != null)
            resultContainer.style.display = DisplayStyle.None;

        if (resultLabel != null)
            resultLabel.text = "";
    }

    private void ShowFallback()
    {
        if (questionText != null)
            questionText.text = "Challenge data not available.";

        if (submitButton != null)
            submitButton.SetEnabled(false);

        if (hintButton != null)
            hintButton.SetEnabled(false);

        if (resultContainer != null)
            resultContainer.style.display = DisplayStyle.None;
    }

    private string NormalizeAnswerKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";

        return key
            .Trim()
            .ToLowerInvariant();
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel == null)
            return;

        progressLabel.text = $"Attempt: {wrongAttempts}/{MAX_ATTEMPTS}";
        //progressLabel.style.color = new Color(1f, 0.6f, 0f);
        progressLabel.style.color = correctColor;

    }

    private void SetInputBorderColor(Color color, float width = 5)
    {
        if (answerInput == null) return;

        answerInput.style.borderTopColor = color;
        answerInput.style.borderBottomColor = color;
        answerInput.style.borderLeftColor = color;
        answerInput.style.borderRightColor = color;
        answerInput.style.borderTopWidth = width;
        answerInput.style.borderBottomWidth = width;
        answerInput.style.borderLeftWidth = width;
        answerInput.style.borderRightWidth = width;
    }
}