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

    protected override void Awake()
    {
        base.Awake();
        Stats.MaxHP = (int)(MaxHP * MultiplyHP);
        HP = MaxHP;
    }

    protected override void Start()
    {
        base.Start();
        MirrorEnemy();
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

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ReceiveToolEffects(Battler user, ActiveTool activeTool, float nerfPartition = 1f)
    {
        base.ReceiveToolEffects(user, activeTool);
        if (CurrentBattle?.BattleMenu ?? false) CurrentBattle.BattleMenu.UpdateEnemyEntry(this);
    }
}
