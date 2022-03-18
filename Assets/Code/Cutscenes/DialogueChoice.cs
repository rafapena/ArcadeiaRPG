using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Administrates the game's cutscenes
/// </summary>
public class DialogueChoice : MonoBehaviour
{
    public string Text;
    public int Jump;
    public string Condition;
    public UnityEvent OnDecide;
}
