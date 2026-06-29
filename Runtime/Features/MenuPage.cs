using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuPage : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text welcomeText;

    [Header("Buttons")]
    public Button btnDiscover;
    public Button btnShowMap;
    public Button btnCodex;
    public Button btnLeaderboards;

    void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        // Load and display nickname
        string nickname = PlayerPrefs.GetString("NICKNAME", "Explorer");
        if (welcomeText != null)
        {
            welcomeText.text = $"Hello, {nickname}!";
        }

        // Discover button
        if (btnDiscover != null)
        {
            btnDiscover.onClick.AddListener(() =>
            {
                NavigationManager.Instance.NavigateTo("QRScanner");
            });
        }

        // Show Map Button -> YENİ SAHNEYE YÖNLENDİRİYOR
        if (btnShowMap != null)
        {
            btnShowMap.onClick.AddListener(() =>
            {
                // Kendi sisteminizdeki NavigationManager'ı kullanarak yeni sahneye geçiş yapıyoruz.
                // "MapScene" kısmını kendi verdiğiniz sahne ismiyle değiştirin.
                NavigationManager.Instance.NavigateTo("MapScene");
            });
        }

        // Codex button
        if (btnCodex != null)
        {
            btnCodex.onClick.AddListener(() =>
            {
                NavigationManager.Instance.NavigateTo("Codex");
            });
        }

        // Leaderboards & Badges button
        if (btnLeaderboards != null)
        {
            btnLeaderboards.onClick.AddListener(() =>
            {
                NavigationManager.Instance.NavigateTo("Leaderboard");
            });
        }
    }
}