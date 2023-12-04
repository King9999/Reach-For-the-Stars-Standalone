using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;
using UnityEngine.SceneManagement;

/* This script is similar to game manager, except the information in here is fixed, including the board. there are infinite rounds,
and the player will start with a fixed set of 5 cards (the deck is still random). */
public class TutorialManager : MonoBehaviour
{
    
    public enum GameState
    {
        NewRound, NextPlayerTurn, GetLesson, BeginningNewTurn, CardPhase, ChoosingTarget, PlayingRefill, PlayingJump, ActivateCard, RollingDice, PlayerMoving, 
        PlayerJumping, PlayerDiscardingCards, CheckingSpace, StartEncounter, EndTurn, ExtraRound, EndGame
    }
    public GameState gameState;


    float boardSpaceXOffset {get;} = 2.36f;//1.182f;
    float boardSpaceYOffset {get;} = 2.143f;

    [Header("---Dice---")]
    public Dice dice;
    bool diceIsRolling;

    [Header("---Player---")]    //players are predetermined
    //public Player playerPrefab;
    public List<Player> playerList;
    public int playerIndex;                     //index of player who is taking their turn.
    public Player currentPlayer;               //easier way to access current player.     
    public Player target;                      //used for targeting a player with a card
    public Sprite[] pieceColors;                //each index corresponds to ID from PlayingPieceButton script

    [Header("---Board---")]     //board is predetermined
    public TextAsset boardFile;                 //JSON containing board data
    Boards boardList;
    public int boardID;                         //must keep a copy of this ID from setup manager for save state writing/reading
    public List<string[]> boardData;           //will contain board data from JSON
    public BoardSpace homeSpacePrefab;
    public BoardSpace starCachePrefab;
    public BoardSpace drawCardPrefab;
    public BoardSpace drawCardTwoPrefab;
    public GameObject spaceAura;                //used for highlighting spaces
    public List<BoardSpace> boardSpaceList;
    [HideInInspector]public BoardSpace homeSpace;            //Easy reference to home space since there's always 1.
    int rowCount;                               //used for arranging the board spaces
    int colCount;
    public Color highProbabilityColor {get; set;}         //For the star cache. probability is between 0.7 and 1
    public Color midProbabilityColor {get; set;}          //For the star cache. probability is between 0.4 and 0.6
    public Color lowProbabilityColor {get; set;}          //For the star cache. probability is under 0.4

    [Header("---Arrows---")]
    public GameObject arrowContainer;
    public Arrow arrowLeft, arrowRight, arrowUp, arrowDown;       //used to show which routes a player can take.

    [Header("---Encounter Game---")]
    public Player opponent;
    public bool encounterEnded;                 //used to change game state

    [Header("---Lessons---")]
    public bool lessonViewedThisRound;          //if true, player moves on to next round. Lessons are viewed every 2 rounds.
    public int lessonIndex;                     //starts at 0
    public bool tutorialStarted = true;         //used to display the intro
    public enum TutorialLesson {Intro = 9, End}
    public TutorialLesson tutorial; 

    [Header("---Star Particles---")]
    public ParticleSystem[] starParticlePatterns;   //particles are emitted in a random pattern every time a game starts.

    [Header("---Backgrounds---")]
    public GameObject[] gameBackgrounds;

    

    //singletons
    public static TutorialManager instance;
    UI ui;
    CardManager cm;
    NumberCardManager ncm;
    LessonManager lm;
    AudioManager am;
    UniversalSettings us;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        
    }


    // Start is called before the first frame update
    void Start()
    {
        /* This code is for testing only */
        /*System.Random rand = new System.Random();
        int seed = rand.Next();
        Random.InitState(seed);
        Debug.Log("Seed: " + seed);*/

        ui = UI.instance;
        cm = CardManager.instance;
        ncm = NumberCardManager.instance;
        lm = LessonManager.instance;
        am = AudioManager.instance;
        us = UniversalSettings.instance;

        //star particle setup
        foreach(ParticleSystem particle in starParticlePatterns)
        {
            particle.gameObject.SetActive(false);
        }
        int random = Random.Range(0, starParticlePatterns.Length);
        starParticlePatterns[random].gameObject.SetActive(true);
        starParticlePatterns[random].Play();

        //background setup
        foreach(GameObject bg in gameBackgrounds)
        {
            bg.gameObject.SetActive(false);
        }
        random = Random.Range(0, gameBackgrounds.Length);
        gameBackgrounds[random].gameObject.SetActive(true);

       
        /*get board data and insert into array. Using a list of arrays to store the information since Unity doesn't offer a way
        to parse data into a 2D array.
        The boardData list works as follows:
        First index contains the row from top left to top right of board.
        Last index contains the row from bottom left to bottom right of board. */

        boardList = JsonUtility.FromJson<Boards>(boardFile.text);
        boardData = new List<string[]>();
        char[] delimiters = {',', ' '};

        for (int i = 0; i < boardList.boards[boardID].rows.Length; i++)
        {
            string p = boardList.boards[boardID].rows[i].row;
            boardData.Add(p.Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries));
        }

        /* draw the board. Board data values:
            0 - empty space
            1 - star cache
            H - home
            D - draw card
            2 - draw card x2 */
        
        rowCount = boardList.boards[boardID].rows.Length;
        colCount = boardData[0].Length;                     //any index can be used, they should all have an equal number of columns
        Debug.Log("Row " + rowCount + ", Col " + colCount);

        //creating a parent container for the board space objects
        GameObject boardContainer = new GameObject();
        boardContainer.name = "Board Spaces"; 

        for (int row = 0; row < boardData.Count; row++)
        {
            int col = 0;
            foreach (string space in boardData[row])
            {
                bool spaceAdded = false;
                
                switch(space)
                {
                    case "1":
                        boardSpaceList.Add(Instantiate(starCachePrefab, boardContainer.transform));
                        spaceAdded = true;
                        break;

                    case "H":
                        boardSpaceList.Add(Instantiate(homeSpacePrefab, boardContainer.transform));
                        homeSpace = boardSpaceList[boardSpaceList.Count - 1];
                        spaceAdded = true;
                        break;

                    case "D":
                        boardSpaceList.Add(Instantiate(drawCardPrefab, boardContainer.transform));
                        spaceAdded = true;
                        break;

                    case "2":
                        boardSpaceList.Add(Instantiate(drawCardTwoPrefab, boardContainer.transform));
                        spaceAdded = true;
                        break;
                }

                if (spaceAdded)
                {
                    //position the new space
                    boardSpaceList[boardSpaceList.Count - 1].transform.position = new Vector3((col - colCount / 2) * boardSpaceXOffset, ((rowCount / 2) - row) * boardSpaceYOffset, 0);
                    boardSpaceList[boardSpaceList.Count - 1].row = row;
                    boardSpaceList[boardSpaceList.Count - 1].col = col;
                }
                col++;
            }

        }
              

        ToggleBoardSpaceAura(false);

        //encounter game setup
        ncm.ToggleNumberCardUIContainer(false);
      

        #region UI Setup
        //language update
        ui.UpdateLanguage();

        //Set up colours for Star Cache UI
        highProbabilityColor = new Color(0.2f, 1, 0.2f);        //light green
        midProbabilityColor = new Color(1, 0.6f, 0.2f);         //orange
        lowProbabilityColor = new Color(0.9f, 0.06f, 0.1f);     //dark red

        ui.ToggleMoveModValue(false);
        ui.ToggleAlertUI(false);
        ui.feedbackUIActive = new bool[2];
        ui.ToggleFeedBackUI(0, false);
        ui.ToggleFeedBackUI(1, false);


        //toggles for UI
        EnableArrowContainer(false);
        dice.ShowDice(false);
        #endregion

         //lessons setup
        lm.Initialize();
        ui.TogglePauseText(false);

        //set up deck
        cm.SetupCards();
        cm.ShuffleCards();

        //Set up players
        /*playerList = new List<Player>();
        for (int i = 0; i < sm.numberOfPlayers; i++)
        {
            playerList.Add(Instantiate(playerPrefab));
        }*/

        foreach (Player player in playerList)
        {
            homeSpace.playersOnSpace.Add(player);
            player.currentSpace = homeSpace;
            player.transform.position = homeSpace.transform.position;
            player.row = homeSpace.row;
            player.col = homeSpace.col;
            player.direction = Vector3.zero;
            player.moveMod = 0;
            player.moveTotal = 0;
            player.newGameStarted = true;
            player.ToggleMoveCountUI(false);

            //sprite setup
            int i = playerList.IndexOf(player);
            /*SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            //sr.sprite = sm.selectedPieceColor[i];
            sr.sprite = pieceColors[0];
            player.playingPiece = sm.selectedPieceColor[i];         //will be used for writing and reading save state
            //player.playingPiece = sm.selectedPieceColor[i];  */       //will be used for writing and reading save state

            //draw 3 cards
            player.DrawCard(3);
            //player.hand.Add(cm.cardTypes[6].card);
            //player.playerName = sm.playerNames[i];
            //player.canRollDice = true;

            ui.playerPanels[i].TogglePanel(true);
            ui.playerPanels[i].player = player;
            ui.playerPanels[i].UpdatePlayerName(player);
            ui.playerPanels[i].UpdatePlayerStatus(player);
            ui.playerPanels[i].ToggleCardEffectUI(false);
            ui.playerPanels[i].cardEffectUI.Initialize();
            ui.playerPanels[i].ToggleArrow(false);

            //TODO: When players are placed in the same space, their position must be shifted a bit so they all fit.
            if (i == 1) //player 2
            {
                player.isAI = true;
                homeSpace.SetAlternatePositionOne(player);
            }
        }

        ui.UpdateLeadingPlayer();

        //star cache setup
        ResetStarCaches(1);

        //music setup
        if (us.musicEnabled)
            am.musicMain.Play();

        //set game state. Display the tutorial intro
        SetGameState(GameState.NewRound);
        //SetGameState(GameState.GetLesson);
        
    }

    //activated by the Back to Title button
    public void ReturnToTitle()
    {
        //TODO: add a fade out
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        am.musicEncounter.Stop();
        am.musicMain.Stop();
        Time.timeScale = 1;         //in case player was looking at tutorial lesson and didn't close it
        SceneManager.LoadScene("Title");
    }
    

    public void ShowArrow(Arrow arrow, bool toggle)
    {
        arrow.gameObject.SetActive(toggle);

        if (toggle == true)
        {
            //place arrow on player position
            Player player = playerList[playerIndex];

            if (arrow == arrowRight)
                arrow.transform.position = new Vector3(player.transform.position.x + 1.5f, player.transform.position.y, 0);

            else if (arrow == arrowLeft)      
                arrow.transform.position = new Vector3(player.transform.position.x - 1.5f, player.transform.position.y, 0); 

            else if (arrow == arrowUp)
                arrow.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 1.5f, 0);
                
            else if (arrow == arrowDown)
                arrow.transform.position = new Vector3(player.transform.position.x, player.transform.position.y - 1.5f, 0);

            arrow.OriginalPos = arrow.transform.position;
            arrow.DestinationPos = new Vector3(arrow.OriginalPos.x + arrow.direction.x, arrow.OriginalPos.y + arrow.direction.y, 0);
        }   
    }

    public void EnableArrowContainer(bool toggle)
    {
        arrowContainer.gameObject.SetActive(toggle);
        if (toggle == false)
        {
            //hide all arrows
            ShowArrow(arrowLeft, false);
            ShowArrow(arrowRight, false);
            ShowArrow(arrowUp, false);
            ShowArrow(arrowDown, false);
        }
    }

    public void ToggleStarCacheUI(bool toggle)
    {
        foreach(BoardSpace space in boardSpaceList)
        {
            if (space.TryGetComponent(out StarCache starCache))
            {
                starCache.ToggleProbabilityUI(toggle);
            }
        }   
    }

    public void ResetStarCaches(float value)
    {
        foreach(BoardSpace space in boardSpaceList)
        {
            if (space.TryGetComponent(out StarCache starCache))
            {
                starCache.SetProbability(value, ui.probabilityType);

                if (value >= 0.7f)
                    starCache.SetUIColor(highProbabilityColor);
                else if (value >= 0.4f)
                    starCache.SetUIColor(midProbabilityColor);
                else
                    starCache.SetUIColor(lowProbabilityColor);
            }
        }  
    }

    //Checks current player's space and changes colour of star cache UI.
    public void SetStarCacheProbabilityColor(float value)
    {
        if (value < 0 || value > 1) return;

        if (currentPlayer.currentSpace.TryGetComponent(out StarCache starCache))
        {
            if (value >= 0.7f)
                starCache.SetUIColor(highProbabilityColor);
            else if (value >= 0.4f)
                starCache.SetUIColor(midProbabilityColor);
            else
                starCache.SetUIColor(lowProbabilityColor);
        }
    }

    public void ToggleBoardSpaceAura(bool toggle)
    {
        spaceAura.gameObject.SetActive(toggle);
    }

    //used to shift player positions if there's more than 1 player on a space.
    public void ArrangePlayerPositions(List<Player> players, Player currentPlayer)
    {
        if (players.Count == 2)
        {
            //we check the index of the current player so we know which other players' positions we have to compare.
            switch(playerIndex)
            {
                case 0:     //player 1
                    if (!currentPlayer.currentSpace.OriginalPositionOccupied(players[1]))
                    {
                        currentPlayer.transform.position = currentPlayer.currentSpace.transform.position;
                    }
                    else if (!currentPlayer.currentSpace.AltPositionOneOccupied(players[1]))
                    {
                        currentPlayer.currentSpace.SetAlternatePositionOne(currentPlayer);
                    }
                    else
                    {
                        currentPlayer.currentSpace.SetAlternatePositionTwo(currentPlayer);
                    }
                    break;

                case 1:     //player 2
                    if (!currentPlayer.currentSpace.OriginalPositionOccupied(players[0]))
                    {
                        currentPlayer.transform.position = currentPlayer.currentSpace.transform.position;
                    }
                    else if (!currentPlayer.currentSpace.AltPositionOneOccupied(players[0]))
                    {
                        currentPlayer.currentSpace.SetAlternatePositionOne(currentPlayer);
                    }
                    else
                    {
                        currentPlayer.currentSpace.SetAlternatePositionTwo(currentPlayer);
                    }
                    break;
            }
        }
        else    //3 players
        {
            switch(playerIndex)
            {
                case 0:     //player 1
                    if (!currentPlayer.currentSpace.OriginalPositionOccupied(players[1]) && !currentPlayer.currentSpace.OriginalPositionOccupied(players[2]))
                    {
                        currentPlayer.transform.position = currentPlayer.currentSpace.transform.position;
                    }
                    else if (!currentPlayer.currentSpace.AltPositionOneOccupied(players[1]) && !currentPlayer.currentSpace.AltPositionOneOccupied(players[2]))
                    {
                        currentPlayer.currentSpace.SetAlternatePositionOne(currentPlayer);
                    }
                    else
                    {
                        currentPlayer.currentSpace.SetAlternatePositionTwo(currentPlayer);
                    }
                    break;

                case 1:     //player 2
                    if (!currentPlayer.currentSpace.OriginalPositionOccupied(players[0]) && !currentPlayer.currentSpace.OriginalPositionOccupied(players[2]))
                    {
                        currentPlayer.transform.position = currentPlayer.currentSpace.transform.position;
                    }
                    else if (!currentPlayer.currentSpace.AltPositionOneOccupied(players[0]) && !currentPlayer.currentSpace.AltPositionOneOccupied(players[2]))
                    {
                        currentPlayer.currentSpace.SetAlternatePositionOne(currentPlayer);
                    }
                    else
                    {
                        currentPlayer.currentSpace.SetAlternatePositionTwo(currentPlayer);
                    }
                    break;

                case 2:     //player 3
                    if (!currentPlayer.currentSpace.OriginalPositionOccupied(players[1]) && !currentPlayer.currentSpace.OriginalPositionOccupied(players[0]))
                    {
                        currentPlayer.transform.position = currentPlayer.currentSpace.transform.position;
                    }
                    else if (!currentPlayer.currentSpace.AltPositionOneOccupied(players[1]) && !currentPlayer.currentSpace.AltPositionOneOccupied(players[0]))
                    {
                        currentPlayer.currentSpace.SetAlternatePositionOne(currentPlayer);
                    }
                    else
                    {
                        currentPlayer.currentSpace.SetAlternatePositionTwo(currentPlayer);
                    }
                    break;
            }
        }
    }

    //Most actions should result in changing the game state, so this method will be called often.
    public void SetGameState(GameState state)
    {
        switch(state)
        {
            case GameState.NewRound:
                //once all players have taken their turn, update the round
                //if round > max rounds, stop game
                ToggleStarCacheUI(false);
                gameState = GameState.NewRound;
                
                playerIndex = 0;
                currentPlayer = playerList[playerIndex];

                ui.ToggleCardUIContainer(false);
                ui.ToggleAlertUI(false);
                cm.DisplayHand(currentPlayer, false);

                //first thing to do is display the tutorial intro
                if (!lm.lessonList[(int)TutorialLesson.Intro].lessonViewed)
                    goto case GameState.GetLesson;

                //can player take their turn?
                if (currentPlayer.loseTurn)
                {
                    //currentPlayer.loseTurn = false;
                    ui.DisplayFeedbackMessage("lostTurn", currentPlayer.transform.position);
                }
                else
                {
                    //AI players do not have to choose between rolling dice or playing a card. They will always check
                    //for cards to use before rolling dice.
                    if (!currentPlayer.isAI)
                        goto case GameState.BeginningNewTurn;
                    else
                        goto case GameState.CardPhase;
                }
                
                break;

            case GameState.NextPlayerTurn:
                //clean up current player states before moving on
                ui = UI.instance;
                currentPlayer = playerList[playerIndex];
                ui.playerPanels[playerIndex].ToggleCardEffectUI(false);
                ToggleStarCacheUI(false);
               
                //we don't set gotStarFromHome to false here to prevent the player getting a second star immediately when their turn begins.
                currentPlayer.gotStarFromCache = false;
                currentPlayer.drewCardFromSpace = false;

                //clear movement, card states and roll dice state
                currentPlayer.canRollDice = true;
                currentPlayer.moveMod = 0;
                currentPlayer.cardEffect = null;
                currentPlayer.selectedCard = null;
                currentPlayer.cardPlayed = false;
                //currentPlayer.starTotal = 0;

                //if we were discarding cards, remove card UI
                if (gameState == GameState.PlayerDiscardingCards)
                {
                    ui.ToggleAlertUI(false);
                }


                if (playerIndex + 1 >= playerList.Count)
                {
                    goto case GameState.NewRound;
                }
                else
                {
                
                    gameState = GameState.NextPlayerTurn;

                    playerIndex++;
                    currentPlayer = playerList[playerIndex];

                    if (currentPlayer.loseTurn)
                    {
                        //currentPlayer.loseTurn = false;
                        ui.DisplayFeedbackMessage("lostTurn", currentPlayer.transform.position);
                    }
                    else
                    {
                        //AI players do not have to choose between rolling dice or playing a card. They will always check
                        //for cards to use before rolling dice.
                        if (!currentPlayer.isAI)
                            goto case GameState.BeginningNewTurn;
                        else
                            goto case GameState.CardPhase;
                    }
                        
                }
                break;

            case GameState.GetLesson:
                //lessons are displayed at the end of the human player's turn
                gameState = GameState.GetLesson;
                ui.ToggleAlertUI(false);
                //display intro
                if (!lm.lessonList[(int)TutorialLesson.Intro].lessonViewed)
                {
                    //tutorialStarted = false;
                    lm.ToggleLesson((int)TutorialLesson.Intro, true);
                }
                else
                {
                    //get another tutorial lesson if it's the end of the human player's turn
                } 
                break;
                

            case GameState.BeginningNewTurn:
                //player's turn begins on this state. They can choose between rolling dice or playing a card.
                //clear any alerts           
                ui.ToggleAlertUI(false);

                //cleanup
                if (gameState == GameState.RollingDice)
                {
                    dice.ShowDice(false);
                    dice.ToggleCross(false);
                }

                if (gameState == GameState.CardPhase)
                {
                    ui.ToggleCardUIContainer(false);
                    cm.DisplayHand(currentPlayer, false);
                }
                
                if (currentPlayer.hand.Count > 0)
                {
                    ui.ToggleNewTurnUIContainer(true);
                    gameState = GameState.BeginningNewTurn;
                }
                else
                {
                    //no cards, just roll dice
                    goto case GameState.RollingDice;
                }
                break;

            case GameState.CardPhase:
                //show current player's cards. Player can either play a card or skip the phase
                //do some cleanup depending on what the previous state was
                if (gameState == GameState.BeginningNewTurn)
                {
                    ui.ToggleNewTurnUIContainer(false);
                }

                if (gameState == GameState.PlayingRefill)
                {
                    ToggleBoardSpaceAura(false);
                    EnableArrowContainer(false);
                }

                if (gameState == GameState.PlayingJump)
                {
                    ToggleBoardSpaceAura(false);
                }

                if (gameState == GameState.ChoosingTarget)
                {
                    //hide targeting arrows beside player panels
                    for(int i = 0; i < playerList.Count; i++)
                    {
                        ui.playerPanels[i].ToggleArrow(false);
                    }
                }

                gameState = GameState.CardPhase;
                if (currentPlayer.hand.Count <= 0)
                {
                    goto case GameState.RollingDice;
                }
                else
                {
                    ToggleStarCacheUI(false);
                    ui.ToggleCardUIContainer(true);
                    ui.ToggleAlertUI(false);
                    cm.DisplayHand(currentPlayer, true);                   
                    
                    if (currentPlayer.isAI)
                    {
                        //change player state
                        currentPlayer.SetAIState(Player.AIState.ChoosingCard);
                    }
                }
                break;

            case GameState.ChoosingTarget:
                //show UI to allow player to select a target if the player is human. If the player is AI, they
                //choose a target without showing UI.
                gameState = GameState.ChoosingTarget;
                ui.ToggleBackButton(true);
                ui.TogglePlayCardButton(false);
                ui.ToggleSkipCardButton(false);
                cm.background.gameObject.SetActive(false);
                cm.DisplayHand(currentPlayer, false);
                ui.DisplayAlert("choosePanel");

                //show targeting arrows beside player panels
                for(int i = 0; i < playerList.Count; i++)
                {
                    ui.playerPanels[i].ToggleArrow(true);
                }
                break;

            case GameState.PlayingRefill:
                //lets the player see the probabilities on star caches, the space that will be affected, 
                //and also shows the direction they're currently facing
                gameState = GameState.PlayingRefill;
                ui.ToggleBackButton(true);
                ui.TogglePlayCardButton(true);
                ui.ToggleSkipCardButton(false);
                cm.DisplayHand(currentPlayer, false);
                cm.background.gameObject.SetActive(false);
                ui.DisplayAlert("usingRefillCard");
                
                //show probabilities, and the selected cache if able
                ToggleStarCacheUI(true);

                if (currentPlayer.direction != Vector3.zero)
                {
                    EnableArrowContainer(true);

                    //check player direction
                    if (currentPlayer.direction.x > 0)  //facing right
                        ShowArrow(arrowRight, true);
                    else if (currentPlayer.direction.x < 0) //facing left
                        ShowArrow(arrowLeft, true);
                    else if (currentPlayer.direction.y > 0) //facing up
                        ShowArrow(arrowUp, true);
                    else if (currentPlayer.direction.y < 0) //facing down
                        ShowArrow(arrowDown, true);

                    //show cache
                    if (currentPlayer.targetCache != null)
                    {
                        ToggleBoardSpaceAura(true);
                        ui.ChangePlayCardButtonColor(Color.white);
                        spaceAura.transform.position = currentPlayer.targetCache.transform.position;
                    }
                    else
                    {
                        //don't let player play card
                        ui.ChangePlayCardButtonColor(Color.grey);
                    }
                   
                }
                else
                {
                    //player can't use card
                    ui.ChangePlayCardButtonColor(Color.grey);
                }
                break;

            case GameState.PlayingJump:
                //show where the player is going to jump
                gameState = GameState.PlayingJump;
                ui.ToggleBackButton(true);
                ui.TogglePlayCardButton(true);
                ui.ToggleSkipCardButton(false);
                cm.DisplayHand(currentPlayer, false);
                cm.background.gameObject.SetActive(false);
                ToggleStarCacheUI(true);            //player might be interested in seeing the probabilities.
                ui.DisplayAlert("usingJumpCard");

                //show destination
                if (currentPlayer.jumpDestination != null)
                {
                    ToggleBoardSpaceAura(true);
                    ui.ChangePlayCardButtonColor(Color.white);
                    spaceAura.transform.position = currentPlayer.jumpDestination.transform.position;
                }

                break;

            case GameState.ActivateCard:
                //do some cleanup if applicable
                if (gameState == GameState.PlayingRefill)
                {
                    ToggleBoardSpaceAura(false);
                    EnableArrowContainer(false);
                }

                if (gameState == GameState.PlayingJump)
                {
                    ToggleBoardSpaceAura(false);
                }

                if (gameState == GameState.ChoosingTarget)
                {
                    //hide targeting arrows beside player panels
                    for(int i = 0; i < playerList.Count; i++)
                    {
                        ui.playerPanels[i].ToggleArrow(false);
                    }
                }

                gameState = GameState.ActivateCard;
                ui.ToggleAlertUI(false);
                ui.ToggleCardUIContainer(false);
                cm.DisplayHand(currentPlayer, false);

                //card is used on target, and relevant UI is displayed.
                switch(currentPlayer.selectedCard.targetType)
                {
                    case CardData.Target.Self:
                    case CardData.Target.OnePlayer:
                        currentPlayer.selectedCard.Activate(target);
                        int targetIndex = playerList.IndexOf(target);
                        ui.playerPanels[targetIndex].ToggleCardEffectUI(true);

                        //get card effect information. We make a special case for Swift Movement because its name is long
                        if (currentPlayer.selectedCard.cardID == 0) //swift movement ID
                        {
                            ui.playerPanels[targetIndex].cardEffectUI.SetCardDetails("SWIFT MOVE", currentPlayer.selectedCard.ability);
                        }
                        else
                        {
                            ui.playerPanels[targetIndex].cardEffectUI.SetCardDetails(currentPlayer.selectedCard.cardName.ToUpper(), 
                                currentPlayer.selectedCard.ability);
                        }
                        //ui.DisplayFeedbackMessage(target.cardEffect.cardName, target.transform.position);
                        ui.DisplayFeedbackMessage(target.cardEffect.nameKey, target.transform.position);

                        break;

                    case CardData.Target.Board:
                        //ui.DisplayFeedbackMessage(currentPlayer.selectedCard.cardName, currentPlayer.transform.position);
                        ui.DisplayFeedbackMessage(currentPlayer.selectedCard.nameKey, currentPlayer.targetCache.transform.position);
                        currentPlayer.selectedCard.Activate(currentPlayer, currentPlayer.targetCache);
                        break;
                }

                currentPlayer.DiscardActivatedCard(currentPlayer.selectedCard);
                currentPlayer.cardPlayed = true;
                break;

            case GameState.RollingDice:
                if (gameState == GameState.BeginningNewTurn)
                {
                    ui.ToggleNewTurnUIContainer(false);
                }

                gameState = GameState.RollingDice;
                ui.ToggleCardUIContainer(false);
                ui.ToggleAlertUI(false);
                cm.DisplayHand(currentPlayer, false);
                ToggleStarCacheUI(false);                   //in case refill card was used.

                //show dice rolling, and button to roll dice
                //if player can't roll dice, show feedback and then skip to move phase
                dice.diceIsRolling = true;
                if (!currentPlayer.isAI)
                {
                    if (currentPlayer.canRollDice)
                        dice.ShowDice(dice.diceIsRolling, false, false, currentPlayer.cardPlayed);
                    else
                        dice.ShowDice(dice.diceIsRolling, forcedMove: true, false, currentPlayer.cardPlayed);
                }
                else
                {
                    if (currentPlayer.canRollDice)
                        dice.ShowDice(dice.diceIsRolling, forcedMove: false, playerIsAI: true);
                    else
                        dice.ShowDice(dice.diceIsRolling, forcedMove: true, playerIsAI: true);
                }

                break;

            case GameState.PlayerMoving:
                //player moves on board.
                gameState = GameState.PlayerMoving;
                dice.ShowDice(false);
                dice.ToggleCross(false);
                currentPlayer.ToggleMoveCountUI(true);

                //clear any alerts           
                ui.ToggleAlertUI(false);

                ToggleStarCacheUI(true);

                currentPlayer.StartMoving(currentPlayer.moveTotal);
                break;

            case GameState.PlayerJumping:
                //player used jump card, we don't use regular move code.
                gameState = GameState.PlayerJumping;
                ToggleStarCacheUI(true);
                ui.ToggleAlertUI(false);
                currentPlayer.Jump(currentPlayer.jumpDestination);
                break;

            case GameState.PlayerDiscardingCards:
                gameState = GameState.PlayerDiscardingCards;
                
                ToggleStarCacheUI(false);
                ui.ToggleCardUIContainer(true);
                ui.DisplayAlert("tooManyCards", currentPlayer.hand.Count - cm.maxHand);
                cm.DisplayHand(currentPlayer, true);
                if (currentPlayer.isAI)
                {
                    currentPlayer.SetAIState(Player.AIState.DiscardingCards);
                }
                break;

            case GameState.CheckingSpace:
                //activate the space the current player is on
                if (gameState == GameState.StartEncounter)
                {
                    ToggleStarCacheUI(true);
                    if (us.musicEnabled)
                    {
                        am.musicEncounter.Stop();
                        am.musicMain.Play();
                    }
                }
                
                currentPlayer.ToggleMoveCountUI(false);
                //if the active player lost their turn they cannot check space.
                if (currentPlayer.loseTurn)
                    goto case GameState.NextPlayerTurn;
                else
                {
                    gameState = GameState.CheckingSpace;
                    //currentPlayer.ToggleMoveCountUI(false);
                    currentPlayer.currentSpace.ActivateSpace(currentPlayer);

                    //if space is a star cache, then update colour of probability
                    if (currentPlayer.currentSpace.TryGetComponent(out StarCache starCache))
                    {
                        SetStarCacheProbabilityColor(starCache.probability);
                    }
                }
                break;

            case GameState.StartEncounter:
                //encounter game begins, player must guess high or low
                //determine who the opponents are. If both players are AI, encounter game is not played and a player wins randomly.
                gameState = GameState.StartEncounter;
                encounterEnded = false;
                ToggleStarCacheUI(false);
                currentPlayer.ToggleMoveCountUI(false);

                //change music
                if (us.musicEnabled)
                {
                    am.musicMain.Stop();
                    am.musicEncounter.Play();
                }
                //check if one of the players is human
                if (!currentPlayer.isAI)
                {
                    for (int j = 0; j < currentPlayer.currentSpace.playersOnSpace.Count; j++)
                    {
                        if (currentPlayer.currentSpace.playersOnSpace[j] == currentPlayer) continue;
                        opponent = currentPlayer.currentSpace.playersOnSpace[j];
                    }
                    
                    ncm.StartHighLowGame(currentPlayer, opponent);
                }
                else
                {
                    //find out who the other player is on the space.
                    for (int j = 0; j < currentPlayer.currentSpace.playersOnSpace.Count; j++)
                    {
                        if (currentPlayer.currentSpace.playersOnSpace[j] == currentPlayer) continue;
                        opponent = currentPlayer.currentSpace.playersOnSpace[j];
                    }
                    
                    if (!opponent.isAI)
                    {
                        ncm.StartHighLowGame(currentPlayer, opponent);
                    }
                    
                }
                break;
                         
        }

    }
}
