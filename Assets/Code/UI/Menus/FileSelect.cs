using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileSelect : MonoBehaviour
{
    public enum FileMode { Save, Load }
    public static FileMode FileSelectMode;

    public enum Selections { Files, LoadOrDelete, OverwriteConfirm, DeleteConfirm }
    private Selections Selection;

    public MenuFrame MainFrame;
    public FileSelectionList FilesList;

    public MenuFrame SaveFileInfo;
    public GameObject LoadOrDeleteFrame;
    public GameObject DeleteConfirmationFrame;
    public GameObject OverwriteConfirmationFrame;

    public Image AvatarImage;
    public TextMeshProUGUI ChapterNameLabel;
    public TextMeshProUGUI ChapterNumberLabel;
    public TextMeshProUGUI LevelLabel;
    public TextMeshProUGUI DifficultyLabel;
    public TextMeshProUGUI TotalPlayTimeLabel;
    public TextMeshProUGUI CurrentLocationLabel;
    public TextMeshProUGUI GoldLabel;

    private void Start()
    {
        MainFrame.Activate();
        SaveFileInfo.Deactivate();
        LoadOrDeleteFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(false);
        OverwriteConfirmationFrame.SetActive(false);
        FilesList.Setup();
        Selection = Selections.Files;
    }

    private void Update()
    {
        bool goingBack = InputMaster.GoingBack();
        switch (Selection)
        {
            case Selections.Files:
                if (goingBack) GoBack();
                break;
            case Selections.LoadOrDelete:
                if (goingBack) UndoLoadOrDelete();
                break;
            case Selections.OverwriteConfirm:
                if (goingBack) UndoOverwriteConfirm();
                break;
            case Selections.DeleteConfirm:
                if (goingBack) UndoDeleteConfirm();
                break;
        }
    }

    public void GoBack()
    {
        SceneMaster.CloseFileSelect();
    }

    public void HoverOverFile()
    {
        if (Selection == Selections.Files) SetupFileInfo();
    }

    public void SelectFile()
    {
        switch (FileSelectMode)
        {
            case FileMode.Save:
                break;
            case FileMode.Load:
                break;
        }
        SetupFileInfo();
    }

    private void SetupFileInfo()
    {
        FilesList.SetSelected();
        SaveData saveData = FilesList.SelectedObject;
        if (saveData == null || !saveData.FileExists)
        {
            SaveFileInfo.Deactivate();
            return;
        }
        SaveFileInfo.Activate();
        AvatarImage.sprite = null;
        ChapterNameLabel.text = "";
        ChapterNumberLabel.text = "";
        LevelLabel.text = "";
        DifficultyLabel.text = "";
        TotalPlayTimeLabel.text = "";
        CurrentLocationLabel.text = "";
        GoldLabel.text = "";
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Load or Delete --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupLoadOrDelete()
    {
        Selection = Selections.LoadOrDelete;
        LoadOrDeleteFrame.SetActive(true);
        EventSystem.current.SetSelectedGameObject(transform.GetChild(1).gameObject);
    }

    public void UndoLoadOrDelete()
    {
        Selection = Selections.Files;
        LoadOrDeleteFrame.SetActive(false);
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Delete Confirm --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void SetupDeleteConfirm()
    {
        Selection = Selections.DeleteConfirm;
        DeleteConfirmationFrame.SetActive(true);
        EventSystem.current.SetSelectedGameObject(transform.GetChild(1).gameObject);
    }

    public void UndoDeleteConfirm()
    {
        Selection = Selections.Files;
        DeleteConfirmationFrame.SetActive(false);
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Overwrite Confirm --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void SetupOverwriteConfirm()
    {
        Selection = Selections.OverwriteConfirm;
        OverwriteConfirmationFrame.SetActive(true);
        EventSystem.current.SetSelectedGameObject(transform.GetChild(1).gameObject);
    }

    public void UndoOverwriteConfirm()
    {
        Selection = Selections.Files;
        OverwriteConfirmationFrame.SetActive(false);
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- File Operation --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SaveGame()
    {

    }

    public void LoadGame()
    {

    }
}
