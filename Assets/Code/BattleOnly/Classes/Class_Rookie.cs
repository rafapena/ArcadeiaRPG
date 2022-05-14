using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Class_Rookie : BattlerClass
{
    public Projectile HighSlash;
    public Projectile WideSlash;

    protected override void UsingBasicAttack_Blade()
    {
        if (PassedTime(0.01f, 0)) Attack(HighSlash, 0.4f);
        else if (PassedTime(0.5f, 1)) Attack(HighSlash, 0.4f);
        else if (PassedTime(0.99f, 2)) Attack(WideSlash, 0.6f);
    }

    protected void Attack(Projectile p, float dmgPortion)
    {
        User.Figure.AddForce(Vector3.one * 3);
        SpawnProjectile(p, dmgPortion);
    }
}
