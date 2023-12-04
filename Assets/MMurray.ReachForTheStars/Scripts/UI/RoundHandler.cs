using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

//Displays the next round in the game. Includes coroutine for animating the round number and text.
public class RoundHandler : MonoBehaviour
{
    public TextMeshProUGUI nextRoundValueUI;        //displays the new round
    public TextMeshProUGUI nextRoundTextUI;
    string nextRoundText;
    public ParticleSystem nextRoundEffect;
    public Vector3 originalPos, destinationPos;
    // Start is called before the first frame update
    void Start()
    {
        //originalPos = new Vector3(transform.position.x + 60, transform.position.y, 0);
        //destinationPos = transform.position;
        //nextRoundEffect.transform.position = Camera.main.WorldToScreenPoint(nextRoundValueUI.transform.position);
    }

    /*string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }*/

    /*string GetText (string key, string number)
    {
        string value = SharedState.LanguageDefs?[key];
        return value + number ?? "--missing--";
    }*/

    //must call this during load state
    public void UpdateLanguage()
    {
        nextRoundTextUI.text = UniversalSettings.instance.GetText("nextRoundText");
    }

    // Update is called once per frame
    void Update()
    {
        //nextRoundEffect.transform.position = Camera.main.ScreenToWorldPoint(nextRoundValueUI.transform.position);
    }

    public void ToggleNextRoundLabelUI(bool toggle)
    {
        gameObject.SetActive(toggle);
        nextRoundValueUI.gameObject.SetActive(toggle);
    }

    public void SetNextRoundValueUI(string round)
    {
        if (round == "")
            nextRoundValueUI.text = "";
        else
            StartCoroutine(AnimateNextRound(round));
            //nextRoundValueUI.text = round.ToString();
    }


    /* Text animates by fading in on screen while shifting to the left to the centre of the screen. Once it's there,
    The round number animates to the next value. */
    IEnumerator AnimateNextRound(string newRound)
    {
        GameManager gm = GameManager.instance;
        float alpha = 0;
        TextMeshProUGUI textMesh = gameObject.GetComponent<TextMeshProUGUI>();
        Color valueColor = nextRoundValueUI.color;
        textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
        nextRoundValueUI.color = new Color(valueColor.r, valueColor.g, valueColor.b, alpha);
        nextRoundTextUI.color = new Color(nextRoundTextUI.color.r, nextRoundTextUI.color.g, nextRoundTextUI.color.b, alpha);

        transform.position = originalPos;
        float moveSpeed = 80;
        float fadeSpeed = 2;

        while (transform.position.x > destinationPos.x)
        {
            float vx = moveSpeed * Time.deltaTime;
            alpha += fadeSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x - vx, transform.position.y, 0);
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
            nextRoundValueUI.color = new Color(valueColor.r, valueColor.g, valueColor.b, alpha);
            nextRoundTextUI.color = new Color(nextRoundTextUI.color.r, nextRoundTextUI.color.g, nextRoundTextUI.color.b, alpha);
            yield return null;
        }

        transform.position = destinationPos;
        yield return new WaitForSeconds(0.3f);

        //next we animate the new round
        nextRoundValueUI.text = newRound;
        nextRoundEffect.Play();
        float scaleSpeed = 4;
        yield return ScaleNextRoundValue(scaleSpeed);

        yield return new WaitForSeconds(0.5f);

        //fade out
        while(alpha > 0)
        {
            alpha -= fadeSpeed * Time.deltaTime;
            textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
            nextRoundValueUI.color = new Color(valueColor.r, valueColor.g, valueColor.b, alpha);
            nextRoundTextUI.color = new Color(nextRoundTextUI.color.r, nextRoundTextUI.color.g, nextRoundTextUI.color.b, alpha);
            yield return null;
        }


        ToggleNextRoundLabelUI(false);

        //change gamestate to card phase TODO: For some reason, cards don't display after doing this.
        //gm.SetGameState(GameManager.GameState.BeginningNewTurn);
    }

    IEnumerator ScaleNextRoundValue(float scaleSpeed)
    {
        Vector3 originalScale = nextRoundValueUI.transform.localScale;
        Vector3 destinationScale = originalScale * 1.4f;
        while (nextRoundValueUI.transform.localScale.x < destinationScale.x)
        {
            float vx = nextRoundValueUI.transform.localScale.x + scaleSpeed * Time.deltaTime;
            float vy = nextRoundValueUI.transform.localScale.y + scaleSpeed * Time.deltaTime;
            nextRoundValueUI.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
        while (nextRoundValueUI.transform.localScale.x > originalScale.x)
        {
            float vx = nextRoundValueUI.transform.localScale.x - scaleSpeed * Time.deltaTime;
            float vy = nextRoundValueUI.transform.localScale.y - scaleSpeed * Time.deltaTime;
            nextRoundValueUI.transform.localScale = new Vector3(vx, vy, 0);
            yield return null;
        }
        nextRoundValueUI.transform.localScale = originalScale;
    }
}
