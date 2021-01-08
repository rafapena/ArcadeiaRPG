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
    private const string TITLE_SCREEN_SCENE = "Title";
    public MenuFrame PauseFrame;
    public GameObject ButtonList;
    public MenuFrame OptionsFrame;
    public GameObject ConfirmReturnToTitleFrame;
    public GameObject ConfirmExitGameFrame;

    private int SelectedIndex;
    [HideInInspector] public PlayerParty PartyInfo;

    private void Awake()
    {
        PartyInfo = MenuMaster.PartyInfo;
        SetupPauseMenu();
    }

    private void Update()
    {
        switch (Selection)
        {
            case Selections.PauseMain:
                if (InputMaster.GoingBack()) Continue();
                break;
            case Selections.Options:
            case Selections.ConfirmReturnTitle:
            case Selections.ConfirmExitGame:
                if (InputMaster.GoingBack()) BackToPauseMain();
                break;
        }
    }

    public void SetupPauseMenu()
    {
        Selection = Selections.PauseMain;
        PauseFrame.Activate();
        OptionsFrame.Deactivate();
        ConfirmReturnToTitleFrame.SetActive(false);
        ConfirmExitGameFrame.SetActive(false);
        EventSystem.current.SetSelectedGameObject(ButtonList.transform.GetChild(0).gameObject);
    }

    public void Continue()
    {
        SceneMaster.ClosePauseMenu(PartyInfo);
    }

    public void GoToMenu()
    {
        SceneMaster.ClosePauseMenu(PartyInfo);
        SceneMaster.OpenMapMenu(PartyInfo);
    }

    public void GoToLoadGame()
    {
        SceneMaster.ClosePauseMenu(PartyInfo);
        SceneMaster.OpenFileSelect(FileSelect.FileMode.LoadOrDelete, PartyInfo);
    }

    public void GoToOptions()
    {
        Selection = Selections.Options;
        OptionsFrame.Activate();
        ConfirmReturnToTitleFrame.gameObject.SetActive(false);
        ConfirmExitGameFrame.gameObject.SetActive(false);
        HighlightSelection();
        //EventSystem.current.SetSelectedGameObject(OptionsFrame.transform.GetChild(1).gameObject);
    }

    public void GoToReturnToTitle()
    {
        Selection = Selections.ConfirmReturnTitle;
        OptionsFrame.Deactivate();
        ConfirmReturnToTitleFrame.gameObject.SetActive(true);
        ConfirmExitGameFrame.gameObject.SetActive(false);
        HighlightSelection();
        EventSystem.current.SetSelectedGameObject(ConfirmReturnToTitleFrame.transform.GetChild(1).gameObject);
    }

    public void GoToExitGame()
    {
        Selection = Selections.ConfirmExitGame;
        OptionsFrame.Deactivate();
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
    /// -- Options --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void AdjustMusic()
    {

    }

    public void AdjustSound()
    {

    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Return to Title and Exit --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ReturnToTitle()
    {
        Continue();
        SceneMaster.ChangeScene(TITLE_SCREEN_SCENE, 2f);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
