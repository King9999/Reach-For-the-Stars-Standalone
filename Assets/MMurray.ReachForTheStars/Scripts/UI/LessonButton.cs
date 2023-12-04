using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MMurray.ReachForTheStars;

//used with Viewed Lessons UI. Opens the lesson based on the lesson ID.
public class LessonButton : MonoBehaviour
{
    public byte lessonID;
    public TextMeshProUGUI buttonText;
    public bool buttonTextUpdated;      //once text is updated, don't want to do it again
    public bool lessonAlreadyViewed;    //used by the Lesson script to pause game while lesson is opened, and to close lesson without advancing round.

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    string GetText (string key)
    {
        string value = SharedState.LanguageDefs?[key];
        return value ?? "--missing--";
    }

    //updates button name if lesson has been viewed
    public void CheckViewedStatus()
    {
        if (buttonTextUpdated) return;

        LessonManager lm = LessonManager.instance;
        if (!buttonTextUpdated && lm.lessonList[lessonID].lessonViewed)
        {
            //TextMeshProUGUI buttonName = GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = GetText(lm.lessonList[lessonID].titleKey).ToUpper();
            buttonTextUpdated = true;
        }
    }

    public void OnLessonButtonClicked()
    {
        LessonManager lm = LessonManager.instance;
        //if the lesson hasn't been viewed yet, return
        if (!lm.lessonList[lessonID].lessonViewed) return;

        //find the corresponding ID
        lessonAlreadyViewed = true;

        lm.ToggleLesson(lessonID, true);

        //hide window temporarily
        UI ui = UI.instance;
        ui.viewedLessonsContainer.gameObject.SetActive(false);
    }
}
