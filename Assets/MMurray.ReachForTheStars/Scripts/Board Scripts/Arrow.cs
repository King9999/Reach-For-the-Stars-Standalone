using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//These are clickable objects that will determine a player's route on the board.
namespace MMurray.ReachForTheStars
{
    public class Arrow : MonoBehaviour
    {
        public Vector3 OriginalPos {get; set; }
        public Vector3 direction;           //controls where the arrow animates.
        public Vector3 DestinationPos {get; set;}
        //BoardSpace chosenRoute;           //the space that player will select if this arrow is clicked.
        public BoardSpace ChosenRoute {get; set;}
        public Player currentPlayer;
        public enum Direction {Up, Down, Left, Right}
        public Direction arrowDirection;       

        // Start is called before the first frame update
        void Start()
        {
            OriginalPos = transform.position;
            DestinationPos = new Vector3(OriginalPos.x + direction.x, OriginalPos.y + direction.y, OriginalPos.z);
        }

        // Update is called once per frame
        void Update()
        {
            //animate the arrows
            if (direction.x > 0 && transform.position.x > DestinationPos.x)
            {
                transform.position = OriginalPos;
            }
            else if (direction.x < 0 && transform.position.x < DestinationPos.x)
            {
                transform.position = OriginalPos;
            }
            else if (direction.y > 0 && transform.position.y > DestinationPos.y)
            {
                transform.position = OriginalPos;
            }
            else if (direction.y < 0 && transform.position.y < DestinationPos.y)
            {
                transform.position = OriginalPos;
            }

            float vx = transform.position.x + (direction.x * 2) * Time.deltaTime;
            float vy = transform.position.y + (direction.y * 2) * Time.deltaTime;
            transform.position = new Vector3(vx, vy, transform.position.z);
        }

        public void AssignRoute(Player player, BoardSpace space)
        {
            if (space == null) return;

            currentPlayer = player;
            ChosenRoute = space;
        }

        //This method is executed when arrow is clicked. Removes all routes until chosen route remains
        public void PickRoute()
        {
            AudioManager am = AudioManager.instance;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            GameManager gm = GameManager.instance;
            TutorialManager tm = TutorialManager.instance;

            if (tm == null)
            {
                if (currentPlayer.isAI) return;
                if (gm.gameState == GameManager.GameState.PlayingRefill) return;    //not picking a route, only showing a player's current direction
                if (ChosenRoute == null || currentPlayer == null) return;

                for (int i = 0; i < currentPlayer.route.Count; i++)
                {
                    if (currentPlayer.route[i] != ChosenRoute)
                    {
                        //delete space
                        currentPlayer.route.Remove(currentPlayer.route[i]);
                        i--;
                    }
                }

                //hide all arrows once selection made
                Debug.Log("Chosen Route: " + ChosenRoute.spaceType + "(" + ChosenRoute.row + "," + ChosenRoute.col + ")");
                
                gm.EnableArrowContainer(false);
                UI ui = UI.instance;
                ui.ToggleAlertUI(false);
            }
            else
            {
                if (currentPlayer.isAI) return;
                if (tm.gameState == TutorialManager.GameState.PlayingRefill) return;    //not picking a route, only showing a player's current direction
                if (ChosenRoute == null || currentPlayer == null) return;

                for (int i = 0; i < currentPlayer.route.Count; i++)
                {
                    if (currentPlayer.route[i] != ChosenRoute)
                    {
                        //delete space
                        currentPlayer.route.Remove(currentPlayer.route[i]);
                        i--;
                    }
                }

                //hide all arrows once selection made
                Debug.Log("Chosen Route: " + ChosenRoute.spaceType + "(" + ChosenRoute.row + "," + ChosenRoute.col + ")");
                
                tm.EnableArrowContainer(false);
                UI ui = UI.instance;
                ui.ToggleAlertUI(false);
            }
        }

    }
}
