using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;
using TMPro;

/* Players have a chance to acquire a star on this space. Every time a star is obtained, the chance to gain a star on the same
space is reduced by half. If the probability is under 10%, it instead becomes 0. 
Any time a player doesn't get a star, the probability increases by 0.2 (20%) */
public class StarCache : BoardSpace
{
    public float probability;          //begins at 1 
    public TextMeshProUGUI probabilityUI;

    // Start is called before the first frame update
    void Start()
    {
        //probability = 1;

        //clamp UI so that it appears over the space
        Vector3 newPos = new Vector3(transform.position.x, transform.position.y - 0.5f, 0);
        Vector3 probUIPos = Camera.main.WorldToScreenPoint(newPos);   //attach to an avatar object
        //probUIPos = new Vector3(probUIPos.x, probUIPos.y / 1.125f, 0);
        probabilityUI.transform.position = probUIPos;
        probabilityUI.text = probability.ToString();
    }

    //Attempt to get a star from a cache
   public override void ActivateSpace(Player player)
    {
        if (player.gotStarFromCache) return;

        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        UI ui = UI.instance;
        AudioManager am = AudioManager.instance;

        if (tm == null)
        {
            if (gm.extraRound)
            {
                player.starTotal++;
                player.gotStarFromCache = true;
                //ui.DisplayFeedbackMessage("GOT A STAR!", player.transform.position);
                ui.DisplayFeedbackMessage("starGet", player.transform.position);
                ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
                ui.UpdateLeadingPlayer();
                am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                return;
            }
        }
        
        if (Random.value <= probability)
        {
            //check for extra mode "Trapped Caches"
            ExtraModManager em = ExtraModManager.instance;
            if (em.activeMod != null && em.activeMod.boardID == 2)
            {
                em.activeMod.ActivateSecondary();
            }
            else
            {
                player.starTotal++;
                player.gotStarFromCache = true;
                am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
                ui.DisplayFeedbackMessage("starGet", player.transform.position);
                if (tm == null)
                    ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
                else
                    ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(tm.currentPlayer);
            
                    
                ui.UpdateLeadingPlayer();
            }
            //check for extra mode mod "Star-ving"
            if (em.activeMod != null && em.activeMod.boardID == 0)
            {
                em.activeMod.Activate();
            }
            else
            {
                probability *= 0.5f;
                if (probability < 0.1f)
                    probability = 0;

                //show feedback
                //ui.DisplayFeedbackMessage("GOT A STAR!", player.transform.position);
                //ui.DisplayFeedbackMessage("starGet", player.transform.position);
                
                /*if (tm == null)
                    ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
                else
                    ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(tm.currentPlayer);
                    
                ui.UpdateLeadingPlayer();*/
                

                //display tutorial when player lands on cache for first time in the game
                if (tm == null)
                {
                    LessonManager lm = LessonManager.instance;
                    if (!lm.miniLessonList[0].lessonViewed && gm.playerIndex == 0 /*&& probability == 0.5f*/)
                    {
                        gm.miniLessonIndex = 0;
                        gm.SetGameState(GameManager.GameState.GetMiniLesson);
                    }

                    if (!lm.miniLessonList[0].gameObject.activeSelf && !lm.miniLessonList[8].lessonViewed && gm.playerIndex == 0 && probability == 0.25f)
                    {
                        gm.miniLessonIndex = 8;
                        gm.SetGameState(GameManager.GameState.GetMiniLesson);
                    }
                    else
                    {
                        lm.miniLessonList[8].lessonViewed = false;
                    }
                }
            }
        }
        else
        {
            //check for mod "Rarer stars"
            ExtraModManager em = ExtraModManager.instance;
            if (em.activeMod != null && em.activeMod.boardID == 4)
            {
                em.activeMod.ActivateSecondary();
            }
            else
            {
                probability += 0.2f;
                if (probability > 1)
                    probability = 1;
            }
            
            //show mini lesson
            if (tm == null)
            {
                LessonManager lm = LessonManager.instance;
                if (!lm.miniLessonList[3].lessonViewed && gm.playerIndex == 0)
                {
                    gm.miniLessonIndex = 3;
                    gm.SetGameState(GameManager.GameState.GetMiniLesson);
                }

            }
            
            //ui.DisplayFeedbackMessage("NO LUCK...", player.transform.position);
            am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
            ui.DisplayFeedbackMessage("noStar", player.transform.position);
        }

        //update UI
        //Debug.Log("Decimal places: " + DecimalPlaces(probability));
        SetProbability(probability, ui.probabilityType);
    }

    public void ToggleProbabilityUI(bool toggle)
    {
        probabilityUI.gameObject.SetActive(toggle);
    }

    public void SetUIColor(Color color)
    {
        probabilityUI.color = color;
    }

    public void SetProbability(float value)
    {
        if (value > 1 || value < 0) return;

        probability = value;
        probabilityUI.text = probability.ToString();
    }

    public void SetProbability(float value, UI.ProbabilityType probType)
    {
        if (value > 1 || value < 0) return;

        probability = value;

        switch(probType)
        {
             case UI.ProbabilityType.Decimal:
                probability = Mathf.Round(probability * 1000) / 1000.0f;
                probabilityUI.text = probability.ToString();    
                break;

            case UI.ProbabilityType.Percent:
                probabilityUI.text = (Mathf.Round(probability * 100 * 10) / 10.0f) + "%";
                break;

            case UI.ProbabilityType.Fraction:
                //float numerator = 1;    //probability value divided by itself
                //float denominator = float.IsInfinity(1 / probability) ?  0 : Mathf.Round(1 / probability * 10) / 10.0f;
                //probabilityUI.text = (denominator == 0) ? "0" : numerator + "/" + denominator;

                //doing a different (better) conversion that will give whole numbers
                float numerator = (float)DecimalPlaces(probability) <= 0 ? 1 : 10 * (float)DecimalPlaces(probability);
                float denominator = float.IsInfinity(1 / probability) ? 0 : numerator;
                numerator *= probability;

                 //simplify fraction
                int i = (int)denominator;       //greatest common denominator
                int a = (int)numerator;
                while (i > 0)
                {
                    int rem = a % i;
                    a = i;
                    i = rem;
                }

                numerator /= a;
                denominator /= a;

                //round numerator in case we get a large number
                numerator = Mathf.Round(numerator * 100) / 100.0f;
                probabilityUI.text = (float.IsInfinity(1 / probability)) ? "0" : numerator + "/" + denominator;
                break;
        }
    }

    //returns number of decimal places by converting a float to a string and then counting every digit
    //after the decimal.
    int DecimalPlaces(float value)
    {
        int decimalPlaces = 0;

        string probString = value.ToString();
        bool decimalFound = false;
        for (int i = 0; i < probString.Length; i++)
        {
            //Debug.Log(probString[i]);
            if (probString[i].Equals('.'))
            {
                decimalFound = true;
                continue;
            }

            if (decimalFound)
            {
                decimalPlaces++;
            }
        }

        return decimalPlaces;
    }
}
