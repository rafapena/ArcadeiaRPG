/*using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Administrates the game's cutscenes
/// </summary>
public class DialogueText : MonoBehaviour
{
    public TextMeshProUGUI Text;
    [HideInInspector] string TextEntered;
    private char[] TextEnteredSplit;
    private char[] TextToPrintSplit;

    private bool IsPrinting = false;
    private float CharPrintDelay = 0.01f;

    private void Start()
    {
        Text = GetComponent<TextMeshProUGUI>();
        TextEnteredSplit = TextEntered.ToCharArray();
        TextToPrintSplit = new char[TextEnteredSplit.Length];
    }

    private void Update()
    {
        if (InputMaster.Interact())
        {
            if (!IsPrinting)
            {
                TextToPrintSplit = new char[TextEnteredSplit.Length];
                StartCoroutine(PrintText());
            }
        }
    }

    IEnumerator PrintText()
    {
        IsPrinting = true;
        for (int i = 0; i < TextEnteredSplit.Length; i++)
        {
            TextToPrintSplit[i] = TextEnteredSplit[i];
            string s = new string(TextToPrintSplit);
            Text.text = s;
            yield return new WaitForSeconds(CharPrintDelay);
        }
        IsPrinting = false;
    }
}*/
