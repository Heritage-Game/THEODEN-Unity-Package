using UnityEngine;

public class UrlOpener : MonoBehaviour
{
    public static void OpenURL(string url)
    {
        Application.OpenURL(url);
    }
}