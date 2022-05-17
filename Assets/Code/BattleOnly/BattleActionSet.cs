using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public abstract class BattleActionSet : MonoBehaviour
{
    protected Battler User;
    private float ProjectileMultuplier;

    public void Awake()
    {
        User = transform.parent?.gameObject.GetComponent<Battler>();
        if (!User) Debug.LogError("A battler action set does not have a battler as a game object");
    }

    public void SetProjectileMultiplier(float multiplier)
    {
        ProjectileMultuplier = multiplier;
    }

    public Projectile SpawnProjectile(Projectile p0, float nerfPartition = 1f)
    {
        Projectile p = Instantiate(p0, User.transform);
        p.transform.position = User.Sprite.ObjectSpawnPoint.position;
        p.SetBattleInfo(User, User.SelectedAction, nerfPartition);
        p.Direct(User.Direction);
        return p;
    }

    public void ChargeEffect(ParticleSystem ps)
    {
        //
    }

    public void FireBasicWeaponProjectile(float nerf)
    {
        ProjectileMultuplier = nerf;
        if (User.SelectedWeapon.Ranged) FireAimedProjectile(User.SelectedWeapon.Projectile);
        else MeeleeProjectile(User.SelectedWeapon.Projectile);
    }

    public void MeeleeProjectile(Projectile p)
    {
        //
    }

    public void FireAimedProjectile(Projectile p)
    {
        //
    }

    public void SummonBattler(BattlerAI b0)
    {
        var b = User.CurrentBattle.InstantiateBattler(b0, User.ScopedTargetDestination);
        b.StatConversion();
        b.IsSummon = true;
        if (b is BattleAlly a) User.CurrentBattle.PlayerParty.Allies.Add(a);
        else if (b is BattleEnemy e) User.CurrentBattle.EnemyParty.Enemies.Add(e);
    }
}