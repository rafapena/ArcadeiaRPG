using UnityEngine;

/// <summary>
/// Game over screen
/// </summary>
public class GameOver : MonoBehaviour
{
    public AudioSource EndSound;
    public AudioSource ButtonClickSFX;

    private void Start()
    {
        EndSound.Play();
    }

    public void TryAgain()
    {
        ButtonClickSFX.Play();
        //ScreenFadeManager.ChangeScene("gameplayLvl" + Globals.Level, 1, "");
    }

    public void BackToMenu()
    {
        ButtonClickSFX.Play();
        ScreenTransitioner.ChangeScene("Title", 2, "");
    }
}
