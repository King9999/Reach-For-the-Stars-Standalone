using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MMurray.ReachForTheStars;

//opens up tutorial lessons. Game is paused when a tutorial is open.
public class HelpButton : MonoBehaviour
{
    public int lessonID;
    bool animateCoroutineOn = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!animateCoroutineOn)
            StartCoroutine(AnimateHelpButton());
        
        //Clamp(transform.position);
    }

    //activated by button press
    public void OnHelpButtonClicked()
    {
        if (Time.timeScale == 0) return;    

        LessonManager lm = LessonManager.instance;
        UI ui = UI.instance;
        lm.ToggleLesson(lessonID, true);
        ui.TogglePauseText(true);
        Time.timeScale = 0;
    }

    public void Clamp(Vector3 position)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(position); 
        transform.position = worldPos;
    }

    //button floats in place
    IEnumerator AnimateHelpButton()
    {
        animateCoroutineOn = true;
        Vector3 originalPos = transform.position;
        Vector3 destinationPos = new Vector3(originalPos.x, originalPos.y + 5, 0);
        float moveSpeed = 12;
        
        //float up
        while(transform.position.y < destinationPos.y)
        {
            float vy = transform.position.y + moveSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, vy, 0);
            yield return null;
        }

        //float down
        while(transform.position.y > originalPos.y)
        {
            float vy = transform.position.y - moveSpeed * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, vy, 0);
            yield return null;
        }

        transform.position = originalPos;
        animateCoroutineOn = false;
    }

}
