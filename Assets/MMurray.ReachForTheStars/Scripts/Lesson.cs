using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MMurray.ReachForTheStars;
using LoLSDK;

/* These are windows that appear at fixed points in the game. They provide information
to the student. */
public class Lesson : MonoBehaviour
{
    public TextMeshProUGUI lessonTitle, lessonDetails, lessonSidebar;  //sidebar is used with image
    public string titleKey, detailsKey, sidebarKey;
    public Button closeButton;
    public Image lessonImage;       //this is optional
    public byte lessonID;
    [Header("---Display Settings---")]
    public bool sidebarEnabled;     //if true, image is also enabled, and details is disabled
    public bool detailsEnabled = true;
    public bool imageEnabled;
    public int roundToDisplayLesson;    //lesson is displayed at the end of the round matching the value.
    [Header("---Lesson Viewed?---")]
    public bool lessonViewed; 

    //coroutine
    IEnumerator animateText;        

    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }

    //changes close button to different languages
    public void UpdateCloseButtonLanguage()
    {
        closeButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("closeButtonText"); 
    }

    public void ToggleUIElements(bool detailsEnabled = true, bool sidebarEnabled = false, bool imageEnabled = false)
    {
        /*if (sidebarEnabled)
        {
            detailsEnabled = false;
        }*/

        lessonDetails.gameObject.SetActive(detailsEnabled);
        lessonSidebar.gameObject.SetActive(sidebarEnabled);
        lessonImage.gameObject.SetActive(imageEnabled);

        //populate the lesson
        lessonTitle.text = GetText(titleKey);
        lessonDetails.text = GetText(detailsKey);
        lessonSidebar.text = GetText(sidebarKey);
        //image is populated in Inspector if applicable

        //animate text
        if (Time.timeScale == 1)
        {
            closeButton.gameObject.SetActive(false);
            StartCoroutine(AnimateText(0.032f, lessonDetails.text));
        }
        else
        {
            closeButton.gameObject.SetActive(true);
        }
    }

    public void OnCloseButtonClicked()
    {
        TutorialManager tm = TutorialManager.instance;
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        //first we check if the lesson has been viewed previously
        UI ui = UI.instance;
        if (tm == null)
        {
            if (lessonID < 3 && ui.lessonButtons[lessonID].lessonAlreadyViewed)
            {
                gameObject.SetActive(false);

                //show window
                ui.viewedLessonsContainer.gameObject.SetActive(true);
            }
            else
            {
                //move to new round
                GameManager gm = GameManager.instance;
                gm.SetGameState(GameManager.GameState.NewRound);
                gameObject.SetActive(false);
            }

            //stop TTS
            ((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();
            StopAllCoroutines();
        }
        else
        {
            //move to next turn
            if (lessonID != 6)  //we don't change turns on the intro tutorial
                tm.SetGameState(TutorialManager.GameState.NextPlayerTurn);
            else
                tm.SetGameState(TutorialManager.GameState.NewRound);

            ((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();
            StopAllCoroutines();
            gameObject.SetActive(false);
        }
    }

    //used only if player pressed help button in tutorial to open the lesson
    public void OnCloseHelpScreen()
    {
        Time.timeScale = 1;
        UI ui = UI.instance;
        ui.TogglePauseText(false);
        ((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    //used at the end of the game, and only if player lost the game
    public void OnCloseButtonClickedEndGame()
    {
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        ((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();
        StopAllCoroutines();
        gameObject.SetActive(false);

        //enable end game UI
        UI ui = UI.instance;
        ui.ToggleEndGameUIContainer(true);
        ui.resultsHandler.ToggleResultsHandler(true);
    }

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
}
