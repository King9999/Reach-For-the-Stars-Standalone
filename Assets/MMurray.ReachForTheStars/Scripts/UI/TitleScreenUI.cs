using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using LoLSDK;

//this script contains all UI for the title screen.
namespace MMurray.ReachForTheStars
{
    public class TitleScreenUI : MonoBehaviour
    {
        public Button startButton;                  //moves to game scene. Will update to have it move to board/player select.
        public Button continueButton;
        public Button tutorialButton;
        public Button creditsButton;
        AudioManager am;
        //[SerializeField]GameData gameData;

        void Awake()
        {
            UpdateLanguage();
        }
        

        // Start is called before the first frame update
        void Start()
        {
            //continue button is greyed out by default. If there's a save state, then the button becomes clickable.
            //I think it's better to show the button rather than hide it so the player knows it's possible to resume a game.
            //Helper.StateButtonInitialize<GameData>(startButton, continueButton, OnLoad);
            TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.color = Color.grey;

             //Check for a save state.
            TitleManager tm = TitleManager.instance;
            /*LOLSDK.Instance.LoadState<GameData>(state =>
            {
                if (state != null)
                {
                    //callback(state.data);
                    tm.saveStateFound = true;
                    buttonText.color = Color.white;
                }
                
            });*/

           
            /*if (tm.saveStateFound)
            {
                buttonText.color = Color.white;
            }*/

            am = AudioManager.instance;

        }

        /*void OnLoad(GameData loadedData)
        {
            if (loadedData != null)
                gameData = loadedData;

            TextMeshProUGUI buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.color = Color.grey;

            if (gameData != null)
            {
                TitleManager tm = TitleManager.instance;
                tm.saveStateFound = true;
                buttonText.color = Color.white;
                Debug.Log("Found state");
            }
        }*/

        /*string GetText (string key)
        {
            string value = SharedState.LanguageDefs?[key];
            return value ?? "--missing--";
        }*/

        void UpdateLanguage()
        {
            UniversalSettings us = UniversalSettings.instance;
            startButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("newGame");
            continueButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("resumeGame");
            tutorialButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("tutorial");
            creditsButton.GetComponentInChildren<TextMeshProUGUI>().text = us.GetText("credits");
        }

        public void StartGame()
        {
            TitleManager tm = TitleManager.instance;
            tm.newGameStarted = true;
            UniversalSettings us = UniversalSettings.instance;
            us.allLessonsViewed = false;
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            SceneManager.LoadScene("Setup");
        }


        public void ResumeGame()
        {
            TitleManager tm = TitleManager.instance;
            ScreenFade sf = ScreenFade.instance;
            if (tm.saveStateFound == false || sf.coroutineOn) return;
            
            //when the scene is changed, the game must load the save state
            am.soundSource.PlayOneShot(am.click, am.soundVolume);

            //use fade out here while game loads
            sf.ChangeSceneFadeOut("Game");
            //SceneManager.LoadScene("Game");
        }

        public void TutorialButton()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            SceneManager.LoadScene("TutorialBoard");
        }

        public void CreditsButton()
        {
            am.soundSource.PlayOneShot(am.click, am.soundVolume);
            SceneManager.LoadScene("Credits");
        }
    }
}
