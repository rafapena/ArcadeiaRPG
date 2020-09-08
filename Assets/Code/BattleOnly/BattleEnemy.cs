using System.Collections.Generic;
using UnityEngine;

public class BattleEnemy : BattlerAI
{
    public int Exp;
    public int Gold;
    public List<ItemDropRate> DroppedItems;
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

    }

    public void DecideMove(List<Player> players, List<Enemy> enemies)
    {
        if (!IsConscious) return;
        SelectedTargets.Clear();
        SelectedSkill = Skills.Count > 0 ? Skills[RandInt(0, Skills.Count - 1)] : null;
        SelectedItem = null;
        SelectedWeapon = Weapons.Count > 0 && SelectedSkill != null && SelectedSkill.IsOffense() ? Weapons[RandInt(0, Weapons.Count - 1)] : null;
        SelectedTargets.Add(players[RandInt(0, players.Count - 1)]);
    }*/
}
