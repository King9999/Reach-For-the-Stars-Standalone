using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SimpleJSON;
using LoLSDK;

//This is the script for all UI in the game scene.
namespace MMurray.ReachForTheStars
{
    public class UI : MonoBehaviour
    {
        [Serializable]
        public struct PlayerStatsUI
        {
            public TextMeshProUGUI PlayerNameUI;
            public TextMeshProUGUI StarTotalUI;
            public TextMeshProUGUI CardTotalUI;
        }

        
        public enum ProbabilityType {Decimal, Percent, Fraction, End}   //End is used to mark the final enum and is not used in the game
        [Header("---Probability Display---")]
        public ProbabilityType probabilityType;         //used to display probabiity as different values to the player.
        public TextMeshProUGUI probabilityFormatUI;    //shows the player which format the game is currently displaying.

        [Header("---Board UI---")]
        public TextMeshProUGUI alertUI;                 //used to display general messages to player.
        public GameObject alertUIContainer;
        public TextMeshProUGUI currentRoundUI;           //displays current round UI
        public TextMeshProUGUI totalRoundsUI;           //displays max rounds.
        public TextMeshProUGUI feedbackUI;              //for displaying board results to player.
        public TextMeshProUGUI[] feedbackUIs;
        public bool[] feedbackUIActive;
        public RoundHandler roundHandler;
        public ResultsHandler resultsHandler;

        [Header("---Player UI---")]
        public GameObject[] playerUIContainer;            //holds player stats. Can use it to turn UI on and off at once.
        public PlayerStatsUI[] playerStatsUI;
        public PlayerPanel[] playerPanels;
        public GameObject newTurnUIContainer;           //holds UI for when a player's turn begins
        public Button selectRollDiceButton;
        public Button selectPlayCardButton;

        [Header("---Dice UI---")]
        public GameObject diceUIContainer;
        public TextMeshProUGUI moveModValueUI;          //disabled by default
        public Button rollDiceButton;
        public Button rollDiceBackButton;               //returns to beginning new turn state
        public TextMeshProUGUI diceValueUI;             //displays dice result
        public GameObject diceRollRecordContainer;      //holds all UI for displaying dice roll record.
        public TextMeshProUGUI[] diceRollRecordUI;      //contains record of all dice values rolled as well as the total rolls.
        public TextMeshProUGUI totalRollsUIValue;
        public TextMeshProUGUI totalRollsText;
        [HideInInspector]public bool diceRollRecordContainerToggle;

        [Header("---Card UI---")]
        public GameObject cardUIContainer;              //UI for viewing and selecting cards to play
        public GameObject cardDrawRatesContainer;     //UI for seeing draw rates for each card.
        public TextMeshProUGUI[] cardDrawRatesUI;       //displays remaining cards of each type in deck. The index corresponds to the card ID variable in CardData
        public TextMeshProUGUI totalCardsText;
        public TextMeshProUGUI totalCardsUIValue;
        [HideInInspector]public bool cardDrawRatesContainerToggle;
        public Button playCardButton;
        public Button skipCardButton;               //skips card phase
        public Button discardButton;
        public Button backButton;                   //returns to card selection
        public Image textBackground;
        public TextMeshProUGUI cardNameTextUI;
        public TextMeshProUGUI abilityTextUI;
        public TextMeshProUGUI tipTextUI;
        public string[] cardNameText, abilityText, tipText;  //gets text from language.json. index corresponds to card ID
        public GameObject discardUIContainer;
        public TextMeshProUGUI discardCardUIValue;

        [Header("---Viewed Lessons UI---")]
        [HideInInspector]public bool viewedLessonsContainerToggle;
        public GameObject viewedLessonsContainer;
        public LessonButton[] lessonButtons;              //each index corresponds to the lesson ID.
        bool cardUIenabled = false;                       //restores card UI after closing viewed lesson.
        bool alertUIenabled = false;
        bool endGameUIEnabled = false;
        public TextMeshProUGUI pauseText, resumeGameText;

        [Header("---Tutorial UI---")]
        public TextMeshProUGUI homeHelpText; 
        public TextMeshProUGUI starCacheHelpText; 
        public TextMeshProUGUI drawCardHelpText; 
        public TextMeshProUGUI encounterHelpText;
        public TextMeshProUGUI cardsHelpText;
        public Button exitTutorialButton;

        [Header("---Mini Lesson UI---")]
        public GameObject miniLessonUIContainer;
        public Arrow diceRollRecordButtonIndicator;
        public Arrow cardEffectIndicator;
        public Arrow ProbabilityFormatIndicator;

        //[Header("---Player Targeting UI---")]
        //public GameObject targetPlayerUIContainer;
        //public Button[] targetButtons;              //displays names of valid targets.

        [Header("---End Game UI---")]
        public GameObject endGameUIContainer;
        public Button restartGameButton;
        public Button exitGameButton;

        [Header("---Extra Mode UI---")]
        public GameObject extraModeUIContainer;
        public TextMeshProUGUI modNameText;
        public TextMeshProUGUI jumpChanceText;

        //singleton
        AudioManager am;
        public static UI instance;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        void Start()
        {
            am = AudioManager.instance;
        }

        /*string GetText (string key)
        {
            string value = SharedState.LanguageDefs?[key];
            return value ?? "--missing--";
        }*/

        /*string GetText (string key, string playerName)
        {
            string value = SharedState.LanguageDefs?[key];
            value = value.Trim('A', 'I', 'O', 'p', 'p', 'o', 'n', 'e', 'n', 't'); //removing these letters so it doesn't appear in game, but TTS will read
            return playerName + value ?? "--missing--";
        }*/

        /*string GetText (string key, int number)
        {
            string value = SharedState.LanguageDefs?[key];
            return value + number ?? "--missing--";
        }*/
        
        //called in ReadState() in GameClasses.cs. Roll dice button is updated in Dice.cs
        public void UpdateLanguage()
        {
            UniversalSettings us = UniversalSettings.instance;
            playCardButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("playCardButtonText");
            skipCardButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("skipCardButtonText");
            discardButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("discardButtonText");
            backButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("backButtonText");
            restartGameButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("restartGameButtonText");
            exitGameButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("exitGameButtonText");
            selectRollDiceButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("selectRollDiceButtonText");
            selectPlayCardButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("playCardButtonText");
            rollDiceBackButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("backButtonText");
            pauseText.text = us.GetText("gamePaused");
            resumeGameText.text = us.GetText("unpauseGame");
            totalRollsText.text = us.GetText("totalRolls");
            totalCardsText.text = us.GetText("totalCards");
            
            //tutorial stuff
            TutorialManager tm = TutorialManager.instance;
            if (tm != null)
            {
                exitTutorialButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("returnTitleButtonText");
                homeHelpText.text = us.GetText("tutorial_homeName");
                starCacheHelpText.text = us.GetText("tutorial_starCacheName");
                drawCardHelpText.text = us.GetText("tutorial_drawCardName");
                encounterHelpText.text = us.GetText("tutorial_encounter");
                cardsHelpText.text = us.GetText("tutorial_cards");
            }
           
        


            //round handler
            roundHandler.UpdateLanguage();

            //updating buttons for number card manager here just to be consistent and keep everything in one spot
            NumberCardManager ncm = NumberCardManager.instance;
            ncm.highButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("highButtonText");
            ncm.lowButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("lowButtonText");
            ncm.lowestToHighestText.text = us.GetText("encounter_lowToHigh");
            ncm.instructionsText.text = us.GetText("encounter_instructions");
            ncm.titleBarText.text = us.GetText("encounter_title");

            //card text
            CardManager cm = CardManager.instance;
            for (int i = 0; i < cm.cardTypes.Length; i++)
            {
                switch(cm.cardTypes[i].card.cardID)
                {
                    case 0: //swift movement
                        cardNameText[i] = us.GetText("cardName_0");
                        abilityText[i] = us.GetText("ability_0");
                        tipText[i] = us.GetText("tip_0");
                        cm.cardTypes[i].card.nameKey = "cardName_0";
                        cm.cardTypes[i].card.abilityKey = "ability_0";
                        cm.cardTypes[i].card.tipKey = "tip_0";
                        break;

                    case 1: //Move 1
                        cardNameText[i] = us.GetText("cardName_1");
                        abilityText[i] = us.GetText("ability_1");
                        tipText[i] = us.GetText("tip_1");
                        cm.cardTypes[i].card.nameKey = "cardName_1";
                        cm.cardTypes[i].card.abilityKey = "ability_1";
                        cm.cardTypes[i].card.tipKey = "tip_1";
                        break;

                    case 2: //Move 2
                        cardNameText[i] = us.GetText("cardName_2");
                        abilityText[i] = us.GetText("ability_2");
                        tipText[i] = us.GetText("tip_1");
                        cm.cardTypes[i].card.nameKey = "cardName_2";
                        cm.cardTypes[i].card.abilityKey = "ability_2";
                        cm.cardTypes[i].card.tipKey = "tip_1";
                        break;

                    case 3: //Move 3
                        cardNameText[i] = us.GetText("cardName_3");
                        abilityText[i] = us.GetText("ability_3");
                        tipText[i] = us.GetText("tip_1");
                        cm.cardTypes[i].card.nameKey = "cardName_3";
                        cm.cardTypes[i].card.abilityKey = "ability_3";
                        cm.cardTypes[i].card.tipKey = "tip_1";
                        break;

                    case 4: //Extend
                        cardNameText[i] = us.GetText("cardName_4");
                        abilityText[i] = us.GetText("ability_4");
                        tipText[i] = us.GetText("tip_4");
                        cm.cardTypes[i].card.nameKey = "cardName_4";
                        cm.cardTypes[i].card.abilityKey = "ability_4";
                        cm.cardTypes[i].card.tipKey = "tip_4";
                        break;

                    case 5: //Refill
                        cardNameText[i] = us.GetText("cardName_5");
                        abilityText[i] = us.GetText("ability_5");
                        tipText[i] = us.GetText("tip_5");
                        cm.cardTypes[i].card.nameKey = "cardName_5";
                        cm.cardTypes[i].card.abilityKey = "ability_5";
                        cm.cardTypes[i].card.tipKey = "tip_5";
                        break;

                    case 6: //Go Home
                        cardNameText[i] = us.GetText("cardName_6");
                        abilityText[i] = us.GetText("ability_6");
                        tipText[i] = us.GetText("tip_6");
                        cm.cardTypes[i].card.nameKey = "cardName_6";
                        cm.cardTypes[i].card.abilityKey = "ability_6";
                        cm.cardTypes[i].card.tipKey = "tip_6";
                        break;

                    case 7: //Jump
                        cardNameText[i] = us.GetText("cardName_7");
                        abilityText[i] = us.GetText("ability_7");
                        tipText[i] = us.GetText("tip_7");
                        cm.cardTypes[i].card.nameKey = "cardName_7";
                        cm.cardTypes[i].card.abilityKey = "ability_7";
                        cm.cardTypes[i].card.tipKey = "tip_7";
                        break;
                }
            }
            
        }

        public void DisplayAlert(string key)
        {
            alertUIContainer.gameObject.SetActive(true);
            alertUI.text = UniversalSettings.instance.GetText(key);
            //UniversalSettings us = UniversalSettings.instance;
            //if (us.ttsEnabled)
                //LOLSDK.Instance.SpeakText(key);
        }

        public void DisplayAlert(string key, string playerName)
        {
            UniversalSettings us = UniversalSettings.instance;
            alertUIContainer.gameObject.SetActive(true);
            //alertUI.text = GetText(key, playerName);
            string text = us.GetText(key).Trim('A', 'I', 'O', 'p', 'p', 'o', 'n', 'e', 'n', 't'); //removing these letters so it doesn't appear in game
            alertUI.text = playerName + text;

            //if (us.ttsEnabled)
               // LOLSDK.Instance.SpeakText(key);
        }

        public void DisplayAlert(string key, int value)
        {
            UniversalSettings us = UniversalSettings.instance;
            alertUIContainer.gameObject.SetActive(true);
            //alertUI.text = GetText(key, value);
            alertUI.text = us.GetText(key) + value;

            //if (us.ttsEnabled)
            //LOLSDK.Instance.SpeakText(key);
        }

        public void UpdatePlayerStaus(int playerIndex, int starTotal, int cardCount)
        {
            playerStatsUI[playerIndex].StarTotalUI.text = "x " + starTotal;
            playerStatsUI[playerIndex].CardTotalUI.text = "x " + cardCount;
        }

        public void DisplayFeedbackMessage(string key, Vector3 textPos, float delayDuration = 0)
        {
            int i = 0;
            if (feedbackUIActive[i] == false)
                StartCoroutine(AnimateFeedbackUI(key, textPos, i));
            else
                StartCoroutine(AnimateFeedbackUI(key, textPos, i + 1, delayDuration));
        }

        public void DisplayDiceValue(int amount)
        {
            diceValueUI.gameObject.SetActive(true);
            diceValueUI.text = amount.ToString();
        }

        public void UpdateCurrentRound(int round)
        {
            currentRoundUI.text = round.ToString();
        }

        public void UpdateTotalRounds(int round)
        {
            totalRoundsUI.text = round.ToString();
        }

        public void UpdateMoveModValue(int value)
        {
            moveModValueUI.text = "+" + value.ToString();
        }

        //places a crown on player UI icon if they have the most stars
        public void UpdateLeadingPlayer()
        {
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;
            Player leadingPlayer;

            if (tm == null)
                leadingPlayer = gm.playerList[gm.playerIndex].GetLeadingPlayer(gm.playerList);
            else
                leadingPlayer = tm.playerList[tm.playerIndex].GetLeadingPlayer(tm.playerList);

            foreach(PlayerPanel panel in playerPanels)
            {
                panel.ToggleCrown(false);
                if (leadingPlayer == panel.player)
                {
                    panel.ToggleCrown(true);
                }
            }
            
        }

        #region Button Methods

        //change roll button text depending on game state
        public void ChangeRollButtonText(string key)
        {
            rollDiceButton.GetComponentInChildren<TextMeshProUGUI>().text = UniversalSettings.instance.GetText(key);
        }

        public void OnSelectRollDiceButtonClicked()
        {
            ToggleNewTurnUIContainer(false);
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;

            if (tm == null)
            {
                gm.SetGameState(GameManager.GameState.RollingDice);
            }
            else
                tm.SetGameState(TutorialManager.GameState.RollingDice);
        }

        public void OnSelectPlayCardButtonClicked()
        {
            ToggleNewTurnUIContainer(false);
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;

            if (tm == null)
                gm.SetGameState(GameManager.GameState.CardPhase);
            else
                tm.SetGameState(TutorialManager.GameState.CardPhase);
        }

        public void OnBackButtonClicked()
        {
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;

            if (tm == null)
            {
                if (gm.gameState == GameManager.GameState.GetMiniLesson) return;    //prevents interaction if lesson is open
                gm.SetGameState(GameManager.GameState.BeginningNewTurn);
            }
            else
                tm.SetGameState(TutorialManager.GameState.BeginningNewTurn);
        }
        #endregion

#region UI Toggles

        public void ToggleExtraModeContainer(bool toggle)
        {
            extraModeUIContainer.gameObject.SetActive(toggle);
            //modNameText.gameObject.SetActive(toggle);
            jumpChanceText.gameObject.SetActive(false);
        }

        public void ToggleJumpChanceText(bool toggle)
        {
            jumpChanceText.gameObject.SetActive(toggle);
        }

        public void UpdateJumpChance(float value)
        {
            value = Mathf.Round(value * 100 * 10) / 10.0f;
            jumpChanceText.text = UniversalSettings.instance.GetText("extraMode_jumpChance") + " " + value + "%";
        }
        public void ToggleMiniLessonUIContainer(bool toggle)
        {
            miniLessonUIContainer.gameObject.SetActive(toggle);
            if (toggle == false)
            {
                cardEffectIndicator.gameObject.SetActive(false);
                diceRollRecordButtonIndicator.gameObject.SetActive(false);
                ProbabilityFormatIndicator.gameObject.SetActive(false);
            }
        }

        public void ToggleDiceRollRecordIndicator(bool toggle)
        {
            miniLessonUIContainer.gameObject.SetActive(toggle);
            diceRollRecordButtonIndicator.gameObject.SetActive(toggle);
        }

        public void ToggleCardEffectIndicator(bool toggle)
        {
            miniLessonUIContainer.gameObject.SetActive(toggle);
            cardEffectIndicator.gameObject.SetActive(toggle);
        }

        public void ToggleProbabilityFormatIndicator(bool toggle)
        {
            miniLessonUIContainer.gameObject.SetActive(toggle);
            ProbabilityFormatIndicator.gameObject.SetActive(toggle);
        }

        public void ToggleNewTurnUIContainer(bool toggle)
        {
            newTurnUIContainer.gameObject.SetActive(toggle);
        }
        public void ToggleMoveModValue(bool toggle)
        {
            moveModValueUI.gameObject.SetActive(toggle);
        }

        public void TogglePlayerUI(int player, bool toggle)
        {
            if (player < 0 || player > 2) return;
            playerUIContainer[player].gameObject.SetActive(toggle);
        }

        public void ToggleAlertUI(bool toggle)
        {
            alertUIContainer.gameObject.SetActive(toggle);
        }

        public void ToggleFeedBackUI(bool toggle)
        {
            feedbackUI.gameObject.SetActive(toggle);
        }

        public void ToggleFeedBackUI(int index, bool toggle)
        {
            feedbackUIs[index].gameObject.SetActive(toggle);
        }

        public void ToggleDiceContainerUI(bool toggle)
        {
            diceUIContainer.gameObject.SetActive(toggle);

            //hide the other UI elements by default
            ToggleMoveModValue(false);
            ToggleDiceValueUI(false);
            ToggleRollButton(false);
        }

        public void ToggleDiceValueUI(bool toggle)
        {
            diceValueUI.gameObject.SetActive(toggle);
        }

        public void ToggleRollButton(bool toggle)
        {
            rollDiceButton.gameObject.SetActive(toggle);
        }

        public void ToggleBackButton(bool toggle)
        {
            backButton.gameObject.SetActive(toggle);
        }

        public void TogglePauseText(bool toggle)
        {
            pauseText.gameObject.SetActive(toggle);
        }

        public void ToggleRollDiceBackButton(bool toggle)
        {
            rollDiceBackButton.gameObject.SetActive(toggle);
        }

        public void TogglePlayCardButton(bool toggle)
        {
            playCardButton.gameObject.SetActive(toggle);
        }

        public void ToggleSkipCardButton(bool toggle)
        {
            skipCardButton.gameObject.SetActive(toggle);
        }

        public void ToggleCardUIContainer(bool toggle)
        {
            cardUIContainer.gameObject.SetActive(toggle);
            //playCardButton.gameObject.SetActive(toggle);
            //skipCardButton.gameObject.SetActive(toggle);
            textBackground.gameObject.SetActive(false);     //this is always false until a card is highlighted

            cardNameTextUI.gameObject.SetActive(toggle);
            cardNameTextUI.text = "";

            abilityTextUI.gameObject.SetActive(toggle);
            abilityTextUI.text = "";

            tipTextUI.gameObject.SetActive(toggle);
            tipTextUI.text = "";

            //toggle background
            CardManager cm = CardManager.instance;
            cm.background.gameObject.SetActive(toggle);

            //play card button text colour defaults to grey to indicate that button can't be pressed yet
            TextMeshProUGUI buttonText = playCardButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.color = Color.grey;

            //extra steps if we're discarding cards
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;

            if (tm == null)
            {
                if (!gm.currentPlayer.isAI)
                {
                    if (gm.gameState == GameManager.GameState.PlayerDiscardingCards)
                    {
                        //show a discard button
                        discardButton.gameObject.SetActive(true);
                    }
                    else
                    {
                        discardButton.gameObject.SetActive(false);
                        backButton.gameObject.SetActive(false);         //only appears when backing out of playing a card
                        playCardButton.gameObject.SetActive(toggle);
                        skipCardButton.gameObject.SetActive(toggle);
                    }
                }
            }
            else
            {
                if (!tm.currentPlayer.isAI)
                {
                    if (tm.gameState == TutorialManager.GameState.PlayerDiscardingCards)
                    {
                        //show a discard button
                        discardButton.gameObject.SetActive(true);
                    }
                    else
                    {
                        discardButton.gameObject.SetActive(false);
                        backButton.gameObject.SetActive(false);         //only appears when backing out of playing a card
                        playCardButton.gameObject.SetActive(toggle);
                        skipCardButton.gameObject.SetActive(toggle);
                    }
                }
            }
        }

        public void OnTTSButtonClicked()
        {
            UniversalSettings us = UniversalSettings.instance;
            us.ttsEnabled = !us.ttsEnabled;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
        }

        public void OnMusicButtonClicked()
        {
            UniversalSettings us = UniversalSettings.instance;
            us.musicEnabled = !us.musicEnabled;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);

            //play music
            if (us.musicEnabled)
            {
                GameManager gm = GameManager.instance;
                if (gm.gameState == GameManager.GameState.StartEncounter)
                {
                    am.musicEncounter.Play();
                }
                else
                {
                    am.musicMain.Play();
                }
            }
            else
            {
                am.musicMain.Stop();
                am.musicEncounter.Stop();
            }
        }

        
        //called by button press
        public void ToggleDiceRollRecordContainer()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            if (Time.timeScale == 0) return;    //button does nothing if game is paused.
            diceRollRecordContainerToggle = !diceRollRecordContainerToggle;
            diceRollRecordContainer.gameObject.SetActive(diceRollRecordContainerToggle);

            //remove any other windows
            if (diceRollRecordContainerToggle == true)
            {
                cardDrawRatesContainerToggle = false;
                cardDrawRatesContainer.gameObject.SetActive(cardDrawRatesContainerToggle);

                viewedLessonsContainerToggle = false;
                viewedLessonsContainer.gameObject.SetActive(viewedLessonsContainerToggle);
            }
        }

        //called by button press
        public void ToggleCardDrawRateContainer()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            if (Time.timeScale == 0) return;    //button does nothing if game is paused.
            cardDrawRatesContainerToggle = !cardDrawRatesContainerToggle;
            cardDrawRatesContainer.gameObject.SetActive(cardDrawRatesContainerToggle);

            //remove other windows
            if (cardDrawRatesContainerToggle == true)
            {
                diceRollRecordContainerToggle = false;
                diceRollRecordContainer.gameObject.SetActive(diceRollRecordContainerToggle);

                viewedLessonsContainerToggle = false;
                viewedLessonsContainer.gameObject.SetActive(viewedLessonsContainerToggle);
            }
        }

        //called by button press
        public void ToggleViewedLessonsContainer()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            //button is disabled if a lesson is already on screen
            int i = 0;
            bool lessonEnabled = false;
            LessonManager lm = LessonManager.instance;
            while (i < lm.lessonList.Length && !lessonEnabled)
            {
                if (lm.lessonList[i].gameObject.activeSelf) 
                {
                    lessonEnabled = true;
                }
                else
                {
                    i++;
                }
            }

            if (lessonEnabled) return;

            CardManager cm = CardManager.instance;
            GameManager gm = GameManager.instance;
            viewedLessonsContainerToggle = !viewedLessonsContainerToggle;
            viewedLessonsContainer.gameObject.SetActive(viewedLessonsContainerToggle);

            //remove other windows
            if (viewedLessonsContainerToggle == true)
            {
                diceRollRecordContainerToggle = false;
                diceRollRecordContainer.gameObject.SetActive(diceRollRecordContainerToggle);

                cardDrawRatesContainerToggle = false;
                cardDrawRatesContainer.gameObject.SetActive(cardDrawRatesContainerToggle);

                 //update the button text in the windows
                foreach(LessonButton button in lessonButtons)
                {
                    button.CheckViewedStatus();
                }

                //hide other UI
                if (cardUIContainer.gameObject.activeSelf)
                {
                    cardUIenabled = true;
                    ToggleCardUIContainer(false);
                    cm.DisplayHand(gm.currentPlayer, false);
                }

                if (alertUIContainer.gameObject.activeSelf)
                {
                    alertUIenabled = true;
                    ToggleAlertUI(false);
                }

                if (endGameUIContainer.gameObject.activeSelf)
                {
                    endGameUIEnabled = true;
                    ToggleEndGameUIContainer(false);
                    resultsHandler.ToggleResultsHandler(false);
                }

                //pause game
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
                if (cardUIenabled)
                {
                    cardUIenabled = false;
                    ToggleCardUIContainer(true);
                    cm.DisplayHand(gm.currentPlayer, true);
                }

                if (alertUIenabled)
                {
                    alertUIenabled = false;
                    ToggleAlertUI(true);
                }

                if (endGameUIEnabled)
                {
                    endGameUIEnabled = false;
                    resultsHandler.ToggleResultsHandler(true);
                    ToggleEndGameUIContainer(true);
                }
            }

           
        }

        //called by button press
        public void ToggleProbabilityType()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            if ((int)probabilityType + 1 >= (int)ProbabilityType.End)
            {
                probabilityType = ProbabilityType.Decimal;
            }
            else
            {
                probabilityType++;
            }

            //update format UI
            //probabilityFormatUI.text = probabilityType.ToString();

            /****update various UI values****/
            GameManager gm = GameManager.instance;
            CardManager cm = CardManager.instance;
            UniversalSettings us = UniversalSettings.instance;

            switch(probabilityType)
            {
                case ProbabilityType.Decimal:
                    //star cache UI
                    probabilityFormatUI.text = us.GetText("format_decimal");
                    foreach(BoardSpace space in gm.boardSpaceList)
                    {
                        if (space.TryGetComponent(out StarCache starCache))
                        {
                            starCache.SetProbability(starCache.probability, ProbabilityType.Decimal);
                        }
                    }
                    //draw card rate UI
                    for (int j = 0; j < cardDrawRatesUI.Length; j++)
                    {
                        float cardDrawProb = Mathf.Round((float)cm.cardTypes[j].amount / (float)cm.cards.Count * 1000) / 1000.0f;
                        cardDrawRatesUI[j].text = float.IsNaN(cardDrawProb) ? "0" : cardDrawProb.ToString();
                    }

                    //dice values UI
                    for(int i = 0; i < diceRollRecordUI.Length; i++)
                    {
                        //the value is rounded to 3 decimal places so we don't get long values going off screen.
                        float diceProb = Mathf.Round(((float)gm.diceRollRecord[i] / (float)gm.totalRolls) * 1000) / 1000.0f;
                        diceRollRecordUI[i].text = float.IsNaN(diceProb) ? "0" : diceProb.ToString();
                    }

                    //high/low game
                    if (gm.gameState == GameManager.GameState.StartEncounter)
                    {
                        NumberCardManager nm = NumberCardManager.instance;
                        nm.highOdds = Mathf.Round((float)(nm.maxValue - nm.revealedCard.value) / (float)nm.totalOutcomes * 100) / 100.0f;
                        nm.lowOdds = Mathf.Round((float)(nm.revealedCard.value - nm.minValue) / (float)nm.totalOutcomes * 100) / 100.0f;
                        nm.highButtonText.text = us.GetText("highButtonText") + " (" + nm.highOdds + ")";
                        nm.lowButtonText.text = us.GetText("lowButtonText") + " (" + nm.lowOdds + ")";
                    }
                    break;

                case ProbabilityType.Percent:
                    probabilityFormatUI.text = us.GetText("format_percent");
                    foreach(BoardSpace space in gm.boardSpaceList)
                    {
                        if (space.TryGetComponent(out StarCache starCache))
                        {
                            starCache.SetProbability(starCache.probability, ProbabilityType.Percent);
                        }
                    }

                    for (int j = 0; j < cardDrawRatesUI.Length; j++)
                    {
                        float cardDrawProb = (float)cm.cardTypes[j].amount / (float)cm.cards.Count;
                        cardDrawProb = float.IsNaN(cardDrawProb) ? 0 : Mathf.Round(cardDrawProb * 100 * 10) / 10.0f;
                        cardDrawRatesUI[j].text = cardDrawProb + "%";
                    }

                    for(int i = 0; i < diceRollRecordUI.Length; i++)
                    {
                        //the value is rounded to 1 decimal place so we don't get long values going off screen.
                        float diceProb = (float)gm.diceRollRecord[i] / (float)gm.totalRolls;
                        diceProb = float.IsNaN(diceProb) ? 0 : Mathf.Round(diceProb * 100 * 10) / 10.0f;
                        diceRollRecordUI[i].text = diceProb + "%";
                    }

                    //high/low game
                    if (gm.gameState == GameManager.GameState.StartEncounter)
                    {
                        NumberCardManager nm = NumberCardManager.instance;
                        nm.highOdds = Mathf.Round((float)(nm.maxValue - nm.revealedCard.value) / (float)nm.totalOutcomes * 100 * 10) / 10.0f;
                        nm.lowOdds = Mathf.Round((float)(nm.revealedCard.value - nm.minValue) / (float)nm.totalOutcomes * 100 * 10) / 10.0f;
                        nm.highButtonText.text = us.GetText("highButtonText") + " (" + nm.highOdds + "%)";
                        nm.lowButtonText.text = us.GetText("lowButtonText") + " (" + nm.lowOdds + "%)";
                    }
                    break;

                case ProbabilityType.Fraction:
                    probabilityFormatUI.text = us.GetText("format_fraction");
                    foreach(BoardSpace space in gm.boardSpaceList)
                    {
                        if (space.TryGetComponent(out StarCache starCache))
                        {
                            starCache.SetProbability(starCache.probability, ProbabilityType.Fraction);
                        }
                    }

                    for (int j = 0; j < cardDrawRatesUI.Length; j++)
                    {
                        cardDrawRatesUI[j].text = cm.cards.Count <= 0 ? "0" : cm.cardTypes[j].amount + "/" + cm.cards.Count;
                    }

                    for(int i = 0; i < diceRollRecordUI.Length; i++)
                    {
                        diceRollRecordUI[i].text = gm.totalRolls <= 0 ? "0" : gm.diceRollRecord[i] + "/" + gm.totalRolls;
                    }

                    //high/low game
                    if (gm.gameState == GameManager.GameState.StartEncounter)
                    {
                        NumberCardManager nm = NumberCardManager.instance;
                         //highOdds and lowOdds become numerator
                        nm.highOdds = (float)(nm.maxValue - nm.revealedCard.value);
                        nm.lowOdds = (float)(nm.revealedCard.value - nm.minValue); 
                        float denominator = (float)nm.totalOutcomes;
                        nm.highButtonText.text = us.GetText("highButtonText") + " (" + nm.highOdds + "/" + denominator + ")";
                        nm.lowButtonText.text = us.GetText("lowButtonText") + " (" + nm.lowOdds + "/" + denominator + ")";
                    }
                    break;
            }
           
        }

        //change text colour of play card button if player selects a card
        public void ChangePlayCardButtonColor(Color color)
        {
            TextMeshProUGUI buttonText = playCardButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.color = color;
        }

        public void DisplayCardText(bool toggle, string cardName = "", string ability = "", string tip = "")
        {
            //display background and card text whenever toggle is true
            textBackground.gameObject.SetActive(toggle);
            if (toggle == true)
            {
                cardNameTextUI.gameObject.SetActive(toggle);
                cardNameTextUI.text = cardName;

                abilityTextUI.gameObject.SetActive(toggle);
                abilityTextUI.text = ability;

                tipTextUI.gameObject.SetActive(toggle);
                tipTextUI.text = tip;
            } 
        }

        public void ToggleEndGameUIContainer(bool toggle)
        {
            endGameUIContainer.gameObject.SetActive(toggle);
        }
        #endregion

    #region Coroutines

        //animates the feedback UI. Also handles changing the game state when coroutine is finished.
        IEnumerator AnimateFeedbackUI(string key, Vector3 textPos, int feedbackIndex = 0, float delayDuration = 0)
        {
            //ToggleFeedBackUI(true);
            feedbackUIActive[feedbackIndex] = true;


            yield return new WaitForSeconds(delayDuration);


            //UI travels a short distance upwards, then fades.
            Vector3 newTextPos = Camera.main.WorldToScreenPoint(textPos);
            feedbackUIs[feedbackIndex].transform.position = new Vector3(newTextPos.x, newTextPos.y + 20, 0);
            feedbackUIs[feedbackIndex].text = UniversalSettings.instance.GetText(key);
            ToggleFeedBackUI(feedbackIndex, true);

            Vector3 destinationPos = new Vector3(newTextPos.x, newTextPos.y + 40, 0);


            float moveSpeed = 80;
            while (feedbackUIs[feedbackIndex].transform.position.y < destinationPos.y)
            {
                float vy = feedbackUIs[feedbackIndex].transform.position.y + moveSpeed * Time.deltaTime;
                feedbackUIs[feedbackIndex].transform.position = new Vector3(feedbackUIs[feedbackIndex].transform.position.x, vy, 0);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            //begin fade
            while(feedbackUIs[feedbackIndex].alpha > 0)
            {
                feedbackUIs[feedbackIndex].alpha -= Time.deltaTime;
                yield return null;
            }

            ToggleFeedBackUI(feedbackIndex, false);
            feedbackUIs[feedbackIndex].alpha = 1;
            feedbackUIActive[feedbackIndex] = false;

            //change game state. If player passed through Home but did not stop there, can't change game state.
            //both UI must be inactive before swithcing game state
            while (feedbackUIActive[0] == true || feedbackUIActive[1] == true)
                yield return null;

            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;

            if (tm == null)
            {
                if (gm.gameState != GameManager.GameState.EndGame)
                {
                    if (!gm.extraRound)
                    {
                        if ((gm.gameState == GameManager.GameState.NextPlayerTurn || gm.gameState == GameManager.GameState.NewRound) && gm.currentPlayer.loseTurn)
                        {
                            gm.currentPlayer.loseTurn = false;
                            gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                        }

                        if (gm.gameState == GameManager.GameState.CheckingSpace)
                        {
                            gm.SetGameState(GameManager.GameState.NextPlayerTurn);     
                        }

                        if (gm.gameState == GameManager.GameState.ActivateCard)
                            gm.SetGameState(GameManager.GameState.RollingDice);

                        if (gm.gameState == GameManager.GameState.StartEncounter && gm.encounterEnded)
                        {
                            LessonManager lm = LessonManager.instance;
                            NumberCardManager ncm = NumberCardManager.instance;
                            //the second condition is to prevent lesson from appearing if AI encountered each other.
                            if (!lm.miniLessonList[11].lessonViewed && ncm.humanPlayerEncountered)   
                            {
                                gm.miniLessonIndex = 11;
                                gm.SetGameState(GameManager.GameState.GetMiniLesson);
                            }
                            else if (!lm.miniLessonList[13].lessonViewed && ncm.humanPlayerEncountered)
                            {
                                gm.miniLessonIndex = 13;
                                gm.SetGameState(GameManager.GameState.GetMiniLesson);
                            }
                            else
                                gm.SetGameState(GameManager.GameState.CheckingSpace);
                        }       
                    }
                    else
                    {
                        if ((gm.gameState == GameManager.GameState.NextPlayerTurn || gm.gameState == GameManager.GameState.NewRound) && gm.currentPlayer.loseTurn)
                        {
                            gm.currentPlayer.loseTurn = false;
                            gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                        }

                        if (gm.gameState == GameManager.GameState.CheckingSpace)
                            gm.SetGameState(GameManager.GameState.NextPlayerTurn);

                        if (gm.gameState == GameManager.GameState.StartEncounter && gm.encounterEnded)
                            gm.SetGameState(GameManager.GameState.CheckingSpace);

                    }
                }
            }
            else
            {
                
                if ((tm.gameState == TutorialManager.GameState.NextPlayerTurn || tm.gameState == TutorialManager.GameState.NewRound) && tm.currentPlayer.loseTurn)
                {
                    tm.currentPlayer.loseTurn = false;
                    tm.SetGameState(TutorialManager.GameState.NextPlayerTurn);
                }

                if (tm.gameState == TutorialManager.GameState.CheckingSpace)
                    tm.SetGameState(TutorialManager.GameState.NextPlayerTurn);

                if (tm.gameState == TutorialManager.GameState.ActivateCard)
                    tm.SetGameState(TutorialManager.GameState.RollingDice);

                if (tm.gameState == TutorialManager.GameState.StartEncounter && tm.encounterEnded)
                    tm.SetGameState(TutorialManager.GameState.CheckingSpace);
              
            }
             
        }
    #endregion
    }
}
