using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles distribution and activation of mods for extra mode.
public class ExtraModManager : MonoBehaviour
{
    public ExtraMod[] extraMods;
    public ExtraMod activeMod;
    public GameObject trapPrefab;           //used with Trapped Caches mod
    public List<GameObject> trapSprites;

    public static ExtraModManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    
}
