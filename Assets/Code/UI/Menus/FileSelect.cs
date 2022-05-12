using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileSelect : MonoBehaviour
{
    public enum FileMode { Save, LoadOrDelete }
    public static FileMode FileSelectMode;

    public enum Selections { Files, OverwriteConfirm, SelectFileMode, DeleteConfirm }
    private Selections Selection;

    public MenuFrame MainFrame;
    public TextMeshProUGUI ModeHeader;
    public FileSelectionList FilesList;

    public MenuFrame SaveFileInfo;
    public GameObject SelectModeFrame;
    public GameObject DeleteConfirmationFrame;
    public GameObject OverwriteConfirmationFrame;
    public GameObject WaitingFrame;

    public Image AvatarImage;
    public TextMeshProUGUI ChapterNameLabel;
    public TextMeshProUGUI ChapterNumberLabel;
    public TextMeshProUGUI LevelLabel;
    public TextMeshProUGUI DifficultyLabel;
    public TextMeshProUGUI TotalPlayTimeLabel;
    public TextMeshProUGUI CurrentLocationLabel;
    public TextMeshProUGUI GoldLabel;

    private bool Waiting;

    private void Start()
    {
        Selection = Selections.Files;
        MainFrame.Activate();
        SaveFileInfo.Deactivate();
        SelectModeFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(false);
        OverwriteConfirmationFrame.SetActive(false);
        WaitingFrame.SetActive(false);
        FilesList.Refresh();
        SetupFileInfo();    // Force file entry to show up
        switch (FileSelectMode)
        {
            case FileMode.Save:
                ModeHeader.text = "SAVE GAME";
                break;
            case FileMode.LoadOrDelete:
                ModeHeader.text = "LOAD GAME";
                break;
        }
    }

    private void Update()
    {
        bool goingBack = InputMaster.GoingBack;
        switch (Selection)
        {
            case Selections.Files:
                if (goingBack) GoBack();
                break;
            case Selections.OverwriteConfirm:
                if (goingBack) UndoOverwriteConfirm();
                break;
            case Selections.SelectFileMode:
                if (goingBack) UndoLoadOrDelete();
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

    private bool SetupFileInfo(bool bypassWaiting = false)
    {
        if (Waiting && !bypassWaiting) return false;
        FilesList.SetSelected();
        SaveData saveData = FilesList.SelectedObject;
        if (saveData == null || !saveData.FileDataExists)
        {
            SaveFileInfo.Deactivate();
            return false;
        }
        SaveFileInfo.Activate();
        AvatarImage.sprite = null;
        ChapterNameLabel.text = "TEMPLATE CHAPTER NAME";
        ChapterNumberLabel.text = saveData.Chapter.ToString();
        LevelLabel.text = saveData.Level.ToString();
        DifficultyLabel.text = System.Enum.GetName(typeof(GameplayMaster.Difficulties), saveData.Difficulty);
        TotalPlayTimeLabel.text = GameplayMaster.GetTotalPlayTime(saveData.TotalPlayTime);
        CurrentLocationLabel.text = saveData.Location;
        GoldLabel.text = saveData.Gold.ToString();
        return true;
    }

    public void SelectFile()
    {
        if (Waiting || !MenuMaster.ReadyToSelectInMenu) return;
        bool selectedNonEmptyFile = SetupFileInfo();
        FilesList.UnhighlightAll();
        SelectModeFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(false);
        OverwriteConfirmationFrame.SetActive(false);
        switch (FileSelectMode)
        {
            case FileMode.Save:
                if (selectedNonEmptyFile) SetupOverwriteConfirm();
                else SaveGame();
                break;
            case FileMode.LoadOrDelete:
                if (selectedNonEmptyFile) SetupLoadOrDelete();
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Load or Delete --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupLoadOrDelete()
    {
        Selection = Selections.SelectFileMode;
        SelectModeFrame.SetActive(true);
        FilesList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(SelectModeFrame.transform.GetChild(0).gameObject);
    }

    public void UndoLoadOrDelete()
    {
        Selection = Selections.Files;
        SelectModeFrame.SetActive(false);
        FilesList.UnhighlightAll();
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Delete Confirm --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    public void SetupDeleteConfirm()
    {
        Selection = Selections.DeleteConfirm;
        SelectModeFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(true);
        DeleteConfirmationFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "DELETE\nFILE " + FilesList.SelectedObject.File + "?";
        FilesList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(DeleteConfirmationFrame.transform.GetChild(2).gameObject);
    }

    public void UndoDeleteConfirm()
    {
        Selection = Selections.Files;
        SelectModeFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(false);
        FilesList.UnhighlightAll();
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Overwrite Confirm --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void SetupOverwriteConfirm()
    {
        Selection = Selections.OverwriteConfirm;
        OverwriteConfirmationFrame.SetActive(true);
        OverwriteConfirmationFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "OVERWRITE\nFILE " + FilesList.SelectedObject.File + "?";
        FilesList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(OverwriteConfirmationFrame.transform.GetChild(1).gameObject);
    }

    public void UndoOverwriteConfirm()
    {
        Selection = Selections.Files;
        OverwriteConfirmationFrame.SetActive(false);
        FilesList.UnhighlightAll();
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- File Operation --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SaveGame()
    {
        FilesList.SelectedObject.SaveGame();
        OverwriteConfirmationFrame.SetActive(false);
        StartCoroutine(AwaitWrite("Saving..."));
    }

    public void LoadGame()
    {
        FilesList.SelectedButton.KeepSelected();
        FilesList.SelectedObject.LoadGame();
        SceneMaster.CloseFileSelect();
    }

    public void DeleteGame()
    {
        FilesList.SelectedObject.DeleteGame();
        SelectModeFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(false);
        StartCoroutine(AwaitWrite("Deleting..."));
    }

    private IEnumerator AwaitWrite(string waitingMessage)
    {
        Waiting = true;
        FilesList.SelectedButton.KeepSelected();
        WaitingFrame.SetActive(true);
        WaitingFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = waitingMessage;
        yield return new WaitUntil(() => !FilesList.SelectedObject.CurrentlyWritingData);
        DeclareSuccess();
        yield return new WaitForSecondsRealtime(1);
        ReturnToFileSelect();
        Waiting = false;
    }

    public void DeclareSuccess()
    {
        WaitingFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (FileSelectMode == FileMode.LoadOrDelete) ? "Deletion Successful" : "Save Successful";
        GameObject selected = FilesList.SelectedButton.gameObject;
        FilesList.Refresh();
        EventSystem.current.SetSelectedGameObject(selected);
        SetupFileInfo(true);
    }

    public void ReturnToFileSelect()
    {
        Selection = Selections.Files;
        WaitingFrame.SetActive(false);
        FilesList.UnhighlightAll();
        EventSystem.current.SetSelectedGameObject(FilesList.SelectedButton.gameObject);
    }
}