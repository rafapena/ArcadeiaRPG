﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetField : MonoBehaviour
{
    [HideInInspector] public Rigidbody2D Figure;
    public bool DisposeOnDeactivate;
    protected BattlePlayer Player;
    
    public bool IsPlacement { get; private set; }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        Figure = gameObject.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //
    }

    public virtual void Activate(BattlePlayer p)
    {
        gameObject.SetActive(true);
        Player = p;
    }

    public void Deactivate()
    {
        IsPlacement = false;
        if (DisposeOnDeactivate) Destroy(this);
        else gameObject.SetActive(false);
    }

    public void SetAsPlacement()
    {
        IsPlacement = true;
    }
}
