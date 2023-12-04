using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//Increases max rounds by 1.
[CreateAssetMenu(menuName = "Card/Extend!", fileName = "card_extend")]
public class Extend : CardData
{
    public override void Activate(Player user)
    {
        user.cardEffect = this;
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;

        //This card does nothing in the tutorial
        if (tm == null)
        {
            gm.maxRounds += 1;
            
            UI ui = UI.instance;
            ui.UpdateTotalRounds(gm.maxRounds);
        }

        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.audioCardPlayed);
    }
}
