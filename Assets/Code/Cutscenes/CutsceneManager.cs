using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public MenuFrame[] LeftCharacters;
    public MenuFrame[] RightCharacters;

    public GameObject DialogueFrame;
    public TextMeshProUGUI DialogueLabel;
    public GameObject NameFrame;
    public TextMeshProUGUI NameLabel;
    public GameObject ChoicesFrame;
    public GameObject ChoicesList;

    private string FullText;
    private bool IsPrintingDialogue = false;

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

    public void Open()
    {
        gameObject.SetActive(true);
        ChoicesFrame.SetActive(false);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
