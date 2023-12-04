using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//used to hide the screen when it's loading.
public class ScreenFade : MonoBehaviour
{
    public Image fadeImage;
    public bool coroutineOn;        //used to prevent mouse input
    public static ScreenFade instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(instance);
    }

    // Start is called before the first frame update
    void Start()
    {
        //fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
    }

   
    //fade to black
    public void ChangeSceneFadeOut(string sceneName)
    {
        StartCoroutine(FadeScreenToScene(sceneName));
    }

    //fade back to normal
    public void FadeIn()
    {
        StartCoroutine(FadeFromBlack());
    }

    //changes screen to black then changes scene
    IEnumerator FadeScreenToScene(string sceneName)
    {
        coroutineOn = true;
        float alpha = 0;
        float fadeSpeed = 2;

        while(fadeImage.color.a < 1)
        {
            alpha += fadeSpeed * Time.deltaTime;
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, alpha);
            yield return null;
        }

        coroutineOn = false;
        SceneManager.LoadScene(sceneName);
        yield return FadeFromBlack();
    }

    //fades from black back to normal
    IEnumerator FadeFromBlack()
    {
        coroutineOn = true;
        float alpha = 1;
        float fadeSpeed = 2;

        while(fadeImage.color.a > 0)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, alpha);
            yield return null;
        }

        coroutineOn = false;
    }
}
