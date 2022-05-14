using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_JellyGuy : BattleEnemy
{
    public Projectile JellyFist;

    protected override void UsingUniqueBasicAttack()
    {
        if (PassedTime(0.6f, 0)) SpawnProjectile(JellyFist, 1f);
    }

    protected override void UsingSkill_0()
    {
        float dmgPortion = 0.25f;
        if (PassedTime(0.1f, 0)) SpawnProjectile(JellyFist, dmgPortion);
        if (PassedTime(0.3f, 1)) SpawnProjectile(JellyFist, dmgPortion);
        if (PassedTime(0.6f, 2)) SpawnProjectile(JellyFist, dmgPortion);
        if (PassedTime(0.8f, 3)) SpawnProjectile(JellyFist, dmgPortion);
    }
}
