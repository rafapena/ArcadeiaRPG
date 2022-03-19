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
    public Cutscene JumpToCutscene;

    private bool CurrentlyRunning;
    private bool InteractionBuffer;

    private bool EndingCutscene => CurrentBubble < 0 || CurrentBubble >= Dialogue.Length;

    // Start is called before the first frame update
    void Start()
    {
        //
    }

    // Update is called once per frame
    void Update()
    {
        if (Manager == null) return;
        else if (!Manager.ChoicesFrame.activeSelf && CurrentlyRunning && InputMaster.Interact) CutsceneInteraction();
        else if (Manager.ChoicesFrame.activeSelf) ManageChoicesFrame();
    }

    private void CutsceneInteraction()
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

    private void ManageChoicesFrame()
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

    public void Open(MapPlayer player, bool hadToInteract = true)
    {
        try
        {
            Manager.Open(player);
            CurrentlyRunning = true;
            CurrentBubble = 0;
            RefreshDialogue();
            InteractionBuffer = hadToInteract;
        }
        catch { }
    }

    public void Open(CutsceneManager manager, bool hadToInteract = true)
    {
        Manager = manager;
        Open(manager.Player, hadToInteract);
    }

    private void NextPage()
    {
        DialogueBubble db = Dialogue[CurrentBubble];
        if (db.Jump == 0) Complete();
        else Jump(db.Jump, db.Condition);
    }

    private void SelectChoice(DialogueChoice dc)
    {
        if (dc.Jump == 0) Complete();
        else Jump(dc.Jump, dc.Condition, dc.OnDecide);
    }

    public void ForceJump(int jumpTo)
    {
        Jump(jumpTo, "");
    }

    private void Jump(int jump, string condition, UnityEvent e = null)
    {
        int failedJump = Manager.CheckDialogueCondition(condition);
        CurrentBubble += (failedJump == 0) ? jump : failedJump;
        if (failedJump == 0) e?.Invoke();
        if (EndingCutscene) Complete();
        else RefreshDialogue();
    }

    private void RefreshDialogue()
    {
        Dialogue[CurrentBubble].Display(Manager);
        Dialogue[CurrentBubble].OnDisplay?.Invoke();
    }

    public void Complete()
    {
        OnComplete?.Invoke();
        CurrentlyRunning = false;
        CurrentBubble = 0;
        if (JumpToCutscene != null) JumpToCutscene.Open(Manager, false);
        else Manager.Close();
    }
}