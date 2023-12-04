using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//players keep drawing cards at the start of their turn
[CreateAssetMenu(menuName = "Extra Mode/Draw a Card", fileName = "exMode_drawCard")]
public class DrawCardMod : ExtraMod
{
    public override void Activate()
    {
        //all players get a card
        GameManager gm = GameManager.instance;
        UI ui = UI.instance;
        /*for(int i = 0; i < gm.playerList.Count; i++)
        {
            gm.playerList[i].DrawCard(1);
            ui.playerPanels[i].UpdatePlayerStatus(gm.playerList[i]);
        }*/

        gm.currentPlayer.DrawCard(1);
        ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);
    }
}
