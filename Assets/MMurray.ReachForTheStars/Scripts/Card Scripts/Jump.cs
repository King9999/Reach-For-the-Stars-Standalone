using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//Move to same space as the closest player from player's position. It's possible for the player to end up going backwards.
[CreateAssetMenu(menuName = "Card/Jump!", fileName = "card_jump")]
public class Jump : CardData
{
    public override void Activate(Player user)
    {
        user.cardEffect = this;
        user.canRollDice = false;
        user.moveMod = 0;
        user.moveTotal = 0;
        //search the board and check distance between user and other players.
        //User goes to space with the least distance. The card fails if the user already occupies the same space as the target.
        AudioManager am = AudioManager.instance;
        GameManager gm = GameManager.instance;
        TutorialManager tm = TutorialManager.instance;
        
        am.soundSource.PlayOneShot(am.audioJump);

        if (tm == null)
            gm.SetGameState(GameManager.GameState.PlayerJumping);
        else
            tm.SetGameState(TutorialManager.GameState.PlayerJumping);
        
    }
}
