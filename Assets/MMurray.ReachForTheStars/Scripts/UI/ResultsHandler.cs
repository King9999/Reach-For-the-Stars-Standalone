using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using MMurray.ReachForTheStars;

/* This script deals with displaying the winner of the game. It also deals with draw games. */
public class ResultsHandler : MonoBehaviour
{
    public TextMeshProUGUI resultLabelUI;       //used for both the winner and draw games
    string resultTextDraw, resultTextWin;
    public ParticleSystem confetti;

   /*string GetText (string key, string playerName = "")
   {
        string value = SharedState.LanguageDefs?[key];
        return playerName + value ?? "--missing--";
   }*/

    public void ToggleResultsHandler(bool toggle)
    {
        resultLabelUI.gameObject.SetActive(toggle);
    }

    public void DisplayWinner(string winnerName)
    {
        StartCoroutine(AnimateWinner(winnerName));
    }

    public void DisplayDraw()
    {
        StartCoroutine(AnimateDraw());
    }

    /* The winner's name pops in from center position and increases in scale up to a certain point. A "bounce"(?) effect is applied after the
    scaling is done. Afterwards, particle effect plays. */
    IEnumerator AnimateWinner(string winnerName)
    {
        //scale begins at 0, then increses to 1.5
        Vector3 originalScale = new Vector3(resultLabelUI.transform.localScale.x, resultLabelUI.transform.localScale.y, 0);
        Vector3 destinationScale = resultLabelUI.transform.localScale * 1.4f;
        Vector3 secondaryScale = resultLabelUI.transform.localScale * 1.2f;
        resultLabelUI.transform.localScale = Vector3.zero;
        resultLabelUI.text = winnerName + UniversalSettings.instance.GetText("showWinner");//, winnerName); /*winnerName + " is the winner!"*/;

        float scaleSpeed = 4;
        yield return ScaleWinnerLabel(scaleSpeed, destinationScale.x);
        yield return ScaleWinnerLabelSecondary(scaleSpeed, secondaryScale.x, destinationScale.x);

        //if player didn't win, show tips
        yield return new WaitForSeconds(2);
        GameManager gm = GameManager.instance;
        if (gm.playerList[0].playerName != winnerName)
        {
            LessonManager lm = LessonManager.instance;
            lm.ToggleLesson(4, true);
            //disable other UI
            UI ui = UI.instance;
            ui.ToggleEndGameUIContainer(false);
            ToggleResultsHandler(false);
        }
    }

    //text fades in
    IEnumerator AnimateDraw()
    {
        float alpha = 0;
        resultLabelUI.color = new Color(resultLabelUI.color.r, resultLabelUI.color.g, resultLabelUI.color.b, alpha);
        resultLabelUI.text = UniversalSettings.instance.GetText("drawGame"); //"Draw..."*/;
        float speed = 2;

        while (resultLabelUI.color.a < 1)
        {
            alpha = resultLabelUI.color.a + speed * Time.deltaTime;
            resultLabelUI.color = new Color(resultLabelUI.color.r, resultLabelUI.color.g, resultLabelUI.color.b, alpha);
            yield return null;
        }

        //if player didn't win, show tips
        yield return new WaitForSeconds(2);
        LessonManager lm = LessonManager.instance;
        lm.ToggleLesson(4, true);
        //disable other UI
        UI ui = UI.instance;
        ui.ToggleEndGameUIContainer(false);
        ToggleResultsHandler(false); 
    }

    IEnumerator ScaleWinnerLabel(float scaleSpeed, float maxScale)
    {
        while(resultLabelUI.transform.localScale.x < maxScale)
        {
            float vx = resultLabelUI.transform.localScale.x + scaleSpeed * Time.deltaTime;
            float vy = resultLabelUI.transform.localScale.y + scaleSpeed * Time.deltaTime;
            resultLabelUI.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
    }

    //bounce effect. Scales down, then back up briefly. 
    IEnumerator ScaleWinnerLabelSecondary(float scaleSpeed, float minScale, float maxScale)
    {
        //scale down
        while(resultLabelUI.transform.localScale.x > minScale)
        {
            float vx = resultLabelUI.transform.localScale.x - scaleSpeed * Time.deltaTime;
            float vy = resultLabelUI.transform.localScale.y - scaleSpeed * Time.deltaTime;
            resultLabelUI.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }

        //scale up
        while(resultLabelUI.transform.localScale.x < maxScale)
        {
            float vx = resultLabelUI.transform.localScale.x + scaleSpeed * Time.deltaTime;
            float vy = resultLabelUI.transform.localScale.y + scaleSpeed * Time.deltaTime;
            resultLabelUI.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
    }
}
