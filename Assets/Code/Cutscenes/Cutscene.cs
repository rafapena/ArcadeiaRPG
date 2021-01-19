using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Administrates the game's cutscenes
/// </summary>
public class Cutscene : MonoBehaviour
{
    public CutsceneManager Manager;
    public DialogueBubble[] Dialogue;
    private int CurrentBubble;
    public UnityEvent OnComplete;
    public bool CurrentlyRunning;
    private bool InteractionBuffer;

    // Start is called before the first frame update
    void Start()
    {
        //
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentlyRunning && InputMaster.Interact())
        {
            if (InteractionBuffer)
            {
                InteractionBuffer = false;
                return;
            }
            DialogueBubble db = Dialogue[CurrentBubble];
            if (Manager.CurrentlyPrinting()) Manager.ForceStop();
            else if (db.HasChoices()) db.SetupChoices(Manager);
            else NextPage();
        }
    }

    public void Open(bool hadToInteract = true)
    {
        Manager.Open();
        CurrentlyRunning = true;
        SceneMaster.InCutscene = true;
        CurrentBubble = 0;
        Dialogue[CurrentBubble].Display(Manager);
        InteractionBuffer = hadToInteract;
    }

    private void NextPage()
    {
        CurrentBubble++;
        if (CurrentBubble < Dialogue.Length) Dialogue[CurrentBubble].Display(Manager);
        else Complete();
    }

    public void Complete()
    {
        Manager.Close();
        CurrentlyRunning = false;
        SceneMaster.InCutscene = false;
        CurrentBubble = 0;
    }
}
