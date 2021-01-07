using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public AudioSource OpeningMusic;
    public AudioSource ButtonClickSFX;
    public GameObject NewGameButton;
    public GameObject ContinueButton;
    public GameObject SwitchFilesButton;

    public GameObject DifficultySelect;
    public MenuFrame DifficultyFrame;
    public Transform DifficultyList;
    public MenuFrame DifficultyDescriptions;
    private bool SelectedFinal;

    private void Start()
    {
        // TEST SETUP
        //PlayerPrefs.DeleteAll();

        if (OpeningMusic) OpeningMusic.Play();
        CheckSaves();
        EventSystem.current.SetSelectedGameObject(NewGameButton);
        DifficultySelect.SetActive(false);
    }

    private void Update()
    {
        if (DifficultySelect.activeSelf && InputMaster.GoingBack()) UndoDifficultySelect();
    }

    public void StartNew()
    {
        ButtonClickSFX.Play();
        SetupDifficultySelect();
    }

    public void Continue()
    {
        ButtonClickSFX.Play();
        SaveData data = new SaveData(GameplayMaster.GetLastManagedFile());
        data.LoadGame();
    }

    public void SwitchFiles()
    {
        ButtonClickSFX.Play();
        FileSelectionList.HighlightedButtonAfterUndo = EventSystem.current.currentSelectedGameObject;
        SceneMaster.OpenFileSelect(FileSelect.FileMode.Load);
    }

    public void ExitGame()
    {
        ButtonClickSFX.Play();
        Application.Quit();
    }

    public void CheckSaves()
    {
        bool fileExists = false;
        for (int i = 0; i < 100; i++)
        {
            SaveData s = new SaveData(i);
            if (!s.FileDataExists) continue;
            fileExists = true;
            break;
        }
        if (!fileExists)
        {
            MenuMaster.DisableSelection(ref ContinueButton);
            MenuMaster.DisableSelection(ref SwitchFilesButton);
        }
    }

    private void StartNewGame()
    {
        GameplayMaster.SelectedFile = -1;
        SaveData.SetupForNewGame();
        SceneMaster.ChangeScene("Testland1", 2f);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Difficulty Select --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupDifficultySelect()
    {
        DifficultySelect.SetActive(true);
        DifficultyFrame.Activate();
        DifficultyDescriptions.Activate();
        GameObject highestDifficultyButton = DifficultyList.GetChild(DifficultyList.transform.childCount - 1).gameObject;
        highestDifficultyButton.SetActive(PlayerPrefs.GetInt("GameBeatenInNormalOrHard") > 0);
        EventSystem.current.SetSelectedGameObject(DifficultyList.GetChild(0).gameObject);
    }

    public void HoverOverDifficulty(int index)
    {
        if (SelectedFinal) return;
        foreach (Transform t in DifficultyDescriptions.transform) t.gameObject.SetActive(false);
        DifficultyDescriptions.gameObject.SetActive(true);
        DifficultyDescriptions.transform.GetChild(index).gameObject.SetActive(true);
    }

    public void SelectDifficulty(int index)
    {
        if (SelectedFinal) return;
        switch (index)
        {
            case 0:
                GameplayMaster.Difficulty = GameplayMaster.Difficulties.Easy;
                break;
            case 1:
                GameplayMaster.Difficulty = GameplayMaster.Difficulties.Medium;
                break;
            case 2:
                GameplayMaster.Difficulty = GameplayMaster.Difficulties.Hard;
                break;
            default:
                GameplayMaster.Difficulty = GameplayMaster.Difficulties.Lunatic;
                break;
        }
        SelectedFinal = true;
        StartNewGame();
    }

    public void UndoDifficultySelect()
    {
        if (SelectedFinal) return;
        DifficultySelect.SetActive(false);
        DifficultyFrame.Deactivate();
        DifficultyDescriptions.Deactivate();
        EventSystem.current.SetSelectedGameObject(NewGameButton);
    }
}
