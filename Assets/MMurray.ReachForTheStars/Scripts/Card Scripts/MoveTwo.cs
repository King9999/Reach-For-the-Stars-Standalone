using UnityEngine;
using MMurray.ReachForTheStars;

//Forces player to advance 2 spaces
[CreateAssetMenu(menuName = "Card/Move 2", fileName = "card_move2")]
public class MoveTwo : CardData
{
    public override void Activate(Player target)
    {
        target.cardEffect = this;
        target.moveMod = 0;         //this step is done in case move mod was adjusted by some other effect
        target.canRollDice = false;
        target.SetMovement(2);
        
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.audioCardPlayed);
    }
}
