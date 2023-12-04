using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//Player is sent straight to the Home space. Cannot roll dice this turn.
[CreateAssetMenu(menuName = "Card/Go Home!", fileName = "card_goHome")]
public class GoHome : CardData
{
    public override void Activate(Player user)
    {
        user.cardEffect = this;
        user.canRollDice = false;
        user.moveMod = 0;
        user.moveTotal = 0;
        
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;

        if (tm == null)
            user.Jump(gm.homeSpace);
        else
            user.Jump(tm.homeSpace);
    }
}
