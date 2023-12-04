using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MMurray.ReachForTheStars
{
    public class Deck : MonoBehaviour
    {
        //25 cards total
        public List<Card> cards;
        int deckSize {get;} = 25;
        public static Deck instance;

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
            cards = new List<Card>();
        }

        public void ShuffleCards()
        {
            cards = new List<Card>();

            //keep adding cards until list is full. Cards are placed in random positions in the list. 
        }

    }
}
