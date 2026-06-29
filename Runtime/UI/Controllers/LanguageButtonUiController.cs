using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageButtonUiController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text label;
    //[SerializeField] private Image flagImage;
    [SerializeField] private Button button;

    private string _languageCode;

    /// <summary>
    /// This method is used to assign the correct label to the TMP_Text of the language button prefab.
    /// It receives a <see cref="LanguageEntry"/> as parameter.
    /// </summary>
    /// <param name="entry">Language entry in the LanguageCofig asset</param>
    /// <param name="onClick">Method to handle the selection of User when a language button is clicked</param>
    public void Setup(LanguageEntry data, System.Action<LanguageEntry> onClick)
    {
        label.text = data.displayedName;

        //remove listeners to avoid duplicates IMPORTANT!!!!!!!
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(data));
    }
}
