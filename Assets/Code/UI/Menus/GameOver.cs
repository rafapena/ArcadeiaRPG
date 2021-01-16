using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Game over screen
/// </summary>
public class GameOver : MonoBehaviour
{
    public GameObject ReloadButton;
    public AudioSource ButtonClickSFX;
    public MenuFrame SelectionFrame;

    private float IntroTimer;
    public float IntroTime;

    private void Start()
    {
        SelectionFrame.Deactivate();
        IntroTimer = Time.unscaledTime + IntroTime;
    }

    private void Update()
    {
        if (!SelectionFrame.Activated && Time.unscaledTime > IntroTimer)
        {
            SelectionFrame.Activate();
            EventSystem.current.SetSelectedGameObject(ReloadButton);
        }
    }

    public void ReloadFromLastSave()
    {
        ButtonClickSFX.Play();
        SaveData data = new SaveData(GameplayMaster.GetLastManagedFile());
        data.LoadGame();
        SceneMaster.CloseGameOver();
    }

    public void ReturnToTitle()
    {
        ButtonClickSFX.Play();
        SceneMaster.ChangeScene(SceneMaster.TITLE_SCREEN_SCENE, 2f);
        SceneMaster.CloseGameOver();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
