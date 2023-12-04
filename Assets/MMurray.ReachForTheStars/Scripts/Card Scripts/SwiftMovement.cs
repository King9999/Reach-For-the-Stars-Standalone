using UnityEngine;
using MMurray.ReachForTheStars;

/* Adds 2-4 to dice roll for 1 turn. The total move count can exceed 12 in this way. */
[CreateAssetMenu(menuName = "Card/Swift Movement", fileName = "card_swiftMovement")]
public class SwiftMovement : CardData
{
    public override void Activate(Player self)
    {
        self.cardEffect = this;
        self.canRollDice = true;
        self.moveMod = Random.Range(2, 5);

        AudioManager am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.audioCardPlayed);
    }
}
