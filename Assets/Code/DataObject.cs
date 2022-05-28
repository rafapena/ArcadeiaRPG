using System;
using UnityEngine;

public abstract class DataObject : MonoBehaviour
{
    public int Id;
    public string Name;
    [TextArea] public string Description;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(Name)) Name = name;

        var sprite = gameObject.GetComponent<SpriteRenderer>();
        if (sprite) sprite.enabled = false;
    }
}
