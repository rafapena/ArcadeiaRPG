using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Directly handles screen transitioning and player position/cutscene changes
public class ScreenTransitioner : MonoBehaviour
{
    public enum TransitionModes { BlackScreen, Battle }
    private static TransitionModes TransitionMode;

    public enum SceneChangeModes { Remove, Change, Add }
    private static SceneChangeModes ChangeMode;

    private static string SourceScene;
    private static string TargetScene;
    private static float DefaultBlackScreenTime;
    private static GameObject TransitionScreen;
    private static bool FinishedLoadingContents;

    private static float Fading;
    private const float FADE_IN_TIMER = 0.5f;
    private const float FADE_OUT_TIMER = 0.5f;
    private const float FADE_SPEED = 0.01f;

    // NOTE: Setup components should be called first, beforehand
    void Start()
    {
        FinishedLoadingContents = true;     // FALSE
        Fading = 0;

        for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(false);
        TransitionScreen = transform.GetChild((int)TransitionMode).gameObject;
        TransitionScreen.SetActive(true);
        TransitionScreen.GetComponent<CanvasGroup>().alpha = 0;

        StartCoroutine(ScreenFade());
    }

    private IEnumerator ScreenFade()
    {
        Time.timeScale = 0;
        yield return Waiting(1, FADE_IN_TIMER);

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

        yield return Waiting(0, DefaultBlackScreenTime, FinishedLoadingContents);
        yield return Waiting(-1, FADE_OUT_TIMER);

        Fading = 0;
        Time.timeScale = 1;
        SceneManager.UnloadSceneAsync(SceneMaster.SCREEN_TRANSITION_SCENE);
    }

    void Update()
    {
        var c = TransitionScreen.GetComponent<CanvasGroup>();
        if (c.alpha > 1) c.alpha = 1;
        else if (c.alpha < 0) c.alpha = 0;
        else c.alpha += Fading * FADE_SPEED;
    }

    public static void SetupComponents(string fromScene, string toScene, float inBlackScreenTime, SceneChangeModes changeMode, TransitionModes transitionMode)
    {
        SourceScene = fromScene;
        TargetScene = toScene;
        DefaultBlackScreenTime = inBlackScreenTime;
        ChangeMode = changeMode;
        TransitionMode = transitionMode;
    }

    private IEnumerator Waiting(float fadingMode, float waitTime, bool waitCondition = true)
    {
        Fading = fadingMode;
        yield return new WaitForSecondsRealtime(waitTime);
        yield return new WaitUntil(() => waitCondition);
    }

    public static void NotifyFinishLoading() => FinishedLoadingContents = true;
}
