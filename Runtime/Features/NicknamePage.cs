using UnityEngine;
using UnityEngine.UIElements;

public class NicknamePage : MonoBehaviour
{
    // ============================================================
    // UI REFERENCES
    // ============================================================
    [Header("UI References")]
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private TextField nicknameInput;
    private Button btnContinue;
    private Label errorText;
    private Label titleLabel;

    // ============================================================
    // SCENE NAMES
    // ============================================================
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MenuUIToolkit";

    // ============================================================
    // CONSTANTS
    // ============================================================
    private const string NICKNAME_KEY = "NICKNAME";

    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================
    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[NicknamePage] UIDocument not assigned.");
            return;
        }

        root = uiDocument.rootVisualElement;
        BindUIElements();
        SetupUI();
        LoadSavedNickname();
    }

    private void OnDisable()
    {
        if (btnContinue != null) btnContinue.clicked -= OnContinue;
    }

    // ============================================================
    // UI BINDING
    // ============================================================
    private void BindUIElements()
    {
        nicknameInput = root.Q<TextField>("nickname_form");
        btnContinue = root.Q<Button>("play_button");
        errorText = root.Q<Label>("error_label");
        titleLabel = root.Q<Label>("title_label");

        if (nicknameInput == null)
            Debug.LogWarning("[NicknamePage] 'nickname_input' not found in UXML.");

        if (btnContinue == null)
            Debug.LogWarning("[NicknamePage] 'continue_button' not found in UXML.");

        if (errorText == null)
            Debug.LogWarning("[NicknamePage] 'error_text' not found in UXML.");
    }

    // ============================================================
    // UI SETUP
    // ============================================================
    private void SetupUI()
    {
        if (btnContinue != null)
        {
            btnContinue.clicked -= OnContinue;
            btnContinue.clicked += OnContinue;
        }

        if (nicknameInput != null)
        {
            nicknameInput.maxLength = 20;
            nicknameInput.value = "";
            nicknameInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnContinue();
                }
            });
        }

        if (errorText != null)
        {
            errorText.text = "";
            errorText.style.display = DisplayStyle.None;
        }
    }

    private void LoadSavedNickname()
    {
        string saved = PlayerPrefs.GetString(NICKNAME_KEY, "");

        if (!string.IsNullOrEmpty(saved) && nicknameInput != null)
        {
            nicknameInput.value = saved;
        }
    }

    // ============================================================
    // PLAY BUTTON
    // ============================================================
    private void OnContinue()
    {
        if (nicknameInput == null)
        {
            Debug.LogError("[NicknamePage] Nickname input is missing.");
            return;
        }

        string nickname = nicknameInput.value.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            ShowError("Please enter a nickname!");
            return;
        }

        if (nickname.Length < 3)
        {
            ShowError("Nickname must be at least 3 characters!");
            return;
        }

        if (nickname.Length > 20)
        {
            ShowError("Nickname must be less than 20 characters!");
            return;
        }

        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();

        Debug.Log("[NicknamePage] Saved: " + nickname);

        HideError();

        // go to menu
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.NavigateTo(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[NicknamePage] NavigationManager missing.");
        }
    }

    // ============================================================
    // ERROR HANDLING
    // ============================================================
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.style.color = Color.red;
            errorText.style.display = DisplayStyle.Flex;

            if (nicknameInput != null)
            {
                nicknameInput.style.borderTopColor = Color.red;
                nicknameInput.style.borderBottomColor = Color.red;
                nicknameInput.style.borderLeftColor = Color.red;
                nicknameInput.style.borderRightColor = Color.red;
                nicknameInput.style.borderTopWidth = 1;
                nicknameInput.style.borderBottomWidth = 1;
                nicknameInput.style.borderLeftWidth = 1;
                nicknameInput.style.borderRightWidth = 1;
            }
        }
    }

    private void HideError()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.style.display = DisplayStyle.None;
        }

        if (nicknameInput != null)
        {
            nicknameInput.style.borderTopColor = Color.gray;
            nicknameInput.style.borderBottomColor = Color.gray;
            nicknameInput.style.borderLeftColor = Color.gray;
            nicknameInput.style.borderRightColor = Color.gray;
            nicknameInput.style.borderTopWidth = 1;
            nicknameInput.style.borderBottomWidth = 1;
            nicknameInput.style.borderLeftWidth = 1;
            nicknameInput.style.borderRightWidth = 1;
        }
    }

    // ============================================================
    // PUBLIC METHODS
    // ============================================================
    /// <summary>
    /// Forza il salvataggio del nickname (per debug)
    /// </summary>
    public void ForceSaveNickname(string nickname)
    {
        if (!string.IsNullOrEmpty(nickname))
        {
            PlayerPrefs.SetString(NICKNAME_KEY, nickname);
            PlayerPrefs.Save();

            if (nicknameInput != null)
                nicknameInput.value = nickname;
        }
    }
}