using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MMurray.ReachForTheStars;
using Unity.VectorGraphics;

//This script handles the UI for player information, including player name, star and card count.
//The object that the script will be attached to can be clicked on for targeting players with a card effect.
public class PlayerPanel : MonoBehaviour
{
    public Player player;                   //the reference to a player. Will be used for targeting with cards
    public TextMeshProUGUI playerNameUI;
    public TextMeshProUGUI starTotalUI;
    public TextMeshProUGUI cardTotalUI;
    public CardEffectUI cardEffectUI;       //used when player is affected by a card
    public Image panelImage;                //changes colour when it's player's turn
    public SVGImage playerIcon;
    public Image crown;                  //applied to the player who's in the lead
    Color panelColor;
    public Arrow panelArrow;         //used to show player where to click when using a Move card.


    public void OnPlayerPanelClicked()
    {
        Debug.Log("clicked on " + player.playerName + "'s panel");
        //activate a card effect
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        if (tm == null)
        {
            if (gm.currentPlayer.selectedCard == null || gm.gameState != GameManager.GameState.ChoosingTarget) return;

            AudioManager am = AudioManager.instance;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            gm.target = player;
            gm.SetGameState(GameManager.GameState.ActivateCard);
        }
        else
        {
            if (tm.currentPlayer.selectedCard == null || tm.gameState != TutorialManager.GameState.ChoosingTarget) return;

            AudioManager am = AudioManager.instance;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            tm.target = player;
            tm.SetGameState(TutorialManager.GameState.ActivateCard);
        }
    }

    //Update star and card total.
    public void UpdatePlayerStatus(Player player)
    {
        this.player = player;
        starTotalUI.text = player.starTotal.ToString();
        starTotalUI.color = Color.white;

        //update colour if player has the required stars
        if (player.starTotal >= 10)
            starTotalUI.color = new Color(0, 0.8f, 1);      //teal
            

        //apply crown
        /*ToggleCrown(false);
        GameManager gm = GameManager.instance;
        Player leadingPlayer = player.GetLeadingPlayer(gm.playerList);
        if (leadingPlayer != null && leadingPlayer == player)
            ToggleCrown(true);*/
       

        cardTotalUI.text = player.hand.Count.ToString();

        //add player icon
        if (playerIcon.sprite == null)
        {
            GameManager gm = GameManager.instance;
            playerIcon.sprite = gm.pieceColors[player.playingPiece];
            //playerIcon.sprite = player.playingPiece;
        }
    }

    public void ToggleCrown(bool toggle)
    {
        crown.gameObject.SetActive(toggle);
    }

    public void ToggleArrow(bool toggle)
    {
        panelArrow.gameObject.SetActive(toggle);
    }

    public void UpdatePlayerName(Player player)
    {
        this.player = player;
        playerNameUI.text = player.playerName;
    }

    public void TogglePanel(bool toggle)
    {
        gameObject.SetActive(toggle);   
    }

    public void ToggleCardEffectUI(bool toggle)
    {
        cardEffectUI.gameObject.SetActive(toggle);
        if (toggle == true)
        {
            cardEffectUI.ResetCoroutine();
        }
    }

    void Update()
    {
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        if (tm == null)
        {
            if (gm.gameState != GameManager.GameState.ChoosingTarget)   //if I don't do this, panel colour won't turn red when choosing target
                UpdatePanelColor();
        }
        else
        {
            if (tm.gameState != TutorialManager.GameState.ChoosingTarget)   //if I don't do this, panel colour won't turn red when choosing target
                UpdatePanelColor();
        }
    }

    //update panel colour when player's turn comes up
    public void UpdatePanelColor()
    {
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        Player currentPlayer = (tm == null) ? gm.currentPlayer : tm.currentPlayer;
        //if (tm == null)
        //{
            if (player == currentPlayer/*gm.currentPlayer*/)
            {
                panelImage.color = new Color(0, 0.3f, 0, panelImage.color.a);    //green
            }
            else
            {
                panelImage.color = new Color(0, 0, 0, panelImage.color.a);
            }
        //}
        /*else
        {
            if (player == tm.currentPlayer)
            {
                panelImage.color = new Color(0, 0.3f, 0, panelImage.color.a);    //green
            }
            else
            {
                panelImage.color = new Color(0, 0, 0, panelImage.color.a);
            }   
        } */    
    }
}
