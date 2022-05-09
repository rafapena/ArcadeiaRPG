using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Erca : BattlePlayer
{
    public Projectile Fireball;
    public Projectile Laser;

    protected override void UseSkill_0()
    {
        //
    }

    protected override void UseSkill_1()
    {
        //
    }

    protected override void UseSkill_2()
    {
        //
    }

    protected override void UseSkill_3()
    {
        //
    }

    protected override void AnimatingSkill_0()
    {
        float dmgPortion = 0.2f;
        if (PassedTime(0.1f, 0)) SpawnProjectile(Fireball, dmgPortion);
        if (PassedTime(0.2f, 1)) SpawnProjectile(Fireball, dmgPortion);
        if (PassedTime(0.3f, 2)) SpawnProjectile(Fireball, dmgPortion);
        if (PassedTime(0.4f, 3)) SpawnProjectile(Fireball, dmgPortion);
        if (PassedTime(0.95f, 4)) SpawnProjectile(Fireball, dmgPortion);
    }

    protected override void AnimatingSkill_1()
    {
        //
    }

    protected override void AnimatingSkill_2()
    {
        //
    }

    protected override void AnimatingSkill_3()
    {
        //
    }
}
