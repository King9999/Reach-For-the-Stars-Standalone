using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using LoLSDK;

namespace MMurray.ReachForTheStars
{
    [Serializable]
    public class CardData : ScriptableObject
    {
        public byte cardID;     //used for matching cards with their text in the cards.json file
        public string nameKey, abilityKey, tipKey;      //used to idenitfy card in language.json
        public string cardName;
        public string ability;
        public string tip;      //useful information on how to use card
        public int artID;       //reference to card sprite
        public byte weight;     //used by AI. Determines the card the AI is most likely to use. Weight total is 200.

        public enum Target
        {
            OnePlayer, Self, Board
        }
        public Target targetType;

        //public virtual void Activate(Player user, Player target){}
        public virtual void Activate(Player target){}
        public virtual void Activate(List<BoardSpace> target){}
        public virtual void Activate(Player user, StarCache targetSpace){}

    }

    /* BOARD CLASSES FOR JSON
    This class pulls data from a JSON file. This data determines the board layout */
    [Serializable]
    public class Board
    {
        public string boardName;
        public string spaceCount;
        public Row[] rows;
        public List<Player> players;    //holds position of player piece on the board

    }

    [Serializable]
    public class Boards
    {
        public Board[] boards;
    }

    [Serializable]
    public class Row
    {
        public string row;
    }

    /*** Card Classes for JSON ***/
    [Serializable]
    public class CardText
    {
        public byte cardID;
        public byte amount;
        public byte weight;
        public string cardName;
        public string ability;
        public string tip;
    }

    [Serializable]
    public class CardTexts
    {
        public CardText[] cardTypes;
    }

    /****JSON classes for random player names****/
    [Serializable]
    public class PlayerName
    {
        public string name;
    }

    [Serializable]
    public class PlayerNames
    {
        public PlayerName[] playerNames;
    }

    /*****JSON for reading/writing save states*****/
    [Serializable]
    public class SaveState
    {
        //const string resourcesFolder = Application.dataPath + "/MMurray.ReachForTheStars/Resources/";   //not sure if this will work in WebGL
        string GetText (string key)
        {
            string value = SharedState.LanguageDefs?[key];
            return value ?? "--missing--";
        }

        public void WriteState(GameData gameData)
        {
            GameManager gm = GameManager.instance;
            CardManager cm = CardManager.instance;
            UniversalSettings us = UniversalSettings.instance;

            //do a deep copy of playerList and boardSpaceList
            gameData.player_data = new List<PlayerData>();
            foreach(Player player in gm.playerList)
            {
                PlayerData newPlayer = new PlayerData();
                newPlayer.player_name = player.playerName;
                newPlayer.playing_piece = player.playingPiece;
                newPlayer.star_total = player.starTotal;
                newPlayer.move_total = player.moveTotal;               
                newPlayer.move_mod = player.moveMod;                 
                newPlayer.row = player.row;
                newPlayer.col = player.col;                         
                //newPlayer.hand = player.hand;
                newPlayer.card_effect = player.cardEffect;
                newPlayer.position = player.transform.position;
                newPlayer.direction = player.direction;
                newPlayer.can_roll_dice = player.canRollDice;
                newPlayer.is_ai = player.isAI;
                newPlayer.new_game_started = true;
                newPlayer.lose_turn = player.loseTurn;

                //get player hand
                newPlayer.hand = new List<CardData>();
                foreach(CardData card in player.hand)
                {
                    newPlayer.hand.Add(card);
                }

                gameData.player_data.Add(newPlayer);
            }

            gameData.board_data = new List<BoardSpaceData>();
            foreach(BoardSpace space in gm.boardSpaceList)
            {
                BoardSpaceData newSpace = new BoardSpaceData();
                newSpace.space_type = space.spaceType;
                newSpace.row = space.row;
                newSpace.col = space.col;
                newSpace.board_position = space.transform.position;

                //check if space is a star cache and get its current probability
                if (space.TryGetComponent(out StarCache cache))
                {
                    newSpace.probability = cache.probability;
                }
                else
                    newSpace.probability = 0;

                gameData.board_data.Add(newSpace);
            }

            //board ID
            gameData.board_id = gm.boardID;

            //write rest of data
            gameData.deck = new List<CardData>();
            foreach(CardData card in cm.cards)
            {
                gameData.deck.Add(card);
            }

            gameData.current_round = gm.currentRound;
            gameData.max_round = gm.maxRounds;
            gameData.extra_round = gm.extraRound;
            gameData.total_rolls = gm.totalRolls;

            //lessons
            LessonManager lm = LessonManager.instance;
            gameData.lessons_viewed = new bool[lm.lessonList.Length];
            gameData.mini_lessons_viewed = new bool[lm.miniLessonList.Length];

            for(int i = 0; i < gameData.lessons_viewed.Length; i++)
            {
                gameData.lessons_viewed[i] = lm.lessonList[i].lessonViewed;
            }
            gameData.lesson_viewed_this_round = gm.lessonViewedThisRound;
            gameData.lesson_index = gm.lessonIndex;

            for(int i = 0; i < gameData.mini_lessons_viewed.Length; i++)
            {
                gameData.mini_lessons_viewed[i] = lm.miniLessonList[i].lessonViewed;
            }
            gameData.mini_lesson_index = gm.miniLessonIndex;

            gameData.all_lessons_viewed = us.allLessonsViewed;

            //dice roll record
            gameData.dice_roll_record = new int[gm.diceRollRecord.Length];
            for (int i = 0; i < gameData.dice_roll_record.Length; i++)
            {
                gameData.dice_roll_record[i] = gm.diceRollRecord[i];
            }
            gameData.total_rolls = gm.totalRolls;
            
            //card draw record
            gameData.card_types = new CardManager.CardType[cm.cardTypes.Length];
            for (int i = 0; i < gameData.card_types.Length; i++)
            {
                gameData.card_types[i].amount = cm.cardTypes[i].amount;
            }

            /*string fileName = Application.persistentDataPath + "/save_state_data.json";
            string jsonData = JsonUtility.ToJson(gameData);
            File.WriteAllText(fileName, jsonData);    //file will be written to game root directory

            if (File.Exists(fileName))
            {
                Debug.Log(fileName + " save state was successful.");
            }
            else
            {
                Debug.Log("There was a problem saving the state.");
            }*/

            //universal & extra mode settings
            gameData.music_enabled = us.musicEnabled;
            gameData.extra_mode_enabled = us.extraModeEnabled;
            if (us.extraModeEnabled)
            {
                ExtraModManager em = ExtraModManager.instance;
                gameData.jump_chance = em.activeMod != null ? em.activeMod.jumpChance : 0;
                gameData.trap_locations = new List<Vector3>();
                //em.activeMod.trapLocations = new List<Vector3>();
                for (int i = 0; i < em.activeMod.trapLocations.Count; i++)
                {
                    gameData.trap_locations.Add(em.activeMod.trapLocations[i]);
                }

                //Homeless mod
                /*if (gm.homeSpace.TryGetComponent<Home>(out Home home))
                {
                    gameData.can_get_stars = home.canGetStars;
                }*/
                
            } 

            //submit current progress
            LOLSDK.Instance.SubmitProgress(gameData.player_data[0].star_total, gameData.current_round, gameData.max_round);
            LOLSDK.Instance.SaveState(gameData);
        }

        public void ReadState(GameData gameData)
        {
            /*NOTE: UI Setup must be done in here*/
            GameManager gm = GameManager.instance;
            CardManager cm = CardManager.instance;
            AudioManager am = AudioManager.instance;
            UniversalSettings us = UniversalSettings.instance;
            AssessmentGame ag = AssessmentGame.instance;
            UI ui = UI.instance;

            //update language
            ui.UpdateLanguage();

            //string fileName = Application.persistentDataPath + "/save_state_data.json";
            //string jsonData = File.ReadAllText(fileName);
            //gameData = JsonUtility.FromJson<GameData>(jsonData);

            //get data from LOL LoadState
            //if (gameData != null)
                //gm.gameData = gameData;

            //set up cards    
            cm.SetupCards();    //need to do this so cards work properly
            cm.cards = new List<CardData>();

            foreach(PlayerData player in gameData.player_data)
            {
                Player newPlayer = GameManager.Instantiate(gm.playerPrefab);
                newPlayer.playerName = player.player_name;
                newPlayer.playingPiece = player.playing_piece;
                newPlayer.starTotal = player.star_total;
                newPlayer.moveTotal = player.move_total;               
                newPlayer.moveMod = player.move_mod;                 
                newPlayer.row = player.row;
                newPlayer.col = player.col;
                //newPlayer.hand = player.hand;
                newPlayer.cardEffect = player.card_effect;
                newPlayer.transform.position = player.position;
                newPlayer.direction = player.direction;
                newPlayer.isAI = player.is_ai;
                newPlayer.newGameStarted = player.new_game_started;
                newPlayer.canRollDice = player.can_roll_dice;
                newPlayer.loseTurn = player.lose_turn;
                newPlayer.ToggleMoveCountUI(false);

                //set up player hand
                newPlayer.hand = new List<CardData>();
                foreach(CardData card in player.hand)
                {
                    newPlayer.hand.Add(cm.cardTypes[card.cardID].card);
                }

                //sprite setup
                SpriteRenderer sr = newPlayer.GetComponent<SpriteRenderer>();
                sr.sprite = gm.pieceColors[newPlayer.playingPiece];
                //sr.sprite = newPlayer.playingPiece;

                gm.playerList.Add(newPlayer);

                //UI setup
                int i = gm.playerList.IndexOf(newPlayer);
                ui.playerPanels[i].TogglePanel(true);
                ui.playerPanels[i].player = newPlayer;
                ui.playerPanels[i].UpdatePlayerName(newPlayer);
                ui.playerPanels[i].UpdatePlayerStatus(newPlayer);
                ui.playerPanels[i].ToggleCardEffectUI(false);
                ui.playerPanels[i].cardEffectUI.Initialize();
                ui.playerPanels[i].ToggleArrow(false);

                if (newPlayer.cardEffect != null)
                {
                    //grab card data
                    ui.playerPanels[i].ToggleCardEffectUI(true);
                    if (newPlayer.cardEffect.cardID == 0) //swift movement ID
                    {
                        ui.playerPanels[i].cardEffectUI.SetCardDetails(GetText("swiftMove"), newPlayer.cardEffect.ability);
                    }
                    else
                    {
                        ui.playerPanels[i].cardEffectUI.SetCardDetails(newPlayer.cardEffect.cardName.ToUpper(), newPlayer.cardEffect.ability);
                    }
                }
                else
                    ui.playerPanels[i].ToggleCardEffectUI(false);
            }

            ui.UpdateLeadingPlayer();

            //board data array setup
            gm.boardID = gameData.board_id;

            Boards boardList = JsonUtility.FromJson<Boards>(gm.boardFile.text);
            gm.boardData = new List<string[]>();
            char[] delimiters = {',', ' '};

            for (int i = 0; i < boardList.boards[gm.boardID].rows.Length; i++)
            {
                string p = boardList.boards[gm.boardID].rows[i].row;
                gm.boardData.Add(p.Split(delimiters, System.StringSplitOptions.RemoveEmptyEntries));
            }

            //board space setup
            GameObject boardContainer = new GameObject();
            boardContainer.name = "Board Spaces";

            foreach(BoardSpaceData space in gameData.board_data)
            {
                BoardSpace newSpace = null;

                switch(space.space_type)
                {
                    case BoardSpace.SpaceType.Home:
                        newSpace = GameManager.Instantiate(gm.homeSpacePrefab, boardContainer.transform);
                        gm.homeSpace = newSpace;
                        break;

                    case BoardSpace.SpaceType.StarCache:
                        newSpace = GameManager.Instantiate(gm.starCachePrefab, boardContainer.transform);
                        if (newSpace.TryGetComponent(out StarCache cache))
                        {
                            //cache.probability = space.probability;
                            cache.SetProbability(space.probability);

                            if (cache.probability >= 0.7f)
                                cache.SetUIColor(gm.highProbabilityColor);
                            else if (cache.probability >= 0.4f)
                                cache.SetUIColor(gm.midProbabilityColor);
                            else
                                cache.SetUIColor(gm.lowProbabilityColor);
                        }
                        break;

                    case BoardSpace.SpaceType.DrawCard:
                        newSpace = GameManager.Instantiate(gm.drawCardPrefab, boardContainer.transform);
                        break;

                    case BoardSpace.SpaceType.DrawCard2:
                        newSpace = GameManager.Instantiate(gm.drawCardTwoPrefab, boardContainer.transform);
                        break;
                }

                newSpace.spaceType = space.space_type;
                newSpace.row = space.row;
                newSpace.col = space.col;
                newSpace.transform.position = space.board_position;

                //check if players are on this space
                foreach(Player player in gm.playerList)
                {
                    if (player.row == newSpace.row && player.col == newSpace.col)
                    {
                        player.currentSpace = newSpace;
                        newSpace.playersOnSpace.Add(player);
                        gm.playerIndex = gm.playerList.IndexOf(player); //need this for ArrangePlayerPositions
                        gm.ArrangePlayerPositions(gm.playerList, player);
                    }
                }
                gm.playerIndex = 0;
                gm.boardSpaceList.Add(newSpace);
            }

            //Homeless mod - toggle cross to false
            if (gm.homeSpace.TryGetComponent<Home>(out Home home))
            {
                home.ToggleCross(false);
            }

            //deck setup
            foreach(CardData card in gameData.deck)
            {
                cm.cards.Add(cm.cardTypes[card.cardID].card);
            }

            gm.currentRound = gameData.current_round;
            gm.maxRounds = gameData.max_round;
            gm.extraRound = gameData.extra_round;
            ui.UpdateTotalRounds(gm.maxRounds);

            //lessons
            LessonManager lm = LessonManager.instance;
            for(int i = 0; i < lm.lessonList.Length; i++)
            {
                lm.lessonList[i].lessonViewed = gameData.lessons_viewed[i];
            }
            gm.lessonViewedThisRound = gameData.lesson_viewed_this_round;
            gm.lessonIndex = gameData.lesson_index;

            //mini lessons
            for(int i = 0; i < lm.miniLessonList.Length; i++)
            {
                lm.miniLessonList[i].lessonViewed = gameData.mini_lessons_viewed[i];
            }
            gm.miniLessonIndex = gameData.mini_lesson_index;

            us.allLessonsViewed = gameData.all_lessons_viewed;

            //Update dice roll record & UI
            gm.totalRolls = gameData.total_rolls;
            for (int i = 0; i < gm.diceRollRecord.Length; i++)
            {
                gm.diceRollRecord[i] = gameData.dice_roll_record[i];
            }

            ui.totalRollsUIValue.text = gm.totalRolls.ToString();

            switch(ui.probabilityType)
            {
                case UI.ProbabilityType.Decimal:
                    for(int i = 0; i < ui.diceRollRecordUI.Length; i++)
                    {
                        //the value is rounded to 3 decimal places so we don't get long values going off screen.
                        float diceProb = Mathf.Round(((float)gm.diceRollRecord[i] / (float)gm.totalRolls) * 1000) / 1000.0f;
                        ui.diceRollRecordUI[i].text = diceProb.ToString();
                    }
                    break;
                
                case UI.ProbabilityType.Percent:
                    for(int i = 0; i < ui.diceRollRecordUI.Length; i++)
                    {
                        //the value is rounded to 1 decimal place so we don't get long values going off screen.
                        float diceProb = (float)gm.diceRollRecord[i] / (float)gm.totalRolls;
                        diceProb = Mathf.Round(diceProb * 100 * 10) / 10.0f;
                        ui.diceRollRecordUI[i].text = diceProb + "%";
                    }
                    break;

                case UI.ProbabilityType.Fraction:
                    for(int i = 0; i < ui.diceRollRecordUI.Length; i++)
                    {
                        ui.diceRollRecordUI[i].text = gm.diceRollRecord[i] + "/" + gm.totalRolls;
                    }
                    break;
            }

            //Update Card draw rate & UI
            //gameData.card_types = new CardManager.CardType[cm.cardTypes.Length];
            for (int i = 0; i < cm.cardTypes.Length; i++)
            {
                cm.cardTypes[i].amount = gameData.card_types[i].amount;
            }

            ui.totalCardsUIValue.text = cm.cards.Count.ToString();

            for(int i = 0; i < ui.cardDrawRatesUI.Length; i++)
            {
                float cardDrawProb = Mathf.Round((float)cm.cardTypes[i].amount / (float)cm.cards.Count * 1000) / 1000.0f;
                ui.cardDrawRatesUI[i].text = cardDrawProb.ToString();
            }

            //extra mode setup
            us.extraModeEnabled = gameData.extra_mode_enabled;
            ExtraModManager em = ExtraModManager.instance;
            if (us.extraModeEnabled)
            {
                ui.ToggleExtraModeContainer(true);
                int i = 0;
                bool modFound = false;
                while (!modFound && i < em.extraMods.Length)
                {
                    if (gm.boardID == em.extraMods[i].boardID)
                    {
                        modFound = true;
                        switch(gm.boardID)
                        {
                            case 0:
                                em.activeMod = ScriptableObject.CreateInstance<StarvingMod>();
                                em.activeMod.trapLocations = new List<Vector3>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                //ui.ToggleExtraModeContainer(true);
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 1:
                                em.activeMod = ScriptableObject.CreateInstance<JumpMod>();
                                em.activeMod.trapLocations = new List<Vector3>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;

                                //if (!gm.extraRound)
                                    //ui.ToggleJumpChanceText(true);
                                    ui.ToggleJumpChanceText(gm.extraRound == false ? true : false);
                                //else
                                    //ui.ToggleJumpChanceText(false);
                                ui.modNameText.text = em.activeMod.modName;
                                em.activeMod.jumpChance = gameData.jump_chance;
                                ui.UpdateJumpChance(em.activeMod.jumpChance);
                                break;
                            
                            case 2:
                                em.activeMod = ScriptableObject.CreateInstance<StunMod>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                //setup trap locations
                                if (em.activeMod.trapLocations == null)
                                    em.activeMod.trapLocations = new List<Vector3>();
                                for (int j = 0; j < gameData.trap_locations.Count; j++)
                                {
                                    em.activeMod.trapLocations.Add(gameData.trap_locations[j]);
                                    em.trapSprites.Add(GameObject.Instantiate(em.trapPrefab, gameData.trap_locations[j], Quaternion.identity));
                                } 
                                break;

                            case 3:
                                em.activeMod = ScriptableObject.CreateInstance<DrawCardMod>();
                                em.activeMod.trapLocations = new List<Vector3>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 4:
                                em.activeMod = ScriptableObject.CreateInstance<RareMod>();
                                em.activeMod.trapLocations = new List<Vector3>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;
                                break;

                            case 5:
                                em.activeMod = ScriptableObject.CreateInstance<HomelessMod>();
                                em.activeMod.trapLocations = new List<Vector3>();
                                em.activeMod.modName = GetText(em.extraMods[i].modNameKey);
                                em.activeMod.boardID = em.extraMods[i].boardID;
                                ui.modNameText.text = em.activeMod.modName;

                                //get Home space state
                                //home.canGetStars = gameData.can_get_stars;
                                //em.activeMod.Activate();    //this will check Home space
                                /*if (gm.homeSpace.TryGetComponent<Home>(out Home home))
                                {
                                    
                                    if (!home.canGetStars)
                                        home.ToggleCross(true);
                                    else
                                        home.ToggleCross(false);
                                }*/
                                break;
                        }
                        
                    }
                    else
                    {
                        i++; 
                    }
                }

            }
            else
            {
                ui.ToggleExtraModeContainer(false);
            }
            
            //TODO: Must send progress using LOL code. Check Cooking Example to see how to do that.
            LOLSDK.Instance.SubmitProgress(gm.playerList[0].starTotal, gm.currentRound, gm.maxRounds);


            //play music
            us.musicEnabled = gameData.music_enabled;
            if (us.musicEnabled)
                am.musicMain.Play();

            gm.SetGameState(GameManager.GameState.NewRound);
        }

        public void LoadState(Action<GameData> callback)
        {
            LOLSDK.Instance.LoadState<GameData>(state =>
            {
                if (state != null)
                {
                    callback(state.data);
                    //LOLSDK.Instance.SubmitProgress(state.data.star_total, state.data.current_round, state.data.max_round);
                }
                
            });
        }
            
    }
    

    //the following struct is used to save certain data so game can be resumed later.
    //when the game is resumed, it resumes from the beginning of the latest round.
    [Serializable]
    public class GameData
    {
        public int board_id;                         //used to set up board data array in Start method
        public List<PlayerData> player_data;        //should also save players' cards and star count
        public List<BoardSpaceData> board_data;
        public List<CardData> deck;
        public CardManager.CardType[] card_types;    //used for tracking remaining cards
        public int current_round, max_round;
        public bool extra_round;
        public int[] dice_roll_record;        //the number of times each value is rolled. Index 0 is 2, index 1 is 3, etc.
        public int total_rolls;
        public bool[] lessons_viewed;         //used to save viewed lessons.
        public bool[] mini_lessons_viewed;         //used to save viewed lessons.
        public bool lesson_viewed_this_round;
        public int lesson_index;
        public byte mini_lesson_index;
        public bool all_lessons_viewed;     //universal setting
        public bool music_enabled;

        //extra mode
        public bool extra_mode_enabled;
        public float jump_chance;           //only write this value if "Random Jumps" mod is active
        public List<Vector3> trap_locations;    
        public bool can_get_stars;          //for the Homeless mod       
    }

    [Serializable]
    public class PlayerData
    {
        public string player_name;
        public int playing_piece;           //reference to playing piece sprite
        public int star_total;               
        public int move_mod;                 //used with swift movement card ability
        public int move_total;              //in case player is affected by a Move card
        public int row, col;                //used to track location of player on board.
        public Vector3 position;
        public Vector3 direction;
        public List<BoardSpaceData> route;  //this is to prevent player from having to choose a path when game resumes.
        public List<CardData> hand;
        public CardData card_effect;
        public bool can_roll_dice;
        public bool is_ai;
        public bool new_game_started;       //used to prevent player from gaining a star while sitting on Home space. Defaults to true.        
        public bool lose_turn;
    }
    
    [Serializable]
    public class BoardSpaceData
    {
        public BoardSpace.SpaceType space_type;
        public int row, col;
        public float probability;   //only applies if space is a star cache.
        public Vector3 board_position;
    }

   

    public abstract class BoardSpace : MonoBehaviour
    {
        public enum SpaceType {Home, StarCache, DrawCard, DrawCard2}
        public SpaceType spaceType;
        public List<Player> playersOnSpace;     //record of players currently occupying the space. Max list size is 3.
        Vector3 altPositionOne, altPositionTwo;     //alternate positions for when there are more than 1 player on a space.
        int maxPlayersOnSpace {get;} = 3;
        public int row, col;                    //location of space in array.
        public byte weight;                     //used by AI. weight changes depending on player's status. Max weight is 10.
        public byte maxWeight {get;} = 10;

        public virtual void ActivateSpace(Player player){}      //triggers effect for the player that landed on the space
        public bool SpaceIsOccupied() { return (playersOnSpace.Count >= 1); }
        public void SetAlternatePositionOne(Player player)
        {
            altPositionOne = new Vector3(transform.position.x - 0.8f, transform.position.y + 0.5f, 0);
            player.transform.position = altPositionOne;
        }

        public bool AltPositionOneOccupied(Player player)
        {
            return player.transform.position == altPositionOne;
        }

        public void SetAlternatePositionTwo(Player player)
        {
            altPositionTwo = new Vector3(transform.position.x + 0.8f, transform.position.y + 0.5f, 0);
            player.transform.position = altPositionTwo;
        }

        public bool AltPositionTwoOccupied(Player player)
        {
            return player.transform.position == altPositionTwo;
        }

        public bool OriginalPositionOccupied(Player player)
        {
            return player.transform.position == transform.position;
        }


        public void SetDefaultPosition(Player player)
        {
            player.transform.position = transform.position;
        }
        
    }

}
