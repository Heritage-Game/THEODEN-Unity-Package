using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Transitions : MonoBehaviour
{
    [SerializeField] Animation panel;
    [SerializeField] AnimationClip fadeIn;

    public static Transitions instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
        panel.gameObject.SetActive(false);
    }

    public static void LoadScene(string name)
    {
        if(instance == null)
        { SceneManager.LoadScene(name);
            Debug.LogError("Nessun Transitions nella scena");
        }
        else
            instance.StartCoroutine(MyLoadScene(name));
    }

    private static IEnumerator MyLoadScene(string name)
    {
        //Activates the panel and starts the transition animation
        AsyncOperation asOp = SceneManager.LoadSceneAsync(name);
        asOp.allowSceneActivation = false;

        instance.panel.gameObject.SetActive(true);
        instance.panel["FadeIn"].speed = 1;
        instance.panel.clip = instance.fadeIn;
        instance.panel.Play();

        yield return new WaitForSeconds(instance.fadeIn.length);

        //Starts the animation backeards and deactivates the transition panel after the animation
        //instance.panel.clip = instance.fadeOut;
        asOp.allowSceneActivation = true;
        instance.panel["FadeIn"].speed = -1;
        instance.panel.Play();
        print(instance.fadeIn.length);

        yield return new WaitForSeconds(instance.fadeIn.length);
        instance.panel.gameObject.SetActive(false);
    }

}
