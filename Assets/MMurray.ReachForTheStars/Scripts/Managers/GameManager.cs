using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLSDK;
using MMurray.ReachForTheStars;
using UnityEngine.SceneManagement;
using TMPro;


public class GameManager : MonoBehaviour
{   
    
    public GameData gameData;
    SaveState saveState;
    public bool resumedSaveState;       //if true, continuing game from saved state

    public enum GameState
    {
        NewRound, NextPlayerTurn, GetLesson, GetMiniLesson, BeginningNewTurn, CardPhase, ChoosingTarget, PlayingRefill, PlayingJump, ActivateCard, RollingDice, PlayerMoving, 
        PlayerJumping, PlayerDiscardingCards, CheckingSpace, StartEncounter, StartAssessment, EndTurn, ExtraRound, EndGame
    }
    public GameState gameState;


    float screenBoundaryX {get;} = 8.88f;
    float screenBoundaryY {get;} = 5;
    float boardSpaceXOffset {get;} = 2.36f;//1.182f;
    float boardSpaceYOffset {get;} = 2.143f;

    [Header("---Dice---")]
    public int[] diceRollRecord;        //the number of times each value is rolled. Index 0 is 2, index 1 is 3, etc.
    int totalDiceValues {get;} = 11;
    public int totalRolls;
    public Dice dice;
    bool diceIsRolling;

    [Header("---Round Data---")]
    public int currentRound;
    public int maxRounds;               //default is 10 but can be extended with a card
    public bool extraRound;             //if true, certain game states become unavailable.

    [Header("---Player---")]
    public Player playerPrefab;
    public List<Player> playerList;
    public List<Player> tiedPlayers;            //used to check if an extra round is played
    public int playerIndex;                     //index of player who is taking their turn.
    public Player currentPlayer;               //easier way to access current player.     
    public Player target;                      //used for targeting a player with a card
    public Sprite[] pieceColors;                //each index corresponds to ID from PlayingPieceButton script

    [Header("---Board---")]
    public TextAsset boardFile;                 //JSON containing board data
    Boards boardList;
    public int boardID;                         //must keep a copy of this ID from setup manager for save state writing/reading
    public List<string[]> boardData;           //will contain board data from JSON
    public BoardSpace homeSpacePrefab;
    public BoardSpace starCachePrefab;
    public BoardSpace drawCardPrefab;
    public BoardSpace drawCardTwoPrefab;
    public GameObject spaceAura;                //used for highlighting spaces
    SpriteRenderer auraSr;
    public List<BoardSpace> boardSpaceList;
    public BoardSpace homeSpace;            //Easy reference to home space since there's always 1.
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
    public byte miniLessonIndex;

    [Header("---Star Particles---")]
    public ParticleSystem[] starParticlePatterns;   //particles are emitted in a random pattern every time a game starts.

    [Header("---Backgrounds---")]
    public GameObject[] gameBackgrounds;

    

    //singletons
    public static GameManager instance;
    UI ui;
    CardManager cm;
    TitleManager tm;
    SetupManager sm;
    NumberCardManager ncm;
    LessonManager lm;
    AudioManager am;
    UniversalSettings us;
    AssessmentGame ag;
    ExtraModManager em;
    bool animateAuraCoroutineOn;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Update()
    {
        if (spaceAura.gameObject.activeSelf && !animateAuraCoroutineOn)
            StartCoroutine(AnimateSpaceAura());
    }

    //OnDestroy is used to save the game state
    /*void OnDestroy()
    {
        //must ensure active gameplay is being saved
        //saveState.WriteState(gameData);
    }*/

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
        tm = TitleManager.instance;
        sm = SetupManager.instance;
        ncm = NumberCardManager.instance;
        lm = LessonManager.instance;
        am = AudioManager.instance;
        us = UniversalSettings.instance;
        ag = AssessmentGame.instance;
        em = ExtraModManager.instance;

        saveState = new SaveState();
        gameData = new GameData(); 


        diceRollRecord = new int[totalDiceValues];
        totalRolls = 0;

        for (int i = 0; i < totalDiceValues; i++)
            diceRollRecord[i] = 0;

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

        if (tm.newGameStarted)
        {    
            boardID = sm.boardID;
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
        }
        
        /***FOR TESTING ONLY**/
        /*string board = "";
        for (int i = 0; i < boardData.Count; i++)
        {
            foreach(string word in boardData[i])
            {
                board += word + " ";
            }
            board += "\n";
        }
       
        Debug.Log(board);*/

      

        /***********EVERYTHING PAST THIS POINT CAN BE RESET IF RESETTING GAME WITHOUT GOING BACK TO TITLE*******/
        ToggleBoardSpaceAura(false);
        animateAuraCoroutineOn = false;

        //encounter game setup
        ncm.ToggleNumberCardUIContainer(false);

        //assessment game setup
        ag.ToggleGameContainer(false);
      

        #region UI Setup
        //language update
        ui.UpdateLanguage();

        //Set up colours for Star Cache UI
        highProbabilityColor = new Color(0.2f, 1, 0.2f);        //light green
        midProbabilityColor = new Color(1, 0.6f, 0.2f);         //orange
        lowProbabilityColor = new Color(0.9f, 0.06f, 0.1f);     //dark red

        foreach(PlayerPanel panel in ui.playerPanels)
        {
            panel.TogglePanel(false);
        }

        ui.ToggleMoveModValue(false);
        ui.ToggleAlertUI(false);
        ui.feedbackUIActive = new bool[2];
        ui.ToggleFeedBackUI(0, false);
        ui.ToggleFeedBackUI(1, false);
        ui.roundHandler.originalPos = new Vector3(ui.roundHandler.transform.position.x + 60, ui.roundHandler.transform.position.y, 0);
        ui.roundHandler.destinationPos = ui.roundHandler.transform.position;
        ui.resultsHandler.ToggleResultsHandler(false);
        ui.ToggleEndGameUIContainer(false);

        //extra mode setup
        if (us.extraModeEnabled)
            ui.ToggleExtraModeContainer(true);
        else
            ui.ToggleExtraModeContainer(false);

        //Dice Roll menu
        ui.diceRollRecordContainerToggle = false;
        ui.diceRollRecordContainer.gameObject.SetActive(false);
        foreach (TMPro.TextMeshProUGUI diceUI in ui.diceRollRecordUI)
        {
            diceUI.text = "0";
        }
        ui.totalRollsUIValue.text = "0";

        //Card Draw Rates menu
        ui.cardDrawRatesContainerToggle = false;
        ui.cardDrawRatesContainer.gameObject.SetActive(false);

        //The cards are set up so get their values
        ui.totalCardsUIValue.text = cm.cards.Count.ToString();

        for(int i = 0; i < ui.cardDrawRatesUI.Length; i++)
        {
            float cardDrawProb = Mathf.Round((float)cm.cardTypes[i].amount / (float)cm.cards.Count * 1000) / 1000.0f;
            ui.cardDrawRatesUI[i].text = cardDrawProb.ToString();
        }

        //Viewed Lessons
        ui.viewedLessonsContainerToggle = false;
        ui.viewedLessonsContainer.gameObject.SetActive(false);

        foreach(LessonButton button in ui.lessonButtons)
        {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = "???";
        }

        //mini lesson UI
        ui.ToggleMiniLessonUIContainer(false);

        //round display setup
        ui.roundHandler.SetNextRoundValueUI("");

        //toggles for UI
        EnableArrowContainer(false);
        dice.ShowDice(false);
        #endregion

        //lessons setup
        lm.Initialize();

        /*****TRY LOADING SAVE STATE HERE*******/
        
        if (tm.saveStateFound && !tm.newGameStarted)
        {
            //saveState.ReadState(gameData);
            resumedSaveState = true;
            saveState.LoadState(saveState.ReadState);
            //ui.UpdateLanguage();
            //SetGameState(GameState.NewRound);
        }
        else
        {
            //set up deck
            cm.SetupCards();
            cm.ShuffleCards();

            //Set up players
            playerList = new List<Player>();
            for (int i = 0; i < sm.numberOfPlayers; i++)
            {
                playerList.Add(Instantiate(playerPrefab));
            }

            foreach (Player player in playerList)
            {
                homeSpace.playersOnSpace.Add(player);
                player.currentSpace = homeSpace;
                player.transform.position = homeSpace.transform.position;
                player.row = homeSpace.row;
                player.col = homeSpace.col;
                //Debug.Log(player.playerName + " Row " + player.row + ", " + player.playerName + " Col " + player.col);
                player.direction = Vector3.zero;
                player.moveMod = 0;
                player.moveTotal = 0;
                player.newGameStarted = true;
                player.ToggleMoveCountUI(false);

                //sprite setup
                int i = playerList.IndexOf(player);
                SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
                //sr.sprite = sm.selectedPieceColor[i];
                sr.sprite = pieceColors[sm.selectedPieceColor[i]];
                player.playingPiece = sm.selectedPieceColor[i];         //will be used for writing and reading save state
                //player.playingPiece = sm.selectedPieceColor[i];         //will be used for writing and reading save state

                //draw 3 cards
                player.DrawCard(3);
                //player.DrawCard(5);
                //player.hand.Add(cm.cardTypes[7].card);
                player.playerName = sm.playerNames[i];
                player.canRollDice = true;

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

                    //AI opponents get an advantage in Extra mode
                    if (us.extraModeEnabled)
                    {
                        player.starTotal = 3;
                        ui.playerPanels[i].UpdatePlayerStatus(player);
                    }
                }
                else if (i == 2)    //player 3
                {
                    player.isAI = true;
                    homeSpace.SetAlternatePositionTwo(player);
                    if (us.extraModeEnabled)
                    {
                        player.starTotal = 3;
                        ui.playerPanels[i].UpdatePlayerStatus(player);
                    }
                }
            }

            ui.UpdateLeadingPlayer();

            //star cache setup
            ResetStarCaches(1);

            //Check for extra mode mod
            if (us.extraModeEnabled)
            {

                int i = 0;
                bool modFound = false;
                while (!modFound && i < em.extraMods.Length)
                {
                    if (boardID == em.extraMods[i].boardID)
                    {
                        modFound = true;
                        switch(boardID)
                        {
                            case 0:
                                em.activeMod = ScriptableObject.CreateInstance<StarvingMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                //ui.ToggleExtraModeContainer(true);
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 1:
                                em.activeMod = ScriptableObject.CreateInstance<JumpMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                //ui.ToggleExtraModeContainer(true);
                                ui.ToggleJumpChanceText(true);
                                ui.modNameText.text = em.activeMod.modName;
                                ui.UpdateJumpChance(em.activeMod.jumpChance);
                                break;
                            
                            case 2:
                                em.activeMod = ScriptableObject.CreateInstance<StunMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 3:
                                em.activeMod = ScriptableObject.CreateInstance<DrawCardMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 4:
                                em.activeMod = ScriptableObject.CreateInstance<RareMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 5:
                                em.activeMod = ScriptableObject.CreateInstance<HomelessMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                break;
                        }
                        
                    }
                    else
                    {
                        i++; 
                    }
                }

                //must initialize any lists even if they aren't being used by the mod
                em.activeMod.trapLocations = new List<Vector3>();

            }

            //check for mod "Rarer stars"
            if (em.activeMod != null && em.activeMod.boardID == 4)
            {
                em.activeMod.Activate();
            }

            //Homeless mod - toggle cross to false
            if (homeSpace.TryGetComponent<Home>(out Home home))
            {
                home.ToggleCross(false);
            }

            //set rounds
            currentRound = 0;
            maxRounds = 10;
            extraRound = false;
            //playerList[0].starTotal = 10;

            //music setup
            if (us.musicEnabled)
                am.musicMain.Play();

            //show introductory lesson explaining the game
            //lm.ToggleMiniLesson(6, true);

            //set game state
            SetGameState(GameState.NewRound);
        }
    }

    //activated by the Quit Game button
    public void ReturnToSetup()
    {
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        am.musicMain.Stop();
        tm.newGameStarted = true;           //must do this set game to initial state.
        us.allLessonsViewed = true;
        //SceneManager.LoadScene("Setup");
        ScreenFade sf = ScreenFade.instance;
        sf.ChangeSceneFadeOut("Setup");
        //LOLSDK.Instance.CompleteGame();
    }
    
    //Ends the game
    public void QuitGame()
    {
        LOLSDK.Instance.CompleteGame();
    }

    //used to show shortened version of Swift Movement name in player panel.
    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }


    /* This method is only used to restart a game with existing board and players, and is called by the Restart Game button */
    public void RestartGame()
    {
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        ToggleBoardSpaceAura(false);

        //encounter game setup
        ncm.ToggleNumberCardUIContainer(false);

        //set up deck
        cm.SetupCards();
        cm.ShuffleCards();
      
        #region UI Setup

        ui.ToggleMoveModValue(false);
        ui.ToggleAlertUI(false);
        ui.ToggleFeedBackUI(0, false);
        ui.ToggleFeedBackUI(1, false);
        ui.resultsHandler.ToggleResultsHandler(false);
        ui.resultsHandler.resultLabelUI.transform.localScale = Vector3.one;
        ui.ToggleEndGameUIContainer(false);

        //Dice Roll menu. Dice roll record does not reset
        ui.diceRollRecordContainerToggle = false;
        ui.diceRollRecordContainer.gameObject.SetActive(false);

        //Card Draw Rates menu
        ui.cardDrawRatesContainerToggle = false;
        ui.cardDrawRatesContainer.gameObject.SetActive(false);

        //The cards are set up so get their values
        ui.totalCardsUIValue.text = cm.cards.Count.ToString();

        for(int i = 0; i < ui.cardDrawRatesUI.Length; i++)
        {
            float cardDrawProb = Mathf.Round((float)cm.cardTypes[i].amount / (float)cm.cards.Count * 1000) / 1000.0f;
            ui.cardDrawRatesUI[i].text = cardDrawProb.ToString();
        }

        //Viewed Lessons
        ui.viewedLessonsContainerToggle = false;
        ui.viewedLessonsContainer.gameObject.SetActive(false);


        //round display setup
        ui.roundHandler.SetNextRoundValueUI("");

        //toggles for UI
        EnableArrowContainer(false);
        dice.ShowDice(false);
        #endregion

        //reset board
        foreach(BoardSpace space in boardSpaceList)
        {
            space.playersOnSpace.Clear();
        }
        
        //Set up players
        //homeSpace.playersOnSpace.Clear();
        foreach (Player player in playerList)
        {
            homeSpace.playersOnSpace.Add(player);
            player.currentSpace = homeSpace;
            player.transform.position = homeSpace.transform.position;
            player.row = homeSpace.row;
            player.col = homeSpace.col;
            //Debug.Log(player.playerName + " Row " + player.row + ", " + player.playerName + " Col " + player.col);
            player.direction = Vector3.zero;
            player.moveMod = 0;
            player.moveTotal = 0;
            player.starTotal = 0;
            player.newGameStarted = true;
            player.cardEffect = null;
            player.selectedCard = null;
            player.ToggleMoveCountUI(false);
            player.route.Clear();
            player.route.TrimExcess();

            //sprite setup
            /*int i = playerList.IndexOf(player);
            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            sr.sprite = sm.selectedPieceColor[i];
            player.playingPiece = sm.selectedPieceColor[i]; */        //will be used for writing and reading save state

            //draw 3 cards
            player.hand.Clear();
            player.DrawCard(3);
            //player.playerName = sm.playerNames[i];
            player.canRollDice = true;

            int i = playerList.IndexOf(player);
            ui.playerPanels[i].UpdatePlayerStatus(player);
            ui.playerPanels[i].ToggleCardEffectUI(false);
            ui.playerPanels[i].cardEffectUI.Initialize();

            //shift player positions
            if (i == 1) //player 2
            {
                player.isAI = true;
                homeSpace.SetAlternatePositionOne(player);
                //AI opponents get an advantage in Extra mode
                if (us.extraModeEnabled)
                {
                    player.starTotal = 3;
                    ui.playerPanels[i].UpdatePlayerStatus(player);
                }
            }
            else if (i == 2)    //player 3
            {
                player.isAI = true;
                homeSpace.SetAlternatePositionTwo(player);
                //AI opponents get an advantage in Extra mode
                if (us.extraModeEnabled)
                {
                    player.starTotal = 3;
                    ui.playerPanels[i].UpdatePlayerStatus(player);
                }
            }
        }

        ui.UpdateLeadingPlayer();

        //star cache setup
        ResetStarCaches(1);

        //extra mode setup
        if (us.extraModeEnabled)
        {
            switch(em.activeMod.boardID)
            {
                case 0:
                    break;
                
                case 1: //random jumps
                    em.activeMod.jumpChance = 0;
                    ui.UpdateJumpChance(em.activeMod.jumpChance);
                    ui.ToggleJumpChanceText(true);
                    break;

                case 2: //trapped caches
                    em.activeMod.trapLocations.Clear();
                    for (int i = 0; i < em.trapSprites.Count; i++)
                    {
                        Destroy(em.trapSprites[i]);
                        i--;
                    }
                    em.trapSprites.Clear();
                    break;

                case 4: //rarer stars
                    em.activeMod.Activate();
                    break;
            }
        }
        

        //set rounds
        currentRound = 0;
        maxRounds = 10;
        extraRound = false;
        ui.UpdateTotalRounds(maxRounds);

        //fade in
        //ScreenFade sf = ScreenFade.instance;
        //sf.FadeIn();

        //music setup
        if (us.musicEnabled)
            am.musicMain.Play();

        //set game state
        SetGameState(GameState.NewRound);
    }

    /* Setup extra round. In an extra round, no cards are used, and draw card spaces do nothing. Star caches always grant stars. */
    public void SetupExtraRound()
    {
        //first thing to do is remove players who are not tied. This step is only done with 3 players
        if (playerList.Count == 3)
        {
            foreach(PlayerPanel panel in ui.playerPanels)
            {
                panel.TogglePanel(false);
            }
        
            for (int i = 0; i < playerList.Count; i++)
            {
                if (!tiedPlayers.Contains(playerList[i]))
                {
                    playerList[i].gameObject.SetActive(false);
                    playerList.Remove(playerList[i]);
                    i--;
                }
                
            }
        }

        //reset board
        foreach(BoardSpace space in boardSpaceList)
        {
            space.playersOnSpace.Clear();
        }

        //remove extra mode stuff
        if (em.activeMod != null && em.activeMod.boardID == 1)
        {
            ui.ToggleJumpChanceText(false);
        }

        if (em.activeMod != null && em.activeMod.boardID == 2)
        {
            em.activeMod.trapLocations.Clear();
            for (int i = 0; i < em.trapSprites.Count; i++)
            {
                //Destroy(em.trapSprites[i]);
                //i--;
                em.trapSprites[i].gameObject.SetActive(false);
            }
            //em.trapSprites.Clear();
        }

        //check for homeless mod
        if (em.activeMod != null && em.activeMod.boardID == 5)
        {
            em.activeMod.Activate();    //this will remove the cross and allow Home to grant stars.
        }
        
        //player setup
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

            //cards are destroyed
            player.hand.Clear();
            player.canRollDice = true;
            player.starTotal = 0;

            int i = playerList.IndexOf(player);
            ui.playerPanels[i].TogglePanel(true);
            ui.playerPanels[i].UpdatePlayerName(player);
            ui.playerPanels[i].UpdatePlayerStatus(player);
            ui.playerPanels[i].ToggleCardEffectUI(false);

            //TODO: When players are placed in the same space, their position must be shifted a bit so they all fit.
            if (i == 1) //player 2
            {
                player.isAI = true;
                homeSpace.SetAlternatePositionOne(player);
            }
            else if (i == 2)    //player 3
            {
                player.isAI = true;
                homeSpace.SetAlternatePositionTwo(player);
            }
        }

        ui.UpdateLeadingPlayer();

        //star caches always grant stars. UI is hidden since it won't change in this round.
        ResetStarCaches(1);
        ToggleStarCacheUI(false);


        //set round
        currentRound = 1;
        maxRounds = 1;
        extraRound = true;

        ui.UpdateCurrentRound(currentRound);
        ui.UpdateTotalRounds(maxRounds);
        //ui.roundHandler.ToggleNextRoundLabelUI(true);
        //ui.roundHandler.SetNextRoundValueUI("EX");
    }

    

    public void ShowArrow(Arrow arrow, bool toggle)
    {
        arrow.gameObject.SetActive(toggle);

        if (toggle == true)
        {
            //place arrow on player position
            Player player = playerList[playerIndex];
            //arrow.OriginalPos = new Vector3(player.transform.position.x + 1, player.transform.position.y + 1, 0);

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
        if (auraSr == null)
            auraSr = spaceAura.GetComponent<SpriteRenderer>();
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
                if (gameState == GameState.StartAssessment)
                {
                    ag.ToggleGameContainer(false);
                }

                gameState = GameState.NewRound;



                if (extraRound)
                {
                    playerIndex = 0;
                    currentPlayer = playerList[playerIndex];
                    ui.ToggleNewTurnUIContainer(false); //in case game is resuming from a load state
                    ui.ToggleCardUIContainer(false);
                    cm.DisplayHand(currentPlayer, false);

                    if (!lm.miniLessonList[22].lessonViewed)
                    {
                        miniLessonIndex = 22;
                        SetGameState(GameState.GetMiniLesson);
                    }
                    else
                        goto case GameState.NextPlayerTurn;
                }
                else if (currentRound + 1 > maxRounds)
                {
                    //game is over, check for a tie
                    //submit current progress
                    LOLSDK.Instance.SubmitProgress(playerList[0].starTotal, maxRounds, maxRounds);
                    //saveState.WriteState(gameData);
                    //foreach(Player player in playerList)
                        //player.starTotal = 0;
                    goto case GameState.EndGame;
                }
                else if (!us.allLessonsViewed && 
                    (tm.newGameStarted && !lm.lessonList[13].lessonViewed) || (!lm.lessonList[lessonIndex].lessonViewed && lm.lessonList[lessonIndex].roundToDisplayLesson == currentRound && !lessonViewedThisRound && currentRound > 0))
                {
                    //open up a lesson
                    lessonViewedThisRound = true;
                    saveState.WriteState(gameData);
                    goto case GameState.GetLesson;    
                }
                else
                {
                    //check for "Trapped Caches" mod
                    if (!resumedSaveState && em.activeMod != null && em.activeMod.boardID == 2)
                    {
                        em.activeMod.Activate();
                    }
                    //check for "Homeless" mod
                    if (em.activeMod != null && em.activeMod.boardID == 5)
                    {
                        em.activeMod.Activate();
                    }
                    
                    resumedSaveState = false;
                    saveState.WriteState(gameData);
                    lessonViewedThisRound = false;
                    currentRound++;
                    //currentRound += 10;
                    //currentRound += 3;

                    playerIndex = 0;
                    currentPlayer = playerList[playerIndex];
                    ui.roundHandler.ToggleNextRoundLabelUI(true);
                    ui.roundHandler.SetNextRoundValueUI(currentRound.ToString());
                    ui.UpdateCurrentRound(currentRound);

                    ui.ToggleCardUIContainer(false);
                    ui.ToggleAlertUI(false);
                    cm.DisplayHand(currentPlayer, false);

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
                        //Draw a Card mod
                        if (!resumedSaveState && em.activeMod != null && em.activeMod.boardID == 3)
                        {
                            em.activeMod.Activate();
                        }
                        if (!currentPlayer.isAI)
                        {
                            if (currentPlayer.hand.Count > 5)
                                goto case GameState.PlayerDiscardingCards;
                            else
                                goto case GameState.BeginningNewTurn;
                        }
                        else
                            goto case GameState.CardPhase;
                    }
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
                    if (!extraRound)
                    {
                        //check for a lesson
                        if (!lm.miniLessonList[14].lessonViewed && currentRound == 4)
                        {
                            miniLessonIndex = 14;
                            SetGameState(GameState.GetMiniLesson);
                        }
                        //check for assessment
                        else if (currentRound == 6)
                        {
                            goto case GameState.StartAssessment;
                        }
                        else
                            goto case GameState.NewRound;
                    }
                    else
                    {
                        playerIndex = 0;
                        currentPlayer = playerList[playerIndex];
                        goto case GameState.RollingDice;
                    }
                }
                else
                {
                    if (gameState == GameState.NewRound && extraRound)
                    {
                        ui.roundHandler.ToggleNextRoundLabelUI(true);
                        ui.roundHandler.SetNextRoundValueUI("EX");
                        playerIndex = -1;   //prevents first player's turn from being skipped in extra round.
                    }

                    gameState = GameState.NextPlayerTurn;

                    playerIndex++;
                    currentPlayer = playerList[playerIndex];

                    if (!extraRound)
                    {
                        if (currentPlayer.loseTurn)
                        {
                            //currentPlayer.loseTurn = false;
                            ui.DisplayFeedbackMessage("lostTurn", currentPlayer.transform.position);
                        }
                        else
                        {
                            //AI players do not have to choose between rolling dice or playing a card. They will always check
                            //for cards to use before rolling dice.
                            //Draw a Card mod
                            if (!resumedSaveState && em.activeMod != null && em.activeMod.boardID == 3)
                            {
                                em.activeMod.Activate();
                            }
                            if (!currentPlayer.isAI)
                            {
                                if (currentPlayer.hand.Count > 5)
                                    goto case GameState.PlayerDiscardingCards;
                                else
                                    goto case GameState.BeginningNewTurn;
                            }
                            else
                            {
                                if (currentPlayer.hand.Count > 5)
                                    goto case GameState.PlayerDiscardingCards;
                                else
                                    goto case GameState.CardPhase;
                            }     
                        }
                    }    
                    else 
                    {   //go to extra round
                        if (currentPlayer.loseTurn)
                        {
                            //currentPlayer.loseTurn = false;
                            ui.DisplayFeedbackMessage("lostTurn", currentPlayer.transform.position);
                        }
                        else
                            goto case GameState.RollingDice;
                    }
                }
                break;
                
            case GameState.GetLesson:
                //if all lessons have been viewed, we skip this state
                gameState = GameState.GetLesson;
                ui.ToggleAlertUI(false);
                
                //if first time playing, prevent round start until lesson is closed.
                if (!lm.lessonList[13].lessonViewed)
                {
                    playerIndex = 0;
                    currentPlayer = playerList[playerIndex];
                    cm.DisplayHand(currentPlayer, false);
                    ui.ToggleCardUIContainer(false);
                    ui.ToggleNewTurnUIContainer(false);
                    ui.roundHandler.ToggleNextRoundLabelUI(false);

                    lm.ToggleLesson(13, true);
                }
                else
                {
                    lm.ToggleLesson(lessonIndex++, true);
                }
              
              
                /*if (!lm.lessonList[0].lessonViewed)
                {
                    lm.ToggleLesson(0, true);
                    //lm.ToggleLesson(8, true);
                }
                else
                {
                    int randLesson;
                    do
                    {
                        randLesson = Random.Range(1, lm.lessonList.Length - 1); //excluding the tips lesson, which is the last one
                    }
                    while(lm.lessonList[randLesson].lessonViewed == true);
                    lm.ToggleLesson(randLesson, true);
                }*/
                break;

            case GameState.GetMiniLesson:
                //if all lessons have been viewed, we skip this state
                if ((gameState == GameState.PlayerJumping || gameState == GameState.StartEncounter) && miniLessonIndex == 5)
                {
                     //never show the card effect lesson in this state because it overlaps with encounter window.
                    lm.ToggleMiniLesson(miniLessonIndex, false);
                    lm.miniLessonList[5].lessonViewed = false;  
                }
                else
                {
                    gameState = GameState.GetMiniLesson;
                    lm.ToggleMiniLesson(miniLessonIndex, true);
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

                if (gameState == GameState.CardPhase || gameState == GameState.PlayerDiscardingCards)
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
                    ui.ToggleNewTurnUIContainer(false);
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
                        //ui.DisplayAlert("lookingAtHand", currentPlayer.playerName);
                        //ui.DisplayAlert(currentPlayer.playerName + " is looking at their hand");
                        currentPlayer.SetAIState(Player.AIState.ChoosingCard);
                    }
                    else
                    {
                        //show mini lesson for first time
                        if (!lm.miniLessonList[4].lessonViewed)
                        {
                            miniLessonIndex = 4;
                            SetGameState(GameState.GetMiniLesson);
                        }
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
                //ui.ToggleCardUIContainer(false);
                ui.ToggleBackButton(true);
                ui.TogglePlayCardButton(true);
                ui.ToggleSkipCardButton(false);
                cm.DisplayHand(currentPlayer, false);
                cm.background.gameObject.SetActive(false);
                
                
                //ui.DisplayAlert("Here's the situation");

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
                        ui.DisplayAlert("usingRefillCard");
                        ToggleBoardSpaceAura(true);
                        ui.ChangePlayCardButtonColor(Color.white);
                        spaceAura.transform.position = currentPlayer.targetCache.transform.position;
                    }
                    else
                    {
                        //don't let player play card
                        ui.DisplayAlert("usingRefillCard_noSpace");
                        ui.ChangePlayCardButtonColor(Color.grey);
                    }
                    
                }
                else
                {
                    //don't let player play card
                    ui.DisplayAlert("usingRefillCard_noSpace");
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

                //if player is AI, display the card that they're using.
                if (currentPlayer.isAI)
                {
                    switch(currentPlayer.selectedCard.cardID)
                    {
                        case 0:     //swift movement
                            ui.DisplayAlert("aiPlayingCard_swift", currentPlayer.playerName);
                            break;
                        
                        case 1:     //move 1
                            ui.DisplayAlert("aiPlayingCard_move1", currentPlayer.playerName);
                            break;

                        case 2:     //move 2
                            ui.DisplayAlert("aiPlayingCard_move2", currentPlayer.playerName);
                            break;

                        case 3:     //move 3
                            ui.DisplayAlert("aiPlayingCard_move3", currentPlayer.playerName);
                            break;

                        case 4:     //extend
                            ui.DisplayAlert("aiPlayingCard_extend", currentPlayer.playerName);
                            break;

                        case 5:     //refill
                            ui.DisplayAlert("aiPlayingCard_refill", currentPlayer.playerName);
                            break;

                        case 6:     //go home
                            ui.DisplayAlert("aiPlayingCard_goHome", currentPlayer.playerName);
                            break;

                        case 7:     //jump
                            ui.DisplayAlert("aiPlayingCard_jump", currentPlayer.playerName);
                            break;
                    }
                }

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
                            ui.playerPanels[targetIndex].cardEffectUI.SetCardDetails(GetText("swiftMove")/*"SWIFT MOVE"*/, currentPlayer.selectedCard.ability);
                        }
                        else
                        {
                            ui.playerPanels[targetIndex].cardEffectUI.SetCardDetails(currentPlayer.selectedCard.cardName.ToUpper(), 
                                currentPlayer.selectedCard.ability);
                        }
                        //ui.DisplayFeedbackMessage(target.cardEffect.cardName, target.transform.position);
                        ui.DisplayFeedbackMessage(target.cardEffect.nameKey, target.transform.position);

                        //display mini lesson here. Don't show this lesson if the encounter game lesson is open.
                        if (/*(targetIndex == 0 || currentPlayer == playerList[0])*/ playerList[0].cardEffect != null && !lm.miniLessonList[5].lessonViewed && !lm.miniLessonList[9].gameObject.activeSelf)
                        {
                            miniLessonIndex = 5;
                            SetGameState(GameState.GetMiniLesson);
                        }

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
                    
                     //show mini lesson for first time
                    if (currentRound == 3 && !lm.miniLessonList[6].lessonViewed)
                    {
                        miniLessonIndex = 6;
                        SetGameState(GameState.GetMiniLesson);
                    }
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
                //currentPlayer.SetMovement(currentPlayer.moveTotal, currentPlayer.moveMod);
                currentPlayer.ToggleMoveCountUI(true);

                //clear any alerts           
                ui.ToggleAlertUI(false);

                if (!extraRound)
                    ToggleStarCacheUI(true);

                currentPlayer.StartMoving(currentPlayer.moveTotal);
                break;

            case GameState.PlayerJumping:
                //player used jump card, we don't use regular move code.
                gameState = GameState.PlayerJumping;
                ToggleStarCacheUI(true);
                ui.ToggleAlertUI(false);
                dice.ShowDice(false);       //in case player has extra mod "Random Jumps" on and skipped move phase
                dice.ToggleCross(false);

                //never show the card effect lesson in this state because it overlaps with encounter window.
                if (!lm.miniLessonList[5].lessonViewed)
                {
                    lm.ToggleMiniLesson(5, false);
                    lm.miniLessonList[5].lessonViewed = false;
                }
                currentPlayer.Jump(currentPlayer.jumpDestination);
                //ArrangePlayerPositions(playerList, currentPlayer);
                break;
                //goto case GameState.StartEncounter;

            case GameState.PlayerDiscardingCards:
                gameState = GameState.PlayerDiscardingCards;
                
                ToggleStarCacheUI(false);
                ui.ToggleCardUIContainer(true);
                ui.ToggleNewTurnUIContainer(false);
                ui.DisplayAlert("tooManyCards", currentPlayer.hand.Count - cm.maxHand);
                //ui.DisplayAlert("Too many cards! Cards to discard: " + (currentPlayer.hand.Count - cm.maxHand));
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
                //goto case GameState.NextPlayerTurn;
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


                switch(currentPlayer.currentSpace.playersOnSpace.Count)
                {
                    case 2:
                        //check if one of the players is human
                        if (!currentPlayer.isAI)
                        {
                            for (int j = 0; j < currentPlayer.currentSpace.playersOnSpace.Count; j++)
                            {
                                if (currentPlayer.currentSpace.playersOnSpace[j] == currentPlayer) continue;
                                opponent = currentPlayer.currentSpace.playersOnSpace[j];
                            }

                            //mini lesson appears before game starts.
                            if (!lm.miniLessonList[9].lessonViewed)
                            {
                                //if card effect lesson is currently open, close it for now and let player view it later.
                                if (lm.miniLessonList[5].gameObject.activeSelf)
                                {
                                    lm.ToggleMiniLesson(5, false);
                                    ui.ToggleCardEffectIndicator(false);
                                    lm.miniLessonList[5].lessonViewed = false;
                                }
    
                                miniLessonIndex = 9;
                                SetGameState(GameState.GetMiniLesson);
                            }
                            else
                            {
                                ncm.StartHighLowGame(currentPlayer, opponent);
                            }
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

                                //mini lesson appears before game starts.
                                if (!lm.miniLessonList[9].lessonViewed)
                                {
                                    //if card effect lesson is currently open, close it for now and let player view it later.
                                    if (lm.miniLessonList[5].gameObject.activeSelf)
                                    {
                                        lm.ToggleMiniLesson(5, false);
                                        ui.ToggleCardEffectIndicator(false);
                                        lm.miniLessonList[5].lessonViewed = false;
                                    }
                                    miniLessonIndex = 9;
                                    SetGameState(GameState.GetMiniLesson);
                                }
                                else
                                {
                                    ncm.StartHighLowGame(currentPlayer, opponent);
                                }
                            }
                            else
                            {
                                //both players are AI
                                ncm.GetAIWinner(currentPlayer, opponent);
                            }
                            
                        }
                        break;

                    case 3:
                        //the opponent is the player with the most stars. If there's a tie, pick a random opponent.
                        if (!currentPlayer.isAI)    //current player is the attacker
                        {
                            //check who the opponent is going to be
                            opponent = playerList[1];
                            if (opponent.starTotal < playerList[2].starTotal)
                            {
                                opponent = playerList[2];
                            }
                            else
                            {
                                //there's a tie; pick a random opponent.
                                int randOpponent = Random.Range(1, 3);
                                opponent = playerList[randOpponent];
                            }

                            //mini lesson appears before game starts.
                            if (!lm.miniLessonList[9].lessonViewed)
                            {
                                miniLessonIndex = 9;
                                SetGameState(GameState.GetMiniLesson);
                            }
                            else
                            {
                                ncm.StartHighLowGame(currentPlayer, opponent);
                            }

                        }
                        else
                        {
                            //AI picks opponent. Minigame is only played if human player is the opponent.
                            switch(playerIndex)
                            {
                                case 1:
                                    opponent = playerList[0];   //human player
                                    if (opponent.starTotal < playerList[2].starTotal)
                                    {
                                        opponent = playerList[2];
                                    }
                                    else
                                    {
                                        //there's a tie; pick a random opponent.
                                        //int randOpponent;
                                        do
                                        {
                                            int randOpponent = Random.Range(0, 3);
                                            opponent = playerList[randOpponent];
                                        }
                                        while(opponent == currentPlayer);
                                    }
                                    break;

                                case 2:
                                    opponent = playerList[0];   //human player
                                    if (opponent.starTotal < playerList[1].starTotal)
                                    {
                                        opponent = playerList[1];
                                    }
                                    else
                                    {
                                        //there's a tie; pick a random opponent.                                       
                                        int randOpponent = Random.Range(0, 2);
                                        opponent = playerList[randOpponent];
                                    }
                                    break;
                            }

                            //are we playing the minigame?
                            if (!opponent.isAI)
                            {
                                //mini lesson appears before game starts.
                                if (!lm.miniLessonList[9].lessonViewed)
                                {
                                    miniLessonIndex = 9;
                                    SetGameState(GameState.GetMiniLesson);
                                }
                                else
                                {
                                    ncm.StartHighLowGame(currentPlayer, opponent);
                                }
                            }
                            else
                            {
                                //pick an AI winner at random 
                                ncm.GetAIWinner(currentPlayer, opponent);
                            }

                        }
                        break;
                }

                //when encounter is finished, check space
                //goto case GameState.CheckingSpace;
                break;

            case GameState.StartAssessment:
                gameState = GameState.StartAssessment;
                
                //show instructions
                if (!lm.miniLessonList[16].lessonViewed)
                {
                    miniLessonIndex = 16;
                    SetGameState(GameState.GetMiniLesson);
                }
                else
                {
                    ag.ToggleGameContainer(true);
                }
                break;

            case GameState.EndTurn:
                break;

            case GameState.ExtraRound:
                //if there's a tie between two or more players, start an extra round to determine the winner. Some rules change
                //quick check to see if human player is among the tied players
                //gameState = GameState.ExtraRound;
                tiedPlayers = new List<Player>();
                if (playerList.Count == 3)
                {
                    int highestValue = 0;
                    foreach(Player player in playerList)
                    {
                        if (player.starTotal > highestValue)
                            highestValue = player.starTotal;
                    }

                    //now check for a tie
                    foreach(Player player in playerList)
                    {
                        if (player.starTotal == highestValue)
                        {
                            tiedPlayers.Add(player);
                        }
                    }

                    //check if one of the tied players is human
                    bool humanPlayerFound = false;
                    int i = 0;
                    while (i < tiedPlayers.Count && !humanPlayerFound)
                    {
                        if (!tiedPlayers[i].isAI)
                        {
                            humanPlayerFound = true;
                        }
                        else
                        {
                            i++;
                        }
                    }

                    if (!humanPlayerFound)
                    {
                        //determine a winner at random
                        Player winner = (Random.value <= 0.5f) ? tiedPlayers[0] : tiedPlayers[1];
                        //run coroutine for winner
                        ui.resultsHandler.ToggleResultsHandler(true);
                        ui.resultsHandler.DisplayWinner(winner.playerName);
                        break;
                    }
                    else
                    {                   
                        //set up extra round
                        SetupExtraRound();
                        playerIndex = 0;       
                        currentPlayer = playerList[playerIndex];
                        saveState.WriteState(gameData);
                        goto case GameState.NewRound;
                    }
                }
                else
                {
                    //set up extra round
                    SetupExtraRound();
                    playerIndex = 0;       
                    currentPlayer = playerList[playerIndex];
                    saveState.WriteState(gameData);
                    goto case GameState.NewRound;
                }
                //break;

            case GameState.EndGame:
                //game is over, highlight the winner or if the game ended in a draw
                //if a player is on home, they're the winner
                gameState = GameState.EndGame;
                ToggleStarCacheUI(false);
                if (!extraRound)
                {
                    if (currentPlayer.currentSpace == homeSpace && currentPlayer.starTotal >= 10)
                    {
                        //run a coroutine to show winner's name and some special effects
                        ui.resultsHandler.ToggleResultsHandler(true);
                        ui.resultsHandler.DisplayWinner(currentPlayer.playerName);
                        ui.ToggleEndGameUIContainer(true);
                        if (us.musicEnabled)
                        {
                            am.musicMain.Stop();
                            am.musicWinner.Play();
                        }
                    }
                    else
                    {
                        //check star count of each player. Whoever has the most stars is the winner
                        Player leadingPlayer = currentPlayer.GetLeadingPlayer(playerList);

                        //now check if there's a tie
                        tiedPlayers = new List<Player>();
                        if (leadingPlayer == null)
                        {
                            //play an extra round with no cards. If one of the players isn't the human player, the winner is determined randomly.
                            //tiedPlayers.Add(leadingPlayer);
                            goto case GameState.ExtraRound;
                        }
                        else
                        {
                            //we have a definitive winner; run coroutine
                            ui.resultsHandler.ToggleResultsHandler(true);
                            ui.resultsHandler.DisplayWinner(leadingPlayer.playerName);
                            ui.ToggleEndGameUIContainer(true);
                            if (us.musicEnabled)
                            {
                                am.musicMain.Stop();
                                am.musicWinner.Play();
                            }
                        }
                        /*if (leadingPlayer != null)
                        {
                            foreach(Player player in playerList)
                            {
                                if (player == leadingPlayer) continue;
                                if (player.starTotal == leadingPlayer.starTotal)
                                {
                                    tiedPlayers.Add(player);
                                }
                            }
                        }

                        if (tiedPlayers.Count <= 0)
                        {
                            //we have a definitive winner; run coroutine
                            ui.resultsHandler.ToggleResultsHandler(true);
                            ui.resultsHandler.DisplayWinner(leadingPlayer.playerName);
                            ui.ToggleEndGameUIContainer(true);
                            am.musicMain.Stop();
                            am.musicWinner.Play();
                        }
                        else
                        {
                            //play an extra round with no cards. If one of the players isn't the human player, the winner is determined randomly.
                            tiedPlayers.Add(leadingPlayer);
                            goto case GameState.ExtraRound;
                        }*/
                    }
                }
                else
                {
                    //check star count of each player. Whoever has the most stars is the winner
                    Player leadingPlayer = currentPlayer.GetLeadingPlayer(playerList);

                    //now check if there's a tie
                    tiedPlayers = new List<Player>();
                    if (leadingPlayer == null)
                    {
                        //game ends in a draw; run coroutine
                        ui.resultsHandler.ToggleResultsHandler(true);
                        ui.resultsHandler.DisplayDraw();
                        ui.ToggleEndGameUIContainer(true);
                    }
                    else
                    {
                        //we have a definitive winner; run coroutine
                        ui.resultsHandler.ToggleResultsHandler(true);
                        ui.resultsHandler.DisplayWinner(leadingPlayer.playerName);
                        ui.ToggleEndGameUIContainer(true);
                        if (us.musicEnabled)
                        {
                            am.musicMain.Stop();
                            am.musicWinner.Play();
                        }
                    }
                    /*if (leadingPlayer != null)
                    {
                        foreach(Player player in playerList)
                        {
                            if (player == leadingPlayer) continue;
                            if (player.starTotal == leadingPlayer.starTotal)
                            {
                                tiedPlayers.Add(player);
                            }
                        }
                    }

                    if (tiedPlayers.Count <= 0)
                    {
                        //we have a definitive winner; run coroutine
                        ui.resultsHandler.ToggleResultsHandler(true);
                        ui.resultsHandler.DisplayWinner(leadingPlayer.playerName);
                        ui.ToggleEndGameUIContainer(true);
                        am.musicMain.Stop();
                        am.musicWinner.Play();
                    }
                    else 
                    {
                        //game ends in a draw; run coroutine
                        ui.resultsHandler.ToggleResultsHandler(true);
                        ui.resultsHandler.DisplayDraw();
                        ui.ToggleEndGameUIContainer(true);
                    }*/
                }
                break;
                   
        }

        //gameState = state;
    }

        //board aura pulsates
        IEnumerator AnimateSpaceAura()
        {
            animateAuraCoroutineOn = true;
            float changeSpeed = 2;
            while (auraSr.color.a > 0)
            {
                float alpha = auraSr.color.a - changeSpeed * Time.deltaTime;
                auraSr.color = new Color(auraSr.color.r, auraSr.color.g, auraSr.color.b, alpha);
                yield return null;
            }

            //change back
            while (auraSr.color.a < 0.7f)
            {
                float alpha = auraSr.color.a + changeSpeed * Time.deltaTime;
                auraSr.color = new Color(auraSr.color.r, auraSr.color.g, auraSr.color.b, alpha);
                yield return null;
            }

            animateAuraCoroutineOn = false;

        }

    
}

