using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VectorGraphics;

public class TTSButton : MonoBehaviour
{
    public Sprite ttsOnSprite;
    public Sprite ttsOffSprite;
    SVGImage image;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<SVGImage>();
        image.sprite = ttsOffSprite;            //TTS is off by default
    }

    // Update is called once per frame
    void Update()
    {
        UniversalSettings us = UniversalSettings.instance;
        if (us.ttsEnabled)
        {
            image.sprite = ttsOnSprite;
        }
        else
        {
            image.sprite = ttsOffSprite;
        }
    }

    /*public void OnTTSButtonClicked()
    {
        UniversalSettings us = UniversalSettings.instance;
        if (us.ttsEnabled)
        {
            image.sprite = ttsOnSprite;
        }
        else
        {
            image.sprite = ttsOffSprite;
        }
    }*/
}
