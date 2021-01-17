using UnityEngine;

/// <summary>
/// Administrates the game's cutscenes
/// </summary>
public class Cutscenes_Backup : MonoBehaviour
{
    public AudioSource BGM;
    private Transform ImageSet;
    private int CurrentImage;

    // Start is called before the first frame update
    void Start()
    {
        if (BGM) BGM.Play();
        ImageSet = transform;
        foreach (Transform image in ImageSet)
            image.gameObject.SetActive(false);
        ImageSet.GetChild(0).gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        Transform img = ImageSet.GetChild(CurrentImage);
        if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space)) && img.childCount <= 0)  // Forced to choose buttons if the sub-scene has any buttons to choose from
        {
            FlowJumper j = img.GetComponent<FlowJumper>();
            if (!j) NextPage();
            else if (j.ToImage == 0) CompleteCutscene(j);
            else JumpTo(j.ToImage);
        }
    }

    // NOTE: Nothing happens when this is called at the last page
    // Last piece of text must be a FlowJumper where ToImage == 0, in order to leave the cutscene
    private void NextPage()
    {
        if (CurrentImage == ImageSet.GetChild(CurrentImage).childCount - 1) return;
        ImageSet.GetChild(CurrentImage).gameObject.SetActive(false);
        CurrentImage++;
        ImageSet.GetChild(CurrentImage).gameObject.SetActive(true);
    }

    // USED FROM FLOWJUMPER.CS: image and text use index 1 as first element, instead of 0
    // Also used for buttons: Cannot be used to end a cutscene
    public void JumpTo(int image)
    {
        ImageSet.GetChild(CurrentImage).gameObject.SetActive(false);
        CurrentImage = image - 1;
        ImageSet.GetChild(CurrentImage).gameObject.SetActive(true);
    }

    // Finish cutscene and land on the next level or page
    public void CompleteCutscene(FlowJumper jumper)
    {
        //if (jumper.ToGamePlay) ScreenTransitioner.ChangeScene("gameplayLvl" + jumper.GoToLevel, 2, "Level " + jumper.GoToLevel);
        //else if (jumper.ToYouWinScreen) ScreenTransitioner.ChangeScene("Title", 3, "");
        //else if (jumper.ToGameOverScreen) ScreenTransitioner.ChangeScene("GameOver", 2, "");
        //else ScreenTransitioner.ChangeScene(0, 2, "");
    }
}
