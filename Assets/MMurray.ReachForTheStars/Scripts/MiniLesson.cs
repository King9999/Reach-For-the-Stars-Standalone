using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MMurray.ReachForTheStars;
using LoLSDK;

/* These are smaller windows that are displayed when gameplay is occurring. */
public class MiniLesson : MonoBehaviour
{
    public TextMeshProUGUI lessonDetails;  //sidebar is used with image
    public string detailsKey;
    public Button closeButton;
    public byte lessonID;

    [Header("---Lesson Viewed?---")]
    public bool lessonViewed;

    //changes close button to different languages
    public void UpdateCloseButtonLanguage()
    {
        UniversalSettings us = UniversalSettings.instance;
        switch(lessonID)
        {
            case 6: case 9: case 11: case 14: case 16: case 17:
                closeButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("nextButtonText");
                break;
            
            default:
                closeButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("closeButtonText");
                break; 
        }
        /*if (lessonID == 6 || lessonID == 9 || lessonID == 11 || lessonID == 14)
            closeButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("nextButtonText");
        else 
            closeButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("closeButtonText");*/ 
    }

    /*string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }*/

    public void ToggleUIElements(bool toggle)
    {
        lessonDetails.gameObject.SetActive(toggle);
        //populate the lesson
        lessonDetails.text = UniversalSettings.instance.GetText(detailsKey);
        closeButton.gameObject.SetActive(false);
        StartCoroutine(AnimateText(0.032f, lessonDetails.text));
    }

    public void OnCloseButtonClicked()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        //first we check if the lesson has been viewed previously
        UI ui = UI.instance;

        gameObject.SetActive(false);
        

        //stop TTS
        //((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();
        StopAllCoroutines();
       
        
        //move to next player's turn. Special case if certain lessons are active
        GameManager gm = GameManager.instance;
        LessonManager lm = LessonManager.instance;
        AssessmentGame ag = AssessmentGame.instance;

        switch(lessonID)
        {
            case 2: //Home lesson
                //check if player was still moving when they were stopped
                if (gm.currentPlayer.playerMoving)
                    gm.gameState = GameManager.GameState.PlayerMoving;  //if I called SetGameState the movement would be reset
                else
                    gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                break;
            
            case 4: //card phase lesson
                //player is viewing card phase for first time and hasn't taken turn yet. Go back go card phase.
                gm.gameState = GameManager.GameState.CardPhase;
                break;
            
            case 5: //card effect lesson.
                ui.ToggleMiniLessonUIContainer(false);              //closing card effect indicator.
                gm.SetGameState(GameManager.GameState.RollingDice); //player just used a card, now they move.
                break;

            case 6: //rolling dice lesson
                //go to next page
                lm.ToggleMiniLesson(7, true);
                break;
            
            case 7:
                gm.gameState = GameManager.GameState.RollingDice;
                ui.ToggleMiniLessonUIContainer(false);              //closing dice roll record indicator.
                break;

            case 9: //high/low game lesson
                //go to next page
                lm.ToggleMiniLesson(10, true);
                break;

            case 10:
                //start the high/low game
                NumberCardManager ncm = NumberCardManager.instance;
                gm.gameState = GameManager.GameState.StartEncounter;
                ncm.StartHighLowGame(gm.currentPlayer, gm.opponent);
                break;
            
            case 11: //end of encounter lesson.
                //go to next page
                lm.ToggleMiniLesson(12, true);
                //gm.SetGameState(GameManager.GameState.CheckingSpace);
                break;

            case 12: //end of encounter lesson.
            case 13:
                gm.gameState = GameManager.GameState.StartEncounter;   //this will allow the music to change back to normal upon entering checking space state.
                gm.SetGameState(GameManager.GameState.CheckingSpace);
                break;

            case 14: //end of probability format lesson page 1.
                //go to next page
                lm.ToggleMiniLesson(15, true);
                break;

            case 15:    //end of probability format lesson
                ui.ToggleProbabilityFormatIndicator(false);              //closing dice roll record indicator.
                gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                break;

            case 16: //end of assessment intro page 1.
                //go to next page
                lm.ToggleMiniLesson(17, true);
                break;
            
            case 17: //end of assessment intro page 2.
                //go to next page
                lm.ToggleMiniLesson(18, true);
                break;

            case 18: //end of assessment intro
                gm.SetGameState(GameManager.GameState.StartAssessment);
                break;

            case 19: //end of assessment                
                gm.SetGameState(GameManager.GameState.NewRound);
                break;

            case 20:    //incorrect answer of assessment, re-roll
            case 21:    //all values equal, re-roll                
                ag.ToggleGameContainer(true);
                ag.ToggleQuestions(false);
                ag.ToggleDice(true);
                ui.ToggleProbabilityFormatIndicator(false);
                break;

            case 22:    //EX round 
                gm.SetGameState(GameManager.GameState.NewRound);
                break;
            
            default:
                gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                break;
            
        }
        
    }

#region Coroutines
    //Displays lesson details text one letter at a time. Should not run again once the text is fully displayed.
    IEnumerator AnimateText(float scrollSpeed, string textToAnimate)
    {
        List<string> copy = new List<string>();
        int i = 0;
        string p = "";
        while (i < textToAnimate.Length)
        {
            //if there's a color tag, the entire tag must be treated as one character so the entire tag is displayed at once.
            if (textToAnimate.Substring(i, 1).Equals("<"))
            {
                //keep incrementing i until we reach the end of tag
                string tag = "";
                do
                {
                    tag += textToAnimate.Substring(i, 1);
                    i++;
                }
                while(!textToAnimate.Substring(i, 1).Equals(">"));
                tag += textToAnimate.Substring(i++, 1); //adding the >
                copy.Add(tag);
            }
            else
            {
                p = textToAnimate.Substring(i, 1);
                copy.Add(p);
                //Debug.Log(textToAnimate.Substring(i, 1)); 
                i++;
            }
        }

        /*i = 0;
        while(i < copyList.Count)
        {
            Debug.Log(copyList[i]);
            i++;
        }*/
               
        lessonDetails.text = "";
        
        i = 0;
        while (i < copy.Count)
        {           
            lessonDetails.text += copy[i];
            i++;
            yield return new WaitForSeconds(scrollSpeed);
        }

        //show close button.
        yield return AnimateCloseButton();
        //closeButton.gameObject.SetActive(true);
    }

    IEnumerator AnimateCloseButton()
    {
        //scale begins at 0, then increses to 1
        closeButton.gameObject.SetActive(true);
        Vector3 originalScale = new Vector3(closeButton.transform.localScale.x, closeButton.transform.localScale.y, 0);
        Vector3 destinationScale = closeButton.transform.localScale * 1.2f;
        Vector3 secondaryScale = closeButton.transform.localScale;
        closeButton.transform.localScale = Vector3.zero;

        float scaleSpeed = 2;
        yield return ScaleCloseButton(scaleSpeed, destinationScale.x);
        yield return ScaleCloseButtonSecondary(scaleSpeed, secondaryScale.x, destinationScale.x);
        closeButton.transform.localScale = Vector3.one;
    }

    IEnumerator ScaleCloseButton(float scaleSpeed, float maxScale)
    {
        while(closeButton.transform.localScale.x < maxScale)
        {
            float vx = closeButton.transform.localScale.x + scaleSpeed * Time.deltaTime;
            float vy = closeButton.transform.localScale.y + scaleSpeed * Time.deltaTime;
            closeButton.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
    }

    //bounce effect. Scales down, then back up briefly. 
    IEnumerator ScaleCloseButtonSecondary(float scaleSpeed, float minScale, float maxScale)
    {
        //scale down
        while(closeButton.transform.localScale.x > minScale)
        {
            float vx = closeButton.transform.localScale.x - scaleSpeed * Time.deltaTime;
            float vy = closeButton.transform.localScale.y - scaleSpeed * Time.deltaTime;
            closeButton.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }

        //scale up
        while(closeButton.transform.localScale.x < maxScale)
        {
            float vx = closeButton.transform.localScale.x + scaleSpeed * Time.deltaTime;
            float vy = closeButton.transform.localScale.y + scaleSpeed * Time.deltaTime;
            closeButton.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
    }

    #endregion

}
