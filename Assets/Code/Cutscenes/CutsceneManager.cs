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

    [HideInInspector] string TextInput;
    private char[] TextInputSplit;
    private char[] TextToPrintSplit;
    private bool IsPrintingDialogue = false;

    public void SetText(string s)
    {
        TextInput = s;
        TextInputSplit = s.ToCharArray();
        TextToPrintSplit = new char[TextInputSplit.Length];
        StartCoroutine(PrintText());
    }

    IEnumerator PrintText()
    {
        IsPrintingDialogue = true;
        for (int i = 0; i < TextInputSplit.Length; i++)
        {
            TextToPrintSplit[i] = TextInputSplit[i];
            string s = new string(TextToPrintSplit);
            DialogueLabel.text = s;
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
        TextToPrintSplit = TextInputSplit;
        DialogueLabel.text = TextInput;
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
