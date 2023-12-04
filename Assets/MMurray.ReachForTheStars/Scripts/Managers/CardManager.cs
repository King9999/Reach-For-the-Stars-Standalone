using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This script handles the transferring of cards to the deck so they can be shuffled. */
namespace MMurray.ReachForTheStars
{
    public class CardManager : MonoBehaviour
    {
        public Card cardPrefab;
        public List<CardData> cards;
        public GameObject cardAura;                     //highlights a selected card
        SpriteRenderer auraSr;
        public GameObject background;                   //used to darken the screen so the cards stand out more. Not in UI canvas so it doesn't clash with card sprites.
        public List<Card> discardedCards;               //will be used to prevent instantiating new cards where applicable
        public bool[] cardPositionOccupied;            //manages the placement of each card on screen.
        public Card[] playerHand;                       //shows the current player's hand on screen.
        public Sprite[] cardArt;                    //index corresponds to artID in CardData script
        int deckSize {get;} = 23;

        [System.Serializable]
        public struct CardType
        {
            public CardData card;
            public int amount;
        }

        public CardType[] cardTypes;
        public int maxHand {get;} = 5;

        [Header("---JSON---")]
        public TextAsset cardTextFile;
        CardTexts cardTextList;                 //contains data from cards.json

        //coroutine check
        bool animateAuraCoroutineOn;
        [HideInInspector]public bool cardSelected;
        
        AudioManager am;
        GameManager gm;
        TutorialManager tm;
        public static CardManager instance;

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
           
            //aura set up
            DisplayCardAura(false);
            cardSelected = false;
            animateAuraCoroutineOn = false;

            am = AudioManager.instance;
            gm = GameManager.instance;
            tm = TutorialManager.instance;
            auraSr = cardAura.GetComponent<SpriteRenderer>();
        }

        //Hides/shows current player's cards
        public void ToggleCardManager(bool toggle)
        {
            gameObject.SetActive(toggle);
        }

        //initializes the cards for shuffling later.
        public void SetupCards()
        {
            cards = new List<CardData>();
            /*for (int i = 0; i < deckSize; i++)
            {
                cards.Add(ScriptableObject.CreateInstance<CardData>());
            }*/
            

            //must fill the deck with dummy cards so that new cards can be inserted in random locations.
            for (int i = 0; i < deckSize; i++)
            {
                cards.Add(cardTypes[0].card);
                //cards[i] = cardTypes[0].card;
            }

            //set up card text
            cardTextList = JsonUtility.FromJson<CardTexts>(cardTextFile.text);
            UI ui = UI.instance;

            for (int i = 0; i < cardTypes.Length; i++)
            {
                int j = 0;
                bool matchFound = false;
                while (!matchFound && j < cardTextList.cardTypes.Length)
                {
                    if (cardTypes[i].card.cardID == cardTextList.cardTypes[j].cardID)
                    {
                        matchFound = true;
                        cardTypes[i].amount = cardTextList.cardTypes[j].amount;
                        cardTypes[i].card.weight = cardTextList.cardTypes[j].weight;
                        //cardTypes[i].card.cardName = cardTextList.cardTypes[j].cardName;
                        //cardTypes[i].card.ability = cardTextList.cardTypes[j].ability;
                        //cardTypes[i].card.tip = cardTextList.cardTypes[j].tip;

                        //set up card name, ability and tip using the text from UI script. It will contain the correct language.

                        cardTypes[i].card.cardName = ui.cardNameText[j];
                        cardTypes[i].card.ability = ui.abilityText[j];
                        cardTypes[i].card.tip = ui.tipText[j];
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        public void ShuffleCards()
        {
            //need to track which spaces are filled with cards so we don't overwrite previously added cards.
            if (cards.Count <= 0) 
            {
                Debug.Log("Cards in Card Manager are not set up");
                return;
            }
            
            bool[] deckSpace = new bool[deckSize];
            for (int i = 0; i < deckSpace.Length; i++)
            {
                deckSpace[i] = false;
            }

            //CHeck each card type and add cards
            for (int i = 0; i < cardTypes.Length; i++)
            {
                for (int j = 0; j < cardTypes[i].amount; j++)
                {
                    //add cards to random locations in the deck
                    int randSpace = Random.Range(0, deckSize);
                    do 
                    {
                        randSpace = Random.Range(0, deckSize);
                    }
                    while (deckSpace[randSpace] == true);

                    cards[randSpace] = cardTypes[i].card;
                    deckSpace[randSpace] = true;
                }
            }
        }

        public void DisplayHand(Player player, bool toggle)
        {
            //display player's hand on screen.
            //TODO: If player is AI, can only see card back, and card details are not shown to human player.
            if (toggle == true)
            {
                for (int i = 0; i < playerHand.Length; i++)
                {
                    playerHand[i].gameObject.SetActive(false);
                    if (i < player.hand.Count)
                    {
                        playerHand[i].gameObject.SetActive(true);
                        playerHand[i].GetCardData(player.hand[i]);
                    }
                }

                //cannot see AI player's cards
                //GameManager gm = GameManager.instance;
                if (tm == null)
                {
                    if (!gm.currentPlayer.isAI)
                    {
                        ShowCardArt();
                    }
                    else
                    {
                        HideCards();
                    }
                }
                else
                {
                    if (!tm.currentPlayer.isAI)
                    {
                        ShowCardArt();
                    }
                    else
                    {
                        HideCards();
                    }
                }
            }
            else
            {
                for (int i = 0; i < playerHand.Length; i++)
                {
                    playerHand[i].gameObject.SetActive(false);
                }
                
                DisplayCardAura(false);
                cardSelected = false;
                animateAuraCoroutineOn = false;
            }   
        }

        
        public void StartAuraAnimation()
        {
            //if (!animateAuraCoroutineOn)
                //StartCoroutine(AnimateCardAura());
            cardSelected = true;
        }

        public void DisplayCardAura(bool toggle)
        {
            cardAura.gameObject.SetActive(toggle);
        }

        //method is called when Play Card button is clicked
        public void PlayCard()
        {
            //am.soundSource.PlayOneShot(am.click, am.soundVolume);
            //GameManager gm = GameManager.instance;
            
            if (tm == null)
            {
                if (gm.gameState == GameManager.GameState.GetMiniLesson) return;    //prevents interaction if lesson is open
                if (cardSelected || gm.gameState == GameManager.GameState.PlayingRefill || 
                    gm.gameState == GameManager.GameState.PlayingJump)    //last 2 conditions are for confirming player wants to use Refill/Jump
                {
                    //activate card effects. Need to check the target type
                    switch(gm.currentPlayer.selectedCard.targetType)
                    {
                        case CardData.Target.OnePlayer:
                            gm.SetGameState(GameManager.GameState.ChoosingTarget);
                            break;

                        case CardData.Target.Self:
                            if (gm.currentPlayer.selectedCard.cardID == 7 && gm.gameState != GameManager.GameState.PlayingJump)
                            {
                                Player closestPlayer = gm.currentPlayer.GetNearestPlayer(gm.playerList);
                                gm.SetGameState(GameManager.GameState.PlayingJump);
                            }
                            else
                            {
                                //activate card effect
                                gm.target = gm.currentPlayer;
                                gm.SetGameState(GameManager.GameState.ActivateCard);
                            }
                            break;

                        case CardData.Target.Board:     //only Refill uses this
                            //activate card effect
                            if (gm.currentPlayer.selectedCard.cardID == 5 && gm.gameState != GameManager.GameState.PlayingRefill)
                            {
                                gm.currentPlayer.targetCache = gm.currentPlayer.NearestStarCache(gm.currentPlayer.direction, 0.5f); 
                                gm.SetGameState(GameManager.GameState.PlayingRefill);
                            }
                            else
                            {
                                //card cannot be used if there's no valid space
                                if (gm.currentPlayer.targetCache != null)
                                    gm.SetGameState(GameManager.GameState.ActivateCard);
                            }
                            break;
                    }
                }
            }
            else    //tutorial
            {
                if (cardSelected || tm.gameState == TutorialManager.GameState.PlayingRefill || 
                    tm.gameState == TutorialManager.GameState.PlayingJump)    //last 2 conditions are for confirming player wants to use Refill/Jump
                {
                    //activate card effects. Need to check the target type
                    switch(tm.currentPlayer.selectedCard.targetType)
                    {
                        case CardData.Target.OnePlayer:
                            tm.SetGameState(TutorialManager.GameState.ChoosingTarget);
                            break;

                        case CardData.Target.Self:
                            if (tm.currentPlayer.selectedCard.cardID == 7 && tm.gameState != TutorialManager.GameState.PlayingJump)
                            {
                                Player closestPlayer = tm.currentPlayer.GetNearestPlayer(tm.playerList);
                                tm.SetGameState(TutorialManager.GameState.PlayingJump);
                            }
                            else
                            {
                                //activate card effect
                                tm.target = tm.currentPlayer;
                                tm.SetGameState(TutorialManager.GameState.ActivateCard);
                            }
                            break;

                        case CardData.Target.Board:     //only Refill uses this
                            //activate card effect
                            if (tm.currentPlayer.selectedCard.cardID == 5 && tm.gameState != TutorialManager.GameState.PlayingRefill)
                            {
                                tm.currentPlayer.targetCache = tm.currentPlayer.NearestStarCache(tm.currentPlayer.direction, 0.5f); 
                                tm.SetGameState(TutorialManager.GameState.PlayingRefill);
                            }
                            else
                            {
                                if (tm.currentPlayer.targetCache != null)
                                    tm.SetGameState(TutorialManager.GameState.ActivateCard);
                            }
                            break;
                    }
                }
            }
        }

        //used when AI is checking hand. Human player cannot see card art.
        void HideCards()
        {
            SpriteRenderer sr;
            foreach(Card card in playerHand)
            {
                sr = card.GetComponent<SpriteRenderer>();
                sr.sprite = card.cardBack;
            }
        }

        void ShowCardArt()
        {
            SpriteRenderer sr;
            foreach(Card card in playerHand)
            {
                sr = card.GetComponent<SpriteRenderer>();
                sr.sprite = cardArt[card.artID];
            }
        }

        //discards a selected card. Activated with a button press
        public void DiscardCard()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            //GameManager gm = GameManager.instance;
            if (tm == null)
            {
                CardData discardedCard = gm.currentPlayer.selectedCard;
                if (discardedCard == null) return;

                playerHand[gm.currentPlayer.hand.IndexOf(discardedCard)].gameObject.SetActive(false);   //hide the card we're about to remove
                gm.currentPlayer.hand.Remove(discardedCard);
                discardedCard = null;
                DisplayCardAura(false);
                cardSelected = false;
                animateAuraCoroutineOn = false;

                //update card count
                UI ui = UI.instance;
                ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);

                //update discard count. If there are no more cards to discard, then change game state
                if (gm.currentPlayer.hand.Count > maxHand)
                {
                    ui.DisplayAlert("tooManyCards", gm.currentPlayer.hand.Count - maxHand);
                    //ui.DisplayAlert("Too many cards! Cards to discard: " + (gm.currentPlayer.hand.Count - maxHand));
                }
                else
                {
                    //special condition check for when "Draw a Card" extra mod is active
                    ExtraModManager em = ExtraModManager.instance;
                    if (em.activeMod != null && em.activeMod.boardID == 3)   
                        gm.SetGameState(GameManager.GameState.BeginningNewTurn);
                    else    //playing normal game
                        gm.SetGameState(GameManager.GameState.NextPlayerTurn);
                }
            }
            else
            {
                //in the tutorial, the discarded card goes back to the deck.
                CardData discardedCard = tm.currentPlayer.selectedCard;
                if (discardedCard == null) return;

                playerHand[tm.currentPlayer.hand.IndexOf(discardedCard)].gameObject.SetActive(false);   //hide the card we're about to remove
                cards.Add(discardedCard);
                tm.currentPlayer.hand.Remove(discardedCard);
                //discardedCard = null;
                DisplayCardAura(false);
                cardSelected = false;
                animateAuraCoroutineOn = false;

                //update card count
                UI ui = UI.instance;
                ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(tm.currentPlayer);

                //update discard count. If there are no more cards to discard, then change game state
                if (tm.currentPlayer.hand.Count > maxHand)
                {
                    ui.DisplayAlert("tooManyCards", tm.currentPlayer.hand.Count - maxHand);
                }
                else
                {
                    tm.SetGameState(TutorialManager.GameState.NextPlayerTurn);
                }
            }
        }
    

        //called when the "Don't play card" button is pressed
        public void SkipCardPhase()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            GameManager gm = GameManager.instance;
            gm.SetGameState(GameManager.GameState.RollingDice);
        }

        //called when Back button is pressed 
        public void BackToCardPhase()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            //GameManager gm = GameManager.instance;
            if (tm == null)
                gm.SetGameState(GameManager.GameState.CardPhase);
            else
                tm.SetGameState(TutorialManager.GameState.CardPhase);
        }


        void Update()
        {
            if (cardSelected && !animateAuraCoroutineOn)
                StartCoroutine(AnimateCardAura());
        }

        #region Coroutines

        //card aura pulsates whenever a card is selected
        IEnumerator AnimateCardAura()
        {
            animateAuraCoroutineOn = true;
            float changeSpeed = 2;
            while (auraSr.color.a > 0)
            {
                float alpha = auraSr.color.a - changeSpeed * Time.deltaTime;
                auraSr.color = new Color(auraSr.color.r, auraSr.color.g, auraSr.color.b, alpha);
                yield return null;
            }

            //change back
            while (auraSr.color.a < 0.7f)
            {
                float alpha = auraSr.color.a + changeSpeed * Time.deltaTime;
                auraSr.color = new Color(auraSr.color.r, auraSr.color.g, auraSr.color.b, alpha);
                yield return null;
            }

            animateAuraCoroutineOn = false;
            /*Vector3 originalScale = cardAura.transform.localScale;
            Vector3 destinationScale = new Vector3(originalScale.x + 0.2f, originalScale.y + 0.2f, 0);
            float delta = 0.4f;

            while (cardAura.transform.localScale.x < destinationScale.x)
            {
                float vx = cardAura.transform.localScale.x + delta * Time.deltaTime;
                float vy = cardAura.transform.localScale.y + delta * Time.deltaTime;
                cardAura.transform.localScale = new Vector3(vx, vy, 0);
                yield return null;
            }
            animateAuraCoroutineOn = false;
            cardAura.transform.localScale = originalScale;*/

        }
        #endregion

    }
}
