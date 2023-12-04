using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
//using LoLSDK;

//This script provides card information to the player. If the player is AI, both the card art and the details are hidden.
namespace MMurray.ReachForTheStars
{
    public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public CardData ability;
        public string cardName;
        public string abilityDetails;
        public string tip;      //useful information on how to use card
        public int artID;
        public Sprite cardBack; //used by AI to hide their cards
        public string nameKey, abilityKey, tipKey;          //used by TTS
        public CardData.Target targetType;
       SpriteRenderer sr;

        void Start()
        {
            sr = GetComponent<SpriteRenderer>();
        }

       
        public void GetCardData(CardData data)
        {
            ability = data;             //need this to get the scriptable object
            cardName = data.cardName;
            abilityDetails = data.ability;
            tip = data.tip;
            artID = data.artID;
            targetType = data.targetType;
            nameKey = data.nameKey;
            abilityKey = data.abilityKey;
            tipKey = data.tipKey;

            //sr.sprite = art;
            //add card art
            /*sr.sprite = cardBack;
            GameManager gm = GameManager.instance;
            if (!gm.currentPlayer.isAI)
                sr.sprite = art;*/
            //else
                //sr.sprite = cardBack;
        }

        //card info disappears when no longer mousing over a card
        public void OnPointerExit(PointerEventData pointer)
        {
            //stop TTS
            //((ILOLSDK_EXTENSION)LOLSDK.Instance.PostMessage).CancelSpeakText();
            StopAllCoroutines();
            UI ui = UI.instance;
            ui.DisplayCardText(false);

            //if the dice roll record or card draw rate window is open, they're displayed again.
            if (ui.diceRollRecordContainerToggle == true)
                ui.diceRollRecordContainer.gameObject.SetActive(true);
            
            if (ui.cardDrawRatesContainerToggle == true)
                ui.cardDrawRatesContainer.gameObject.SetActive(true);
        }

        public void OnPointerClick(PointerEventData pointer)
        {
            AudioManager am = AudioManager.instance;
            //am.soundSource.PlayOneShot(am.click, am.soundVolume);

            UI ui = UI.instance;
            if (ui.roundHandler.gameObject.activeSelf) return;

            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;
            

            if (tm == null)
            {
                if (gm.gameState == GameManager.GameState.GetMiniLesson) return;    //prevents interaction with cards if lesson is open
                if (!gm.currentPlayer.isAI)
                {
                    am.soundSource.PlayOneShot(am.click, am.soundVolume);
                    CardManager cm = CardManager.instance;
                    cm.DisplayCardAura(true);
                    cm.cardAura.transform.position = transform.position;
                    cm.StartAuraAnimation();

                    //change play card button text colour to indicate it can be clicked now.
                    ui.ChangePlayCardButtonColor(Color.white);

                    //copy the selected card for later use
                    gm.currentPlayer.selectedCard = this.ability;
                }
            }
            else
            {
                if (!tm.currentPlayer.isAI)
                {
                    CardManager cm = CardManager.instance;
                    cm.DisplayCardAura(true);
                    cm.cardAura.transform.position = transform.position;
                    cm.StartAuraAnimation();

                    //change play card button text colour to indicate it can be clicked now.
                    ui.ChangePlayCardButtonColor(Color.white);

                    //copy the selected card for later use
                    tm.currentPlayer.selectedCard = this.ability;
                }
            }
        }

        //when mousing over a card, its information will be displayed. Also the card will be highlighted
        public void OnPointerEnter(PointerEventData pointer)
        {
            UI ui = UI.instance;
            if (ui.roundHandler.gameObject.activeSelf) return;
            //Debug.Log("Mousing over card " + this);
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;
            if (gm.gameState == GameManager.GameState.GetMiniLesson) return;    //prevents interaction with cards if lesson is open
            bool playerIsAI = (tm == null) ? gm.currentPlayer.isAI : tm.currentPlayer.isAI;
            if (!playerIsAI /*!gm.currentPlayer.isAI*/)
            {
                //UI ui = UI.instance;
                ui.DisplayCardText(true, cardName, abilityDetails, tip);

                //TTS describes the card
                /*UniversalSettings us = UniversalSettings.instance;
                if (us.ttsEnabled)
                {
                    //string[] keys = {nameKey, abilityKey, tipKey};
                    //StartCoroutine(PlayTTS(keys, 5.5f));
                    StartCoroutine(PlayTTS(nameKey, abilityKey, tipKey));
                }*/
            }

            //if the dice roll record or card draw rate window is open, they're temporarily hidden.
            if (ui.diceRollRecordContainerToggle == true)
                ui.diceRollRecordContainer.gameObject.SetActive(false);

            if (ui.cardDrawRatesContainerToggle == true)
                ui.cardDrawRatesContainer.gameObject.SetActive(false);
        }


        /*IEnumerator PlayTTS(string[] keys, float duration = 0)
        {
            int i = 0;
            while(i < keys.Length)
            {
                //LOLSDK.Instance.SpeakText(keys[i]);
                //yield return new WaitUntil(() => !audioSource.isPlaying);
                yield return new WaitForSeconds(duration);
                i++;
            }
        }*/

        /*IEnumerator PlayTTS(string nameKey, string abilityKey, string tipKey)
        {
            LOLSDK.Instance.SpeakText(nameKey);
            yield return new WaitForSeconds(2);

            LOLSDK.Instance.SpeakText(abilityKey);
            yield return new WaitForSeconds(5.5f);

            LOLSDK.Instance.SpeakText(tipKey);
            yield return new WaitForSeconds(5.5f);

        }*/

        /*IEnumerator PlayTTS(string key, float duration = 0)
        {
            LOLSDK.Instance.SpeakText(key);
            yield return new WaitForSeconds(duration);
        }*/

        //this method is activated with a button press
        /*public void PlayCard()
        {
            CardManager cm = CardManager.instance;
            if (cm.cardSelected)
            {
                //activate card effects. Need to check the target type
                switch(targetType)
                {
                    case CardData.Target.OnePlayer:
                        //change game state to selecting a target before activating card
                        //GameManager gm = GameManager.instance;
                        //Player player = gm.playerList[gm.currentPlayer];
                        //player.selectedCard = this;
                        //change state here
                        //UI ui = UI.instance;
                        //ui.ToggleCardUIContainer(false);
                        gm.SetGameState(GameManager.GameState.ChoosingTarget);
                        break;

                    case CardData.Target.Self:
                        //activate card effect
                        break;

                    case CardData.Target.Board:
                        //activate card effect
                        break;
                }
            }
        }*/
    }
}
