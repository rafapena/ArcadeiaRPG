using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Class_Rookie : BattlerClass
{
    public Projectile HighSlash;
    public Projectile WideSlash;

    protected override void UseBasicAttack_Weaponless()
    {
        //
    }

    protected override void UseBasicAttack_Blade()
    {
        //
    }

    protected override void UseBasicAttack_Staff()
    {
        //
    }

    protected override void AnimateBasicAttack_Weaponless()
    {
        //
    }

    protected override void AnimateBasicAttack_Blade()
    {
        if (PassedTime(0.4f, 0)) Attack(HighSlash, 0.4f);
        else if (PassedTime(0.7f, 1)) Attack(WideSlash, 0.6f);
    }

    protected override void AnimateBasicAttack_Staff()
    {
        //
    }

    protected void Attack(Projectile p, float dmgPortion)
    {
        User.Figure.AddForce(Vector3.one * 3);
        SpawnProjectile(p, dmgPortion);
    }
}
