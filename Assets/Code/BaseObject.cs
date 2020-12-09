using UnityEngine;

public abstract class BaseObject : MonoBehaviour
{
    public int Id;
    public string Name;
    [TextArea] public string Description;
}
