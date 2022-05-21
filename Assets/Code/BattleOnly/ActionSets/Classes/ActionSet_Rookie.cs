using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class ActionSet_Rookie : BattleActionSet
{
    public Projectile HighBlade;
    public Projectile WideBlade;

    public void WeakBasicSlash()
    {
        MeeleeProjectile(WideBlade, 0.4f, false);
    }

    public void StrongBasicSlash()
    {
        MeeleeProjectile(HighBlade, 0.6f, true);
    }
}