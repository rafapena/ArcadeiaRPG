using UnityEngine;

public abstract class BaseObject : MonoBehaviour
{
    public int Id;
    public string Name;
    [TextArea] public string Description;

    protected virtual void Awake()
    {
        if (Name.Equals("")) Name = name;
    }
}
