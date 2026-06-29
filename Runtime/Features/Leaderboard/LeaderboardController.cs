using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text playerNameText;

    [Header("PlayerPrefs")]
    [SerializeField] private string nicknamePlayerPrefsKey = "NICKNAME";
    [SerializeField] private string fallbackNickname = "Player";

    [Header("Fallback Navigation")]
    [SerializeField] private string fallbackBackSceneName = "Menu";

    private void Awake()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        else
        {
            Debug.LogWarning("[LeaderboardController] BackButton reference is missing.");
        }
    }

    private void Start()
    {
        LoadNickname();
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    private void LoadNickname()
    {
        string nickname = PlayerPrefs.GetString(nicknamePlayerPrefsKey, fallbackNickname);

        if (string.IsNullOrWhiteSpace(nickname))
        {
            nickname = fallbackNickname;
        }

        if (playerNameText != null)
        {
            playerNameText.text = nickname;
        }
        else
        {
            Debug.LogWarning("[LeaderboardController] PlayerNameText reference is missing.");
        }
    }

    private void OnBackClicked()
    {
        // Prefer your NavigationManager if it exists in the project.
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.GoBack();
            return;
        }

        // Fallback if the leaderboard scene was opened directly during testing.
        if (!string.IsNullOrWhiteSpace(fallbackBackSceneName))
        {
            SceneManager.LoadScene(fallbackBackSceneName);
        }
        else
        {
            Debug.LogWarning("[LeaderboardController] No NavigationManager found and no fallback scene set.");
        }
    }
}
