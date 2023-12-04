using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Cannot gain stars from Home until final round. */
[CreateAssetMenu(menuName = "Extra Mode/Homeless", fileName = "exMode_homeless")]
public class HomelessMod : ExtraMod
{
    public override void Activate()
    {
        GameManager gm = GameManager.instance;
        if (gm.homeSpace.TryGetComponent<Home>(out Home home))
        {
            if (gm.currentRound + 1 < gm.maxRounds || !gm.extraRound)
            {
                home.ToggleCross(true);
                home.canGetStars = false;
            }
            else
            {
                home.ToggleCross(false);
                home.canGetStars = true;
            }
        }
    }

    public override void ActivateSecondary()
    {
        //does nothing
    }
}
