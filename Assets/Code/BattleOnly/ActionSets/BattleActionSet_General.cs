using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class BattleActionSet_General : BattleActionSet
{
    private Projectile DefaultWeaponProjectile()
    {
        if (!User?.SelectedWeapon) return null;
        else if (User.SelectedWeapon.Ranged) return FireAimedProjectile(User.SelectedWeapon.Projectile, 1f, true);
        else return MeeleeProjectile(User.SelectedWeapon.Projectile, 1f, true);
    }

    private ParticleSystem DefaultWeaponParticles()
    {
        if (!User?.SelectedWeapon?.ChargingEffect) return null;
        var ps = User.SelectedWeapon.ChargingEffect;
        ps.Play();
        return ps;
    }

    private Projectile ThrowItem()
    {
        var it = User.SelectedAction as Item;
        if (!it) return null;
        var p = FireAimedProjectile(User.CurrentBattle.ItemProjectile, 1f, true);
        p.GetComponent<SpriteRenderer>().sprite = it.GetComponent<SpriteRenderer>().sprite;
        return p;
    }

    private ParticleSystem GenerateParticles(ParticleSystem ps)
    {
        ps.Play();
        return ps;
    }

    private BattlerAI SummonBattler(BattlerAI b0)
    {
        var b = Instantiate(b0);// User.CurrentBattle.InstantiateBattler(b0, User.ScopedTargetDestination);
        b.StatConversion();
        b.IsSummon = true;
        if (b is BattleAlly a) User.CurrentBattle.PlayerParty.Allies.Add(a);
        else if (b is BattleEnemy e) User.CurrentBattle.EnemyParty.Enemies.Add(e);
        return b;
    }
}