using UnityEngine;
using UnityEngine.UIElements;

public class LeaderboardController : MonoBehaviour
{
    [SerializeField]
    private UIDocument uiDocument;

    private Button backButton;
    private Button syncButton;

    private Label playerLabel;
    private Label scoreLabel;
    private Label totalTimeLabel;
    private Label levelsCompletedLabel;
    private Label completionPercentageLabel;

    private VisualElement progressBarFill;
    private VisualElement leaderboardEntries;

    private void OnEnable()
    {
        VisualElement root =
            uiDocument.rootVisualElement;

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

        progressBarFill =
            root.Q<VisualElement>("progress_bar_fill");

        leaderboardEntries =
            root.Q<VisualElement>("leaderboard_entries");

        syncButton =
            root.Q<Button>("sync_leaderboard_button");

        //syncButton.clicked += OnSyncLeaderboardClicked;
    }
}