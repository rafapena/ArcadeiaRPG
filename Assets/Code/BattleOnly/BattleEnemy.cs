using System.Collections.Generic;
using UnityEngine;

public abstract class BattleEnemy : BattlerAI
{
    public int Exp;
    public int Gold;
    public ItemDropRate[] DroppedItems;
    public WeaponDropRate[] DroppedWeapons;
    public float MultiplyHP = 1f;
    public int ExtraTurns;

    [HideInInspector] public bool IsSummon;

    protected new void Start()
    {
        base.Start();
        Stats.MaxHP = (int)(Stats.MaxHP * MultiplyHP);
        HP = Stats.MaxHP;
    }

    protected new void Update()
    {
        base.Update();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Other --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*
    public void SetAllStats(int level, double hpMultiplier)
    {
        SetAllStats(level);
        if (Class != null) Stats = new Stats(level, Class.BaseStats, ScaledStats);
        else Stats = new Stats(level, ScaledStats);
        Stats.Multiply(0, hpMultiplier);
    }

    public void AddSkillAI(EnemyTool<Skill> ai)
    {

    }
    public void AddItemAI(EnemyTool<Item> ai)
    {

    }
    public void AddWeaponAI(EnemyTool<Item> ai)
    {

    }*/
}
