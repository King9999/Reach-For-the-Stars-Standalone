using UnityEngine;
using MMurray.ReachForTheStars;

//Forces player to advance 3 spaces
[CreateAssetMenu(menuName = "Card/Move 3", fileName = "card_move3")]
public class MoveThree : CardData
{
    public override void Activate(Player target)
    {
        target.cardEffect = this;
        target.moveMod = 0;         //this step is done in case move mod was adjusted by some other effect
        target.canRollDice = false;
        target.SetMovement(3);
        
        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.audioCardPlayed);
    }
}
