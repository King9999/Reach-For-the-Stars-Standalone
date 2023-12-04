using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//used in assessment game.
public class AnswerButton : MonoBehaviour
{
    public Button answerButon;
    public TextMeshProUGUI buttonText;
    //public float probability;
    public int storedValue;

   
    public void OnAnswerSelected()
    {
        AssessmentGame ag = AssessmentGame.instance;
       //ag.selectedAnswer = storedValue;
        //Debug.Log("Selected " + ag.selectedAnswer);

        ag.GetResult(storedValue);
    }
}
