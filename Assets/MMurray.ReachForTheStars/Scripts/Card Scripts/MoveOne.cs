using UnityEngine;
using MMurray.ReachForTheStars;

//Forces player to advance 1 space
[CreateAssetMenu(menuName = "Card/Move 1", fileName = "card_move1")]
public class MoveOne : CardData
{
    public override void Activate(Player target)
    {
        target.cardEffect = this;
        target.moveMod = 0;         //this step is done in case move mod was adjusted by some other effect
        target.canRollDice = false;
        target.SetMovement(1);
        
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.audioCardPlayed);
    }
}
