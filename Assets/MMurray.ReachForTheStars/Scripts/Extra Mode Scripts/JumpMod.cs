using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//Used in Horned board. Player has a chance to jump instead of rolling dice. Chance to jump rises each turn. Once a player jumps, chance to jump
//resets to 0.
[CreateAssetMenu(menuName = "Extra Mode/Jump Mod", fileName = "exMode_jump")]
public class JumpMod : ExtraMod
{
    public override void Activate()
    {
        //roll for jump chance for the current player. This action occurs after player rolls dice. This action still occurs
        //Even if player used a Move card, because they still technically roll dice afterwards.
        GameManager gm = GameManager.instance;
        if (Random.value <= jumpChance)
        {
            jumpChance = 0;
            Player closestPlayer = gm.currentPlayer.GetNearestPlayer(gm.playerList);
            if (closestPlayer != null)
            {
                gm.currentPlayer.jumpDestination = closestPlayer.currentSpace;
                AudioManager am = AudioManager.instance;                
                am.soundSource.PlayOneShot(am.audioJump);
                gm.SetGameState(GameManager.GameState.PlayerJumping);
            }
        }
        else
        {
            jumpChance += 0.1f;
            gm.SetGameState(GameManager.GameState.PlayerMoving);
        }

        UI ui = UI.instance;
        ui.UpdateJumpChance(jumpChance); 
    }
}
