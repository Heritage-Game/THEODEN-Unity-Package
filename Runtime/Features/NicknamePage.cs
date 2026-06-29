using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NicknamePage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button btnContinue;
    [SerializeField] private TMP_Text errorText;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    private const string NICKNAME_KEY = "NICKNAME";

    private void Start()
    {
        if (btnContinue != null)
        {
            btnContinue.onClick.RemoveAllListeners();
            btnContinue.onClick.AddListener(OnContinue);
        }

        string saved = PlayerPrefs.GetString(NICKNAME_KEY, "");

        if (!string.IsNullOrEmpty(saved) && nicknameInput != null)
            nicknameInput.text = saved;

        if (errorText != null)
            errorText.text = "";
    }

    private void OnContinue()
    {
        if (nicknameInput == null)
        {
            Debug.LogError("[Nickname] Nickname input is missing.");
            return;
        }

        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            if (errorText != null)
            {
                errorText.text = "Please enter a nickname!";
                errorText.color = Color.red;
            }

            return;
        }

        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();

        Debug.Log("[Nickname] Saved: " + nickname);

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.NavigateTo(mainMenuSceneName);
        else
            Debug.LogError("[Nickname] NavigationManager missing.");
    }

    private void OnDestroy()
    {
        if (btnContinue != null)
            btnContinue.onClick.RemoveAllListeners();
    }
}