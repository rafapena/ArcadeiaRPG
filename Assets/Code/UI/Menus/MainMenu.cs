using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public AudioSource OpeningMusic;
    public AudioSource ButtonClickSFX;

    public GameObject PressAnyKeyInput;
    private bool PressedKey;

    public MenuFrame SelectionFrame;
    public GameObject NewGameButton;
    public GameObject ContinueButton;
    public GameObject SwitchFilesButton;
    public GameObject OptionsButtton;

    public GameObject DifficultySelect;
    public MenuFrame DifficultyFrame;
    public Transform DifficultyList;
    public MenuFrame DifficultyDescriptions;
    private bool SelectedFinal;

    public GameObject OptionsMenu;
    public OptionsFrame OptionsFrame;

    private float IntroTimer;
    public float IntroTime;

    private void Start()
    {
        // TEST SETUP
        //PlayerPrefs.DeleteAll();

        Time.timeScale = 1;
        IntroTimer = Time.time + IntroTime;
        if (OpeningMusic) OpeningMusic.Play();
        PressAnyKeyInput.SetActive(false);
        SelectionFrame.Deactivate();
        CheckSaves();
        DifficultySelect.SetActive(false);
        OptionsMenu.SetActive(false);
    }

    private void Update()
    {
        if (!PressedKey && Time.time > IntroTimer)
        {
            PressAnyKeyInput.SetActive(Time.time % 1 < 0.5f);
            if (Input.anyKeyDown)
            {
                PressedKey = true;
                SelectionFrame.Activate();
                EventSystem.current.SetSelectedGameObject(NewGameButton);
                PressAnyKeyInput.SetActive(false);
            }
        }
        else if (InputMaster.GoingBack())
        {
            if (OptionsMenu.activeSelf) GoBackFromOptions();
            else if (DifficultySelect.activeSelf) UndoDifficultySelect();
        }
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
        SceneMaster.OpenFileSelect(FileSelect.FileMode.LoadOrDelete);
    }

    public void GoToOptions()
    {
        OptionsMenu.SetActive(true);
        OptionsFrame.Activate();
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

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Options --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void GoBackFromOptions()
    {
        OptionsFrame.Deactivate();
        OptionsMenu.SetActive(false);
        EventSystem.current.SetSelectedGameObject(OptionsButtton);
    }
}
