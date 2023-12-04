using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLSDK;
using MMurray.ReachForTheStars;

public class LessonManager : MonoBehaviour
{
    public Lesson[] lessonList;
    public MiniLesson[] miniLessonList;

    public static LessonManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        
    }

    // Start is called before the first frame update
    void Start()
    {
        /*foreach(Lesson lesson in lessonList)
        {
            lesson.gameObject.SetActive(false);
        }*/
    }

    //sets lessons to false.
    public void Initialize()
    {
        UniversalSettings us = UniversalSettings.instance;
        foreach(Lesson lesson in lessonList)
        {
            lesson.UpdateCloseButtonLanguage();
            if (us.allLessonsViewed)
                lesson.lessonViewed = true;
            lesson.gameObject.SetActive(false);
        }

        foreach(MiniLesson lesson in miniLessonList)
        {
            lesson.UpdateCloseButtonLanguage();
            if (us.allLessonsViewed)
                lesson.lessonViewed = true;
            lesson.gameObject.SetActive(false);
        }
    }


    public void ToggleLesson(int id, bool toggle)
    {
        lessonList[id].gameObject.SetActive(toggle);
        if (toggle == true)
        {
            lessonList[id].ToggleUIElements(lessonList[id].detailsEnabled, lessonList[id].sidebarEnabled, lessonList[id].imageEnabled);
            lessonList[id].lessonViewed = true;

            //TTS
            UniversalSettings us = UniversalSettings.instance;
            //if (us.ttsEnabled)
            //{
                //LOLSDK.Instance.SpeakText(lessonList[id].detailsKey);
                /*switch(id)
                {
                    case 5: case 6: case 9: case 10: case 12: case 15:
                        LOLSDK.Instance.SpeakText(lessonList[id].sidebarKey);
                        break;

                    default:
                        LOLSDK.Instance.SpeakText(lessonList[id].detailsKey);
                        break;

                }*/
                /*if (lessonList[id].detailsEnabled)
                    LOLSDK.Instance.SpeakText(lessonList[id].detailsKey);
                else if (lessonList[id].sidebarEnabled)
                    LOLSDK.Instance.SpeakText(lessonList[id].sidebarKey);*/
            //}
        }
    }

    public void ToggleMiniLesson(byte id, bool toggle)
    {
        miniLessonList[id].gameObject.SetActive(toggle);
        if (toggle == true)
        {
            miniLessonList[id].ToggleUIElements(true);
            miniLessonList[id].lessonViewed = true;

            //various UI is also toggled if certain lessons are open.
            UI ui = UI.instance;
            switch(id)
            {
                case 5:
                    ui.ToggleCardEffectIndicator(true);
                    break;

                case 7:
                    ui.ToggleDiceRollRecordIndicator(true);
                    break;

                case 14: case 20:
                    ui.ToggleProbabilityFormatIndicator(true);
                    break; 
            }
            

            //TTS
            /*UniversalSettings us = UniversalSettings.instance;
            if (us.ttsEnabled)
            {
                LOLSDK.Instance.SpeakText(miniLessonList[id].detailsKey);
            }*/
        }
    }
}
