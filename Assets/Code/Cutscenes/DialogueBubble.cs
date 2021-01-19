using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Administrates the game's cutscenes
/// </summary>
public class DialogueBubble : MonoBehaviour
{
    public enum DialogueCharacter { None, Left1, Left2, Right1, Right2 }

    [TextArea] public string Text;
    public int JumpTo = 0;
    public Image LeftCharacter1;
    public Image LeftCharacter2;
    public Image RightCharacter1;
    public Image RightCharacter2;
    public string Name;
    public DialogueCharacter TalkingCharacter;

    public DialogueChoice[] Choices;

    public void Display(CutsceneManager cm)
    {
        cm.SetText(Text);
        cm.NameLabel.text = Name;
        EstablishCharacters(cm);
    }

    private void EstablishCharacters(CutsceneManager cm)
    {

    }

    public void SetupChoices(CutsceneManager cm)
    {
        bool choices = HasChoices();
        cm.ChoicesFrame.gameObject.SetActive(choices);
        if (!choices) return;
        int i = 1;
        foreach (Transform t in cm.ChoicesList.transform)
        {
            bool overflow = i >= Choices.Length;
            t.gameObject.SetActive(!overflow);
            if (!overflow) t.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = Choices[i].Text;
            i++;
        }
    }

    public bool HasChoices()
    {
        return Choices.Length >= 2;
    }
}
