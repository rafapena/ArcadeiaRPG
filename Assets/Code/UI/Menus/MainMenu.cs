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
        RetrieveLatestGameLoaded();
    }

    public void SwitchFiles()
    {
        ButtonClickSFX.Play();
        FileSelectionList.HighlightedButtonAfterUndo = EventSystem.current.currentSelectedGameObject;
        SceneMaster.OpenFileSelect(FileSelect.FileMode.Load, null);
    }

    public void ExitGame()
    {
        ButtonClickSFX.Play();
        Application.Quit();
    }

    public void RetrieveLatestGameLoaded()
    {
        //
    }

    public void CheckSaves()
    {
        Color c = Color.white;
        c.a = 0.2f;
        ContinueButton.GetComponent<Button>().interactable = false;
        ContinueButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = c;
        SwitchFilesButton.GetComponent<Button>().interactable = false;
        SwitchFilesButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = c;
    }

    private void StartGame()
    {
        GameplayMaster.SelectedFile = 0;
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
        StartGame();
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
