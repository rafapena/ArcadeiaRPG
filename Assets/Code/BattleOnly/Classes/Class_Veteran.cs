using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Class_Veteran : BattlerClass
{
    public Projectile HighSlash;
    public Projectile WideSlash;

    protected override void UsingBasicAttack_Weaponless()
    {
        //
    }

    protected override void UsingBasicAttack_Blade()
    {
        if (PassedTime(0.2f, 0)) Attack(WideSlash);
        else if (PassedTime(0.4f, 1)) Attack(WideSlash);
        else if (PassedTime(0.7f, 2)) Attack(HighSlash);
    }

    protected override void UsingBasicAttack_Staff()
    {
        //
    }

    protected void Attack(Projectile p)
    {
        User.Figure.AddForce(Vector3.one * 3);
        SpawnProjectile(p);
    }
}
