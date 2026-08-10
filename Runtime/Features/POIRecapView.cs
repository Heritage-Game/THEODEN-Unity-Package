using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class POIRecapView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Swipe")]
    [SerializeField] private float swipeThreshold = 50f;

    private Label titleLabel;
    private Image poiImage;
    private Label imageCounterLabel;

    private Label shortSummaryLabel;
    private Label fullNarrativeLabel;
    private Label questionLabel;
    private Label correctAnswerLabel;

    private VisualElement submittedAnswerContainer;
    private Label submittedAnswerLabel;

    private Label pointsLabel;
    private Label attemptsLabel;
    private Label completionTimeLabel;

    private VisualElement badgeContainer;
    private Image badgeImage;

    private Button backButton;
    private Button codexButton;

    private POIModel currentPoi;
    private PoiProgressData currentProgress;

    private readonly List<Sprite> poiImages =
        new List<Sprite>();

    private int currentImageIndex;
    private Vector2 swipeStartPosition;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError(
                "[POIRecapView] UIDocument not assigned."
            );

            return;
        }

        BindUIElements();
        SetupButtons();
        LoadData();
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.clicked -= OnCodexClicked;

        if (codexButton != null)
            codexButton.clicked -= OnCodexClicked;
    }

    private void Update()
    {
        HandleSwipe();
    }

    private void BindUIElements()
    {
        VisualElement root =
            uiDocument.rootVisualElement;

        titleLabel = root.Q<Label>("title_label");
        poiImage = root.Q<Image>("poi_image");
        imageCounterLabel =
            root.Q<Label>("image_counter_label");

        shortSummaryLabel =
            root.Q<Label>("short_summary_label");

        fullNarrativeLabel =
            root.Q<Label>("full_narrative_label");

        questionLabel =
            root.Q<Label>("question_label");

        correctAnswerLabel =
            root.Q<Label>("correct_answer_label");

        submittedAnswerContainer =
            root.Q<VisualElement>(
                "submitted_answer_container"
            );

        submittedAnswerLabel =
            root.Q<Label>("submitted_answer_label");

        pointsLabel =
            root.Q<Label>("points_label");

        attemptsLabel =
            root.Q<Label>("attempts_label");

        completionTimeLabel =
            root.Q<Label>("completion_time_label");

        badgeContainer =
            root.Q<VisualElement>("badge_container");

        badgeImage =
            root.Q<Image>("badge_image");

        backButton =
            root.Q<Button>("back_button");

        codexButton =
            root.Q<Button>("codex_button");
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.clicked -= OnCodexClicked;
            backButton.clicked += OnCodexClicked;
        }

        if (codexButton != null)
        {
            codexButton.clicked -= OnCodexClicked;
            codexButton.clicked += OnCodexClicked;
        }
    }

    private void LoadData()
    {
        if (DataManager.Instance == null)
        {
            ShowFallback("DataManager is missing.");
            return;
        }

        currentPoi = DataManager.Instance.SelectedPOI;

        if (currentPoi == null)
        {
            ShowFallback("POI data is not available.");
            return;
        }

        DisplayPoiContent();
        LoadImages();
        DisplayBadge();

        bool progressFound =
            PlayerProgressService.TryGetPoiProgress(
                currentPoi.poiId,
                out currentProgress
            );

        if (!progressFound)
        {
            Debug.LogError(
                "[POIRecapView] No completion data found for: " +
                currentPoi.poiId
            );

            DisplayMissingProgress();
            return;
        }

        DisplayProgress();
    }

    private void DisplayPoiContent()
    {
        if (titleLabel != null)
        {
            titleLabel.text =
                string.IsNullOrWhiteSpace(currentPoi.poiName)
                    ? "Unknown POI"
                    : currentPoi.poiName;
        }

        if (shortSummaryLabel != null)
        {
            shortSummaryLabel.text =
                string.IsNullOrWhiteSpace(currentPoi.shortSummary)
                    ? "No summary available."
                    : currentPoi.shortSummary;
        }

        if (fullNarrativeLabel != null)
        {
            fullNarrativeLabel.text =
                string.IsNullOrWhiteSpace(currentPoi.fullNarrative)
                    ? "No additional narrative available."
                    : currentPoi.fullNarrative;
        }

        if (questionLabel != null)
        {
            questionLabel.text =
                string.IsNullOrWhiteSpace(currentPoi.question)
                    ? "Question not available."
                    : currentPoi.question;
        }

        if (correctAnswerLabel != null)
            correctAnswerLabel.text =
                GetCorrectAnswerText(currentPoi);
    }

    private void DisplayProgress()
    {
        if (pointsLabel != null)
        {
            pointsLabel.text =
                currentProgress.awardedPoints > 0
                    ? "+" + currentProgress.awardedPoints
                    : "0";
        }

        if (attemptsLabel != null)
        {
            attemptsLabel.text =
                currentProgress.attemptsUsed +
                "/" +
                ChallengeSessionService.MaxAttempts;
        }

        if (completionTimeLabel != null)
        {
            completionTimeLabel.text =
                FormatTime(
                    currentProgress.completionTimeSeconds
                );
        }

        DisplaySubmittedAnswer();
    }

    private void DisplaySubmittedAnswer()
    {
        bool isDifferent =
            !IsSubmittedAnswerCorrect(
                currentPoi,
                currentProgress.submittedAnswer
            );

        bool shouldShow =
            !string.IsNullOrWhiteSpace(
                currentProgress.submittedAnswer
            ) &&
            isDifferent;

        if (submittedAnswerContainer != null)
        {
            submittedAnswerContainer.style.display =
                shouldShow
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        if (submittedAnswerLabel != null && shouldShow)
        {
            submittedAnswerLabel.text =
                ResolveAnswerText(
                    currentPoi,
                    currentProgress.submittedAnswer
                );
        }
    }

    private void DisplayBadge()
    {
        bool hasBadge = currentPoi.poiBadge != null;

        if (badgeContainer != null)
        {
            badgeContainer.style.display =
                hasBadge
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        if (badgeImage != null && hasBadge)
        {
            badgeImage.sprite = currentPoi.poiBadge;
            badgeImage.scaleMode = ScaleMode.ScaleToFit;
        }
    }

    private void DisplayMissingProgress()
    {
        if (pointsLabel != null)
            pointsLabel.text = "—";

        if (attemptsLabel != null)
            attemptsLabel.text = "—";

        if (completionTimeLabel != null)
            completionTimeLabel.text = "—";

        if (submittedAnswerContainer != null)
        {
            submittedAnswerContainer.style.display =
                DisplayStyle.None;
        }
    }

    private void LoadImages()
    {
        poiImages.Clear();

        if (currentPoi.images != null)
        {
            foreach (POIModel.ImageReference imageReference
                     in currentPoi.images)
            {
                if (imageReference?.sprite != null)
                    poiImages.Add(imageReference.sprite);
            }
        }

        currentImageIndex = 0;
        UpdateImage();
    }

    private void UpdateImage()
    {
        if (poiImage == null)
            return;

        if (poiImages.Count == 0)
        {
            poiImage.style.display = DisplayStyle.None;

            if (imageCounterLabel != null)
                imageCounterLabel.text = "";

            return;
        }

        poiImage.style.display = DisplayStyle.Flex;
        poiImage.sprite = poiImages[currentImageIndex];
        poiImage.scaleMode = ScaleMode.ScaleToFit;

        if (imageCounterLabel != null)
        {
            imageCounterLabel.text =
                (currentImageIndex + 1) +
                "/" +
                poiImages.Count;
        }
    }

    private void HandleSwipe()
    {
        if (poiImages.Count <= 1)
            return;

        if (Input.touchCount > 0)
            HandleTouchSwipe();
        else
            HandleMouseSwipe();
    }

    private void HandleMouseSwipe()
    {
        if (Input.GetMouseButtonDown(0))
            swipeStartPosition = Input.mousePosition;

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 delta =
                (Vector2)Input.mousePosition -
                swipeStartPosition;

            ProcessSwipe(delta);
        }
    }

    private void HandleTouchSwipe()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
            swipeStartPosition = touch.position;

        if (touch.phase == TouchPhase.Ended)
        {
            Vector2 delta =
                touch.position -
                swipeStartPosition;

            ProcessSwipe(delta);
        }
    }

    private void ProcessSwipe(Vector2 delta)
    {
        bool isHorizontalSwipe =
            Mathf.Abs(delta.x) >
            Mathf.Abs(delta.y);

        if (!isHorizontalSwipe ||
            Mathf.Abs(delta.x) < swipeThreshold)
        {
            return;
        }

        currentImageIndex += delta.x > 0 ? -1 : 1;

        if (currentImageIndex < 0)
            currentImageIndex = poiImages.Count - 1;

        if (currentImageIndex >= poiImages.Count)
            currentImageIndex = 0;

        UpdateImage();
    }

    private string GetCorrectAnswerText(POIModel poi)
    {
        if (poi is MultipleChoicePOIModel multipleChoice)
        {
            return ResolveMultipleChoiceAnswer(
                multipleChoice,
                multipleChoice.correctAnswer
            );
        }

        if (poi is OpenAnswerPOIModel openAnswer)
        {
            if (openAnswer.correctAnswers == null ||
                openAnswer.correctAnswers.Count == 0)
            {
                return "Answer not available.";
            }

            return string.Join(
                ", ",
                openAnswer.correctAnswers
            );
        }

        return "Answer not available.";
    }

    private string ResolveAnswerText(
        POIModel poi,
        string answer)
    {
        if (poi is MultipleChoicePOIModel multipleChoice)
        {
            return ResolveMultipleChoiceAnswer(
                multipleChoice,
                answer
            );
        }

        return string.IsNullOrWhiteSpace(answer)
            ? "Not available."
            : answer;
    }

    private string ResolveMultipleChoiceAnswer(
        MultipleChoicePOIModel poi,
        string answerKey)
    {
        string normalizedAnswer =
            NormalizeAnswer(answerKey);

        if (poi.answers != null)
        {
            foreach (
                MultipleChoicePOIModel.AnswerEntry answer
                in poi.answers)
            {
                if (answer == null)
                    continue;

                if (NormalizeAnswer(answer.key) !=
                    normalizedAnswer)
                {
                    continue;
                }

                return (
                    (answer.key ?? "") +
                    " " +
                    (answer.value ?? "")
                ).Trim();
            }
        }

        return string.IsNullOrWhiteSpace(answerKey)
            ? "Answer not available."
            : answerKey;
    }

    private bool IsSubmittedAnswerCorrect(
        POIModel poi,
        string submittedAnswer)
    {
        if (poi is MultipleChoicePOIModel multipleChoice)
        {
            return NormalizeAnswer(submittedAnswer) ==
                   NormalizeAnswer(
                       multipleChoice.correctAnswer
                   );
        }

        if (poi is OpenAnswerPOIModel openAnswer &&
            openAnswer.correctAnswers != null)
        {
            foreach (string correctAnswer
                     in openAnswer.correctAnswers)
            {
                if (string.Equals(
                        submittedAnswer?.Trim(),
                        correctAnswer?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string NormalizeAnswer(string answer)
    {
        return string.IsNullOrWhiteSpace(answer)
            ? ""
            : answer
                .Trim()
                .Replace(".", "")
                .ToUpperInvariant();
    }

    private string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(
            Mathf.Max(0f, seconds)
        );

        return time.TotalHours >= 1
            ? time.ToString(@"hh\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    private void OnCodexClicked()
    {
        if (NavigationManager.Instance == null)
        {
            Debug.LogError(
                "[POIRecapView] NavigationManager missing."
            );

            return;
        }

        NavigationManager.Instance.NavigateTo(
            "CodexUIToolkit"
        );
    }

    private void ShowFallback(string message)
    {
        Debug.LogError("[POIRecapView] " + message);

        if (titleLabel != null)
            titleLabel.text = "Recap unavailable";

        if (shortSummaryLabel != null)
            shortSummaryLabel.text = message;

        if (codexButton != null)
            codexButton.SetEnabled(true);
    }
}