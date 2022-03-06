using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    public DialoguePortrait[] Characters;

    public GameObject DialogueFrame;
    public TextMeshProUGUI DialogueLabel;
    public GameObject NameFrameL;
    public TextMeshProUGUI NameLabelL;
    public GameObject NameFrameR;
    public TextMeshProUGUI NameLabelR;
    public GameObject ChoicesFrame;
    public GameObject ChoicesList;

    private Vector2 NameFrameDefaultPosition;

    private string FullText;
    private bool IsPrintingDialogue = false;

    public void Open()
    {
        SceneMaster.OpenCutscene();
        gameObject.SetActive(true);
        ChoicesFrame.SetActive(false);
    }

    public void Close()
    {
        SceneMaster.CloseCutscene();
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
}
