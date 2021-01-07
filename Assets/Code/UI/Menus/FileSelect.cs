﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileSelect : MonoBehaviour
{
    public enum FileMode { Save, Load, Delete }
    public static FileMode FileSelectMode;

    public enum Selections { Files, OverwriteConfirm, DeleteConfirm, Waiting, Success }
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

    private float WriteWaitingTimer;
    private const float WRITE_WAITING_TIMER_LIMIT = 1f;
    private float SuccessWaitingTimer;
    private const float SUCCESS_WAITING_TIMER_LIMIT = 1f;

    private void Start()
    {
        Selection = Selections.Files;
        MainFrame.Activate();
        SaveFileInfo.Deactivate();
        SelectModeFrame.SetActive(false);
        DeleteConfirmationFrame.SetActive(false);
        OverwriteConfirmationFrame.SetActive(false);
        WaitingFrame.SetActive(false);
        FilesList.Setup();
        SetupFileInfo();    // Force file entry to show up
        switch (FileSelectMode)
        {
            case FileMode.Save:
                ModeHeader.text = "SAVE GAME";
                break;
            case FileMode.Load:
                ModeHeader.text = "LOAD GAME";
                break;
            case FileMode.Delete:
                ModeHeader.text = "DELETE GAME";
                break;
        }
    }

    private void Update()
    {
        bool goingBack = InputMaster.GoingBack();
        switch (Selection)
        {
            case Selections.Files:
                if (goingBack) GoBack();
                break;
            case Selections.OverwriteConfirm:
                if (goingBack) UndoOverwriteConfirm();
                break;
            case Selections.DeleteConfirm:
                if (goingBack) UndoDeleteConfirm();
                break;
            case Selections.Waiting:
                if (!WaitingForWriting()) DeclareSuccess();
                break;
            case Selections.Success:
                if (!WaitingForSuccess()) ReturnToFileSelect();
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
        if (Waiting() && !bypassWaiting) return false;
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
        if (Waiting()) return;
        bool selectedNonEmptyFile = SetupFileInfo();
        switch (FileSelectMode)
        {
            case FileMode.Save:
                if (selectedNonEmptyFile) SetupOverwriteConfirm();
                else SaveGame();
                break;
            case FileMode.Load:
                if (selectedNonEmptyFile) LoadGame();
                break;
            case FileMode.Delete:
                if (selectedNonEmptyFile) SetupDeleteConfirm();
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Load or Delete --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupLoadOrDelete()
    {
        //Selection = Selections.LoadOrDelete;
        SelectModeFrame.SetActive(true);
        EventSystem.current.SetSelectedGameObject(transform.GetChild(1).gameObject);
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
        DeleteConfirmationFrame.SetActive(true);
        DeleteConfirmationFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "DELETE\nFILE " + FilesList.SelectedObject.FileNumber + "?";
        FilesList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(transform.GetChild(1).gameObject);
    }

    public void UndoDeleteConfirm()
    {
        Selection = Selections.Files;
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
        OverwriteConfirmationFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "OVERWRITE\nFILE " + FilesList.SelectedObject.FileNumber + "?";
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
        AwaitWrite("Saving...");
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
        DeleteConfirmationFrame.SetActive(false);
        AwaitWrite("Deleting...");
    }

    private void AwaitWrite(string waitingMessage)
    {
        Selection = Selections.Waiting;
        FilesList.SelectedButton.KeepSelected();
        WriteWaitingTimer = Time.unscaledTime + WRITE_WAITING_TIMER_LIMIT;
        WaitingFrame.SetActive(true);
        WaitingFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = waitingMessage;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Waiting and Success --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool WaitingForWriting()
    {
        return WriteWaitingTimer > Time.unscaledTime;
    }

    public bool WaitingForSuccess()
    {
        return SuccessWaitingTimer > Time.unscaledTime;
    }

    public bool Waiting()
    {
        return Selection == Selections.Waiting || Selection == Selections.Success;
    }

    public void DeclareSuccess()
    {
        Selection = Selections.Success;
        WaitingFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (FileSelectMode == FileMode.Delete) ? "Deletion Successful" : "Save Successful";
        SuccessWaitingTimer = Time.unscaledTime + SUCCESS_WAITING_TIMER_LIMIT;
        GameObject selected = FilesList.SelectedButton.gameObject;
        FilesList.Setup();
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