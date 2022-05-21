using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class ActionSet_JellyGuy : BattleActionSet
{
    public Projectile Bash;

    public void Tackle()
    {
        MeeleeProjectile(Bash, 1f, true);
    }
}