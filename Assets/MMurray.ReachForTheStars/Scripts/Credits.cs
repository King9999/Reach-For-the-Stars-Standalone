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

    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }

    void UpdateLanguage()
    {
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("backButtonText");
        mainCreditText.text = GetText("credit_main");
        gameDesignText.text = GetText("credit_gameDesign");
        programmerText.text = GetText("credit_program");
        artText.text = GetText("credit_art");
        uiText.text = GetText("credit_ui");
        musicText.text = GetText("credit_music");
        soundText.text = GetText("credit_sfx");
    }

    public void OnBackButtonClicked()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        SceneManager.LoadScene("Title");
    }
}
