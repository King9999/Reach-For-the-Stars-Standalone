using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VectorGraphics;
using LoLSDK;
using MMurray.ReachForTheStars;

public class NumberCardManager : MonoBehaviour
{
    public NumberCard[] numberCards;
    public NumberCard[] flippedNumberCards;     //used by hidden card. Unity does not have a way to flip UI sprites.
    public NumberCard revealedCard, hiddenCard;
    SVGImage revealedCardImage, hiddenCardImage;
    public GameObject screenBackground;
    
    public enum PlayerChoice {Higher, Lower}
    public PlayerChoice choice;
    public bool humanPlayerEncountered;         //used to prevent mini lesson from showing if AI opponents encountered each other.

    [Header("---Number Card UI---")]
    public GameObject numberCardUIContainer;
    public Button highButton, lowButton;
    public TextMeshProUGUI titleBarText, instructionsText, lowestToHighestText;
    public TextMeshProUGUI attackerNameText, defenderNameText;        //designated attacker and defender
    public TextMeshProUGUI highButtonText, lowButtonText;               //used to update with probabilities
    public float highOdds, lowOdds;   //probability of the hidden card being high or low. Displayed next to button text
    [HideInInspector]public byte maxValue {get;} = 10;
    [HideInInspector]public byte minValue {get;} = 1;
    [HideInInspector]public byte totalOutcomes {get;} = 9;        //hidden value can never be the same as revealed value so there are only 9 outcomes


    public static NumberCardManager instance;
    GameManager gm;
    TutorialManager tm;

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
        revealedCardImage = revealedCard.GetComponent<SVGImage>();
        hiddenCardImage = hiddenCard.GetComponent<SVGImage>();
        gm = GameManager.instance;
        tm = TutorialManager.instance;
    }

    string GetText (string key, string playerName = "")
    {
        string value = SharedState.LanguageDefs?[key];
        return value + playerName ?? "--missing--";
    }

    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }

    public void StartHighLowGame(Player attacker, Player defender)
    {
        humanPlayerEncountered = true;
        ToggleNumberCardUIContainer(true);

        //set up attacker and defender. Attacker is always the current player
        attackerNameText.text = GetText("encounter_attacker", attacker.playerName);
        defenderNameText.text = GetText("encounter_defender", defender.playerName);

        //get 2 random number cards
        int randCard = Random.Range(0, numberCards.Length);
        revealedCardImage.sprite = numberCards[randCard].cardFace;
        revealedCard.value = numberCards[randCard].value;
        byte revealedValue = numberCards[randCard].value;

        do
        {
            randCard = Random.Range(0, flippedNumberCards.Length);
            hiddenCard.value = flippedNumberCards[randCard].value;
        }
        while(revealedCard.value == hiddenCard.value);
        byte hiddenValue = flippedNumberCards[randCard].value;

        //hide the hidden card        
        hiddenCard.cardFace = flippedNumberCards[randCard].cardFace;

        //play TTS
        UniversalSettings us = UniversalSettings.instance;
        if (us.ttsEnabled)
        {
            string[] keys = {"encounter_instructions", "encounter_lowToHigh"};
            StartCoroutine(PlayTTS(keys, 5.5f));
        }

        //get the odds
        //float highOdds = 0;
        //float lowOdds = 0;

        //get the probability format
        UI ui = UI.instance;
        switch(ui.probabilityType)
        {
            case UI.ProbabilityType.Decimal:
                highOdds = Mathf.Round((float)(maxValue - revealedValue) / (float)totalOutcomes * 100) / 100.0f;
                lowOdds = Mathf.Round((float)(revealedValue - minValue) / (float)totalOutcomes * 100) / 100.0f;
                highButtonText.text = GetText("highButtonText") + " (" + highOdds + ")";
                lowButtonText.text = GetText("lowButtonText") + " (" + lowOdds + ")";
                break;

            case UI.ProbabilityType.Fraction:
                //highOdds and lowOdds become numerator
                highOdds = (float)(maxValue - revealedValue);
                lowOdds = (float)(revealedValue - minValue); 
                float denominator = (float)totalOutcomes;
                highButtonText.text = GetText("highButtonText") + " (" + highOdds + "/" + denominator + ")";
                lowButtonText.text = GetText("lowButtonText") + " (" + lowOdds + "/" + denominator + ")";
                break;

            case UI.ProbabilityType.Percent:
                highOdds = Mathf.Round((float)(maxValue - revealedValue) / (float)totalOutcomes * 100 * 10) / 10.0f;
                lowOdds = Mathf.Round((float)(revealedValue - minValue) / (float)totalOutcomes * 100 * 10) / 10.0f;
                highButtonText.text = GetText("highButtonText") + " (" + highOdds + "%)";
                lowButtonText.text = GetText("lowButtonText") + " (" + lowOdds + "%)";
                break;
        }

        
    }

    public void ToggleNumberCardUIContainer(bool toggle)
    {
        numberCardUIContainer.gameObject.SetActive(toggle);
        screenBackground.gameObject.SetActive(toggle);  //not part of UI but is handled here for convenience
    }

    //flips hidden card from back to face
    IEnumerator FlipCardAndGetResult()
    {
        /*flip card by rotating Y axis 90 degrees. IMPORTANT NOTES:
        --Must use Transform.Rotate
        --Must change *euler angles*, not rotation
        --Going below 0 degrees *does not* put degrees into negative, unlike what the inspector tells you!*/

        float rotateSpeed = 128;
        do
        {
            float vy = -rotateSpeed * Time.deltaTime;
            hiddenCard.transform.Rotate(0, vy, 0);
            Debug.Log(hiddenCard.transform.eulerAngles);
            yield return null;
        }
        while (hiddenCard.transform.eulerAngles.y > 270);

        //change card sprite and rotate some more
        hiddenCardImage.sprite = hiddenCard.cardFace;

        do
        {
            float vy = -rotateSpeed * Time.deltaTime;
            hiddenCard.transform.Rotate(0, vy, 0);
            yield return null;
        }
        while (hiddenCard.transform.eulerAngles.y > 180);

        //change rotation and sprite back to normal
        yield return new WaitForSeconds(1);
        hiddenCard.transform.eulerAngles = Vector3.zero;
        hiddenCardImage.sprite = hiddenCard.cardBack;
        ToggleNumberCardUIContainer(false);

        //stop TTS in case it's still running
        ((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();

        //GameManager gm = GameManager.instance;
        if (tm == null)
            GetWinnerOfEncounter(gm.currentPlayer, gm.opponent);
        else
            GetWinnerOfEncounter(tm.currentPlayer, tm.opponent);
    }

    void GetWinnerOfEncounter(Player attacker, Player defender)
    {
        AudioManager am = AudioManager.instance;
        GameManager gm = GameManager.instance;
        UI ui = UI.instance;
        float delayDuration = 2f;
        float delaySoundDuration = 2;
        if (choice == PlayerChoice.Higher && hiddenCard.value > revealedCard.value || 
            choice == PlayerChoice.Lower && hiddenCard.value < revealedCard.value)
        {
            //player wins encounter. Are they attacker or defender?
            if (!attacker.isAI)
            {
                //take star from opponent. If no stars to take, then defender lose their cards/turn.
                if (defender.starTotal > 0)
                {
                    attacker.starTotal++;
                    defender.starTotal--;
                    am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starTaken", attacker.transform.position);
                    am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starStolen", defender.transform.position, delayDuration);
                }
                else
                {
                    if (defender.hand.Count > 0)
                    {
                        defender.hand.Clear();
                        am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
                        ui.DisplayFeedbackMessage("encounter_allCardsDiscarded", defender.transform.position);
                    }
                    else
                    {
                        defender.loseTurn = true;
                        defender.gotStarFromHome = defender.currentSpace == gm.homeSpace ? true : false;
                        am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
                        ui.DisplayFeedbackMessage("encounter_loseTurn", defender.transform.position);
                    }
                }
               
            }
            else if (!defender.isAI)
            {
                //attacker loses cards/turn
                if (attacker.hand.Count > 0)
                {
                    attacker.hand.Clear();
                    am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starsProtected", defender.transform.position);
                    am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_allCardsDiscarded", attacker.transform.position, delayDuration);
                }
                else
                {
                    //lose turn
                    attacker.loseTurn = true;
                    attacker.gotStarFromHome = attacker.currentSpace == gm.homeSpace ? true : false;
                    am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starsProtected", defender.transform.position);
                    am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_loseTurn", attacker.transform.position, delayDuration);
                }
            }
        }
        else //player guessed wrong
        {
            if (!attacker.isAI)
            {
                //lose cards/turn
                if (attacker.hand.Count > 0)
                {
                    attacker.hand.Clear();
                    am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starsProtected", defender.transform.position);
                    am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_allCardsDiscarded", attacker.transform.position, delayDuration);
                }
                else
                {
                    //lose turn
                    attacker.loseTurn = true;
                    attacker.gotStarFromHome = attacker.currentSpace == gm.homeSpace ? true : false;
                    am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starsProtected", defender.transform.position);
                    am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_loseTurn", attacker.transform.position, delayDuration);
                }
            }
            else if (!defender.isAI)
            {
                //opponent takes star from human player.
                if (defender.starTotal > 0)
                {
                    attacker.starTotal++;
                    defender.starTotal--;
                    am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starTaken", attacker.transform.position);
                    am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_starStolen", defender.transform.position, delayDuration);
                }
                else
                {
                    if (defender.hand.Count > 0)
                    {
                        defender.hand.Clear();
                        am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
                        ui.DisplayFeedbackMessage("encounter_allCardsDiscarded", defender.transform.position);
                    }
                    else
                    {
                        defender.loseTurn = true;
                        defender.gotStarFromHome = defender.currentSpace == gm.homeSpace ? true : false;   //prevents player from getting a star if they're on Home
                        am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
                        ui.DisplayFeedbackMessage("encounter_loseTurn", defender.transform.position);
                    }
                }
            }
        }

        //update UI
        //GameManager gm = GameManager.instance;
        if (tm == null)
        {
            for (int i = 0; i < gm.playerList.Count; i++)
            {
                ui.playerPanels[i].UpdatePlayerStatus(gm.playerList[i]);
            }
            ui.UpdateLeadingPlayer();
            gm.encounterEnded = true;

            //check for mini lesson
            /*LessonManager lm = LessonManager.instance;
            if (!lm.miniLessonList[11].lessonViewed)
            {
                gm.miniLessonIndex = 11;
                gm.SetGameState(GameManager.GameState.GetMiniLesson);
            }
            else if (!lm.miniLessonList[13].lessonViewed)
            {
                gm.miniLessonIndex = 13;
                gm.SetGameState(GameManager.GameState.GetMiniLesson);
            }*/
        }
        else
        {
            for (int i = 0; i < tm.playerList.Count; i++)
            {
                ui.playerPanels[i].UpdatePlayerStatus(tm.playerList[i]);
            }
            ui.UpdateLeadingPlayer();
            tm.encounterEnded = true;
        }
       
    }

    public void GetAIWinner(Player attacker, Player defender)
    {
        humanPlayerEncountered = false;
        AudioManager am = AudioManager.instance;
        GameManager gm = GameManager.instance;
        float delaySoundDuration = 2;
        //pick an AI winner at random
        UI ui = UI.instance; 
        ui.DisplayAlert("encounter_aiPlayingEncounterGame");
        Player winner, loser;
        if (Random.value <= 0.5f)
        {
            winner = attacker;
            loser = defender;
        }
        else
        {
            winner = defender;
            loser = attacker;
        }

        float delayDuration = 2;
        if (winner == attacker)    //current player is the attacker
        {
            //take star from opponent. If no stars to take, then defender lose their cards/turn.
            if (loser.starTotal > 0)
            {
                winner.starTotal++;
                loser.starTotal--;
                am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                ui.DisplayFeedbackMessage("encounter_starTaken", winner.transform.position);
                am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                ui.DisplayFeedbackMessage("encounter_starStolen", loser.transform.position, delayDuration);
            }
            else
            {
                if (loser.hand.Count > 0)
                {
                    loser.hand.Clear();
                    am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_allCardsDiscarded", loser.transform.position);
                }
                else
                {
                    loser.loseTurn = true;
                    loser.gotStarFromHome = loser.currentSpace == gm.homeSpace ? true : false;   //prevents player from getting a star if they're on Home
                    am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
                    ui.DisplayFeedbackMessage("encounter_loseTurn", loser.transform.position);
                }
            }
        }
        else if (loser == attacker)
        {
            //lose cards/turn
            if (loser.hand.Count > 0)
            {
                loser.hand.Clear();
                am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                ui.DisplayFeedbackMessage("encounter_starsProtected", winner.transform.position);
                am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                ui.DisplayFeedbackMessage("encounter_allCardsDiscarded", loser.transform.position, delayDuration);
            }
            else
            {
                //lose turn
                loser.loseTurn = true;
                loser.gotStarFromHome = loser.currentSpace == gm.homeSpace ? true : false;   //prevents player from getting a star if they're on Home
                am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                ui.DisplayFeedbackMessage("encounter_starsProtected", winner.transform.position);
                am.PlayDelayedSound(am.audioNoLuck, delaySoundDuration, am.soundVolume);
                ui.DisplayFeedbackMessage("encounter_loseTurn", loser.transform.position, delayDuration);
            }
        }

        //update UI
        //GameManager gm = GameManager.instance;
        for (int i = 0; i < gm.playerList.Count; i++)
        {
            ui.playerPanels[i].UpdatePlayerStatus(gm.playerList[i]);
        }
        ui.UpdateLeadingPlayer();
        gm.encounterEnded = true;
    }

    #region Buttons

    public void OnHighButtonClicked()
    {
        choice = PlayerChoice.Higher;
        Debug.Log("Guessed higher");
        StartCoroutine(FlipCardAndGetResult());
    }

    public void OnLowButtonClicked()
    {
        choice = PlayerChoice.Lower;
        Debug.Log("Guessed lower");
        StartCoroutine(FlipCardAndGetResult());
    }
    #endregion

    IEnumerator PlayTTS(string[] keys, float duration = 0)
    {
        int i = 0;
        while(i < keys.Length)
        {
            LOLSDK.Instance.SpeakText(keys[i]);
            yield return new WaitForSeconds(duration);
            i++;
        }
    }
}
