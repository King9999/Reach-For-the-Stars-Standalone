using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMurray.ReachForTheStars;

//base class for extra mode effects
public class ExtraMod : ScriptableObject
{
    public string modNameKey;
    public string modName;
    public int boardID;         //the board the mod is active on. Valid values are 0 to 5.
    public float jumpChance;    //used with Random Jumps mod
    public List<Vector3> trapLocations; //stores board space location of traps. Can only be placed on Star Caches. Used with Trapped Caches mod.
    
    public virtual void Activate() {}
    public virtual void ActivateSecondary() {}      //used for any other effects
   
}
