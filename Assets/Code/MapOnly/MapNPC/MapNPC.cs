using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNPC : MapExplorer
{
    [HideInInspector] public bool CloseToPlayer;
    public bool Interacting;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void AnimateDirection()
    {
        //
    }

    public void InteractWith(MapPlayer opener)
    {
        //
    }
}
