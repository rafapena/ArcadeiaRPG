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
    public TextMeshProUGUI GoldAmount;
    private int DisplayedGold;
    private int GoldChangeSpeed;

    private string FullText;
    private bool IsPrintingDialogue = false;

    private void Update()
    {
        UpdateGoldDisplay();
    }

    public void Open(MapPlayer player)
    {
        SceneMaster.OpenCutscene();
        Player = player;
        DisplayedGold = player.Party.Inventory.Gold;
        gameObject.SetActive(true);
        ChoicesFrame.SetActive(false);
    }

    public void Close()
    {
        SceneMaster.CloseCutscene(Player);
        gameObject.SetActive(false);
        ChoicesFrame.SetActive(false);
    }

    public void SetText(string s)
    {
        FullText = s;
        StartCoroutine(PrintText());
    }

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
        Player.Party.Inventory.Gold += amount;
        if (Player.Party.Inventory.Gold <= 0) Player.Party.Inventory.Gold = 0;
        GoldChangeSpeed = amount / 60 + (amount > 0 ? 1 : -1);
    }

    private void UpdateGoldDisplay()
    {
        GoldAmount.text = DisplayedGold.ToString();
        DisplayedGold += GoldChangeSpeed;
        if (GoldChangeSpeed < 0 && DisplayedGold < Player.Party.Inventory.Gold || GoldChangeSpeed > 0 && DisplayedGold > Player.Party.Inventory.Gold)
        {
            DisplayedGold = Player.Party.Inventory.Gold;
            GoldChangeSpeed = 0;
        }
    }
}
