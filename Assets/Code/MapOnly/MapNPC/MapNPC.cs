﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapNPC : MapExplorer
{
    public Cutscene Cutscene;
    [HideInInspector] public MapPlayer Player;
    [HideInInspector] public bool CloseToPlayer;
    [HideInInspector] public bool Interacting;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        Cutscene = GetComponent<Cutscene>();
        Cutscene.enabled = false;
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
        if (Interacting)
        {
            if (!SceneMaster.InCutscene) Interacting = false;
            return;
        }
        // Moving
    }

    protected override void AnimateDirection()
    {
        //
    }

    public void InteractWith(MapPlayer interactor)
    {
        if (Interacting) return;
        Interacting = true;
        Player = interactor;
        Cutscene.Open(interactor);
        Cutscene.gameObject.SetActive(true);
    }
}
