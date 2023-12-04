using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LoLSDK;
using SimpleJSON;

namespace MMurray.ReachForTheStars
{
    public class TitleManager : MonoBehaviour
    {
        //SaveState saveState;                        //used to resume a game
        //GameData gameData;
        public bool saveStateFound;
        public bool newGameStarted;                 //used to prevent loading save state if one exists.

        public static TitleManager instance;


        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            Application.runInBackground = false;    //should be false by default but I want to be sure
            
            /*string saveStateFile = Application.persistentDataPath + "/save_state_data.json";

            if (File.Exists(saveStateFile))
            {
                saveStateFound = true;
            }*/
            //CheckState(OnLoad);

            //Check for a save state.
            /*LOLSDK.Instance.LoadState<GameData>(state =>
            {
                if (state != null)
                {
                    //callback(state.data);
                    saveStateFound = true;
                }
                
            });*/
            

            //Debug.Log(Application.identifier);
            //Debug.Log(Application.persistentDataPath);

        }

        // Start is called before the first frame update
        void Start()
        {
            /*saveStateFound = false;
            LOLSDK.Instance.LoadState<GameData>(state =>
            {
                if (state != null)
                {
                    //callback(state.data);
                    saveStateFound = true;
                }
                
            });*/
            /*if (File.Exists(saveStateFile))
            {
                saveStateFound = true;
            }*/
            ScreenFade sf = ScreenFade.instance;
            sf.FadeIn();    //I do this so the game has time to load state for the resume game button.
            
        }

        /*void OnLoad(GameData gameData)
        {
            if (gameData != null)
            {
                saveStateFound = true;
                Debug.Log("Found state");
            }
        }

        void CheckState(System.Action<GameData> callback)
        {
            LOLSDK.Instance.LoadState<GameData>(state =>
            {
                if (state != null)
                {
                    callback(state.data);
                }
                
            });
        }*/

        public void OnTTSButtonClicked()
        {
            AudioManager am = AudioManager.instance;
            UniversalSettings us = UniversalSettings.instance;
            us.ttsEnabled = !us.ttsEnabled;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
        }
    }
}
