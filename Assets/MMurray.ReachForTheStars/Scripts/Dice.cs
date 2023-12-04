using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLSDK;

//used to display dice rolls
namespace MMurray.ReachForTheStars
{
    public class Dice : MonoBehaviour
    {
        int die1, die2;
        public bool diceIsRolling;     //controls the rolling of dice
        public Sprite[] diceSprites;
        public GameObject[] dieObjects;     //index 0 is die 1, 1 is die 2
        public GameObject cross;        //used when dice can't be rolled
        SpriteRenderer dieOneSr;
        SpriteRenderer dieTwoSr;
        int diceValue;              //the resulting dice roll plus any movement mods.
        public ParticleSystem diceResultParticle;

        //singletons
        GameManager gm;
        TutorialManager tm;

        // Start is called before the first frame update
        void Start()
        {
            dieOneSr = dieObjects[0].GetComponent<SpriteRenderer>();
            dieTwoSr = dieObjects[1].GetComponent<SpriteRenderer>();
            gm = GameManager.instance;
            tm = TutorialManager.instance;
        }

        // Update is called once per frame
        void Update()
        {
            //GameManager gm = GameManager.instance;
            //TutorialManager tm = TutorialManager.instance;

            if (tm == null)
            {
                if (gm.gameState == GameManager.GameState.RollingDice)
                {
                    RollDice(diceIsRolling);
                    if (diceIsRolling == false)
                    {
                        //get result and move player
                        gm.currentPlayer.SetMovement(diceValue, gm.currentPlayer.moveMod);
                    }
                }
            }
            else
            {
                if (tm.gameState == TutorialManager.GameState.RollingDice)
                {
                    RollDice(diceIsRolling);
                    if (diceIsRolling == false)
                    {
                        //get result and move player
                        tm.currentPlayer.SetMovement(diceValue, tm.currentPlayer.moveMod);
                    }
                }
            } 
        }

        public void ToggleCross(bool toggle)
        {
            cross.gameObject.SetActive(toggle);
        }

        public void ShowDice(bool toggle, bool forcedMove = false, bool playerIsAI = false, bool cardPlayed = false)
        {
            UI ui = UI.instance;

            if (!playerIsAI)
            {
                ui.ToggleDiceContainerUI(toggle);
                ui.ToggleRollButton(toggle);
                ToggleCross(false);
                ui.ToggleMoveModValue(false);
                ui.ChangeRollButtonText("rollDiceButtonText");

                //back button is displayed only if card was not used and no extra round, or if player has no cards.
                GameManager gm = GameManager.instance;
                if (gm != null)
                {
                    if (cardPlayed || gm.extraRound /*|| gm.playerList[0].hand.Count <= 0*/)
                        ui.ToggleRollDiceBackButton(false);
                    else
                        ui.ToggleRollDiceBackButton(toggle);
                }
                else    //tutorial
                {
                    if (cardPlayed)
                        ui.ToggleRollDiceBackButton(false);
                    else
                        ui.ToggleRollDiceBackButton(toggle);
                }
                
                
            }
            else
            {
                //buttons are not shown
                GameManager gm = GameManager.instance;
                ui.ToggleDiceContainerUI(toggle);
                ToggleCross(false);
                ui.ToggleMoveModValue(false);

                TutorialManager tm = TutorialManager.instance;
                if (tm == null)
                    ui.DisplayAlert("rollingDice", gm.currentPlayer.playerName);
                else
                    ui.DisplayAlert("rollingDice", tm.currentPlayer.playerName);
            }
            gameObject.SetActive(toggle);


            if (toggle == true && forcedMove == true)
            {
                //show alternate UI displaying the player's move total and no dice roll.
                ToggleCross(true);
                GameManager gm = GameManager.instance;
                //ui.DisplayAlert("Can't roll dice this turn!");
                ui.DisplayAlert("cantRollDice");
                ui.ChangeRollButtonText("rollDiceButtonTextForcedMove");

                if (tm == null)
                    ui.DisplayDiceValue(gm.currentPlayer.moveTotal);               
                else              
                    ui.DisplayDiceValue(tm.currentPlayer.moveTotal);
            }

            if (playerIsAI)
            {
                Invoke("StopDice", time: 1.5f);
            }
        }

        //this method is called when the appropriate button is clicked. AI also uses this method
        public void StopDice()
        {
            //GameManager gm = GameManager.instance;
            if (tm == null)
            {
                if (gm.gameState != GameManager.GameState.RollingDice) return;  //if lesson is open, don't do anything
                if (gm.currentPlayer.canRollDice)
                {
                    diceIsRolling = false;
                    diceValue = die1 + die2;
                
                    Debug.Log("Rolled " + (diceValue));

                    //record the roll at this point. We don't want to record any modified values
                    gm.diceRollRecord[diceValue - 2]++;
                    gm.totalRolls++;
                    //if (gm.extraRound) 
                        //diceValue = 10;
                
                    
                    //update UI for all dice roll values
                    UI ui = UI.instance;
                    ui.totalRollsUIValue.text = gm.totalRolls.ToString();
                    ui.ToggleAlertUI(false);        //this is done in case AI rolled this turn

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
                    

                    //show dice result
                    StartCoroutine(AnimateDiceValue(diceValue));
                }
                else
                {
                    //if player didn't use a jump card, then we just do a forced move
                    if (gm.gameState != GameManager.GameState.PlayerJumping)
                        gm.SetGameState(GameManager.GameState.PlayerMoving);
                }
            }
            else    //tutorial
            {
                if (tm.currentPlayer.canRollDice)
                {
                    diceIsRolling = false;
                    diceValue = die1 + die2;
                
                    Debug.Log("Rolled " + (diceValue));               
                
                    //show dice result
                    StartCoroutine(AnimateDiceValue(diceValue));
                }
                else
                {
                    //if player didn't use a jump card, then we just do a forced move
                    if (tm.gameState != TutorialManager.GameState.PlayerJumping)
                        tm.SetGameState(TutorialManager.GameState.PlayerMoving);
                }
            }
        }

        public int GetDiceValue()
        {
            return diceValue;
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

                //show any bonus movement beside the dice if a card was used.
                //GameManager gm = GameManager.instance;
                if (tm == null)
                {
                    if (gm.currentPlayer.moveMod > 0)
                    {
                        UI ui = UI.instance;
                        ui.ToggleMoveModValue(true);
                        ui.UpdateMoveModValue(gm.currentPlayer.moveMod);
                    }
                }
                else
                {
                    if (tm.currentPlayer.moveMod > 0)
                    {
                        UI ui = UI.instance;
                        ui.ToggleMoveModValue(true);
                        ui.UpdateMoveModValue(tm.currentPlayer.moveMod);
                    }
                }
            }
        }

        IEnumerator AnimateDiceValue(int amount)
        {
            UI ui = UI.instance;
            AudioManager am = AudioManager.instance;

            ui.DisplayDiceValue(diceValue);
            ui.ToggleRollButton(false);
            ui.ToggleRollDiceBackButton(false);
            diceResultParticle.Play();
            am.soundSource.PlayOneShot(am.audioDiceResult, am.soundVolume);

            //animate the dice value
            float scaleSpeed = 4;
            yield return ScaleDiceValue(scaleSpeed);

            //if player used Swift movement, add an additional animation.
            //GameManager gm = GameManager.instance;
            int moveMod = (tm == null) ? gm.currentPlayer.moveMod : tm.currentPlayer.moveMod;
            
            if (/*gm.currentPlayer.moveMod*/moveMod > 0)
            {
                yield return new WaitForSeconds(0.3f);

                //flash the move mod value a bit.
                float duration = 1;
                float currentTime = Time.time;
                bool toggle = true;
                while(Time.time < currentTime + duration)
                {
                    toggle = !toggle;
                    ui.ToggleMoveModValue(toggle);
                    if (toggle == true)
                        am.soundSource.PlayOneShot(am.audioMoveAdded, am.soundVolume);
                    yield return new WaitForSeconds(0.1f);
                }
                ui.ToggleMoveModValue(true);

                //show the new dice value and animate again
                ui.DisplayDiceValue(diceValue + moveMod /*gm.currentPlayer.moveMod*/);
                diceResultParticle.Play();
                am.soundSource.PlayOneShot(am.audioDiceResult, am.soundVolume);
                yield return ScaleDiceValue(scaleSpeed);
            }

            yield return new WaitForSeconds(1);

            //change game state
            if (tm == null)
            {
                //check for extra mode mod "Random Jumps"
                ExtraModManager em = ExtraModManager.instance;
                if (!gm.extraRound && em.activeMod != null && em.activeMod.boardID == 1)
                    em.activeMod.Activate();
                else
                    gm.SetGameState(GameManager.GameState.PlayerMoving);
            }
            else
                tm.SetGameState(TutorialManager.GameState.PlayerMoving);
        }

        //used with AnimateDiceValue only
        IEnumerator ScaleDiceValue(float scaleSpeed)
        {
            UI ui = UI.instance;
            Vector3 originalScale = ui.diceValueUI.transform.localScale;
            Vector3 destinationScale = originalScale * 1.4f;
            while (ui.diceValueUI.transform.localScale.x < destinationScale.x)
            {
                float vx = ui.diceValueUI.transform.localScale.x + scaleSpeed * Time.deltaTime;
                float vy = ui.diceValueUI.transform.localScale.y + scaleSpeed * Time.deltaTime;
                ui.diceValueUI.transform.localScale = new Vector3(vx, vy, 0);
                yield return null;
            }
            while (ui.diceValueUI.transform.localScale.x > originalScale.x)
            {
                float vx = ui.diceValueUI.transform.localScale.x - scaleSpeed * Time.deltaTime;
                float vy = ui.diceValueUI.transform.localScale.y - scaleSpeed * Time.deltaTime;
                ui.diceValueUI.transform.localScale = new Vector3(vx, vy, 0);
                yield return null;
            }
            ui.diceValueUI.transform.localScale = originalScale;
        }
    }
}
