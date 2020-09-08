using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Directly handles screen transitioning and player position/cutscene changes
public class ScreenFadeManager : MonoBehaviour
{
    private static MapPlayer TargetPlayer;
    private static float ToX;
    private static float ToY;

    private static int ToScene;
    private static string ToSceneS;
    private static bool SwitchScenes;
    private static bool IntMode;

    private static float InBlackScreenTime;
    private static float TimeMarker;
    private static int FadeChangesMade;

    private const float FADE_IN_TIMER = 0.5f;
    private const float FADE_OUT_TIMER = 0.5f;
    private const float FADE_SPEED = 0.01f;
    private const string BlackScreenUI = "/UISceneTransition/Fade";

    void Start()
    {
        GameObject.Find(BlackScreenUI).GetComponent<CanvasGroup>().alpha = 1;
        ToScene = -1;
        FadeChangesMade = 1;
        TimeMarker = Time.realtimeSinceStartup + FADE_OUT_TIMER;
        Time.timeScale = 0;
    }

    void Update()
    {
        float time = TimeMarker - Time.realtimeSinceStartup;

        if (time > InBlackScreenTime + FADE_OUT_TIMER)
            GameObject.Find(BlackScreenUI).GetComponent<CanvasGroup>().alpha += FADE_SPEED;

        // Black screen period
        else if (time > FADE_OUT_TIMER)
            GameObject.Find(BlackScreenUI).GetComponent<CanvasGroup>().alpha = 1;

        else if (FadeChangesMade == 0)
        {
            if (SwitchScenes)
            {
                if (ToScene >= 0 && IntMode) SceneManager.LoadScene(ToScene);
                else if (ToSceneS != null && !ToSceneS.Equals("") && !IntMode) SceneManager.LoadScene(ToSceneS);
            }
            else TargetPlayer.transform.position = new Vector3(ToX, ToY);
            FadeChangesMade++;
        }

        else if (time > 0)
            GameObject.Find(BlackScreenUI).GetComponent<CanvasGroup>().alpha -= FADE_SPEED;

        else if (time <= 0 && FadeChangesMade == 1)
            FadeOutEnd();
    }

    public static void ChangeScene(int scene, float blackScreenTime, string transitionText)
    {
        IntMode = true;
        SwitchScenes = true;
        ToScene = scene;
        FadeInStart(blackScreenTime);
    }
    public static void ChangeScene(string scene, float blackScreenTime, string transitionText)
    {
        IntMode = false;
        SwitchScenes = true;
        ToSceneS = scene;
        FadeInStart(blackScreenTime);
    }

    public static void ChangeLocation(float toX, float toY, MapPlayer targetPlayer, float blackScreenTime, string transitionText)
    {
        SwitchScenes = false;
        ToX = toX;
        ToY = toY;
        TargetPlayer = targetPlayer;
        FadeInStart(blackScreenTime);
    }

    private static void FadeInStart(float blackScreenTime)
    {
        FadeChangesMade = 0;
        InBlackScreenTime = blackScreenTime;
        GameObject.Find("/UISceneTransition").GetComponent<Canvas>().sortingOrder = 2;
        Time.timeScale = 0;
        TimeMarker = Time.realtimeSinceStartup + InBlackScreenTime + FADE_IN_TIMER + FADE_OUT_TIMER;
    }

    private static void FadeOutEnd()
    {
        FadeChangesMade++;
        GameObject.Find(BlackScreenUI).GetComponent<CanvasGroup>().alpha = 0;
        Time.timeScale = 1;
        ToScene = -1;
    }
}
