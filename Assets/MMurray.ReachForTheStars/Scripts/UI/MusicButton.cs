using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicButton : MonoBehaviour
{
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;
    Image image;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        image.sprite = musicOnSprite;            //music is on by default
    }

    // Update is called once per frame
    void Update()
    {
        UniversalSettings us = UniversalSettings.instance;
        if (us.musicEnabled)
        {
            image.sprite = musicOnSprite;
        }
        else
        {
            image.sprite = musicOffSprite;
        }
    }
}
