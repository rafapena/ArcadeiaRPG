using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public AudioSource OpeningMusic;
    public AudioSource ButtonClickSFX;

    private void Start()
    {
        if (OpeningMusic) OpeningMusic.Play();
    }

    public void PlayGame()
    {
        ButtonClickSFX.Play();
        //ScreenFadeManager.ChangeScene((int)Globals.Scenes.CIntro, 2, "");
        ScreenFadeManager.ChangeScene("Testland", 2, "");
    }
}
