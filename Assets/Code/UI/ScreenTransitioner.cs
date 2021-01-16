using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Directly handles screen transitioning and player position/cutscene changes
public class ScreenTransitioner : MonoBehaviour
{
    public enum TransitionModes { BlackScreen, Batte }
    private static TransitionModes TransitionMode;

    public enum SceneChangeModes { Remove, Change, Add }
    private static SceneChangeModes ChangeMode;

    private static string SourceScene;
    private static string TargetScene;
    private static GameObject TransitionScreen;

    private bool SceneUpdated;
    private static float InBlackScreenTime;
    private static float TimeMarker;
    private static int FadeChangesMade;

    private const float FADE_IN_TIMER = 0.5f;
    private const float FADE_OUT_TIMER = 0.5f;
    private const float FADE_SPEED = 0.01f;

    // NOTE: Setup components should be called first, beforehand
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
        TransitionScreen = transform.GetChild((int)TransitionMode).gameObject;
        TransitionScreen.SetActive(true);
        TransitionScreen.GetComponent<CanvasGroup>().alpha = 0;
        FadeChangesMade = 1;
        TimeMarker = Time.realtimeSinceStartup + FADE_OUT_TIMER;
        Time.timeScale = 0;
        FadeInStart(InBlackScreenTime);
    }

    void Update()
    {
        float time = TimeMarker - Time.realtimeSinceStartup;

        if (time > InBlackScreenTime + FADE_OUT_TIMER)
            TransitionScreen.GetComponent<CanvasGroup>().alpha += FADE_SPEED;

        // Black screen period
        else if (time > FADE_OUT_TIMER)
        {
            TransitionScreen.GetComponent<CanvasGroup>().alpha = 1;
            if (!SceneUpdated)
            {
                SceneUpdated = true;
                switch (ChangeMode)
                {
                    case SceneChangeModes.Remove:
                        SceneManager.UnloadSceneAsync(TargetScene);
                        break;
                    case SceneChangeModes.Change:
                        SceneManager.UnloadSceneAsync(SourceScene);
                        SceneManager.LoadScene(TargetScene, LoadSceneMode.Additive);
                        break;
                    case SceneChangeModes.Add:
                        SceneManager.LoadScene(TargetScene, LoadSceneMode.Additive);
                        break;
                }
            }
        }

        else if (FadeChangesMade == 0)
            FadeChangesMade++; 

        else if (time > 0)
            TransitionScreen.GetComponent<CanvasGroup>().alpha -= FADE_SPEED;

        else if (time <= 0 && FadeChangesMade == 1)
            FadeOutEnd();
    }

    public static void SetupComponents(string fromScene, string toScene, float inBlackScreenTime, SceneChangeModes changeMode, TransitionModes transitionMode)
    {
        SourceScene = fromScene;
        TargetScene = toScene;
        InBlackScreenTime = inBlackScreenTime;
        ChangeMode = changeMode;
        TransitionMode = transitionMode;
    }

    private static void FadeInStart(float blackScreenTime)
    {
        FadeChangesMade = 0;
        InBlackScreenTime = blackScreenTime;
        Time.timeScale = 0;
        TimeMarker = Time.realtimeSinceStartup + InBlackScreenTime + FADE_IN_TIMER + FADE_OUT_TIMER;
    }

    private static void FadeOutEnd()
    {
        FadeChangesMade++;
        Time.timeScale = 1;
        TransitionScreen.GetComponent<CanvasGroup>().alpha = 0;
        SceneManager.UnloadSceneAsync(SceneMaster.SCREEN_TRANSITION_SCENE);
    }
}
