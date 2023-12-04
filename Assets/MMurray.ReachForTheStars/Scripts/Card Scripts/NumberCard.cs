using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

/* This is for the encounter minigame. For simplicity, only 1 suit of cards is used. */
public class NumberCard : MonoBehaviour
{
    public byte value;      //number from 1 to 10
    public Sprite cardFace;
    public Sprite cardBack;
    public bool isHidden;   //if true, show card back

    public void CardFaceDown()
    {
        SVGImage image = GetComponent<SVGImage>();
        image.sprite = cardBack;
        isHidden = true;
    }

    public void CardFaceUp()
    {
        SVGImage image = GetComponent<SVGImage>();
        image.sprite = cardFace;
        isHidden = false;
    }
   
}
