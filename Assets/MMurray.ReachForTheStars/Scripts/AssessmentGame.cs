using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;
using UnityEngine.UI;
using TMPro;
using LoLSDK;

/* This appears halfway through the game to see if the player understands the concepts. It's a short minigame where the player rolls dice 3 times
and must figure out the highest or lowest value, or if all values are equal. There will be an indicator to help the player understand which values
are "less likely" and which ones are "more likely." */
public class AssessmentGame : MonoBehaviour
{
    public GameObject gameContainer;        //contains all game elements.
    int die1, die2;
    public bool diceIsRolling;     //controls the rolling of dice
    public Sprite[] diceSprites;
    public GameObject[] dieObjects;     //index 0 is die 1, 1 is die 2
    SpriteRenderer dieOneSr;
    SpriteRenderer dieTwoSr;
    int diceValue;              //the resulting dice roll plus any movement mods.
    byte rollCount;              //tracks number of rolls made. Max is 3.
    byte maxRolls {get;} = 3;
    public ParticleSystem diceResultParticle;
    public GameObject questionsContainer;   //also includes probability range slider.

    [Header("---Question & Answer---")]
    public List<int> rolledValues;     //record of dice values and their probabilities
    public List<float> rolledProbs;
    public List<int> correctAnswers;    //there can be more than 1 answer.
    public int selectedAnswer;          //when player clicks AnswerButton, its value is stored here
    public enum Question {HighestValue, LowestValue, End}
    public Question question;

    [Header("---UI---")]
    public Button rollButton;
    public TextMeshProUGUI diceValueText, instructionText;
    public TextMeshProUGUI sliderValue, sliderZeroLabel, sliderMidLabel, sliderOneLabel;
    public TextMeshProUGUI[] diceResultValueUI, diceProbValueUI;
    public TextMeshProUGUI rollResultsText, rollResultsValueText, diceProbText;    //labels
    public Slider probRangeSlider;  //will be used by player to help them understand the values.
    //public Button[] answerButtons;

    //coroutine check
    bool animateDiceValueCoroutineOn;

    public AnswerButton[] answerButtons;

    public static AssessmentGame instance;

    void Awake()
    {
        if (instance != this && instance != null)
        {
            Destroy(instance);
            return;
        }
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        dieOneSr = dieObjects[0].GetComponent<SpriteRenderer>();
        dieTwoSr = dieObjects[1].GetComponent<SpriteRenderer>();
        diceIsRolling = true;
        rollCount = 0;

        //ui init
        diceValueText.text = "";
        instructionText.text = "";
        for (int i = 0; i < diceResultValueUI.Length; i++)
        {
            diceResultValueUI[i].text = "";
            diceProbValueUI[i].text = "";
        }

        probRangeSlider.value = 0.5f;
        sliderValue.text = probRangeSlider.value.ToString();

        animateDiceValueCoroutineOn = false;
        UpdateLanguage();
        ToggleQuestions(false);

        //activate TTS
        UniversalSettings us = UniversalSettings.instance;
        if (us.ttsEnabled)
            LOLSDK.Instance.SpeakText("assessmentGame_rollInstruction");
    }

    // Update is called once per frame
    void Update()
    {
        if (dieObjects[0].gameObject.activeSelf)
            RollDice(diceIsRolling);

        if (probRangeSlider.gameObject.activeSelf)
            UpdateSliderValues();   
    }

    public void UpdateLanguage()
    {
        UniversalSettings us = UniversalSettings.instance;
        rollButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("rollDiceButtonText");
        instructionText.text = us.GetText("assessmentGame_rollInstruction") + (maxRolls - rollCount);
        rollResultsText.text = us.GetText("assessmentGame_rollResultsLabel");
        rollResultsValueText.text = us.GetText("assessmentGame_rollResultsValueLabel");
        diceProbText.text = us.GetText("assessmentGame_diceProbLabel");
    }

    /*string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }*/

    /*string GetText (string key, int number)
    {
        string value = SharedState.LanguageDefs?[key];
        return value + number ?? "--missing--";
    }*/

    void ToggleRollButton(bool toggle)
    {
        rollButton.gameObject.SetActive(toggle);
    }

    //this must run in an update loop
    public void RollDice(bool diceIsRolling)
    {
        if (diceIsRolling == true)
        {
            die1 = Random.Range(1, 7);
            die2 = Random.Range(1, 7);
            //Debug.Log(die1 + "," + die2);

            //show dice on screen
            dieOneSr.sprite = diceSprites[die1 - 1];
            dieTwoSr.sprite = diceSprites[die2 - 1];
  
        }
    }

    //called when rollButton is clicked.
    public void StopDice()
    {
        GameManager gm = GameManager.instance;
        if (animateDiceValueCoroutineOn || gm.gameState != GameManager.GameState.StartAssessment) return;

        diceIsRolling = false;
        diceValue = die1 + die2;
        //diceValue = 12;
        Debug.Log("Rolled " + diceValue);

        //record result and then roll again if there are rolls remaining.
        //if (rollCount < maxRolls)
        //{
            diceResultValueUI[rollCount].text = diceValue.ToString();

            //get the probability and convert based on player settings. The conversion is random to make it a little challenging
            float probability = DiceProbability(diceValue);
            Debug.Log(probability);

            //record probability before conversion. this step is important to get accurate answers.
            rolledValues.Add(diceValue);
            rolledProbs.Add(probability);

            UI ui = UI.instance;
            int randConversion = Random.Range(0, (int)UI.ProbabilityType.End);
            switch((UI.ProbabilityType)randConversion)
            {
                case UI.ProbabilityType.Decimal:
                    probability = Mathf.Round(DiceProbability(diceValue) * 1000) / 1000.0f;
                    diceProbValueUI[rollCount].text = probability.ToString();
                    break;

                case UI.ProbabilityType.Percent:
                    probability = Mathf.Round(DiceProbability(diceValue) * 100 * 10) / 10.0f;
                    diceProbValueUI[rollCount].text = probability + "%";
                    break;

                case UI.ProbabilityType.Fraction:
                    diceProbValueUI[rollCount].text = GetDiceProbabilityFraction(diceValue);    
                    break;
            }

            //record probability after conversion
            answerButtons[rollCount].buttonText.text = diceResultValueUI[rollCount].text /*+ " (" + diceProbValueUI[rollCount].text + ")"*/; 
            answerButtons[rollCount].storedValue = diceValue;
            StartCoroutine(AnimateDiceValue(diceValue));
            //diceIsRolling = true;
        //}
        /*else
        {
            //hide dice and show question
            ToggleDice(false);
            ToggleRangeHelper(true);
            GenerateQuestion();
        }*/
    }

    public void ToggleGameContainer(bool toggle)
    {
        gameContainer.gameObject.SetActive(toggle);
    }

    //also toggles roll dice button. Roll Count is reset
    public void ToggleDice(bool toggle)
    {
        foreach(GameObject die in dieObjects)
        {
            die.gameObject.SetActive(toggle);
        }
        rollButton.gameObject.SetActive(toggle);
        diceValueText.gameObject.SetActive(toggle);
        diceResultParticle.gameObject.SetActive(toggle);

        //reset everything
        if (toggle == true)
        {
            rollCount = 0;
            rolledValues.Clear();
            rolledProbs.Clear();
            correctAnswers.Clear();
            diceIsRolling = true;

            for (int i = 0; i < diceResultValueUI.Length; i++)
            {
                diceResultValueUI[i].text = "";
                diceProbValueUI[i].text = "";
            }

            instructionText.text = UniversalSettings.instance.GetText("assessmentGame_rollInstruction") + (maxRolls - rollCount);
            //UniversalSettings us = UniversalSettings.instance;
            //if (us.ttsEnabled)
                //LOLSDK.Instance.SpeakText("assessmentGame_rollInstruction");
            
        }
    }

    public void ToggleQuestions(bool toggle)
    {
        questionsContainer.gameObject.SetActive(toggle);
    }

    void GenerateQuestion()
    {
        //a question is randomly generated and the answer is found before the question is presented to player.
        int randQuestion = Random.Range(0, (int)Question.End);
        UniversalSettings us = UniversalSettings.instance;

        switch((Question)randQuestion)
        {
            case Question.HighestValue:
                question = Question.HighestValue;
                instructionText.text = us.GetText("assessmentGame_question1");

                //find the highest probability
                float highestProb = 0;
                foreach(float result in rolledProbs)
                {
                    if (result > highestProb)
                    {
                        highestProb = result;
                    }
                }

                //record the answers. Can be more than 1.
                for (int i = 0; i < rolledProbs.Count; i++)
                {
                    if (rolledProbs[i] == highestProb)
                        correctAnswers.Add(rolledValues[i]);
                }

                //if (us.ttsEnabled)
                    //LOLSDK.Instance.SpeakText("assessmentGame_question1");
                break;

            case Question.LowestValue:
                question = Question.LowestValue;
                instructionText.text = us.GetText("assessmentGame_question2");

                //find the lowest probability
                float lowestProb = 1;
                foreach(float result in rolledProbs)
                {
                    if (result < lowestProb)
                    {
                        lowestProb = result;
                    }
                }

                //record the answers. Can be more than 1.
                for (int i = 0; i < rolledProbs.Count; i++)
                {
                    if (rolledProbs[i] == lowestProb)
                        correctAnswers.Add(rolledValues[i]);
                }
                //if (us.ttsEnabled)
                    //LOLSDK.Instance.SpeakText("assessmentGame_question2");
                break;
        }

        //check if all values are the same. The player must re-roll if that's the case.
        if (correctAnswers.Count >= 3)
        {
            ToggleGameContainer(false);
            LessonManager lm = LessonManager.instance;
            lm.ToggleMiniLesson(21, true);
        }

        //The last answer is always the same, which is "they're the same". Set that up now.
        //answerButtons[answerButtons.Length - 1].buttonText.text = GetText("assessmentGame_answerEqual");

    }

    //checks the stored value from AnswerButton against the correct answers. 
    public void GetResult(int playerChoice)
    {
         //checked stored value against the correct answer. Special case if stored value is 0, which means all answers are equal.
        /*if (playerChoice <= 0)
        {
            //if all values are equal, player will roll new values.

            //is the player correct?
            if (correctAnswers.Count >= 3)
            {
                //TODO display mini lesson, then re-roll
                ToggleGameContainer(false);
                LessonManager lm = LessonManager.instance;
                    lm.ToggleMiniLesson(21, true);
                Debug.Log("Correct");
            }
            else
            {
                //TODO: display mini lesson
                ToggleGameContainer(false);
                LessonManager lm = LessonManager.instance;
                    lm.ToggleMiniLesson(22, true);
                Debug.Log("Incorrect");
            }
        }
        else
        {*/
            int i = 0;
            bool answerFound = false;
            while(!answerFound && i < correctAnswers.Count)
            {
                if (playerChoice == correctAnswers[i])
                {
                    answerFound = true;
                }
                else
                {
                    i++;
                }
            }

            if (answerFound)
            {
                //correct answer, show mini lesson
                ToggleGameContainer(false);
                LessonManager lm = LessonManager.instance;
                    lm.ToggleMiniLesson(19, true);
                Debug.Log("Correct answer is " + correctAnswers[i]);
            }
            else
            {
                //wrong answer, show mini lesson
                ToggleGameContainer(false);
                LessonManager lm = LessonManager.instance;
                    lm.ToggleMiniLesson(20, true);
                string answers = "";
                foreach(int answer in correctAnswers)
                {
                    answers += answer + ", ";
                }
                Debug.Log("Incorrect, answer is " + answers);
            }
        //}
    }

    //must run in Update loop
    void UpdateSliderValues()
    {
        UI ui = UI.instance;
        float probability = probRangeSlider.value;
        switch(ui.probabilityType)
        {
            case UI.ProbabilityType.Decimal:
                sliderZeroLabel.text = "0";
                sliderMidLabel.text = "0.5";
                sliderOneLabel.text = "1";
                probability = Mathf.Round(probRangeSlider.value * 1000) / 1000.0f;
                sliderValue.text = probability.ToString();
                break;

            case UI.ProbabilityType.Percent:
                sliderZeroLabel.text = "0%";
                sliderMidLabel.text = "50%";
                sliderOneLabel.text = "100%";
                probability = Mathf.Round(probRangeSlider.value * 100 * 10) / 10.0f;
                sliderValue.text = probability + "%";
                break;

            case UI.ProbabilityType.Fraction:
                sliderZeroLabel.text = "0";
                sliderMidLabel.text = "1/2";
                sliderOneLabel.text = "1";
                //float numerator = (float)DecimalPlaces(probability) <= 0 ? 1 : 10 * (float)DecimalPlaces(probability);
                float numerator = 1;
                //float denominator = float.IsInfinity(1 / probability) ? 0 : numerator;
                float denominator = float.IsInfinity(1 / probability) ? 0 : Mathf.Round((1 / probability) * 100) / 100.0f;
                //numerator *= probability;

                 //simplify fraction
                /*int i = (int)denominator;       //greatest common denominator
                int a = (int)numerator;
                while (i > 0)
                {
                    int rem = a % i;
                    a = i;
                    i = rem;
                }

                numerator /= a;
                denominator /= a;*/

                //round numerator in case we get a large number
                //numerator = Mathf.Round(numerator * 100) / 100.0f;
                sliderValue.text = (float.IsInfinity(1 / probability)) ? "0" : numerator + "/" + denominator;    
                break;
        }

        //update slider value colour based on range
        Color sliderColor;
        if (probRangeSlider.value >= 0.75f)
        {
            sliderColor = new Color(0, 1, 0);
        }
        else if (probRangeSlider.value > 0.5f)
        {
            sliderColor = new Color(0, 0.8f, 0.6f);
        }
        else if (probRangeSlider.value >= 0.49f)    //the value will be rounded to 0.5
        {
            sliderColor = new Color(1, 1, 1);
        }
        else if (probRangeSlider.value >= 0.25f)
        {
            sliderColor = new Color(0.9f, 0.3f, 0); //orange
        }
        else
        {
            sliderColor = new Color(0.75f, 0, 0);
        }

        sliderValue.color = sliderColor;
    }

    //takes a rolled value and returns its probability when rolling 2 six-sided dice.
    float DiceProbability(int diceSum)
    {
        if (diceSum < 2 || diceSum > 12) return 0;
        float probability = 0;

        switch(diceSum)
        {
            case 2:
                probability = 1/36f;
                break;
            case 3:
                probability = 2/36f;
                break;
            case 4:
                probability = 3/36f;
                break;
            case 5:
                probability = 4/36f;
                break;
            case 6:
                probability = 5/36f;
                break;
            case 7:
                probability = 6/36f;
                break;
            case 8:
                probability = 5/36f;
                break;
            case 9:
                probability = 4/36f;
                break;
            case 10:
                probability = 3/36f;
                break;
            case 11:
                probability = 2/36f;
                break;
            case 12:
                probability = 1/36f;
                break;

        }
        return probability;
    }

    string GetDiceProbabilityFraction(int diceSum)
    {
        if (diceSum < 2 || diceSum > 12) return "0";
        string probability = "";

        switch(diceSum)
        {
            case 2:
                probability = "1/36";
                break;
            case 3:
                probability = "1/18";
                break;
            case 4:
                probability = "1/12";
                break;
            case 5:
                probability = "1/9";
                break;
            case 6:
                probability = "5/36";
                break;
            case 7:
                probability = "1/6";
                break;
            case 8:
                probability = "5/36";
                break;
            case 9:
                probability = "1/9";
                break;
            case 10:
                probability = "1/12";
                break;
            case 11:
                probability = "1/18";
                break;
            case 12:
                probability = "1/36";
                break;

        }
        return probability;
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

    IEnumerator AnimateDiceValue(int amount)
    {
        animateDiceValueCoroutineOn = true;
        AudioManager am = AudioManager.instance;

        diceValueText.text = amount.ToString();
        diceResultParticle.Play();
        am.soundSource.PlayOneShot(am.audioDiceResult, am.soundVolume);

        //animate the dice value
        float scaleSpeed = 4;
        yield return ScaleDiceValue(scaleSpeed);

        yield return new WaitForSeconds(0.5f);

        //when this finishes, start the roll again.
        rollCount++;
        instructionText.text = UniversalSettings.instance.GetText("assessmentGame_rollInstruction") + (maxRolls - rollCount);
        diceValueText.text = "";
        animateDiceValueCoroutineOn = false;

        if (rollCount < maxRolls)
        {
            diceIsRolling = true;
        }
        else
        {
            //hide dice move on to question generation
            ToggleDice(false);
            ToggleQuestions(true);
            GenerateQuestion();
        }
       
    }

        //used with AnimateDiceValue only
    IEnumerator ScaleDiceValue(float scaleSpeed)
    {
        Vector3 originalScale = diceValueText.transform.localScale;
        Vector3 destinationScale = originalScale * 1.4f;
        while (diceValueText.transform.localScale.x < destinationScale.x)
        {
            float vx = diceValueText.transform.localScale.x + scaleSpeed * Time.deltaTime;
            float vy = diceValueText.transform.localScale.y + scaleSpeed * Time.deltaTime;
            diceValueText.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
        while (diceValueText.transform.localScale.x > originalScale.x)
        {
            float vx = diceValueText.transform.localScale.x - scaleSpeed * Time.deltaTime;
            float vy = diceValueText.transform.localScale.y - scaleSpeed * Time.deltaTime;
            diceValueText.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
        diceValueText.transform.localScale = originalScale;
    }
}
