using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Mod for the standard board. Anytime a star is acquired from a star cache, the probability drops to 0. */
[CreateAssetMenu(menuName = "Extra Mode/Star-ving", fileName = "exMode_starving")]
public class StarvingMod : ExtraMod
{
    public override void Activate()
    {
        GameManager gm = GameManager.instance;
        if (gm.currentPlayer.currentSpace.TryGetComponent<StarCache>(out StarCache starCache))
        {
            starCache.probability = 0;

            //check for lesson
            LessonManager lm = LessonManager.instance;
            if (!lm.miniLessonList[0].lessonViewed && gm.playerIndex == 0)
            {
                gm.miniLessonIndex = 0;
                gm.SetGameState(GameManager.GameState.GetMiniLesson);
            }
        }
    }
}
