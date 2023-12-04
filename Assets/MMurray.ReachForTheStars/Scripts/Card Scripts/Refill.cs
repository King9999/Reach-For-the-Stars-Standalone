using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//search for nearest space from the player's position for a cache that has low probability.
//If no cache is found, nothing happens and the card is wasted.
[CreateAssetMenu(menuName = "Card/Refill!", fileName = "card_refill")]
public class Refill : CardData
{
    public override void Activate(Player user, StarCache targetSpace)
    {
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        AudioManager am = AudioManager.instance;
        user.cardEffect = this;
        if (targetSpace == null) return;

        //search board for nearest star cache with <= 0.5 probability
        //StarCache nearestCache = user.NearestStarCache(user.direction, 0.5f);
        Debug.Log("Nearest star cache at " + targetSpace.row + ", " + targetSpace.col);
        UI ui = UI.instance;
        targetSpace.SetProbability(1, ui.probabilityType); 
        targetSpace.ToggleProbabilityUI(true);

        //set the colour
        if (tm == null)
            targetSpace.SetUIColor(gm.highProbabilityColor);
        else
            targetSpace.SetUIColor(tm.highProbabilityColor);

        am.soundSource.PlayOneShot(am.audioRefill, am.soundVolume - 0.3f);
        
    }
}
