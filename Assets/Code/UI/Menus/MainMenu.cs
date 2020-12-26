using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public AudioSource OpeningMusic;
    public AudioSource ButtonClickSFX;

    private void Start()
    {
        if (OpeningMusic) OpeningMusic.Play();
    }

    public void StartNew()
    {
        ButtonClickSFX.Play();
        SceneMaster.ChangeScene("Testland1", 2f);
    }

    public void Continue()
    {
        ButtonClickSFX.Play();
        SceneMaster.OpenFileSelect(FileSelect.FileMode.Load, null);
    }

    public void ExitGame()
    {
        ButtonClickSFX.Play();
        Application.Quit();
    }
}
