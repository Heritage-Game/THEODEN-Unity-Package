using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public static void ChangeSceneTo(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}