﻿using UnityEngine;
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
            if (GameplayMaster.NoFileSelected())
            {
                MenuMaster.DisableSelection(ref ReloadButton);
                EventSystem.current.SetSelectedGameObject(ReturnToMenuButton);
            }
            else EventSystem.current.SetSelectedGameObject(ReloadButton);
        }
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
