using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class ActionSet_Rookie : BattleActionSet
{
    public Projectile HighBlade;
    public Projectile WideBlade;

    public void WeakBasicSlash()
    {
        SpawnProjectile(WideBlade, 0.4f);
    }

    public void StrongBasicSlash()
    {
        SpawnProjectile(HighBlade, 0.6f);
    }
}