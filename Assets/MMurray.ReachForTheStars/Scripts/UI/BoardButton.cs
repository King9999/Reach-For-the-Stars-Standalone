using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using LoLSDK;

//used to select a board during game setup
public class BoardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //public Button boardButton;
    public byte boardID;
    public TextMeshProUGUI boardDetails;
    public string key;

    public void OnBoardClicked()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        SetupManager sm = SetupManager.instance;
        sm.boardID = boardID;
        ScreenFade sf = ScreenFade.instance;
        sf.ChangeSceneFadeOut("Game");
        //SceneManager.LoadScene("Game");
    }

    public void OnPointerEnter(PointerEventData pointer)
    {
        boardDetails.color = Color.yellow;
        UniversalSettings us = UniversalSettings.instance;
        if (us.ttsEnabled)
            LOLSDK.Instance.SpeakText(key);
    }

    public void OnPointerExit(PointerEventData pointer)
    {
        boardDetails.color = Color.white;
    }
}
