using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_JellyGuy : BattleEnemy
{
    public Projectile JellyFist;

    protected override void AnimatingSkill_0()
    {
        if (PassedTime(0.1f, 0)) SpawnProjectile(JellyFist);
        if (PassedTime(0.3f, 1)) SpawnProjectile(JellyFist);
        if (PassedTime(0.6f, 2)) SpawnProjectile(JellyFist);
        if (PassedTime(0.8f, 3)) SpawnProjectile(JellyFist);
    }

    protected override void AnimatingSkill_1()
    {
        if (PassedTime(0.7f, 0)) SpawnProjectile(JellyFist);
        if (PassedTime(0.8f, 1)) SpawnProjectile(JellyFist);
        if (PassedTime(0.9f, 2)) SpawnProjectile(JellyFist);
    }
}
