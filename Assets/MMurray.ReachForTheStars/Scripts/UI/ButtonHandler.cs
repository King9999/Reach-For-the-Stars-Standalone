using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

//This script must be attached to a Button UI object.
public class ButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Image buttonImage;                        //using this to change button colour when mouse hovers over it
    Color hoverColor;
    Color normalColor;

    bool animateButtonCoroutineOn;
    bool pointerEntered;                //this is used to trigger the coroutine in the update loop.
    GameManager gm;
    TutorialManager tm;

    // Start is called before the first frame update
    void Start()
    {
        hoverColor = Color.red;
        //normalColor = Color.clear;
        buttonImage = GetComponent<Image>();
        normalColor = buttonImage.color;
        gm = GameManager.instance;
        tm = TutorialManager.instance;
    }

    void Update()
    {
        //GameManager gm = GameManager.instance;
        if (tm == null)
        {
            if (pointerEntered && gm.gameState == GameManager.GameState.ChoosingTarget)
            {
                if (!animateButtonCoroutineOn)
                {
                    StartCoroutine(AnimateButton());
                }
            }
        }
        else
        {
            if (pointerEntered && tm.gameState == TutorialManager.GameState.ChoosingTarget)
            {
                if (!animateButtonCoroutineOn)
                {
                    StartCoroutine(AnimateButton());
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData pointer)
    {
        //GameManager gm = GameManager.instance;
        //TutorialManager tm = TutorialManager.instance;
        if (tm == null)
        {
            if (gm.gameState == GameManager.GameState.ChoosingTarget)
            {
                buttonImage.color = hoverColor; 
                //start coroutine
                pointerEntered = true;
            }
        }
        else
        {
            if (tm.gameState == TutorialManager.GameState.ChoosingTarget)
            {
                buttonImage.color = hoverColor; 
                //start coroutine
                pointerEntered = true;
            }
        }       
    }

    public void OnPointerExit(PointerEventData pointer)
    {
        buttonImage.color = normalColor;

        //don't want coroutine to continue
        StopAllCoroutines();
        animateButtonCoroutineOn = false;
        pointerEntered = false;

    }

    IEnumerator AnimateButton()
    {
        animateButtonCoroutineOn = true;
        float alpha = buttonImage.color.a;

        while(alpha > normalColor.a)
        {
            alpha -= 0.8f * Time.deltaTime;
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, alpha);
            yield return null;
        }

        while(alpha < 1)
        {
            alpha += 0.8f * Time.deltaTime;
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, alpha);
            yield return null;
        }

        animateButtonCoroutineOn = false;
    }
}
