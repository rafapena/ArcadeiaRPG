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

    protected new void Start()
    {
        base.Start();
        MirrorEnemy();
        Stats.MaxHP = (int)(MaxHP * MultiplyHP);
        HP = MaxHP;
    }

    private void MirrorEnemy()
    {
        Direction = Vector3.left;
        var vec = transform.localScale;
        vec.x *= -1;
        transform.localScale = vec;
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
