using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Administrates the game's cutscenes
/// </summary>
public class DialogueBubble : MonoBehaviour
{
    [TextArea] public string Text;

    [Tooltip("Jump to dialogue bubble in cutscene\nIf value is -1 => Next bubble in the sequence\nIf value is > # of dialogue bubbles => End cutscene")]
    public int JumpTo = -1;

    public enum DialogueCharacter { None, Left1, Right1, Left2, Right2 }

    public Sprite LeftCharacter1;
    public Sprite RightCharacter1;
    public Sprite LeftCharacter2;
    public Sprite RightCharacter2;

    public string Name;
    public DialogueCharacter TalkingCharacter;

    public UnityEvent OnComplete;
    public DialogueChoice[] Choices;

    public bool HasChoices => Choices.Length >= 2;

    public void Display(CutsceneManager cm)
    {
        cm.SetText(Text);
        cm.NameLabelL.text = Name;
        cm.NameLabelR.text = Name;
        EstablishCharacters(cm);
    }

    private void EstablishCharacters(CutsceneManager cm)
    {
        if (LeftCharacter1 != null) cm.ActivateCharacter(0, LeftCharacter1);
        else cm.DeactivateCharacter(0);

        if (RightCharacter1 != null) cm.ActivateCharacter(1, RightCharacter1);
        else cm.DeactivateCharacter(1);

        if (LeftCharacter2 != null) cm.ActivateCharacter(2, LeftCharacter2);
        else cm.DeactivateCharacter(2);

        if (RightCharacter2 != null) cm.ActivateCharacter(3, RightCharacter2);
        else cm.DeactivateCharacter(3);

        if (TalkingCharacter == DialogueCharacter.None) cm.HighlightAll();
        else cm.HighlightCharacter((int)TalkingCharacter - 1);
    }

    public void SetupChoices(CutsceneManager cm)
    {
        cm.ChoicesFrame.gameObject.SetActive(HasChoices);
        if (!HasChoices) return;
        int i = 0;
        foreach (Transform t in cm.ChoicesList.transform)
        {
            bool overflow = i >= Choices.Length;
            t.gameObject.SetActive(!overflow);
            if (!overflow) t.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text = Choices[i].Text;
            i++;
        }
    }
}
