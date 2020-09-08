using System.Collections.Generic;
using UnityEngine;

public abstract class MapExplorer : MonoBehaviour
{
    protected Vector3 Movement;
    public float Speed;

    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

    protected abstract void AnimateDirection();
}
