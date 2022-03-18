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
    public string Condition;

    [Tooltip("Skip dialogue bubbles in cutscene\nIf value is 0 => Immediately end cutscene")]
    public int Jump = 1;

    public enum DialogueCharacter { None, Left1, Right1, Left2, Right2 }

    public Sprite LeftCharacter1;
    public Sprite RightCharacter1;
    public Sprite LeftCharacter2;
    public Sprite RightCharacter2;

    public string Name;
    public DialogueCharacter TalkingCharacter;

    public DialogueChoice[] Choices;
    public UnityEvent OnDisplay;

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
        EstablishCharacter(0, LeftCharacter1, cm);
        EstablishCharacter(1, RightCharacter1, cm);
        EstablishCharacter(2, LeftCharacter2, cm);
        EstablishCharacter(3, RightCharacter2, cm);
        if (TalkingCharacter == DialogueCharacter.None) cm.HighlightAll();
        else cm.HighlightCharacter((int)TalkingCharacter - 1);
    }

    private void EstablishCharacter(int index, Sprite character, CutsceneManager cm)
    {
        if (character != null) cm.ActivateCharacter(index, character);
        else cm.DeactivateCharacter(index);
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
