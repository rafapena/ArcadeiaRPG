using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct DialogueChoice
{
    public string Text;
    public int Jump;
    public string Condition;
    public UnityEvent OnDecide;
}
