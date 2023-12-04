using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script is used to manage user settings such as TTS and music.
public class UniversalSettings : MonoBehaviour
{
    public bool ttsEnabled;          //false by default
    public bool musicEnabled;       //true by default
    public bool allLessonsViewed;   //if true, no lessons will be shown. Used for when player plays multiple times.
    public bool extraModeEnabled;   //hard mode

    public static UniversalSettings instance;

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

    void Start()
    {
        musicEnabled = true;
    }
    
    public void OnTTSButtonClicked()
    {
        ttsEnabled = !ttsEnabled;
        Debug.Log("TTS Button clicked, setting is " + ttsEnabled);
    }

    public void OnMusicButtonClicked()
    {
        musicEnabled = !musicEnabled;
        Debug.Log("Music Button clicked, setting is " + musicEnabled);
    }
}
