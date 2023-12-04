using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VectorGraphics;

//This script displays and animates an icon whenever a card is used on a player. If you hover over this icon, 
//you will get details on the card affecting the player.
namespace MMurray.ReachForTheStars
{
    public class CardEffectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI cardNameUI;            //displays name of card. Swift Movement must be shortened.
        public TextMeshProUGUI cardEffectUI;            //shows card details when mouse is hovering over icon.
        public SVGImage cardEffectIconUI;
        public Image cardEffectBackgroundUI;
        bool animateCardEffectCoroutineOn;
        public Vector3 originalCardNameUIScale; 
        public Vector3 originalCardEffectIconUIScale;          //used for whenever the coroutine resets


        //this is used instead of Start, and is called from game manager.
        public void Initialize()
        {
            animateCardEffectCoroutineOn = false;
            cardEffectUI.gameObject.SetActive(false);
            cardEffectBackgroundUI.gameObject.SetActive(false);
            originalCardNameUIScale = cardNameUI.transform.localScale;
            originalCardEffectIconUIScale = cardEffectIconUI.transform.localScale;
        }

        // Update is called once per frame
        void Update()
        {
            if (!animateCardEffectCoroutineOn)
            {
                StartCoroutine(AnimateCardEffectIcon());
            }
        }

        public void ResetCoroutine()
        {
            StopAllCoroutines();
            animateCardEffectCoroutineOn = false;
            cardEffectIconUI.transform.localScale = originalCardEffectIconUIScale;
            cardNameUI.transform.localScale = originalCardNameUIScale;
        }

        public void SetCardDetails(string cardName, string cardEffect)
        {
            cardNameUI.gameObject.SetActive(true);
            cardNameUI.text = cardName;

            //card effect is hidden by default
            cardEffectUI.gameObject.SetActive(false);
            cardEffectUI.text = cardEffect;
        }

        public void OnPointerExit(PointerEventData pointer)
        {
            cardEffectUI.gameObject.SetActive(false);
            cardEffectBackgroundUI.gameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData pointer)
        {
            cardEffectUI.gameObject.SetActive(true);
            cardEffectBackgroundUI.gameObject.SetActive(true);
        }

        IEnumerator AnimateCardEffectIcon()
        {
            animateCardEffectCoroutineOn = true;

            //animate both the card icon and card name.
            originalCardEffectIconUIScale = cardEffectIconUI.transform.localScale;
            originalCardNameUIScale = cardNameUI.transform.localScale;
            Vector3 destinationScale = originalCardEffectIconUIScale * 1.4f;
            float scaleSpeed = 0.3f;
            while (cardEffectIconUI.transform.localScale.x < destinationScale.x)
            {
                float vx = cardEffectIconUI.transform.localScale.x + scaleSpeed * Time.deltaTime;
                float vy = cardEffectIconUI.transform.localScale.y + scaleSpeed * Time.deltaTime;
                float textVx = cardNameUI.transform.localScale.x + scaleSpeed * Time.deltaTime;
                float textVy = cardNameUI.transform.localScale.y + scaleSpeed * Time.deltaTime;
                cardEffectIconUI.transform.localScale = new Vector3(vx, vy, 0);
                cardNameUI.transform.localScale = new Vector3(textVx, textVy, 0);
                yield return null;
            }
            while (cardEffectIconUI.transform.localScale.x > originalCardEffectIconUIScale.x)
            {
                float vx = cardEffectIconUI.transform.localScale.x - scaleSpeed * Time.deltaTime;
                float vy = cardEffectIconUI.transform.localScale.y - scaleSpeed * Time.deltaTime;
                float textVx = cardNameUI.transform.localScale.x - scaleSpeed * Time.deltaTime;
                float textVy = cardNameUI.transform.localScale.y - scaleSpeed * Time.deltaTime;
                cardEffectIconUI.transform.localScale = new Vector3(vx, vy, 0);
                cardNameUI.transform.localScale = new Vector3(textVx, textVy, 0);
                yield return null;
            }
            cardEffectIconUI.transform.localScale = originalCardEffectIconUIScale;
            cardNameUI.transform.localScale = originalCardNameUIScale;
            animateCardEffectCoroutineOn = false;
        }
    }
}
