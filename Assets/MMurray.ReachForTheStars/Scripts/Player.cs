using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MMurray.ReachForTheStars
{
    public class Player : MonoBehaviour
    {
        public string playerName;
        public int playingPiece;
        public int starTotal;               //player needs at least 10 stars to win
        int maxCards {get; } = 5;
        public int moveTotal;               //number of spaces to move on the board
        public int moveMod;                 //used with swift movement card ability
        public int row, col;                //used to track location of player on board.
        public List<BoardSpace> route;     //used for when player needs to choose a route to take after scanning the spaces surrounding them.
        public BoardSpace oldSpace;    //used to ensure player does not go to this space unless there's no other option.
        public BoardSpace currentSpace;
        public BoardSpace jumpDestination;      //used with Jump card.

        [Header("---Card---")]
        public List<CardData> hand;             //players begin with 3 cards
        public CardData cardEffect;             //the card that's currently affecting the player.
        public CardData selectedCard;           //the card that the active player is going to use on a target.
        public StarCache targetCache {get; set;}           //used with Refill card
        public bool cardPlayed;             //if true, Back button does not appear on roll dice phase.

        [Header("---Bools---")]
        public bool canRollDice;            //defaults to true.
        public bool gotStarFromCache;
        public bool gotStarFromHome;        //defaults to true to prevent players getting a star at the start of the game
        public bool drewCardFromSpace;
        public bool newGameStarted;          //when a new game begins, players cannot acquire a star from home.
        public bool loseTurn;               //if true, player takes no action in current round.
        

        /*[Header("---Encounter---")]
        public bool wonEncounter;
        public bool isAttacker;
        public bool isDefender;
        public NumberCardManager.PlayerChoice choice;*/


       [Header("---Player Direction---")]
        //public bool movingUp, movingDown, movingLeft, movingRight;
        public Vector3 direction;           //can be used to tell which way a player is facing/moving
        public Vector3 destination;         //the space the player is currently moving to.
        BoardSpace newSpace;

        [Header("---Player AI---")]
        public bool isAI;                   //if true, the game controls the player.
        public enum AIState {None, ChoosingCard, DiscardingCards}
        public AIState aiState;
        List<BoardSpace> path;       //used to determine which route AI will take. The last space in this list is the destination.

        [Header("---UI---")]
        public TextMeshProUGUI moveCountUI;         //is displayed over player's head


        //coroutines
        bool animateMoveCoroutineOn;
        bool discardCoroutineOn;
        [HideInInspector]public bool playerMoving;

        //singletons
        GameManager gm;
        TutorialManager tm;

        void Start()
        {
            gm = GameManager.instance;
            tm = TutorialManager.instance;
            discardCoroutineOn = false;
        }
        

        void Update()
        {
            if (moveCountUI.gameObject.activeSelf)
            {
                Clamp(new Vector3(transform.position.x, transform.position.y + 1, 0));
            }

            //if player finished moving, ensure they are in the position they're supposed to be in. This should help to prevent issues 
            //where the player misses their destination and ends up moving forever.
            
        }
        
        //Use this if player restarts game without closing app
        public void ResetPlayer(string name, Sprite playingPiece, bool playerisAI = false)
        {
            starTotal = 0;
            hand = new List<CardData>();
            canRollDice = true;
            moveMod = 0;
            moveTotal = 0;
            isAI = playerisAI;
            playerName = name;
            newGameStarted = true;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = playingPiece;

            //give player 3 cards
        }

        public void SetMovement(int moveCount, int moveMod = 0)
        {
            moveTotal = moveCount + moveMod;
            moveCountUI.text = moveTotal.ToString();
        }

        public void StartMoving(int moveCount)
        {
            StartCoroutine(PlayerMovement(moveCount));
        }

        public void ToggleMoveCountUI(bool toggle)
        {
            moveCountUI.gameObject.SetActive(toggle);
            if (toggle == true)
                moveCountUI.text = moveTotal.ToString();

        }


        //move to designated location.
        public void Move(BoardSpace spaceDestination)
        {
            if (moveTotal <= 0 || playerMoving) return;

            oldSpace = currentSpace;
            row = spaceDestination.row;
            col = spaceDestination.col;

            destination = spaceDestination.transform.position;
            newSpace = spaceDestination;
        }

        //used exclusively by the Jump card.
        public void Jump(BoardSpace destinationSpace)
        {
            oldSpace = currentSpace;
            row = destinationSpace.row;
            col = destinationSpace.col;

            direction = Vector3.zero;       //player doesn't know where they're going next
            StartCoroutine(AnimateJump(destinationSpace));
        }

        //check for any spaces surrounding the current space and move to the discovered space. If there's more than once space to move
        //then player must choose with space to move on. The player's previous space must be ignored, unless there's no other path.
        public void CheckSurroundingSpaces(List<string[]> boardData)
        {
            route = new List<BoardSpace>();
            //GameManager gm = GameManager.instance;
            string routeStr = "";
            
            //north check
            if (row > 0 && (boardData[row - 1][col] != "0" || boardData[row - 1][col] != null))
            {
                //search for the board space in boardSpaceList and record it
                int i = 0;
                bool spaceFound = false;

                if (tm == null)
                {
                    while (i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row - 1 == gm.boardSpaceList[i].row && col == gm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(gm.boardSpaceList[i]);
                            routeStr += gm.boardSpaceList[i].spaceType + "(Row " + gm.boardSpaceList[i].row + ", Col " + gm.boardSpaceList[i].col + ")\n";
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    while (i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row - 1 == tm.boardSpaceList[i].row && col == tm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(tm.boardSpaceList[i]);
                            routeStr += tm.boardSpaceList[i].spaceType + "(Row " + tm.boardSpaceList[i].row + ", Col " + tm.boardSpaceList[i].col + ")\n";
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }

            //south check
            if (row < boardData.Count - 1 && (boardData[row + 1][col] != "0" || boardData[row + 1][col] != null))
            {
                int i = 0;
                bool spaceFound = false;
                if (tm == null)
                {
                    while (i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row + 1 == gm.boardSpaceList[i].row && col == gm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(gm.boardSpaceList[i]);
                            routeStr += gm.boardSpaceList[i].spaceType + "(Row " + gm.boardSpaceList[i].row + ", Col " + gm.boardSpaceList[i].col + ")\n";
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                else
                {
                    while (i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row + 1 == tm.boardSpaceList[i].row && col == tm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(tm.boardSpaceList[i]);
                            routeStr += tm.boardSpaceList[i].spaceType + "(Row " + tm.boardSpaceList[i].row + ", Col " + tm.boardSpaceList[i].col + ")\n";
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
            }

            //east check
            if (col < boardData[0].Length - 1 && (boardData[row][col + 1] != "0" || boardData[row][col + 1] != null))
            {
                int i = 0;
                bool spaceFound = false;
                if (tm == null)
                {
                    while (i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row == gm.boardSpaceList[i].row && col + 1 == gm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(gm.boardSpaceList[i]);
                            routeStr += gm.boardSpaceList[i].spaceType + "(Row " + gm.boardSpaceList[i].row + ", Col " + gm.boardSpaceList[i].col + ")\n";
                        }
                        else
                            i++;
                    }
                }
                else
                {
                    while (i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row == tm.boardSpaceList[i].row && col + 1 == tm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(tm.boardSpaceList[i]);
                            routeStr += tm.boardSpaceList[i].spaceType + "(Row " + tm.boardSpaceList[i].row + ", Col " + tm.boardSpaceList[i].col + ")\n";
                        }
                        else
                            i++;
                    }
                }
            }

            //west check
            if (col > 0 && (boardData[row][col - 1] != "0" || boardData[row][col - 1] != null))
            {
                int i = 0;
                bool spaceFound = false;
                if (tm == null)
                {
                    while (i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row == gm.boardSpaceList[i].row && col - 1 == gm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(gm.boardSpaceList[i]);
                            routeStr += gm.boardSpaceList[i].spaceType + "(Row " + gm.boardSpaceList[i].row + ", Col " + gm.boardSpaceList[i].col + ")\n";
                        }
                        else
                            i++;
                    }
                }
                else
                {
                    while (i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (row == tm.boardSpaceList[i].row && col - 1 == tm.boardSpaceList[i].col)
                        {
                            spaceFound = true;
                            route.Add(tm.boardSpaceList[i]);
                            routeStr += tm.boardSpaceList[i].spaceType + "(Row " + tm.boardSpaceList[i].row + ", Col " + tm.boardSpaceList[i].col + ")\n";
                        }
                        else
                            i++;
                    }
                }
            }

            //Debug.Log("Routes: " + routeStr);

        }

        public void DrawCard(int amount = 1)
        {
            CardManager cm = CardManager.instance;
            if (cm.cards.Count <= 0)
            {
                UI ui = UI.instance;
                //ui.DisplayFeedbackMessage("NO MORE CARDS!", transform.position);
                ui.DisplayFeedbackMessage("noCardsInDeck", transform.position);
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                if (cm.cards.Count > 0)
                { 
                    CardData topCard = cm.cards[0];
                    hand.Add(topCard);                 //add card to player hand
                    cm.cards.Remove(cm.cards[0]);

                    //Update card draw rate record.
                    UI ui = UI.instance;
                    ui.totalCardsUIValue.text = cm.cards.Count.ToString();
                    cm.cardTypes[topCard.cardID].amount--;      //deducting the card that was drawn from the deck.

                    //recalculate the draw rate
                    switch(ui.probabilityType)
                    {
                        case UI.ProbabilityType.Decimal:
                            for (int j = 0; j < ui.cardDrawRatesUI.Length; j++)
                            {
                                float cardDrawProb = Mathf.Round((float)cm.cardTypes[j].amount / (float)cm.cards.Count * 1000) / 1000.0f;
                                ui.cardDrawRatesUI[j].text = cardDrawProb.ToString();
                            }
                            break;

                        case UI.ProbabilityType.Percent:
                            for (int j = 0; j < ui.cardDrawRatesUI.Length; j++)
                            {
                                float cardDrawProb = (float)cm.cardTypes[j].amount / (float)cm.cards.Count;
                                cardDrawProb = Mathf.Round(cardDrawProb * 100 * 10) / 10.0f;
                                ui.cardDrawRatesUI[j].text = cardDrawProb + "%";
                            }
                            break;

                        case UI.ProbabilityType.Fraction:
                            for (int j = 0; j < ui.cardDrawRatesUI.Length; j++)
                            {
                                ui.cardDrawRatesUI[j].text = cm.cardTypes[j].amount + "/" + cm.cards.Count;
                            }
                            break;
                    }
                }
                else
                {
                    //no more cards
                    UI ui = UI.instance;
                    //ui.DisplayFeedbackMessage("NO MORE CARDS!", transform.position);
                    ui.DisplayFeedbackMessage("noCardsInDeck", transform.position);
                    return;
                }
            }
            
            //check if cards need to be discarded
            if (hand.Count > maxCards)
            {
                GameManager gm = GameManager.instance;
                gm.SetGameState(GameManager.GameState.PlayerDiscardingCards);
            }
        }


        public void DiscardActivatedCard(CardData card)
        {
            hand.Remove(card);
            UI ui = UI.instance;
            //GameManager gm = GameManager.instance;
            if (tm == null)
                ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
            else
            {
                //in the tutorial, cards are added back to deck
                CardManager cm = CardManager.instance;
                cm.cards.Add(card);
                ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(tm.currentPlayer);
            }
        }

        //used to place the move count UI over player's head
        public void Clamp(Vector3 position)
        {
            Vector3 moveCountUIPos = Camera.main.WorldToScreenPoint(position); 
            moveCountUI.transform.position = moveCountUIPos;
        }

        //This is called by AI players only
        public void PickRoute(Arrow arrow)
        {
            if (arrow.ChosenRoute == null || this == null) return;

            for (int i = 0; i < route.Count; i++)
            {
                if (route[i] != arrow.ChosenRoute)
                {
                    //delete space
                    route.Remove(route[i]);
                    i--;
                }
            }

            //hide all arrows once selection made
            //Debug.Log("Chosen Route: " + chosenRoute.spaceType + "(" + chosenRoute.row + "," + chosenRoute.col + ")");
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;
            if (tm == null)
                gm.EnableArrowContainer(false);
            else
                tm.EnableArrowContainer(false);
            UI ui = UI.instance;
            ui.ToggleAlertUI(false);
        }

        public Player GetLeadingPlayer(List<Player> playerList)
        {
            Player leadingPlayer = null;

            //first we check if there's a tie between all players
            switch(playerList.Count)
            {
                case 2:
                    if (playerList[0].starTotal == playerList[1].starTotal)
                        return null;

                    if (playerList[0].starTotal > playerList[1].starTotal)
                        leadingPlayer = playerList[0];
                    else
                        leadingPlayer = playerList[1];
                    break;

                case 3:
                    int highestValue = 0;
                    foreach(Player player in playerList)
                    {
                        if (player.starTotal > highestValue)
                            highestValue = player.starTotal;
                    }

                    //now check for a tie
                    List<Player> tiedPlayers = new List<Player>();
                    foreach(Player player in playerList)
                    {
                        if (player.starTotal == highestValue)
                        {
                            tiedPlayers.Add(player);
                        }
                    }

                    if (tiedPlayers.Count > 1)
                        return null;    //two or more players have tied
                    else
                        leadingPlayer = tiedPlayers[0];
                    //check for tie between 3 players 
                    /*if (playerList[0].starTotal == playerList[1].starTotal && playerList[0].starTotal == playerList[2].starTotal &&
                        playerList[1].starTotal == playerList[2].starTotal)
                            return null;

                    //check for 2-way tie
                    else
                    {
                        if (playerList[0].starTotal == playerList[1].starTotal && playerList[0].starTotal < playerList[2].starTotal)
                            return null;
                        else if (playerList[0].starTotal == playerList[2].starTotal && playerList[0].starTotal < playerList[1].starTotal)
                            return null;
                        else if (playerList[1].starTotal == playerList[0].starTotal && playerList[1].starTotal < playerList[2].starTotal)
                            return null;
                        else if (playerList[1].starTotal == playerList[2].starTotal && playerList[1].starTotal < playerList[0].starTotal)
                            return null;
                        else if (playerList[2].starTotal == playerList[0].starTotal && playerList[2].starTotal < playerList[1].starTotal)
                            return null;
                        else if (playerList[2].starTotal == playerList[1].starTotal && playerList[2].starTotal < playerList[0].starTotal)
                            return null;
                    }*/
                    break;
            }

            //get leading player
            /*for (int i = 0; i < playerList.Count; i++)
            {
                if (playerList[i] == this) continue;
                if (playerList[i].starTotal > leadingPlayer.starTotal)
                {
                    leadingPlayer = playerList[i];
                }
            }*/

            return leadingPlayer;
        }


        public void SetAIState(AIState aiState)
        {
            switch(aiState)
            {
                case AIState.None:
                    aiState = AIState.None;
                    break;

                case AIState.ChoosingCard:
                    aiState = AIState.ChoosingCard;
                    StartCoroutine(SelectCard());
                    break;

                case AIState.DiscardingCards:
                    aiState = AIState.DiscardingCards;
                    if (!discardCoroutineOn)
                    StartCoroutine(DiscardCards(/*hand.Count - maxCards*/));
                    break;
            }
        }

        //Gets the closest player from the Jump card user's position.
        public Player GetNearestPlayer(List<Player> playerList)
        {
            //GameManager gm = GameManager.instance;
            Player closestPlayer = null;
            List<float> distanceList = new List<float>();
            List<Player> players = new List<Player>();

            if (playerList.Count == 2)
            {
                foreach(Player opponent in playerList)
                {
                    if (opponent == this) continue;
                    closestPlayer = opponent;
                }

                Debug.Log("Jumping to player " + closestPlayer.playerName);
                if (tm == null)
                    gm.target = this;
                else
                    tm.target = this;
                jumpDestination = closestPlayer.currentSpace;
                //gm.SetGameState(GameManager.GameState.PlayerJumping);
            
            }
            else
            {
                foreach(Player opponent in playerList)
                {
                    if (opponent == this) continue;
                    distanceList.Add(Vector3.Distance(transform.position, opponent.transform.position));
                    players.Add(opponent);
                }

                //compare distances of the two opponents
                if (distanceList[0] < distanceList[1])
                {
                    closestPlayer = players[0];
                }
                else
                {
                    closestPlayer = players[1];
                }
                Debug.Log("Jumping to player " + closestPlayer.playerName);
                if (tm == null)
                    gm.target = this;
                else
                    tm.target = this;
                jumpDestination = closestPlayer.currentSpace;
                //gm.SetGameState(GameManager.GameState.PlayerJumping);
            }

            return closestPlayer;
        }

    #region Coroutines

        //this coroutine keeps running until player finishes moving.
        IEnumerator AnimateMovement()
        {
            animateMoveCoroutineOn = true;
            direction = destination - transform.position;
            float moveSpeed = 4;
            playerMoving = true;
            float minDist = 0.12f;   //used to stop player movement onto new space and to not overshoot. If this value is too high, it results in jerky movement.
           

            while (transform.position != destination)
            {
                float vx = moveSpeed * direction.x * Time.deltaTime;
                float vy = moveSpeed * direction.y * Time.deltaTime;

                //transform.position = new Vector3(transform.position.x + vx, transform.position.y + vy, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

                //check if we're close to destination
                /*float diffX = Mathf.Abs(destination.x - transform.position.x);
                float diffY = Mathf.Abs(destination.y - transform.position.y);
                if (diffX >= 0 && diffX <= minDist && diffY >= 0 && diffY <= minDist)
                    transform.position = destination;*/
                
                //we temporarily stop player here if the Home mini lesson appeared 
                LessonManager lm = LessonManager.instance;
                while(lm.miniLessonList[2].gameObject.activeSelf)
                {
                    yield return null;
                }


                yield return null;
            }

            //change player's location once they physically reach reach their destination.    
            currentSpace = newSpace;


            //GameManager gm = GameManager.instance;
            if (tm == null)
            {
                if (currentSpace != gm.homeSpace)
                    gotStarFromHome = false;
            }
            else
            {
                if (currentSpace != tm.homeSpace)
                    gotStarFromHome = false;
            }



            //can now start collecting stars from Home
            newGameStarted = false;
            
            animateMoveCoroutineOn = false;
            playerMoving = false;
        }

        IEnumerator AnimateJump(BoardSpace destination)
        {
            direction = destination.transform.position - transform.position;
            float moveSpeed = 16;
            float minDist = 1.2f;   //used to stop player movement onto new space and to not overshoot. If this value is too high, it results in jerky movement.

            
            while (transform.position != destination.transform.position)
            {
                float vx = moveSpeed * direction.x * Time.deltaTime;
                float vy = moveSpeed * direction.y * Time.deltaTime;

                //transform.position = new Vector3(transform.position.x + vx, transform.position.y + vy, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, destination.transform.position, moveSpeed * Time.deltaTime);

                //check if we're close to destination
                /*float diffX = Mathf.Abs(destination.transform.position.x - transform.position.x);
                float diffY = Mathf.Abs(destination.transform.position.y - transform.position.y);
                if (diffX >= 0 && diffX <= minDist && diffY >= 0 && diffY <= minDist)
                    transform.position = destination.transform.position;*/

                 yield return null;
            }

            //change player's location once they physically reach reach their destination.    
            currentSpace = destination;
            oldSpace.playersOnSpace.Remove(this);
            currentSpace.playersOnSpace.Add(this);
            //GameManager gm = GameManager.instance;
            if (tm == null)
            {
                if (currentSpace != gm.homeSpace)
                    gotStarFromHome = false;

                //can now start collecting stars from Home
                newGameStarted = false;

                //change game state
                if (currentSpace.playersOnSpace.Count > 1)
                {
                    gm.ArrangePlayerPositions(gm.playerList, this);

                    //set game state to encounter
                    gm.SetGameState(GameManager.GameState.StartEncounter);
                }
                else
                    //activate the space the player is on
                    gm.SetGameState(GameManager.GameState.CheckingSpace);
            }
            else
            {
                if (currentSpace != tm.homeSpace)
                    gotStarFromHome = false;

                //can now start collecting stars from Home
                newGameStarted = false;

                //change game state
                if (currentSpace.playersOnSpace.Count > 1)
                {
                    tm.ArrangePlayerPositions(tm.playerList, this);

                    //set game state to encounter
                    tm.SetGameState(TutorialManager.GameState.StartEncounter);
                }
                else
                    //activate the space the player is on
                    tm.SetGameState(TutorialManager.GameState.CheckingSpace);
            }

        }


        /* This coroutine keeps running until player finishes moving. */
        IEnumerator PlayerMovement(int moveCount)
        {
            //GameManager gm = GameManager.instance;
            currentSpace.SetDefaultPosition(this);      //put player back in middle of space before moving
            currentSpace.playersOnSpace.Remove(this);   //remove player from current space since they're going to move

            for (int i = 0; i < moveCount; i++)
            {
                if (tm == null)
                    CheckSurroundingSpaces(gm.boardData);
                else
                    CheckSurroundingSpaces(tm.boardData);

                /*if there's more than one route, must resolve before player can continue
                after player picks a route, delete all other routes.*/
                if (route.Count > 1)
                {
                    //remove previous space first
                    for (int j = 0; j < route.Count; j++)
                    {
                        if (oldSpace != null && oldSpace.row == route[j].row && oldSpace.col == route[j].col)
                        {
                            route.Remove(route[j]);
                            j--;
                        }
                    }

                    //if there's still more than 1 route, run coroutine
                    if (route.Count > 1)
                    {
                        //run coroutine to let player pick a route
                        yield return DisplayRoutes();
                    }
                }

                //if we get here without running the above code, that means the only route is the space the player came from.
                //The player hit a dead end and must turn back.             
                //Debug.Log("Route " + route[0].spaceType);
               
                Move(route[0]);
                yield return AnimateMovement();
                int newMoveCount = moveCount - (i + 1);
                moveCountUI.text = newMoveCount.ToString();
            }

            //add player to space
            currentSpace.playersOnSpace.Add(this);
            oldSpace.playersOnSpace.Remove(this);
            
            //TODO: check for an encounter
            if (tm == null)
            {
                if (currentSpace.playersOnSpace.Count > 1)
                {
                    gm.ArrangePlayerPositions(gm.playerList, this);

                    //set game state to encounter
                    gm.SetGameState(GameManager.GameState.StartEncounter);
                }
                else
                    //activate the space the player is on
                    gm.SetGameState(GameManager.GameState.CheckingSpace);
            }
            else
            {
                if (currentSpace.playersOnSpace.Count > 1)
                {
                    tm.ArrangePlayerPositions(tm.playerList, this);

                    //set game state to encounter
                    tm.SetGameState(TutorialManager.GameState.StartEncounter);
                }
                else
                    //activate the space the player is on
                    tm.SetGameState(TutorialManager.GameState.CheckingSpace);
            }
            
        }

        //displays arrows next to player. These arrows can be clicked on to choose a path to take.
        IEnumerator DisplayRoutes()
        {
            //GameManager gm = GameManager.instance;
            UI ui = UI.instance;
            if (tm == null)
            {
                gm.EnableArrowContainer(true);

                //check all routes and display only arrows for available routes
                for (int i = 0; i < route.Count; i++)
                {
                    if(route[i].row < row)                  //space is above player
                    {
                        gm.ShowArrow(gm.arrowUp, true);
                        gm.arrowUp.AssignRoute(this, route[i]);
                        gm.arrowUp.arrowDirection = Arrow.Direction.Up;
                    }
                    else if (route[i].row > row)            //space is below player
                    {
                        gm.ShowArrow(gm.arrowDown, true);
                        gm.arrowDown.AssignRoute(this, route[i]);
                        gm.arrowDown.arrowDirection = Arrow.Direction.Down;
                    }
                    else if (route[i].col > col)            //space is to the right of player
                    {
                        gm.ShowArrow(gm.arrowRight, true);
                        gm.arrowRight.AssignRoute(this, route[i]);
                        gm.arrowRight.arrowDirection = Arrow.Direction.Right;
                    }
                    else if (route[i].col < col)            //space is to the left of player
                    {
                        gm.ShowArrow(gm.arrowLeft, true);
                        gm.arrowLeft.AssignRoute(this, route[i]);
                        gm.arrowLeft.arrowDirection = Arrow.Direction.Left;
                    }
                }
            }
            else
            {
                tm.EnableArrowContainer(true);

                //check all routes and display only arrows for available routes
                for (int i = 0; i < route.Count; i++)
                {
                    if(route[i].row < row)                  //space is above player
                    {
                        tm.ShowArrow(tm.arrowUp, true);
                        tm.arrowUp.AssignRoute(this, route[i]);
                        tm.arrowUp.arrowDirection = Arrow.Direction.Up;
                    }
                    else if (route[i].row > row)            //space is below player
                    {
                        tm.ShowArrow(tm.arrowDown, true);
                        tm.arrowDown.AssignRoute(this, route[i]);
                        tm.arrowDown.arrowDirection = Arrow.Direction.Down;
                    }
                    else if (route[i].col > col)            //space is to the right of player
                    {
                        tm.ShowArrow(tm.arrowRight, true);
                        tm.arrowRight.AssignRoute(this, route[i]);
                        tm.arrowRight.arrowDirection = Arrow.Direction.Right;
                    }
                    else if (route[i].col < col)            //space is to the left of player
                    {
                        tm.ShowArrow(tm.arrowLeft, true);
                        tm.arrowLeft.AssignRoute(this, route[i]);
                        tm.arrowLeft.arrowDirection = Arrow.Direction.Left;
                    }
                }
            }

            //player will not move until until player clicks one of the arrows to choose a route
            if (isAI)
            {
                //we run a coroutine to determine which route to take
                yield return AIChooseRoute();
            }
            else
            {
                ui.DisplayAlert("choosingRoute");
                //ui.DisplayAlert("Click on an arrow to choose a route.");
                while (route.Count > 1)        
                    yield return null;
            }
    
        }

        IEnumerator AIChooseRoute()
        {
            UI ui = UI.instance;
            //GameManager gm = GameManager.instance;

            //need to know which routes are available.
            List<Arrow> directions = new List<Arrow>();
            List<BoardSpace> destinationSpaces = new List<BoardSpace>();

            if (tm == null)
            {
                if (gm.arrowDown.gameObject.activeSelf)
                    directions.Add(gm.arrowDown);
                if (gm.arrowUp.gameObject.activeSelf)
                    directions.Add(gm.arrowUp);
                if (gm.arrowLeft.gameObject.activeSelf)
                    directions.Add(gm.arrowLeft);
                if (gm.arrowRight.gameObject.activeSelf)
                    directions.Add(gm.arrowRight);
            }
            else
            {
                if (tm.arrowDown.gameObject.activeSelf)
                    directions.Add(tm.arrowDown);
                if (tm.arrowUp.gameObject.activeSelf)
                    directions.Add(tm.arrowUp);
                if (tm.arrowLeft.gameObject.activeSelf)
                    directions.Add(tm.arrowLeft);
                if (tm.arrowRight.gameObject.activeSelf)
                    directions.Add(tm.arrowRight);
            }

            Debug.Log("# of Directions for AI: " + directions.Count);
            
            while (route.Count > 1)
            {
                ui.DisplayAlert("aiChooseRoute", playerName);
                //ui.DisplayAlert(playerName + " is choosing route");


                //look ahead of each route and check which space we will land on
                for (int i = 0; i < directions.Count; i++)
                {
                    destinationSpaces.Add(LookAhead(moveTotal, directions[i].arrowDirection));
                    Debug.Log("Destination " + i + ": " + destinationSpaces[i].spaceType + "(" + destinationSpaces[i].row + ", " + destinationSpaces[i].col + ")");
                }

                //Look at what the spaces are and determine which way to go. A space's priority changes depending on player's current status   
                foreach(BoardSpace destination in destinationSpaces)
                {
                    switch(destination.spaceType)
                    {
                        case BoardSpace.SpaceType.StarCache:
                            //look at star total. If player is trailing, prioritize getting stars
                            if (tm == null)
                            {
                                if (!gm.extraRound)
                                {
                                    Player leadingPlayer = GetLeadingPlayer(gm.playerList);

                                    if (leadingPlayer != null && leadingPlayer != this)
                                    {
                                        destination.weight = 9;
                                    }
                                    else
                                    {
                                        destination.weight = 3;
                                    }
                                }
                                else
                                {
                                    destination.weight = 9;
                                }
                            }
                            else
                            {
                                Player leadingPlayer = GetLeadingPlayer(tm.playerList);

                                if (leadingPlayer != null && leadingPlayer != this)
                                {
                                    destination.weight = 9;
                                }
                                else
                                {
                                    destination.weight = 3;
                                }
                            }
                            break;

                        case BoardSpace.SpaceType.DrawCard:
                            //if player is leading in stars, then 50% chance of going for cards.
                            if (tm == null)
                            {
                                if (!gm.extraRound)
                                {
                                    Player leadingPlayer = GetLeadingPlayer(gm.playerList);

                                    if (leadingPlayer != null && leadingPlayer == this)
                                    {
                                        if (Random.value <= 0.5f)
                                            destination.weight = 9;
                                        else
                                            destination.weight = 3;
                                    }
                                    if (hand.Count <= 0)   //higher chance going for cards if they have none
                                    {
                                        if (Random.value <= 0.75f)
                                            destination.weight = 9;
                                        else
                                            destination.weight = 3;
                                    }
                                    else
                                    {
                                        destination.weight = 0;
                                    }
                                }
                                else
                                {
                                    destination.weight = 1; //if compared to home space during an extra round, this is preferable if not leading player
                                }
                            }
                            else
                            {
                                Player leadingPlayer = GetLeadingPlayer(tm.playerList);

                                if (leadingPlayer != null && leadingPlayer == this)
                                {
                                    if (Random.value <= 0.5f)
                                        destination.weight = 9;
                                    else
                                        destination.weight = 3;
                                }
                                if (hand.Count <= 0)   //higher chance going for cards if they have none
                                {
                                    if (Random.value <= 0.75f)
                                        destination.weight = 9;
                                    else
                                        destination.weight = 3;
                                }
                                else
                                {
                                    destination.weight = 0;
                                }
                            }
                            break;

                        case BoardSpace.SpaceType.Home:
                            //player always chooses home if it's available, regardless of star count. In an extra round, 
                            //player only goes home if they're in the lead
                            if (tm == null)
                            {
                                if (!gm.extraRound)
                                    destination.weight = destination.maxWeight;
                                else
                                {
                                    Player leadingPlayer = GetLeadingPlayer(gm.playerList);
                                    if (leadingPlayer != null)
                                    {
                                        if (leadingPlayer != this)
                                        {
                                            //can the player win if they move to this space?
                                            if (starTotal + 1 > leadingPlayer.starTotal)
                                                destination.weight = destination.maxWeight;
                                            else
                                                destination.weight = 0;
                                        }
                                        else
                                            destination.weight = destination.maxWeight;
                                    }
                                }
                            }
                            else
                            {
                                destination.weight = destination.maxWeight;
                            }
                            break;
                    }
                }

                //AI picks the space with the highest weight
                int chosenDirection = 0;
                BoardSpace chosenSpace = destinationSpaces[0];

                foreach(BoardSpace space in destinationSpaces)
                {
                    if (space.weight > chosenSpace.weight)
                    {
                        chosenSpace = space;
                        chosenDirection = destinationSpaces.IndexOf(space);
                    }
                }

                //check for rotues with equal weight
                List<BoardSpace> tiedSpaces = new List<BoardSpace>();
                foreach(BoardSpace space in destinationSpaces)
                {
                    if (space.weight == chosenSpace.weight)
                    {
                        tiedSpaces.Add(space);
                    }
                }
                
                //if there's more than 1 tied space, must choose a random destination
                if (tiedSpaces.Count > 1)
                {
                    if (tiedSpaces.Count == 2)
                    {
                        Debug.Log("2 Routes are equal, AI is choosing a random route");
                        chosenDirection = Random.value <= 0.5f ? destinationSpaces.IndexOf(tiedSpaces[0]) : destinationSpaces.IndexOf(tiedSpaces[1]);
                    }
                    else if (tiedSpaces.Count == 3)
                    {
                        Debug.Log("3 Routes are equal, AI is choosing a random route");
                        float routeChance = Random.value;
                        if (routeChance <= 0.33f)
                            chosenDirection = destinationSpaces.IndexOf(tiedSpaces[0]);
                        else if (routeChance <= 0.66f)
                            chosenDirection = destinationSpaces.IndexOf(tiedSpaces[1]);
                        else
                            chosenDirection = destinationSpaces.IndexOf(tiedSpaces[2]);
                    }
                }

                yield return new WaitForSeconds(1.5f);

                //finally, AI moves along their chosen route
                Debug.Log(playerName + " picked " + destinationSpaces[chosenDirection].spaceType + "(" + destinationSpaces[chosenDirection].row + ", " + destinationSpaces[chosenDirection].col + ")");
                PickRoute(directions[chosenDirection]);
            }

            if (tm == null)
                gm.EnableArrowContainer(false);
            else
                tm.EnableArrowContainer(false);
            ui.ToggleAlertUI(false);
        }
        
        //look at a number of board spaces with a given direction. 
        BoardSpace LookAhead(int moveCount, Arrow.Direction direction)
        {
            //GameManager gm = GameManager.instance;

            int i = 0;
            bool multiplePathsFound = false;
            int rowCopy = row;
            int colCopy = col;
            BoardSpace currentSpace = null;
            BoardSpace previousSpace = this.currentSpace;
            List<BoardSpace> route = new List<BoardSpace>();
            bool spaceFound = false;

            //the get the first space to look at. This will establish the direction the player takes to look at the remaining spaces.
            switch(direction)
            {
                case Arrow.Direction.Up:
                    if (tm == null)
                    {
                        while(i < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[i].row == rowCopy - 1 && gm.boardSpaceList[i].col == colCopy)
                            {
                                spaceFound = true;
                                currentSpace = gm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    else
                    {
                        while(i < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[i].row == rowCopy - 1 && tm.boardSpaceList[i].col == colCopy)
                            {
                                spaceFound = true;
                                currentSpace = tm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    break;

                case Arrow.Direction.Down:
                    if (tm == null)
                    {
                        while(i < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[i].row == rowCopy + 1 && gm.boardSpaceList[i].col == colCopy)
                            {
                                spaceFound = true;
                                currentSpace = gm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    else
                    {
                        while(i < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[i].row == rowCopy + 1 && tm.boardSpaceList[i].col == colCopy)
                            {
                                spaceFound = true;
                                currentSpace = tm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    break;

                case Arrow.Direction.Left:
                    if (tm == null)
                    {
                        while(i < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[i].row == rowCopy && gm.boardSpaceList[i].col == colCopy - 1)
                            {
                                spaceFound = true;
                                currentSpace = gm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    else
                    {
                        while(i < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[i].row == rowCopy && tm.boardSpaceList[i].col == colCopy - 1)
                            {
                                spaceFound = true;
                                currentSpace = tm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    break;

                case Arrow.Direction.Right:
                    if (tm == null)
                    {
                        while(i < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[i].row == rowCopy && gm.boardSpaceList[i].col == colCopy + 1)
                            {
                                spaceFound = true;
                                currentSpace = gm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    else
                    {
                        while(i < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[i].row == rowCopy && tm.boardSpaceList[i].col == colCopy + 1)
                            {
                                spaceFound = true;
                                currentSpace = tm.boardSpaceList[i];
                                route.Add(currentSpace);
                            }
                            else 
                                i++;
                        }
                    }
                    break;
            }

            //check the rest of the spaces and use the remaining moveCount
            moveCount--;
            i = 0;
            rowCopy = currentSpace.row;
            colCopy = currentSpace.col;

            while (i < moveCount && !multiplePathsFound)
            {
                //find the next space by checking surrounding spaces.
                //north check
                if (tm == null)
                {
                    if (rowCopy > 0 && (gm.boardData[rowCopy - 1][colCopy] != "0" || gm.boardData[rowCopy - 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy - 1 && gm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        } 
                    }
                }
                else
                {
                    if (rowCopy > 0 && (tm.boardData[rowCopy - 1][colCopy] != "0" || tm.boardData[rowCopy - 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy - 1 && tm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        } 
                    }
                }

                //south check
                if (tm == null)
                {
                    if (rowCopy < gm.boardData.Count - 1 && (gm.boardData[rowCopy + 1][colCopy] != "0" || gm.boardData[rowCopy + 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy + 1 && gm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                else
                {
                    if (rowCopy < tm.boardData.Count - 1 && (tm.boardData[rowCopy + 1][colCopy] != "0" || tm.boardData[rowCopy + 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy + 1 && tm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }

                //east check
                if (tm == null)
                {
                    if (colCopy < gm.boardData[0].Length - 1 && (gm.boardData[rowCopy][colCopy + 1] != "0" || gm.boardData[rowCopy][colCopy + 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy && gm.boardSpaceList[j].col == colCopy + 1)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                else
                {
                    if (colCopy < tm.boardData[0].Length - 1 && (tm.boardData[rowCopy][colCopy + 1] != "0" || tm.boardData[rowCopy][colCopy + 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy && tm.boardSpaceList[j].col == colCopy + 1)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }

                //west check
                if (tm == null)
                {
                    if (colCopy > 0 && (gm.boardData[rowCopy][colCopy - 1] != "0" || gm.boardData[rowCopy][colCopy - 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy && gm.boardSpaceList[j].col == colCopy - 1)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                else
                {
                    if (colCopy > 0 && (tm.boardData[rowCopy][colCopy - 1] != "0" || tm.boardData[rowCopy][colCopy - 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy && tm.boardSpaceList[j].col == colCopy - 1)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                
                //delete the first space from route as that's an old space. Whatever remains is the new space we move to.
                if (route.Count > 1)
                {
                    previousSpace = route[0];
                    route.Remove(route[0]);
                }

                //if there's still more than 1 route, then AI moves up to the branching path
                if (route.Count == 1)
                {
                    currentSpace = route[0];
                    rowCopy = currentSpace.row;
                    colCopy = currentSpace.col;
                }
                else
                {
                    //TODO: AI must move to the branching path, then look ahead must start again using the remaining move
                }
                i++;
            }


            //what is the final space?
            //Debug.Log("Destination space is " + currentSpace.spaceType + " at row " + currentSpace.row + ", col " + currentSpace.col);
            return currentSpace;

            //TODO: if there were multiple paths, we need to note how many spaces we moved so far by recording i's value
        }

        BoardSpace LookAhead(int moveCount, Vector3 direction)
        {
            //GameManager gm = GameManager.instance;

            int i = 0;
            bool multiplePathsFound = false;
            int rowCopy = row;
            int colCopy = col;
            BoardSpace currentSpace = null;
            BoardSpace previousSpace = this.currentSpace;
            List<BoardSpace> route = new List<BoardSpace>();
            bool spaceFound = false;

            //the get the first space to look at. This will establish the direction the player takes to look at the remaining spaces.
            
            if (direction.y > 0)
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy - 1 && gm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy - 1 && tm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                
            }
            else if (direction.y < 0)
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy + 1 && gm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy + 1 && tm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }
            else if (direction.x < 0)
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy && gm.boardSpaceList[i].col == colCopy - 1)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy && tm.boardSpaceList[i].col == colCopy - 1)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }
            else if (direction.x > 0)
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy && gm.boardSpaceList[i].col == colCopy + 1)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy && tm.boardSpaceList[i].col == colCopy + 1)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }
            
            //if there's no direction, we bail.
            if (currentSpace == null)
                return null;

            //check the rest of the spaces and use the remaining moveCount
            moveCount--;
            i = 0;
            rowCopy = currentSpace.row;
            colCopy = currentSpace.col;

            //find the next space by checking surrounding spaces.
            while (i < moveCount && !multiplePathsFound)
            {
                //north check
                if (tm == null)
                {
                    if (rowCopy > 0 && (gm.boardData[rowCopy - 1][colCopy] != "0" || gm.boardData[rowCopy - 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy - 1 && gm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        } 
                    }
                }
                else
                {
                    if (rowCopy > 0 && (tm.boardData[rowCopy - 1][colCopy] != "0" || tm.boardData[rowCopy - 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy - 1 && tm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        } 
                    }
                }

                //south check
                if (tm == null)
                {
                    if (rowCopy < gm.boardData.Count - 1 && (gm.boardData[rowCopy + 1][colCopy] != "0" || gm.boardData[rowCopy + 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy + 1 && gm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                else
                {
                    if (rowCopy < tm.boardData.Count - 1 && (tm.boardData[rowCopy + 1][colCopy] != "0" || tm.boardData[rowCopy + 1][colCopy] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy + 1 && tm.boardSpaceList[j].col == colCopy)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }

                //east check
                if (tm == null)
                {
                    if (colCopy < gm.boardData[0].Length - 1 && (gm.boardData[rowCopy][colCopy + 1] != "0" || gm.boardData[rowCopy][colCopy + 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy && gm.boardSpaceList[j].col == colCopy + 1)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                else
                {
                    if (colCopy < tm.boardData[0].Length - 1 && (tm.boardData[rowCopy][colCopy + 1] != "0" || tm.boardData[rowCopy][colCopy + 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy && tm.boardSpaceList[j].col == colCopy + 1)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }

                //west check
                if (tm == null)
                {
                    if (colCopy > 0 && (gm.boardData[rowCopy][colCopy - 1] != "0" || gm.boardData[rowCopy][colCopy - 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < gm.boardSpaceList.Count && !spaceFound)
                        {
                            if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy && gm.boardSpaceList[j].col == colCopy - 1)
                            {
                                spaceFound = true;
                                route.Add(gm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                else
                {
                    if (colCopy > 0 && (tm.boardData[rowCopy][colCopy - 1] != "0" || tm.boardData[rowCopy][colCopy - 1] != null))
                    {
                        int j = 0;
                        spaceFound = false;
                        while(j < tm.boardSpaceList.Count && !spaceFound)
                        {
                            if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy && tm.boardSpaceList[j].col == colCopy - 1)
                            {
                                spaceFound = true;
                                route.Add(tm.boardSpaceList[j]);
                            }
                            else 
                                j++;
                        }
                    }
                }
                
                //delete the first space from route as that's an old space. Whatever remains is the new space we move to.
                if (route.Count > 1)
                {
                    previousSpace = route[0];
                    route.Remove(route[0]);
                }

                //if there's still more than 1 route, then AI moves up to the branching path
                if (route.Count == 1)
                {
                    currentSpace = route[0];
                    rowCopy = currentSpace.row;
                    colCopy = currentSpace.col;
                }
                i++;
            }

            //what is the final space?
            //Debug.Log("Destination space is " + currentSpace.spaceType + " at row " + currentSpace.row + ", col " + currentSpace.col);
            return currentSpace;
        }

        //used by AI to search for first available star cache with a given value. Search begins from player's position.
        public StarCache NearestStarCache(Vector3 direction, float maxTargetRange)
        {
            //GameManager gm = GameManager.instance;
            StarCache starCacheSpace = null;

            //int i = 0;
            bool multiplePathsFound = false;
            int rowCopy = row;
            int colCopy = col;
            BoardSpace currentSpace = null;
            BoardSpace previousSpace = this.currentSpace;
            List<BoardSpace> route = new List<BoardSpace>();
            bool spaceFound = false;

            //the get the first space to look at. This will establish the direction the player takes to look at the remaining spaces.
            int i = 0;
            if (direction.y > 0)    //facing up
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy - 1 && gm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy - 1 && tm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }
            else if (direction.y < 0)   //facing down
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy + 1 && gm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy + 1 && tm.boardSpaceList[i].col == colCopy)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }
            else if (direction.x < 0)   //facing left
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy && gm.boardSpaceList[i].col == colCopy - 1)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy && tm.boardSpaceList[i].col == colCopy - 1)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }
            else if (direction.x > 0)   //facing right
            {
                if (tm == null)
                {
                    while(i < gm.boardSpaceList.Count && !spaceFound)
                    {
                        if (gm.boardSpaceList[i].row == rowCopy && gm.boardSpaceList[i].col == colCopy + 1)
                        {
                            spaceFound = true;
                            currentSpace = gm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
                else
                {
                    while(i < tm.boardSpaceList.Count && !spaceFound)
                    {
                        if (tm.boardSpaceList[i].row == rowCopy && tm.boardSpaceList[i].col == colCopy + 1)
                        {
                            spaceFound = true;
                            currentSpace = tm.boardSpaceList[i];
                            route.Add(currentSpace);
                        }
                        else 
                            i++;
                    }
                }
            }

            //check the space that was added to see if it's a star cache. If no space was added, then return null
            bool cacheFound = false;
            if (route.Count <= 0) return null;

            if (route[0].TryGetComponent(out StarCache cache))
            {
                if (cache.probability <= maxTargetRange)
                {
                    cacheFound = true;
                    starCacheSpace = cache;
                }
            }

            if (cacheFound)
            {
                return starCacheSpace;
            }
            else
            {
                //check the rest of the spaces from where i left off.
                i = 0;
                rowCopy = currentSpace.row;
                colCopy = currentSpace.col;
                
                int boardSpaceCount;
                if (tm == null)
                    boardSpaceCount = gm.boardSpaceList.Count;
                else
                    boardSpaceCount = tm.boardSpaceList.Count;

                while (i < boardSpaceCount && !cacheFound)
                {
                    //find the next space by checking surrounding spaces.
                    //north check
                    if (tm == null)
                    {
                        if (rowCopy > 0 && (gm.boardData[rowCopy - 1][colCopy] != "0" || gm.boardData[rowCopy - 1][colCopy] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < gm.boardSpaceList.Count && !spaceFound)
                            {
                                if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy - 1 && gm.boardSpaceList[j].col == colCopy)
                                {
                                    spaceFound = true;
                                    route.Add(gm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            } 
                        }
                    }
                    else
                    {
                        if (rowCopy > 0 && (tm.boardData[rowCopy - 1][colCopy] != "0" || tm.boardData[rowCopy - 1][colCopy] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < tm.boardSpaceList.Count && !spaceFound)
                            {
                                if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy - 1 && tm.boardSpaceList[j].col == colCopy)
                                {
                                    spaceFound = true;
                                    route.Add(tm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            } 
                        }
                    }

                    //south check
                    if (tm == null)
                    {
                        if (rowCopy < gm.boardData.Count - 1 && (gm.boardData[rowCopy + 1][colCopy] != "0" || gm.boardData[rowCopy + 1][colCopy] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < gm.boardSpaceList.Count && !spaceFound)
                            {
                                if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy + 1 && gm.boardSpaceList[j].col == colCopy)
                                {
                                    spaceFound = true;
                                    route.Add(gm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            }
                        }
                    }
                    else
                    {
                        if (rowCopy < tm.boardData.Count - 1 && (tm.boardData[rowCopy + 1][colCopy] != "0" || tm.boardData[rowCopy + 1][colCopy] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < tm.boardSpaceList.Count && !spaceFound)
                            {
                                if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy + 1 && tm.boardSpaceList[j].col == colCopy)
                                {
                                    spaceFound = true;
                                    route.Add(tm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            }
                        }
                    }

                    //east check
                    if (tm == null)
                    {
                        if (colCopy < gm.boardData[0].Length - 1 && (gm.boardData[rowCopy][colCopy + 1] != "0" || gm.boardData[rowCopy][colCopy + 1] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < gm.boardSpaceList.Count && !spaceFound)
                            {
                                if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy && gm.boardSpaceList[j].col == colCopy + 1)
                                {
                                    spaceFound = true;
                                    route.Add(gm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            }
                        }
                    }
                    else
                    {
                        if (colCopy < tm.boardData[0].Length - 1 && (tm.boardData[rowCopy][colCopy + 1] != "0" || tm.boardData[rowCopy][colCopy + 1] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < tm.boardSpaceList.Count && !spaceFound)
                            {
                                if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy && tm.boardSpaceList[j].col == colCopy + 1)
                                {
                                    spaceFound = true;
                                    route.Add(tm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            }
                        }
                    }

                    //west check
                    if (tm == null)
                    {
                        if (colCopy > 0 && (gm.boardData[rowCopy][colCopy - 1] != "0" || gm.boardData[rowCopy][colCopy - 1] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < gm.boardSpaceList.Count && !spaceFound)
                            {
                                if (gm.boardSpaceList[j] != previousSpace && gm.boardSpaceList[j].row == rowCopy && gm.boardSpaceList[j].col == colCopy - 1)
                                {
                                    spaceFound = true;
                                    route.Add(gm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            }
                        }
                    }
                    else
                    {
                        if (colCopy > 0 && (tm.boardData[rowCopy][colCopy - 1] != "0" || tm.boardData[rowCopy][colCopy - 1] != null))
                        {
                            int j = 0;
                            spaceFound = false;
                            while(j < tm.boardSpaceList.Count && !spaceFound)
                            {
                                if (tm.boardSpaceList[j] != previousSpace && tm.boardSpaceList[j].row == rowCopy && tm.boardSpaceList[j].col == colCopy - 1)
                                {
                                    spaceFound = true;
                                    route.Add(tm.boardSpaceList[j]);
                                }
                                else 
                                    j++;
                            }
                        }
                    }
                    
                    //delete the first space from route as that's an old space. Whatever remains is the new space we move to.
                    if (route.Count > 1)
                    {
                        previousSpace = route[0];
                        route.Remove(route[0]);
                    }

                    //if there's still more than 1 route, then AI moves up to the branching path
                    if (route.Count == 1)
                    {
                        currentSpace = route[0];
                        rowCopy = currentSpace.row;
                        colCopy = currentSpace.col;

                        //is this space a star cache?
                        if (route[0].TryGetComponent(out StarCache starCache))
                        {
                            if (starCache.probability <= maxTargetRange)
                            {
                                cacheFound = true;
                                starCacheSpace = starCache;
                            }
                        }
                    }
                    else
                    {
                        //TODO: AI must move to the branching path, then look ahead must start again using the remaining move
                    }
                    i++;
                }

                //what is the final space?
                return starCacheSpace;
            }

        }

        //AI coroutine. Used to select a card based on different criteria.
        IEnumerator SelectCard()
        {
            List<CardData> chosenCards = new List<CardData>();     //cards that AI decides to use. Must narrow these down to 1
            //GameManager gm = GameManager.instance;
            Player leadingPlayer = this;                    //used for checking star totals

            foreach(CardData card in hand)
            {
                switch(card.cardID)
                {
                    case 0:         //swift move
                        //check status
                        if (cardEffect != null && cardEffect.cardID <= 3)    //move 1, 2, 3
                        {
                            chosenCards.Add(card);
                        }
                        else
                        {
                            //AI randomly decides to use this card
                            if (Random.value <= 0.5f)
                                chosenCards.Add(card);
                        }
                        break;

                    case 1:     //'move X' cards
                        //used against the leading player, or AI may use it on itself to reach a desired space.
                        leadingPlayer = (tm == null) ? GetLeadingPlayer(gm.playerList) : GetLeadingPlayer(tm.playerList);

                        if (leadingPlayer != null && leadingPlayer != this && leadingPlayer.cardEffect == null)
                        {
                            //check if leading player is close
                            BoardSpace destinationSpace = LookAhead(1, direction);
                            if (destinationSpace != null && destinationSpace.playersOnSpace.Contains(leadingPlayer))
                            {
                                if (tm == null)
                                    gm.target = this;
                                else
                                    tm.target = this;
                                chosenCards.Add(card);              
                            }
                            else
                            {
                                //try to slow down the player
                                if (tm == null)
                                    gm.target = leadingPlayer;
                                else
                                    tm.target = leadingPlayer;
                                chosenCards.Add(card);
                            }
                        }
                        else
                        {
                            //see if card can be used on AI
                            BoardSpace destinationSpace = LookAhead(1, direction);
                            if (destinationSpace != null && destinationSpace.spaceType == BoardSpace.SpaceType.DrawCard)
                            {
                                if (tm == null)
                                    gm.target = this;
                                else
                                    tm.target = this;
                                chosenCards.Add(card);              
                            }
                        }
                        break;

                    case 2:
                        leadingPlayer = (tm == null) ? GetLeadingPlayer(gm.playerList) : GetLeadingPlayer(tm.playerList);

                        if (leadingPlayer != null && leadingPlayer != this && leadingPlayer.cardEffect == null)
                        {
                            //check if leading player is close
                            BoardSpace destinationSpace = LookAhead(2, direction);
                            if (destinationSpace != null && destinationSpace.playersOnSpace.Contains(leadingPlayer))
                            {
                                if (tm == null)
                                    gm.target = this;
                                else
                                    tm.target = this;
                                chosenCards.Add(card);              
                            }
                            else
                            {
                                if (tm == null)
                                    gm.target = leadingPlayer;
                                else
                                    tm.target = leadingPlayer;
                                chosenCards.Add(card);
                            }
                        }
                        else
                        {
                            //see if card can be used on AI
                            BoardSpace destinationSpace = LookAhead(2, direction);
                            if (destinationSpace != null && destinationSpace.spaceType == BoardSpace.SpaceType.DrawCard)
                            {
                                if (tm == null)
                                    gm.target = this;
                                else
                                    tm.target = this;
                                chosenCards.Add(card);              
                            }
                        }
                        break;

                    case 3:
                        leadingPlayer = (tm == null) ? GetLeadingPlayer(gm.playerList) : GetLeadingPlayer(tm.playerList);

                        if (leadingPlayer != null && leadingPlayer != this && leadingPlayer.cardEffect == null)
                        {
                            //check if leading player is close
                            BoardSpace destinationSpace = LookAhead(3, direction);
                            if (destinationSpace != null && destinationSpace.playersOnSpace.Contains(leadingPlayer))
                            {
                                if (tm == null)
                                    gm.target = this;
                                else
                                    tm.target = this;
                                chosenCards.Add(card);              
                            }
                            else
                            {
                                if (tm == null)
                                    gm.target = leadingPlayer;
                                else
                                    tm.target = leadingPlayer;
                                chosenCards.Add(card);
                            }
                        }
                        else
                        {
                            //here we check if Home is nearby. Move 3 is used to prevent rolling a 2, even if it's a small chance
                            BoardSpace destinationSpace = LookAhead(3, direction);
                            if (destinationSpace != null)
                            {
                                if (destinationSpace.spaceType == BoardSpace.SpaceType.Home)   
                                {
                                    if (tm == null)
                                        gm.target = this;
                                    else
                                        tm.target = this;
                                    chosenCards.Add(card);              
                                }
                                else if (destinationSpace.spaceType == BoardSpace.SpaceType.DrawCard)
                                {
                                    if (tm == null)
                                        gm.target = this;
                                    else
                                        tm.target = this;
                                    chosenCards.Add(card);              
                                }
                            }
                        }
                        break;   

                    case 4:     //extend
                        //AI only uses this card if they aren't in the lead and game has progressed more than halfway through the rounds
                        if (tm == null)
                        {
                            if (gm.currentRound >= 8)
                            {
                                leadingPlayer = GetLeadingPlayer(gm.playerList);

                                if (leadingPlayer != null && leadingPlayer != this)
                                {
                                    gm.target = leadingPlayer;
                                    chosenCards.Add(card);
                                }
                            }
                        }    
                        break;

                    case 5: //refill
                        //look ahead and see if there are any star caches with 0.5 or less. If there is, add this card to list
                        //AI waits until at least round 3 so that reduces the chance of the card being wasted
                        if (tm == null)
                        {
                            if (gm.currentRound >= 3)
                            {
                                targetCache = NearestStarCache(direction, 0.5f);
                                if (targetCache != null)
                                    chosenCards.Add(card);
                            }
                        }
                        else
                        {
                            targetCache = NearestStarCache(direction, 0.5f);
                                if (targetCache != null)
                                    chosenCards.Add(card);
                        }
                        break;

                    case 6: //go home
                        //high value card, but it's only added when player has at least 9 stars and the we're on last round.
                        if (tm == null)
                        {
                            if (starTotal >= 9 && gm.currentRound >= gm.maxRounds)
                                chosenCards.Add(card);
                        }
                        break;

                    case 7: //jump
                        //this card is added if there's a player close by and they have more stars than the current player
                        if (tm == null)
                        {
                            if (gm.currentRound >= 2)
                            {
                                leadingPlayer = GetLeadingPlayer(gm.playerList);
                                Player closestPlayer = GetNearestPlayer(gm.playerList);
                                if (leadingPlayer != null)
                                {
                                    if (leadingPlayer != this && currentSpace != leadingPlayer.currentSpace && leadingPlayer == closestPlayer)
                                    {
                                        chosenCards.Add(card);
                                    }
                                }
                            }
                        }
                        else
                        {
                            leadingPlayer = GetLeadingPlayer(tm.playerList);
                            Player closestPlayer = GetNearestPlayer(tm.playerList);
                            if (leadingPlayer != null)
                            {
                                if (leadingPlayer != this && currentSpace != leadingPlayer.currentSpace && leadingPlayer == closestPlayer)
                                {
                                    chosenCards.Add(card);
                                }
                            }
                            
                        }
                        break;
                }
            }

            Debug.Log("Chosen cards:\n");
            string cards = "";
            foreach(CardData card in chosenCards)
            {
                cards += card.cardName + "\n";
            }
            Debug.Log(cards);

            //check chosen cards and check their weight. Use card with highest weight
            if (chosenCards.Count <= 0)
            {
                //skip card phase
                Debug.Log("No cards chosen");
                if (tm == null)
                    gm.SetGameState(GameManager.GameState.RollingDice);
                else
                    tm.SetGameState(TutorialManager.GameState.RollingDice);
            }
            else
            {
                UI ui = UI.instance;
                ui.DisplayAlert("lookingAtHand", playerName);
                selectedCard = chosenCards[0];
                foreach(CardData card in chosenCards)
                {
                    if (card.weight > selectedCard.weight)
                    {
                        selectedCard = card;
                    }
                }
                yield return new WaitForSeconds(1.5f);

                //use chosen card
                switch(selectedCard.targetType)
                {
                    case CardData.Target.Self:
                        if (tm == null)
                        {
                            gm.target = this;
                            SetAIState(AIState.None);
                            gm.SetGameState(GameManager.GameState.ActivateCard);
                        }
                        else
                        {
                            tm.target = this;
                            SetAIState(AIState.None);
                            tm.SetGameState(TutorialManager.GameState.ActivateCard);
                        }
                        break;

                    default:
                        SetAIState(AIState.None);
                        if (tm == null)
                            gm.SetGameState(GameManager.GameState.ActivateCard);
                        else
                            tm.SetGameState(TutorialManager.GameState.ActivateCard);
                        break;

                    /*case CardData.Target.OnePlayer:
                        //gm.target = leadingPlayer;
                        SetAIState(AIState.None);
                        gm.SetGameState(GameManager.GameState.ActivateCard);
                        break;
                    
                    case CardData.Target.Board:
                        SetAIState(AIState.None);
                        gm.SetGameState(GameManager.GameState.ActivateCard);
                        break;*/
                }
            }
            
        }

        //AI coroutine
        IEnumerator DiscardCards(/*int amount*/)
        {
            discardCoroutineOn = true;
            List<CardData> chosenCards = new List<CardData>();     //cards that AI decides to discard. Must narrow these down to 1
            //GameManager gm = GameManager.instance;
            UI ui = UI.instance;

            ui.DisplayAlert("aiDiscardingCards", playerName);
            //ui.DisplayAlert(playerName + " is choosing cards to discard");

            //check each card and compare weight. AI discards cards with the lowest weight
            while(hand.Count > 5) //for (int i = 0; i < amount; i++)
            {
                CardData lowestWeightCard = hand[0];          //lowest value a card can have
                chosenCards.Add(lowestWeightCard);
                foreach(CardData card in hand)
                {
                    if (card == lowestWeightCard) continue;
                    if (card.weight < lowestWeightCard.weight)
                    {
                        lowestWeightCard = card;
                        chosenCards.Add(card);
                    }
                }

                //check chosen cards and remove the card with the lowest weight
                CardData cardToDiscard = chosenCards[0];
                Debug.Log("Chosen Card Count " + chosenCards.Count);
                foreach(CardData card in chosenCards)
                {
                    if (card.weight < cardToDiscard.weight)
                    {
                        cardToDiscard = card;
                    }
                }

                yield return new WaitForSeconds(1.5f);

                //discard card
                CardManager cm = CardManager.instance;
                //cm.playerHand[hand.IndexOf(cardToDiscard)].gameObject.SetActive(false);   //hide the card we're about to remove
                Debug.Log(playerName + " discarded " + cardToDiscard.cardName);
                hand.Remove(cardToDiscard);
                cm.DisplayHand(this, true);

                //update card count
                if (tm == null)
                    ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(this);
                else
                    ui.playerPanels[tm.playerIndex].UpdatePlayerStatus(this);
            }

            discardCoroutineOn = false;
            SetAIState(AIState.None);
            //special condition check for when "Draw a Card" extra mod is active
            ExtraModManager em = ExtraModManager.instance;
            if (em.activeMod != null && em.activeMod.boardID == 3)   
                gm.SetGameState(GameManager.GameState.CardPhase);
            else    //playing normal game
                gm.SetGameState(GameManager.GameState.NextPlayerTurn);
            /*if (tm == null)
                gm.SetGameState(GameManager.GameState.NextPlayerTurn);
            else
                tm.SetGameState(TutorialManager.GameState.NextPlayerTurn);*/

        }
    #endregion
    }

}
