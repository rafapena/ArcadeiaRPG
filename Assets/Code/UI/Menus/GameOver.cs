using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Game over screen
/// </summary>
public class GameOver : MonoBehaviour
{
    public GameObject ReloadButton;
    public GameObject ReturnToMenuButton;
    public AudioSource ButtonClickSFX;
    public MenuFrame SelectionFrame;
    private float INTRO_TIME = 3f;

    private void Start()
    {
        SelectionFrame.Deactivate();
        StartCoroutine(SetupGameOverScreen());
    }

    private IEnumerator SetupGameOverScreen()
    {
        yield return new WaitForSecondsRealtime(INTRO_TIME);
        SelectionFrame.Activate();
        if (GameplayMaster.NoFileSelected())
        {
            MenuMaster.DisableSelection(ref ReloadButton);
            EventSystem.current.SetSelectedGameObject(ReturnToMenuButton);
        }
        else EventSystem.current.SetSelectedGameObject(ReloadButton);
    }

    public void ReloadFromLastSave()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        ButtonClickSFX.Play();
        SaveData data = new SaveData(GameplayMaster.SelectedFile);
        data.LoadGame();
        SceneMaster.CloseGameOver();
    }

    public void ReturnToTitle()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        ButtonClickSFX.Play();
        SceneMaster.ChangeScene(SceneMaster.TITLE_SCREEN_SCENE, 2f);
        SceneMaster.CloseGameOver();
    }

    public void ExitGame()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        Application.Quit();
    }
}
