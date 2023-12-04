using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//Player draws a card from this space.
public class DrawCard : BoardSpace
{
    public int drawAmount;      //number of cards to draw. Default is 1.
    
    void Start()
    {
        //drawAmount = 1;
    }

   public override void ActivateSpace(Player player)
    {
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        AudioManager am = AudioManager.instance;
        UI ui = UI.instance;

        if (tm == null)
        {
            if (player.drewCardFromSpace || gm.extraRound)
            {
                gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                return;
            }
        }
        else
        {
            if (player.drewCardFromSpace)
            {
                tm.SetGameState(TutorialManager.GameState.NextPlayerTurn);
                return;
            }
        }
        
        CardManager cm = CardManager.instance;
        if (cm.cards.Count <= 0)
        {
            //ui.DisplayFeedbackMessage("NO MORE CARDS", player.transform.position);
            
            am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
            ui.DisplayFeedbackMessage("noCardsInDeck", transform.position);
            return;
        }

        player.DrawCard(drawAmount);
        player.drewCardFromSpace = true;

        //display tutorial when player lands on cache for first time in the game
        if (tm == null)
        {
            LessonManager lm = LessonManager.instance;
            if (!lm.miniLessonList[1].lessonViewed && gm.playerIndex == 0)
            {
                gm.miniLessonIndex = 1;
                gm.SetGameState(GameManager.GameState.GetMiniLesson);
            }
        }
        
        if (tm == null)
            ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
        else
            ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(tm.currentPlayer);
            
        am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
        ui.DisplayFeedbackMessage("drewCard", transform.position);
    }
}
