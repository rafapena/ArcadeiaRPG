using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    private enum Selections { PauseMain, Options, ConfirmReturnTitle, ConfirmExitGame }
    private Selections Selection;

    // Child GameObjects
    public MenuFrame PauseFrame;
    public GameObject ButtonList;
    public OptionsFrame Options;
    public GameObject ConfirmReturnToTitleFrame;
    public GameObject ConfirmExitGameFrame;

    private int SelectedIndex;
    [HideInInspector] public PlayerParty PartyInfo;

    private void Awake()
    {
        PartyInfo = GameplayMaster.Party;
        SetupPauseMenu();
    }

    private void Update()
    {
        bool goingBack = InputMaster.GoingBack();
        switch (Selection)
        {
            case Selections.PauseMain:
                if (goingBack) Continue();
                break;
            case Selections.Options:
                if (goingBack) BackToPauseMain();
                break;
            case Selections.ConfirmReturnTitle:
            case Selections.ConfirmExitGame:
                if (goingBack) BackToPauseMain();
                break;
        }
    }

    public void SetupPauseMenu()
    {
        Selection = Selections.PauseMain;
        PauseFrame.Activate();
        Options.Deactivate();
        ConfirmReturnToTitleFrame.SetActive(false);
        ConfirmExitGameFrame.SetActive(false);
        EventSystem.current.SetSelectedGameObject(ButtonList.transform.GetChild(0).gameObject);
    }

    public void Continue()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        SceneMaster.ClosePauseMenu(PartyInfo);
    }

    public void GoToMenu()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        SceneMaster.ClosePauseMenu(PartyInfo);
        SceneMaster.OpenMapMenu(PartyInfo);
    }

    public void GoToLoadGame()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        SceneMaster.ClosePauseMenu(PartyInfo);
        SceneMaster.OpenFileSelect(FileSelect.FileMode.LoadOrDelete, PartyInfo);
    }

    public void GoToOptions()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        Selection = Selections.Options;
        ConfirmReturnToTitleFrame.gameObject.SetActive(false);
        ConfirmExitGameFrame.gameObject.SetActive(false);
        HighlightSelection();
        Options.Activate();
    }

    public void GoToReturnToTitle()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        Selection = Selections.ConfirmReturnTitle;
        Options.Deactivate();
        ConfirmReturnToTitleFrame.gameObject.SetActive(true);
        ConfirmExitGameFrame.gameObject.SetActive(false);
        HighlightSelection();
        EventSystem.current.SetSelectedGameObject(ConfirmReturnToTitleFrame.transform.GetChild(1).gameObject);
    }

    public void GoToExitGame()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        Selection = Selections.ConfirmExitGame;
        Options.Deactivate();
        ConfirmReturnToTitleFrame.gameObject.SetActive(false);
        ConfirmExitGameFrame.gameObject.SetActive(true);
        HighlightSelection();
        EventSystem.current.SetSelectedGameObject(ConfirmExitGameFrame.transform.GetChild(1).gameObject);
    }

    private void HighlightSelection()
    {
        UnhighlightAll();
        ListSelectable ls = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        SelectedIndex = ls.Index;
        ls.KeepHighlighted();
    }

    public void BackToPauseMain()
    {
        SetupPauseMenu();
        UnhighlightAll();
        EventSystem.current.SetSelectedGameObject(ButtonList.transform.GetChild(SelectedIndex).gameObject);
    }

    private void UnhighlightAll()
    {
        for (int i = 0; i < ButtonList.transform.childCount; i++)
        {
            ButtonList.transform.GetChild(i).GetComponent<ListSelectable>().ClearHighlights();
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Return to Title and Exit --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ReturnToTitle()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        Continue();
        SceneMaster.ChangeScene(SceneMaster.TITLE_SCREEN_SCENE, 2f);
    }

    public void ExitGame()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        Application.Quit();
    }
}
