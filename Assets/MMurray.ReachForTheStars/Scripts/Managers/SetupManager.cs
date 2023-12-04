using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MMurray.ReachForTheStars;
using LoLSDK;

//saves information that will be carried over to the game screen. Will also contain UI elements
public class SetupManager : MonoBehaviour
{
    public byte numberOfPlayers;
    public List<string> playerNames;            //first entry is always the human player's name
    public PlayingPieceButton[] playingPieceColors;
    public BoardButton[] boards;
    public List<int> selectedPieceColor;       //IDs of playing piece colours  
    public int boardID;                         //corresponds to the index of the boardData array in Game Manager.
    public bool ttsEnabled;
    bool playTTSCoroutine = false;

    [Header("---UI---")]
    public Button twoPlayerButton;
    public Button threePlayerButton;
    public TMP_InputField nameEntryField;
    public TMP_Dropdown dropdownNames;      //contains list of names. Make this alphabetical
    public Button backButton;
    public Button confirmNameButton;
    public Button randomNameButton;
    public TextMeshProUGUI howManyPlayersText, enterNameText, choosePlayingPieceText, chooseBoardText;

    [Header("---Extra Mode---")]
    //public Toggle extraModeToggle;
    public Button extraModeButton;
    public TextMeshProUGUI extraModeTitle, extraModeDetails;
    public Sprite extraModeOn, extraModeOff;        //show a checkmark when on
    Image extraModeButtonImage;

    [Header("---Containers---")]
    public GameObject playerSelectUIContainer;
    public GameObject nameEntryUIContainer;
    public GameObject playingPieceSelectUIContainer;
    public GameObject boardSelectUIContainer;

    [Header("---JSON---")]
    public TextAsset nameFile;
    PlayerNames nameList;
    List<string> names;         //contains names from PlayerNames

    public enum SetupState {NumberOfPlayers, PlayerNameEntry, PlayingPieceSelect, SelectBoard}
    public SetupState setupState;

    AudioManager am;
    public static SetupManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        UpdateLanguage();
    }

    // Start is called before the first frame update
    void Start()
    {
        ToggleContainers(false);
        ChangeSetupState(SetupState.NumberOfPlayers);
        nameList = JsonUtility.FromJson<PlayerNames>(nameFile.text);
        //nameEntryField.lineLimit = 1;
        //nameEntryField.characterLimit = 12;

        //get names from PlayerNames
        names = new List<string>();
        foreach (PlayerName playerName in nameList.playerNames)
        {
            names.Add(playerName.name);
        }

        names.Sort();                       //arrange in alphabetical order from A-Z
        dropdownNames.AddOptions(names);

        //extra mode setup
        extraModeButtonImage = extraModeButton.GetComponent<Image>();
        UniversalSettings us = UniversalSettings.instance;
        us.extraModeEnabled = false;    //false by default
        
        am = AudioManager.instance;
    }


    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }

    public void UpdateLanguage()
    {
        twoPlayerButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("twoPlayers");
        threePlayerButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("threePlayers");
        confirmNameButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("confirmName");
        randomNameButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("randomName");
        backButton.GetComponentInChildren<TextMeshProUGUI>().text = GetText("backButtonText");
        howManyPlayersText.text = GetText("howManyPlayers");
        enterNameText.text = GetText("enterName");
        choosePlayingPieceText.text = GetText("chooseColor");
        chooseBoardText.text = GetText("chooseBoard");

        //playing piece colours
        playingPieceColors[0].pieceColorName.text = GetText("pieceColor_blue");
        playingPieceColors[1].pieceColorName.text = GetText("pieceColor_green");
        playingPieceColors[2].pieceColorName.text = GetText("pieceColor_purple");
        playingPieceColors[3].pieceColorName.text = GetText("pieceColor_red");
        playingPieceColors[4].pieceColorName.text = GetText("pieceColor_yellow");
        playingPieceColors[5].pieceColorName.text = GetText("pieceColor_teal");
        playingPieceColors[6].pieceColorName.text = GetText("pieceColor_black");
        playingPieceColors[7].pieceColorName.text = GetText("pieceColor_white");

        //board selection
        boards[0].boardDetails.text = GetText("board_0");
        boards[1].boardDetails.text = GetText("board_1");
        boards[2].boardDetails.text = GetText("board_2");
        boards[3].boardDetails.text = GetText("board_3");
        boards[4].boardDetails.text = GetText("board_4");
        boards[5].boardDetails.text = GetText("board_5");

        //extra mode
        //extraModeToggle.GetComponentInChildren<Text>().text = GetText("extraMode");
        extraModeTitle.text = GetText("extraMode");
        extraModeDetails.text = GetText("extraMode_details");
        //extraModeToggle.isOn = false;
    }

    public void ChangeSetupState(SetupState state)
    {
        UniversalSettings us = UniversalSettings.instance;
        switch(state)
        {
            case SetupState.NumberOfPlayers:
                setupState = SetupState.NumberOfPlayers;
                if (us.ttsEnabled)
                {
                    //string[] keys = {"howManyPlayers", "twoPlayers", "threePlayers", "backButtonText"};
                    //StartCoroutine(PlayTTS(keys, 1.5f));
                    LOLSDK.Instance.SpeakText("howManyPlayers");
                }
                playerSelectUIContainer.gameObject.SetActive(true);
                nameEntryUIContainer.gameObject.SetActive(false);
                break;

            case SetupState.PlayerNameEntry:
                setupState = SetupState.PlayerNameEntry;
                if (us.ttsEnabled)
                {
                    LOLSDK.Instance.SpeakText("enterName");
                    //string[] keys = {"enterName", "confirmName", "randomName", "backButtonText"};
                    //StartCoroutine(PlayTTS(keys, 1.5f));
                }
                playerNames = new List<string>();               //if we return to this state, list should be cleared
                playerSelectUIContainer.gameObject.SetActive(false);
                playingPieceSelectUIContainer.gameObject.SetActive(false);
                nameEntryUIContainer.gameObject.SetActive(true);
                break;

            case SetupState.PlayingPieceSelect:
                setupState = SetupState.PlayingPieceSelect;
                selectedPieceColor.Clear(); //must do this step to prevent more than 3 entries from being entered when returning to this state
                if (us.ttsEnabled)
                {
                    LOLSDK.Instance.SpeakText("chooseColor");
                }
                nameEntryUIContainer.gameObject.SetActive(false);
                boardSelectUIContainer.gameObject.SetActive(false);
                playingPieceSelectUIContainer.gameObject.SetActive(true);
                break;

            case SetupState.SelectBoard:
                setupState = SetupState.SelectBoard;
                if (us.ttsEnabled)
                {
                    LOLSDK.Instance.SpeakText("chooseBoard");
                    //string[] keys = {"chooseBoard", "pieceColor_blue", "pieceColor_green", "pieceColor_purple" ,"pieceColor_red", "pieceColor_yellow",
                         //"pieceColor_teal", "pieceColor_black", "pieceColor_white", "backButtonText"};
                    //StartCoroutine(PlayTTS(keys, 1.5f));
                }
                playingPieceSelectUIContainer.gameObject.SetActive(false);
                boardSelectUIContainer.gameObject.SetActive(true);
                break;
  
        }
    }

    public void ToggleContainers(bool toggle)
    {
        playerSelectUIContainer.gameObject.SetActive(toggle);
        nameEntryUIContainer.gameObject.SetActive(toggle);
        playingPieceSelectUIContainer.gameObject.SetActive(toggle);
        boardSelectUIContainer.gameObject.SetActive(toggle);
    }

#region Button Methods
    public void OnBackButtonClicked()
    {
        am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        switch(setupState)
        {
            case SetupState.NumberOfPlayers:
                //back to title screen
                TitleManager tm = TitleManager.instance;
                tm.newGameStarted = false;
                SceneManager.LoadScene("Title");
                break;

            case SetupState.PlayerNameEntry:
                ChangeSetupState(SetupState.NumberOfPlayers);
                break;

            case SetupState.PlayingPieceSelect:
                ChangeSetupState(SetupState.PlayerNameEntry);
                break;

            case SetupState.SelectBoard:
                ChangeSetupState(SetupState.PlayingPieceSelect);
                break;
        } 
    }

    public void OnTwoPlayerButtonClicked()
    {
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        numberOfPlayers = 2;
        ChangeSetupState(SetupState.PlayerNameEntry);
    }

    public void OnThreePlayerButtonClicked()
    {
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        numberOfPlayers = 3;
        ChangeSetupState(SetupState.PlayerNameEntry);
    }

    public void OnRandomNameButtonClicked()
    {
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        //int randName = Random.Range(0, nameList.playerNames.Length);
        int randName = Random.Range(0, names.Count);
        dropdownNames.captionText.text = names[randName];
        //nameEntryField.text = nameList.playerNames[randName].name;
        //Debug.Log("Random Name: " + nameList.playerNames[randName].name);
        Debug.Log("Random Name: " + names[randName]);
    }

    public void OnConfirmNameButtonClicked()
    {
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        //playerNames.Add(nameEntryField.text);
        playerNames.Add(dropdownNames.captionText.text);

        //add random names for the AI opponents
        for (int i = 1; i < numberOfPlayers; i++)
        {
            int randName;
            string newName;
            do
            {
                //randName = Random.Range(0, nameList.playerNames.Length);
                //newName = nameList.playerNames[randName].name;
                randName = Random.Range(0, names.Count);
                newName = names[randName];
            }
            while(playerNames.Contains(newName));
            playerNames.Add(newName);
        }
        ChangeSetupState(SetupState.PlayingPieceSelect);
    }

    public void OnTTSButtonClicked()
    {
        am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        UniversalSettings us = UniversalSettings.instance;
        us.ttsEnabled = !us.ttsEnabled;
        if (!us.ttsEnabled)
        {
            StopAllCoroutines();
        }
        //ttsEnabled = us.ttsEnabled;
        //ttsEnabled = !ttsEnabled;
    }

    public void OnExtraModeClicked()
    {
        am = AudioManager.instance;
        am.soundSource.PlayOneShot(am.click, am.soundVolume);
        UniversalSettings us = UniversalSettings.instance;
        us.extraModeEnabled = !us.extraModeEnabled;
        //extraModeToggle.isOn = us.extraModeEnabled;
        //change extra mode button sprite
        extraModeButtonImage.sprite = us.extraModeEnabled ? extraModeOn : extraModeOff;
    }

    IEnumerator PlayTTS(string[] keys, float duration = 0)
    {
        int i = 0;
        while(i < keys.Length)
        {
            LOLSDK.Instance.SpeakText(keys[i]);
            yield return new WaitForSeconds(duration);
            i++;
        }
    }


    #endregion
}
