using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    public MapPlayer Player;

    public DialoguePortrait[] Characters;

    public GameObject DialogueFrame;
    public TextMeshProUGUI DialogueLabel;
    public GameObject NameFrameL;
    public TextMeshProUGUI NameLabelL;
    public GameObject NameFrameR;
    public TextMeshProUGUI NameLabelR;
    public GameObject ChoicesFrame;
    public GameObject ChoicesList;

    public MenuFrame GoldFrame;
    public NumberUpdater GoldAmount;
    public MenuFrame ObjectiveFrame;
    public MenuFrame ItemsFrame;
    private float ObjectiveFrameTimer;
    private float ItemsFrameTimer;
    private float OBJECTIVE_FRAME_TIME = 5f;
    private float ITEMS_FRAME_TIME = 4f;

    private string FullText;
    private bool IsPrintingDialogue = false;

    private void Update()
    {
        if (ObjectiveFrame.Activated && Time.unscaledTime > ObjectiveFrameTimer) ObjectiveFrame.Deactivate();
        else if (ItemsFrame.Activated && Time.unscaledTime > ItemsFrameTimer) ItemsFrame.Deactivate();
    }

    public void Open(MapPlayer player)
    {
        SceneMaster.OpenCutscene();
        Player = player;
        GoldAmount.Initialize(player.Party.Inventory.Gold);
        gameObject.SetActive(true);
        ChoicesFrame.SetActive(false);
    }

    public void Close()
    {
        SceneMaster.CloseCutscene(Player);
        HideGold();
        ObjectiveFrame.Deactivate();
        ItemsFrame.Deactivate();
        gameObject.SetActive(false);
        ChoicesFrame.SetActive(false);
    }

    public void SetText(string s)
    {
        FullText = s;
        StartCoroutine(PrintText());
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Printing --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    IEnumerator PrintText()
    {
        IsPrintingDialogue = true;
        for (int i = 0; i <= FullText.Length; i++)
        {
            DialogueLabel.text = IsPrintingDialogue ? FullText.Substring(0, i) : FullText;
            if (!IsPrintingDialogue) break;
            yield return new WaitForSeconds(GameplayMaster.TextSpeed);
        }
        IsPrintingDialogue = false;
    }

    public bool CurrentlyPrinting()
    {
        return IsPrintingDialogue;
    }

    public void ForceStop()
    {
        IsPrintingDialogue = false;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Character sprites --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ActivateCharacter(int index, Sprite sp)
    {
        Characters[index].Frame.ActivateIgnoreBuffer();
        Characters[index].Image.sprite = sp;
    }

    public void DeactivateCharacter(int index)
    {
        Characters[index].Frame.Deactivate();
    }


    public void HighlightCharacter(int index)
    {
        UnhighlightAll();
        Image selectedCharacter = Characters[index].Image;
        selectedCharacter.color = DialoguePortrait.HighlightedColor;
        bool leftDir = Characters[index].Frame.ComeFrom == MenuFrame.DirEnum.Left;
        NameFrameL.SetActive(leftDir);
        NameFrameR.SetActive(!leftDir);
    }

    private void UnhighlightAll()
    {
        foreach (var c in Characters) c.Image.color = DialoguePortrait.UnhighlightedColor;
    }

    public void HighlightAll()
    {
        foreach (var c in Characters) c.Image.color = DialoguePortrait.HighlightedColor;
        NameFrameL.SetActive(false);
        NameFrameR.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Conditions --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Example - Jump 2 if Gold is over 100: 2_G_100_?
    public int CheckDialogueCondition(string condition)
    {
        if (condition.Length == 0) return 0;

        string[] components = condition.Split('_');
        int jumpToIfFailed = int.Parse(components[0]);
        string cond = components[1];
        int val = int.Parse(components[2]);
        bool mustBeFalse = components[3].Equals("!");

        bool metCondition = mustBeFalse ? !CheckCondition(cond, val) : CheckCondition(cond, val);
        return metCondition ? 0 : jumpToIfFailed;
    }

    private bool CheckCondition(string cond, int val)
    {
        switch (cond)
        {
            case "G":
                return Player.Party.Inventory.Gold >= val;
            case "I":
                return Player.Party.Inventory.Items.Find(x => x.Id == val) != null;
            case "W":
                return Player.Party.Inventory.Weapons.Find(x => x.Id == val) != null;
            case "A":
                return Player.Party.Inventory.Weapons.Find(x => x.Id == val) != null;
            case "C":
                return Player.Party.Inventory.CarryWeight >= val;
            case "M":
                return Player.Party.Inventory.WeightCapacity >= val;
            case "P":
                return Player.Party.AllPlayers.Find(x => x.Id == val) != null;
            case "T":
                return Player.Party.Allies.Find(x => x.Id == val) != null;
            // Objectives
            // Current Time
            // Game Variables
            default:
                return true;
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- UI Frame Management --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void DisplayGold()
    {
        GoldFrame.Activate();
    }

    public void HideGold()
    {
        GoldFrame.Deactivate();
    }

    public void ChangeGold(int amount)
    {
        if (!SceneMaster.InCutscene) return;
        GoldAmount.Add(ref Player.Party.Inventory.Gold, amount);
    }

    public void ClearSubObjective(SubObjective obj)
    {
        //Player.Party.LoggedObjectives.Find;
        ObjectiveFrame.Activate();
        ObjectiveFrameTimer = Time.unscaledTime + OBJECTIVE_FRAME_TIME;
        //ObjectiveFrame.transform.GetChild(1).GetComponent<Tmage>(). = ;
        ObjectiveFrame.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = obj.Name;
    }

    public void AddTool(IToolForInventory tool, int quantity)
    {
        Player.Party.Inventory.Add(tool, quantity);
        ItemsFrame.Activate();
        ItemsFrameTimer = Time.unscaledTime + ITEMS_FRAME_TIME;
        ItemsFrame.transform.GetChild(0).GetComponent<Image>().sprite = tool.Info.GetComponent<SpriteRenderer>()?.sprite ?? null;
        ItemsFrame.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = quantity.ToString();
        ItemsFrame.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = tool.Info.Name;
    }
}
