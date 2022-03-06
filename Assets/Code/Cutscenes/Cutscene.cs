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
        if (!Manager.ChoicesFrame.activeSelf && CurrentlyRunning && InputMaster.Interact())
        {
            if (InteractionBuffer)
            {
                InteractionBuffer = false;
                return;
            }
            DialogueBubble db = Dialogue[CurrentBubble];
            if (Manager.CurrentlyPrinting())
            {
                Manager.ForceStop();
            }
            else if (db.HasChoices)
            {
                Manager.ForceStop();
                db.SetupChoices(Manager);
                Manager.ChoicesFrame.SetActive(true);
            }
            else NextPage();
        }
        else if (Manager.ChoicesFrame.activeSelf)
        {
            DialogueBubble db = Dialogue[CurrentBubble];
            string[] choices = { "W", "A", "S", "D" };
            for (int i = 0; i < db.Choices.Length; i++)
            {
                if (Input.inputString.ToUpper() != choices[i]) continue;
                SelectChoice(db.Choices[i]);
                Manager.ChoicesFrame.SetActive(false);
                break;
            }
        }
    }

    public void Open(bool hadToInteract = true)
    {
        Manager.Open();
        CurrentlyRunning = true;
        CurrentBubble = 0;
        Dialogue[CurrentBubble].Display(Manager);
        InteractionBuffer = hadToInteract;
    }

    private void NextPage()
    {
        JumpTo(Dialogue[CurrentBubble].JumpTo);
    }

    private void SelectChoice(DialogueChoice dc)
    {
        JumpTo(dc.JumpTo);
        dc.OnComplete?.Invoke();
    }

    private void JumpTo(int jumpTo)
    {
        CurrentBubble = (jumpTo >= 0) ? jumpTo : (CurrentBubble + 1);
        if (CurrentBubble < Dialogue.Length)
        {
            Dialogue[CurrentBubble].Display(Manager);
            Dialogue[CurrentBubble].OnComplete?.Invoke();
        }
        else Complete();
    }

    public void Complete()
    {
        OnComplete?.Invoke();
        Manager.Close();
        CurrentlyRunning = false;
        CurrentBubble = 0;
    }
}
