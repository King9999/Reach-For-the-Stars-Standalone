using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//All players begin on this space. When a player passes or lands on this space, they are granted a star.
public class Home : BoardSpace
{
    //public List<bool> playerGrantedStar;
    GameManager gm;
    TutorialManager tm;
    public GameObject cross;        //used by Homeless mod only.
    public bool canGetStars;        //used by Homeless mod only.

    // Start is called before the first frame update
    void Start()
    {
        gm = GameManager.instance;
        tm = TutorialManager.instance;
        //ToggleCross(false);
        //canGetStars = true;
    }

    public void ToggleCross(bool toggle)
    {
        cross.gameObject.SetActive(toggle);
    }

    // Update is called once per frame
    void Update()
    {   
        //check if player passes through Home or remains on the space
        if (tm == null)
        {
            if (gm.gameState == GameManager.GameState.PlayerMoving || gm.gameState == GameManager.GameState.CheckingSpace) 
            {
                //Player currentPlayer = gm.playerList[gm.currentPlayer];
                if (!gm.currentPlayer.newGameStarted && gm.currentPlayer.currentSpace == this && !gm.currentPlayer.gotStarFromHome)
                {
                    ActivateSpace(gm.currentPlayer);
                }
            }
        }
        else    //we're in tutorial
        {
            if (tm.gameState == TutorialManager.GameState.PlayerMoving || tm.gameState == TutorialManager.GameState.CheckingSpace) 
            {
                if (!tm.currentPlayer.newGameStarted && tm.currentPlayer.currentSpace == this && !tm.currentPlayer.gotStarFromHome)
                {
                    ActivateSpace(tm.currentPlayer);
                }
            }
        }
        
    }

    public override void ActivateSpace(Player player)
    {
        if (player.gotStarFromHome) return;
        
        UI ui = UI.instance;
        AudioManager am = AudioManager.instance;
        ExtraModManager em = ExtraModManager.instance;
        if (em.activeMod != null && em.activeMod.boardID == 5 && !canGetStars)
        {
            player.gotStarFromHome = true;
            am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
            ui.DisplayFeedbackMessage("noStar", player.transform.position);
            return;
        }
        
        am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
        //GameManager gm = GameManager.instance;
        player.starTotal++;
        player.gotStarFromHome = true;

        //display tutorial when player lands on cache for first time in the game
        if (tm == null)
        {
            LessonManager lm = LessonManager.instance;
            if (!UniversalSettings.instance.extraModeEnabled && !lm.miniLessonList[2].lessonViewed && gm.playerIndex == 0)
            {
                gm.miniLessonIndex = 2;
                gm.SetGameState(GameManager.GameState.GetMiniLesson);
            }
        }

       
        if (tm == null)
            ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
        else
            ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(tm.currentPlayer);

        ui.UpdateLeadingPlayer();
        ui.DisplayFeedbackMessage("starGet", player.transform.position);

        //need to check if game is over
        if (tm == null)
        {
            if (!gm.extraRound)
            {
                if (player.starTotal >= 10 && gm.currentRound >= gm.maxRounds)
                {
                    player.StopAllCoroutines(); //stop player movement
                    gm.SetGameState(GameManager.GameState.EndGame);
                }
            }
            else
            {
                //immediately end game
                /*foreach(Player test in gm.playerList)
                {
                    test.starTotal = 0;
                }*/
                player.StopAllCoroutines();
                gm.SetGameState(GameManager.GameState.EndGame);
            }
        }
        
        
    }
}
