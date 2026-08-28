using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class LeaderboardController : MonoBehaviour
{
    private const string RuntimeConfigResourcesPath =
        "THEODEN/TheodenRuntimeConfig";

    private const string NicknamePlayerPrefsKey =
        "NICKNAME";

    private const string FallbackNickname =
        "Player";

    [Header("UI")]
    [SerializeField]
    private UIDocument uiDocument;

    [Header("Fallback Navigation")]
    [SerializeField]
    private string fallbackBackSceneName = "MenuUIToolkit";

    private Button backButton;
    private Button syncButton;

    private Label playerLabel;
    private Label scoreLabel;
    private Label totalTimeLabel;
    private Label levelsCompletedLabel;
    private Label completionPercentageLabel;
    private Label leaderboardStatusLabel;
    private Label emptyLeaderboardLabel;

    private VisualElement progressBarFill;
    private VisualElement leaderboardHeader;
    private VisualElement leaderboardEntries;

    private TheodenRuntimeConfig runtimeConfig;
    private LeaderboardApiClient apiClient;

    private string currentPlayerId;
    private string currentNickname;
    private int currentTotalTimeSeconds;

    private bool isSyncing;

    private void OnEnable()
    {
        if (!TryBindUi())
            return;

        RegisterCallbacks();

        runtimeConfig =
            Resources.Load<TheodenRuntimeConfig>(
                RuntimeConfigResourcesPath
            );

        UpdateLocalSummary();

        if (runtimeConfig == null)
        {
            DisableLeaderboard(
                "The THEODEN runtime configuration could " +
                "not be loaded."
            );

            return;
        }

        if (!runtimeConfig.HasLeaderboardConfiguration)
        {
            DisableLeaderboard(
                "The leaderboard is not enabled for this project."
            );

            return;
        }

        currentPlayerId =
            PlayerIdentityService.GetOrCreatePlayerId();

        apiClient =
            new LeaderboardApiClient(
                runtimeConfig.LeaderboardBaseUrl
            );

        StartCoroutine(SyncLeaderboard());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (backButton != null)
            backButton.clicked -= OnBackClicked;

        if (syncButton != null)
            syncButton.clicked -= OnSyncLeaderboardClicked;

        isSyncing = false;
    }

    // ============================================================
    // UI INITIALIZATION
    // ============================================================

    private bool TryBindUi()
    {
        if (uiDocument == null)
        {
            Debug.LogError(
                "[LeaderboardController] UIDocument is missing."
            );

            return false;
        }

        VisualElement root =
            uiDocument.rootVisualElement;

        backButton =
            root.Q<Button>("back_button");

        syncButton =
            root.Q<Button>("sync_leaderboard_button");

        playerLabel =
            root.Q<Label>("player_label");

        scoreLabel =
            root.Q<Label>("score_label");

        totalTimeLabel =
            root.Q<Label>("total_time_label");

        levelsCompletedLabel =
            root.Q<Label>("levels_completed_label");

        completionPercentageLabel =
            root.Q<Label>("completion_percentage_label");

        leaderboardStatusLabel =
            root.Q<Label>("leaderboard_status_label");

        emptyLeaderboardLabel =
            root.Q<Label>("empty_leaderboard_label");

        progressBarFill =
            root.Q<VisualElement>("progress_bar_fill");

        leaderboardHeader =
            root.Q<VisualElement>("leaderboard_header");

        leaderboardEntries =
            root.Q<VisualElement>("leaderboard_entries");

        bool requiredUiIsValid =
            syncButton != null &&
            playerLabel != null &&
            scoreLabel != null &&
            totalTimeLabel != null &&
            levelsCompletedLabel != null &&
            completionPercentageLabel != null &&
            leaderboardStatusLabel != null &&
            emptyLeaderboardLabel != null &&
            progressBarFill != null &&
            leaderboardHeader != null &&
            leaderboardEntries != null;

        if (!requiredUiIsValid)
        {
            Debug.LogError(
                "[LeaderboardController] One or more required " +
                "UI Toolkit elements could not be found."
            );

            return false;
        }

        if (backButton == null)
        {
            Debug.LogWarning(
                "[LeaderboardController] back_button could " +
                "not be found in the header template."
            );
        }

        return true;
    }

    private void RegisterCallbacks()
    {
        if (backButton != null)
            backButton.clicked += OnBackClicked;

        syncButton.clicked += OnSyncLeaderboardClicked;
    }

    // ============================================================
    // LOCAL PROGRESS
    // ============================================================

    private void UpdateLocalSummary()
    {
        currentNickname =
            PlayerPrefs.GetString(
                NicknamePlayerPrefsKey,
                FallbackNickname
            ).Trim();

        if (string.IsNullOrWhiteSpace(currentNickname))
            currentNickname = FallbackNickname;

        int score =
            PlayerProgressService.TotalPoints;

        int completedPoiCount =
            PlayerProgressService.GetCompletedPoiCount();

        currentTotalTimeSeconds =
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    PlayerProgressService
                        .GetTotalCompletionTimeSeconds()
                )
            );

        int totalPoiCount =
            runtimeConfig != null
                ? Mathf.Max(0, runtimeConfig.TotalPoiCount)
                : 0;

        float completionRatio =
            totalPoiCount > 0
                ? Mathf.Clamp01(
                    (float)completedPoiCount /
                    totalPoiCount
                )
                : 0f;

        int completionPercentage =
            Mathf.RoundToInt(
                completionRatio * 100f
            );

        playerLabel.text =
            currentNickname;

        scoreLabel.text =
            $"{score} p";

        totalTimeLabel.text =
            FormatTime(currentTotalTimeSeconds);

        levelsCompletedLabel.text =
            $"{completedPoiCount}/{totalPoiCount}";

        completionPercentageLabel.text =
            $"{completionPercentage}% completed";

        progressBarFill.style.width =
            new Length(
                completionPercentage,
                LengthUnit.Percent
            );
    }

    // ============================================================
    // SYNCHRONIZATION
    // ============================================================

    private void OnSyncLeaderboardClicked()
    {
        if (isSyncing || apiClient == null)
            return;

        StartCoroutine(SyncLeaderboard());
    }

    private IEnumerator SyncLeaderboard()
    {
        if (isSyncing)
            yield break;

        isSyncing = true;

        UpdateLocalSummary();
        SetSyncButtonState(true);

        ShowStatus("Updating leaderboard...");

        emptyLeaderboardLabel.style.display =
            DisplayStyle.None;

        LeaderboardSubmissionDTO submission =
            new LeaderboardSubmissionDTO
            {
                ProjectId =
                    runtimeConfig.ProjectId,

                PlayerId =
                    currentPlayerId,

                Nickname =
                    currentNickname,

                Score =
                    PlayerProgressService.TotalPoints,

                TotalTime =
                    currentTotalTimeSeconds
            };

        string requestError = null;

        yield return apiClient.SubmitResult(
            submission,
            _ =>
            {
                // The submitted result was accepted.
            },
            error =>
            {
                requestError = error;
            }
        );

        if (!string.IsNullOrWhiteSpace(requestError))
        {
            FinishWithError(requestError);
            yield break;
        }

        List<LeaderboardEntryDTO> entries = null;
        requestError = null;

        yield return apiClient.GetLeaderboard(
            runtimeConfig.ProjectId,
            results =>
            {
                entries = results;
            },
            error =>
            {
                requestError = error;
            }
        );

        if (!string.IsNullOrWhiteSpace(requestError))
        {
            FinishWithError(requestError);
            yield break;
        }

        RenderLeaderboard(
            entries ??
            new List<LeaderboardEntryDTO>()
        );

        ShowStatus("Leaderboard updated.");

        SetSyncButtonState(false);
        isSyncing = false;
    }

    private void FinishWithError(string error)
    {
        Debug.LogError(
            "[LeaderboardController] " + error
        );

        ShowStatus(
            "Could not update the leaderboard. " +
            "Check the server connection."
        );

        SetSyncButtonState(false);
        isSyncing = false;
    }

    private void SetSyncButtonState(bool syncing)
    {
        syncButton.SetEnabled(!syncing);

        syncButton.text =
            syncing
                ? "Updating..."
                : "Update leaderboard";
    }

    // ============================================================
    // LEADERBOARD RENDERING
    // ============================================================

    private void RenderLeaderboard(
        IReadOnlyList<LeaderboardEntryDTO> entries)
    {
        leaderboardEntries.Clear();

        bool hasEntries =
            entries != null &&
            entries.Count > 0;

        leaderboardHeader.style.display =
            hasEntries
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        emptyLeaderboardLabel.style.display =
            hasEntries
                ? DisplayStyle.None
                : DisplayStyle.Flex;

        if (!hasEntries)
            return;

        for (int index = 0;
             index < entries.Count;
             index++)
        {
            LeaderboardEntryDTO entry =
                entries[index];

            if (entry == null)
                continue;

            VisualElement row =
                CreateLeaderboardRow(
                    index + 1,
                    entry
                );

            leaderboardEntries.Add(row);
        }
    }

    private VisualElement CreateLeaderboardRow(
        int rank,
        LeaderboardEntryDTO entry)
    {
        VisualElement row =
            new VisualElement();

        row.AddToClassList("leaderboard-row");

        row.style.flexDirection =
            FlexDirection.Row;

        row.style.alignItems =
            Align.Center;

        if (string.Equals(
                entry.PlayerId,
                currentPlayerId,
                StringComparison.Ordinal))
        {
            row.AddToClassList(
                "leaderboard-row--current"
            );
        }

        row.Add(
            CreateTableCell(
                rank.ToString(),
                "rank-column"
            )
        );

        row.Add(
            CreateTableCell(
                string.IsNullOrWhiteSpace(entry.Nickname)
                    ? FallbackNickname
                    : entry.Nickname,
                "player-column"
            )
        );

        row.Add(
            CreateTableCell(
                $"{entry.Score} p",
                "score-column"
            )
        );

        row.Add(
            CreateTableCell(
                FormatTime(entry.TotalTime),
                "time-column"
            )
        );

        return row;
    }

    private static Label CreateTableCell(
        string text,
        string columnClass)
    {
        Label label =
            new Label(text);

        label.AddToClassList("table-value");
        label.AddToClassList(columnClass);

        return label;
    }

    // ============================================================
    // STATE AND NAVIGATION
    // ============================================================

    private void DisableLeaderboard(string message)
    {
        apiClient = null;

        syncButton.SetEnabled(false);

        leaderboardEntries.Clear();

        leaderboardHeader.style.display =
            DisplayStyle.None;

        emptyLeaderboardLabel.style.display =
            DisplayStyle.None;

        ShowStatus(message);
    }

    private void ShowStatus(string message)
    {
        leaderboardStatusLabel.text =
            message ?? string.Empty;

        leaderboardStatusLabel.style.display =
            string.IsNullOrWhiteSpace(message)
                ? DisplayStyle.None
                : DisplayStyle.Flex;
    }

    private void OnBackClicked()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.GoBack();
            return;
        }

        if (!string.IsNullOrWhiteSpace(
                fallbackBackSceneName))
        {
            SceneManager.LoadScene(
                fallbackBackSceneName
            );

            return;
        }

        Debug.LogWarning(
            "[LeaderboardController] No NavigationManager " +
            "and no fallback scene were configured."
        );
    }

    private static string FormatTime(int totalSeconds)
    {
        TimeSpan time =
            TimeSpan.FromSeconds(
                Mathf.Max(0, totalSeconds)
            );

        if (time.TotalHours >= 1d)
        {
            return
                $"{(int)time.TotalHours:00}:" +
                $"{time.Minutes:00}:" +
                $"{time.Seconds:00}";
        }

        return
            $"{time.Minutes:00}:" +
            $"{time.Seconds:00}";
    }
}