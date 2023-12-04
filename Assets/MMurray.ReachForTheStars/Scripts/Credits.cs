using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Credits : MonoBehaviour
{
    public Button backButton;

    [Header("---Credits---")]
    public TextMeshProUGUI mainCreditText;
    public TextMeshProUGUI gameDesignText;
    public TextMeshProUGUI programmerText;
    public TextMeshProUGUI artText;
    public TextMeshProUGUI uiText;
    public TextMeshProUGUI musicText;
    public TextMeshProUGUI soundText;
    // Start is called before the first frame update
    void Start()
    {
        UpdateLanguage();
    }

    /*string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }*/

    void UpdateLanguage()
    {
        UniversalSettings us = UniversalSettings.instance;
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("backButtonText");
        mainCreditText.text = us.GetText("credit_main");
        gameDesignText.text = us.GetText("credit_gameDesign");
        programmerText.text = us.GetText("credit_program");
        artText.text = us.GetText("credit_art");
        uiText.text = us.GetText("credit_ui");
        musicText.text = us.GetText("credit_music");
        soundText.text = us.GetText("credit_sfx");
    }

    public void OnBackButtonClicked()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        SceneManager.LoadScene("Title");
    }
}
