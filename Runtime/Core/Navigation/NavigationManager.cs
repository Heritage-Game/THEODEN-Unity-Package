using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// This class is the Manager class for navigating between the scenes of the application. It keeps a stack to trace the
/// previous displayed scene, which makes the GoBack() behaviour possible.
/// This class has a global state and can be used as a singleton across the project.
/// </summary>
public class NavigationManager : MonoBehaviour
{
    private static NavigationManager instance;
    private Stack<string> history = new Stack<string>();

    public static NavigationManager Instance
    {
        get
        {
            //FALLBACK
            if (instance == null)
            {
                GameObject obj = new GameObject("NavigationManager");
                instance = obj.AddComponent<NavigationManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        Debug.Log($"[NavigationManager] Awake");
        // Singleton 
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        //Persistence
        DontDestroyOnLoad(gameObject);
    }
    
    

    // Navigate forward to another scene
    /// <summary>
    /// This method is used to navigate forward to another scene. It saves the current scene inside the hystory before
    /// moving to the following.
    /// </summary>
    /// <param name="sceneName">The name of the scene to navigate to</param>
    public void NavigateTo(string sceneName)
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (sceneName == "Menu")
        {
            history.Clear();
            Transitions.LoadScene(sceneName);
            return;
        }

        history.Push(currentScene);
        Transitions.LoadScene(sceneName);
    }

    // Go back
    /// <summary>
    /// This method is used to navigate back to the previous scene stored in the history stack. 
    /// </summary>
    public void GoBack()
    {
        if (history.Count > 0)
        {
            string previousScene = history.Pop();
            Transitions.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("No previous scene in history");
            Transitions.LoadScene("Menu"); // fallback
        }
    }

    // Clear history > To use when the navigation lands on the main menu?
    public void ClearHistory()
    {
        history.Clear();
    }
}
