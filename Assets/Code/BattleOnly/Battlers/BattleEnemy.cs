using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleEnemy : BattlerAI
{
    public enum EnemyTypes { Regular, MiniBoss, Boss, FinalBoss }

    public EnemyTypes EnemyType;
    public int Exp;
    public int Gold;
    public ItemOrWeaponDropRate[] DroppedTools;
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
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- ActiveTool scoping --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void Select(bool selected)
    {
        base.Select(selected);
        CurrentBattle?.Menu?.SetHighlightSelectedEnemyEntries(this, selected);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ReceiveToolEffects(Battler user, ActiveTool activeTool, Projectile hitProjectile)
    {
        base.ReceiveToolEffects(user, activeTool, hitProjectile);
        if (CurrentBattle?.Menu ?? false) CurrentBattle.Menu.UpdateEnemyEntry(this);
    }

    protected override void Revive()
    {
        base.Revive();
    }

    protected override void GetKOd()
    {
        base.GetKOd();
        var ps = CurrentBattle.EnemyKOParticles[(int)EnemyType];
        float sec = EnemyType == EnemyTypes.Regular ? 0 : 2.5f;
        StartCoroutine(ApplyKOEffect(ps, sec, false));
    }
}
