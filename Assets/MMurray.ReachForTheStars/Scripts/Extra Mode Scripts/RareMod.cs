using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

/* Stars are harder to obtain. Star Caches begin at 50%. When a star is not acquired from a cache, the probability does not rise. 
The deck is filled with Refill cards instead of the usual cards. */
[CreateAssetMenu(menuName = "Extra Mode/Rarer Stars", fileName = "exMode_rarerStars")]
public class RareMod : ExtraMod
{
    public override void Activate()
    {
        //change all cards in the deck to refill cards.
        CardManager cm = CardManager.instance;
        for(int i = 0; i < cm.cards.Count; i++)
        {
            //change to refill
            cm.cards[i] = cm.cardTypes[5].card;
        }

        //update player hand
        GameManager gm = GameManager.instance;
        foreach(Player player in gm.playerList)
        {
            for (int i = 0; i < player.hand.Count; i++)
            {
                player.hand[i] = cm.cardTypes[5].card;
            }
        }

        //Set star cache probability to 0.5
        gm.ResetStarCaches(0.5f);
    }

    public override void ActivateSecondary()
    {
        //probability does not rise. This method does nothing
        /*GameManager gm = GameManager.instance;
        if (gm.currentPlayer.currentSpace.TryGetComponent<StarCache>(out StarCache cache))
        {
            ;   //do nothing
        }*/
    }
}
