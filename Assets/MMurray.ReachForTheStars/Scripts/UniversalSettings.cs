using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

//this script is used to manage user settings such as TTS and music.
public class UniversalSettings : MonoBehaviour
{
    public bool ttsEnabled;          //false by default
    public bool musicEnabled;       //true by default
    public bool allLessonsViewed;   //if true, no lessons will be shown. Used for when player plays multiple times.
    public bool extraModeEnabled;   //hard mode
    public TextAsset dialogueFile;  //this file will be parsed and this script will handle accessing the text.
    JSONNode jsonString;            //will contain contents of dialogue JSON.
    public ScreenFade screenFade { get; private set; }

    public static UniversalSettings instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        screenFade = GetComponentInChildren<ScreenFade>();

        //set up JSON
        jsonString = JSON.Parse(dialogueFile.text);
        Debug.Log(jsonString.ToString());

        DontDestroyOnLoad(instance);

        //change to title screen
        screenFade.ChangeSceneFadeOut("Title");
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

    public string GetText(string key)
    {
        if (jsonString == null)
        {
            jsonString = JSON.Parse(dialogueFile.text);
        }

        string text = jsonString["dialogue"][key].Value;
        return text ?? "---Missing---";
    }
}
