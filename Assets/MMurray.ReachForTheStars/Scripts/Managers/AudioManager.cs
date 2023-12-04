using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles all sounds/music
public class AudioManager : MonoBehaviour
{
    public AudioSource soundSource;
    public float soundVolume;

    [Header("---Sounds---")]
    public AudioClip audioGotAStar;
    public AudioClip audioMoveAdded;      //plays during animateDiceValue coroutine in Dice script.
    public AudioClip audioRefill;
    public AudioClip audioDrawCard;
    public AudioClip audioNoLuck;       //used when star is not acquired, no card drawn, or when losing encounter.
    public AudioClip audioDiceResult;
    public AudioClip audioJump;
    public AudioClip audioCardPlayed;   //used with all cards except Jump and Refill
    public AudioClip click;             //used when a button is pressed
    [Header("---Music---")]
    public AudioSource musicMain;
    public AudioSource musicEncounter;
    public AudioSource musicWinner;
    public static AudioManager instance;

     void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(instance);  
    }
    // Start is called before the first frame update
    void Start()
    {
        soundVolume = 0.4f;
    }

  

    //used in scenarios where feedback UI is displayed back to back.
    public void PlayDelayedSound(AudioClip clip, float delayDuration, float volume = 1)
    {
        StartCoroutine(PlaySoundAfterDelay(clip, delayDuration, volume));
    }

    IEnumerator PlaySoundAfterDelay(AudioClip clip, float delayDuration, float volume = 1)
    {
        yield return new WaitForSeconds(delayDuration);
        soundSource.PlayOneShot(clip, volume);
    }
}
