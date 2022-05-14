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

    protected override void Start()
    {
        base.Start();
        Mirror();
    }

    public override void StatConversion()
    {
        base.StatConversion();
        Stats.MaxHP = (int)(MaxHP * MultiplyHP);
        HP = MaxHP;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ReceiveToolEffects(Battler user, ActiveTool activeTool, float nerfPartition)
    {
        base.ReceiveToolEffects(user, activeTool, nerfPartition);
        if (CurrentBattle?.BattleMenu ?? false) CurrentBattle.BattleMenu.UpdateEnemyEntry(this);
    }
}
