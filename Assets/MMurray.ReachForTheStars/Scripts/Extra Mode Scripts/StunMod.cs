using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

/*Random Star Caches will stun the player instead of granting a star, using the same probability. Starting at Round 2, two bolts are placed on 
random spaces. Then every 2 rounds afterwards, 1 more bolt is added. */
[CreateAssetMenu(menuName = "Extra Mode/Trapped Caches", fileName = "exMode_trappedCaches")]
public class StunMod : ExtraMod
{
    int trapCount;
    public override void Activate()
    {
        trapCount = trapLocations.Count;
        //if we're on round 2, two star caches become trapped.
        GameManager gm = GameManager.instance;
        ExtraModManager em = ExtraModManager.instance;
        if (trapLocations == null)
            trapLocations = new List<Vector3>();

        if (gm.currentRound + 1 == 2 && trapLocations.Count <= 0)
        {
            //get 2 random locations
            trapCount = 2;
            AddTrap(trapCount);
            int i = 0;
            foreach(Vector3 location in trapLocations)
            {
                if (i >= em.trapSprites.Count)
                    em.trapSprites.Add(Instantiate(em.trapPrefab, location, Quaternion.identity));
                else
                {
                    //have an existing sprite, use that. This code is mainly run if player restarted game
                    em.trapSprites[i].gameObject.SetActive(true);
                    em.trapSprites[i].transform.position = trapLocations[i];
                }

                i++;
            }
            /*int count = 0;
            int randSpace;
            while(count < 2)
            {
                do
                {
                    randSpace = Random.Range(0, gm.boardSpaceList.Count);
                }
                while(gm.boardSpaceList[randSpace].spaceType != BoardSpace.SpaceType.StarCache || trapLocations.Contains(gm.boardSpaceList[randSpace].transform.position));
                trapLocations.Add(gm.boardSpaceList[randSpace].transform.position);
                //Add an icon at each position
                em.trapSprites.Add(Instantiate(em.trapPrefab, trapLocations[trapLocations.Count - 1], Quaternion.identity));
                count++;
            }*/
        }
        else if ((gm.currentRound + 1) % 2 == 0)
        {
            //add 1 trap, then randomize locations
            trapLocations = new List<Vector3>();
            AddTrap(++trapCount);

            //re-locate trap sprites, and add more if there aren't enough
            for (int i = 0; i < trapLocations.Count; i++)
            {
                if (i == em.trapSprites.Count)
                {
                    em.trapSprites.Add(Instantiate(em.trapPrefab, trapLocations[i], Quaternion.identity));
                }
                else
                {
                    //relocate current sprite
                    em.trapSprites[i].transform.position = trapLocations[i];
                }
            }
            
            /*int count = 0;
            int randSpace;
            while(count < 2)
            {
                do
                {
                    randSpace = Random.Range(0, gm.boardSpaceList.Count);
                }
                while(gm.boardSpaceList[randSpace].spaceType != BoardSpace.SpaceType.StarCache || trapLocations.Contains(gm.boardSpaceList[randSpace].transform.position));
                trapLocations.Add(gm.boardSpaceList[randSpace].transform.position);
                //Add an icon at each position
                em.trapSprites.Add(Instantiate(em.trapPrefab, trapLocations[trapLocations.Count - 1], Quaternion.identity));
                count++;
            }*/
        }
        Debug.Log("Trap Count: " + trapCount);

    }

    public override void ActivateSecondary()
    {
        //if player is on trapped cache, lose a turn
        GameManager gm = GameManager.instance;
        AudioManager am = AudioManager.instance;
        UI ui = UI.instance;

        Player player = gm.currentPlayer;

        if (trapLocations.Contains(player.transform.position))
        {
            player.loseTurn = true;
            am.soundSource.PlayOneShot(am.audioNoLuck, am.soundVolume);
            ui.DisplayFeedbackMessage("encounter_loseTurn", player.transform.position);
        }
        else
        {
            //got a star
            player.starTotal++;
            player.gotStarFromCache = true;
            am.soundSource.PlayOneShot(am.audioGotAStar, am.soundVolume);
            ui.DisplayFeedbackMessage("starGet", player.transform.position);
            ui.playerPanels[gm.playerIndex].UpdatePlayerStatus(gm.currentPlayer);   
            ui.UpdateLeadingPlayer();
        }
    }

    void AddTrap(int amount)
    {
        GameManager gm = GameManager.instance;
        ExtraModManager em = ExtraModManager.instance;
        int i = 0;
        int randSpace;
        while(i < amount)
        {
            do
            {
                randSpace = Random.Range(0, gm.boardSpaceList.Count);
            }
            while(gm.boardSpaceList[randSpace].spaceType != BoardSpace.SpaceType.StarCache || trapLocations.Contains(gm.boardSpaceList[randSpace].transform.position));
            trapLocations.Add(gm.boardSpaceList[randSpace].transform.position);

            //Add an icon at each position
            //em.trapSprites.Add(Instantiate(em.trapPrefab, trapLocations[trapLocations.Count - 1], Quaternion.identity));
            i++;
        }
    }
}
