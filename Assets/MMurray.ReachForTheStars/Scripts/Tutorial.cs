using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLSDK;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Tutorial : MonoBehaviour
{
    public Button backButton;
    [Header("---How to Win---")]
    public TextMeshProUGUI howToWinText;
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI altObjectiveText;

    [Header("---Spaces---")]
    public TextMeshProUGUI spacesText;
    public TextMeshProUGUI starCacheNameText;
    public TextMeshProUGUI homeNameText;
    public TextMeshProUGUI drawCardNameText;
    public TextMeshProUGUI starCacheDetailsText;
    public TextMeshProUGUI homeDetailsText;
    public TextMeshProUGUI drawCardDetailsText;

    [Header("---Encounter---")]
    public TextMeshProUGUI encounterText;
    public TextMeshProUGUI encounterDetailsText;

    [Header("---Ex Round---")]
    public TextMeshProUGUI exRoundText;
    public TextMeshProUGUI exRoundDetailsText;
    public TextMeshProUGUI exRoundRulesText;

    float ttsDuration = 7;      //how long it takes for TTS to speak each line.
    UniversalSettings us; 

    string[] keys = {"tutorial_objective", "tutorial_altObjective", "tutorial_starCache", "tutorial_home", "tutorial_drawCard", 
        "tutorial_encounterDetails", "tutorial_exRoundDetails", "tutorial_exRoundRules"};
    // Start is called before the first frame update
    void Start()
    {
        UpdateLanguage();
        us = UniversalSettings.instance;
        if (us.ttsEnabled)
            StartCoroutine(PlayTTS(keys, ttsDuration));
    }

    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }

    void UpdateLanguage()
    {
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("backButtonText");
        howToWinText.text = GetText("tutorial_howToWin");
        objectiveText.text = GetText("tutorial_objective");
        altObjectiveText.text = GetText("tutorial_altObjective");
        spacesText.text = GetText("tutorial_spaces");
        starCacheNameText.text = GetText("tutorial_starCacheName");
        homeNameText.text = GetText("tutorial_homeName");
        drawCardNameText.text = GetText("tutorial_drawCardName");
        starCacheDetailsText.text = GetText("tutorial_starCache");
        homeDetailsText.text = GetText("tutorial_home");
        drawCardDetailsText.text = GetText("tutorial_drawCard");
        encounterText.text = GetText("tutorial_encounter");
        encounterDetailsText.text = GetText("tutorial_encounterDetails");
        exRoundText.text = GetText("tutorial_exRound");
        exRoundDetailsText.text = GetText("tutorial_exRoundDetails");
        exRoundRulesText.text = GetText("tutorial_exRoundRules");
    }

    public void OnTTSButtonClicked()
    {
        //UniversalSettings us = UniversalSettings.instance;
        us.ttsEnabled = !us.ttsEnabled;
        if (us.ttsEnabled)
        {
            //play TTS
            StopAllCoroutines();
            StartCoroutine(PlayTTS(keys, ttsDuration));
        }
        else
        {
            StopAllCoroutines();
        }
    }

    public void OnBackButtonClicked()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        SceneManager.LoadScene("Title");
    }

    IEnumerator PlayTTS(string[] keys, float duration = 0)
    {
        int i = 0;
        while(i < keys.Length)
        {
            LOLSDK.Instance.SpeakText(keys[i]);
            yield return new WaitForSeconds(duration);
            i++;
        }
    }
}
