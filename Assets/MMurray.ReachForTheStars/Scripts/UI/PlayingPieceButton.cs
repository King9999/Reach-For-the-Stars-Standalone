using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using LoLSDK;

//used when player is selecting a playing piece.
public class PlayingPieceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Button playingPieceButton;
    public int pieceID;                //used to idenfity which piece was selected. Corresponds to array index.
    public TextMeshProUGUI pieceColorName;
    public string key;

    //called by button
    public void OnPlayingPieceSelected()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        SetupManager sm = SetupManager.instance;
        //sm.selectedPieceColor.Add(playingPieceButton.image.sprite);
        sm.selectedPieceColor.Add(pieceID);

        //AI gets random colours. They will never have the same colour as other players
        for (int i = 1; i < sm.numberOfPlayers; i++)
        {
            int randPiece;
            Image pieceImage;
            do
            {
                randPiece = Random.Range(0, sm.playingPieceColors.Length);
                //pieceImage = sm.playingPieceColors[randPiece].GetComponent<Image>();
            }
            //while (sm.selectedPieceColor.Contains(pieceImage.sprite));
            while (sm.selectedPieceColor.Contains(randPiece));
           
            //sm.selectedPieceColor.Add(pieceImage.sprite);
            sm.selectedPieceColor.Add(randPiece);
        }
        sm.ChangeSetupState(SetupManager.SetupState.SelectBoard);
    }

    public void OnPointerEnter(PointerEventData pointer)
    {
        //text is highlighted
        pieceColorName.color = Color.yellow;
        UniversalSettings us = UniversalSettings.instance;
        if (us.ttsEnabled)
            LOLSDK.Instance.SpeakText(key);
    }

    public void OnPointerExit(PointerEventData pointer)
    {
        pieceColorName.color = Color.white;
    }
}
