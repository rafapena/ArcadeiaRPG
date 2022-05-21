using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public abstract class BattleActionSet : MonoBehaviour
{
    protected Battler User;

    public void Awake()
    {
        User = transform.parent?.gameObject.GetComponent<Battler>();
        if (!User) Debug.LogError("A battler action set does not have a battler as a game object");
    }

    protected Projectile SpawnProjectile(Projectile p0, float nerfPartition, bool finisher)
    {
        Projectile p = Instantiate(p0, User.CurrentBattle.ActiveProjectiles);
        p.transform.position = User.Sprite.ObjectSpawnPoint.position;
        p.SetBattleInfo(User, User.SelectedAction, nerfPartition, finisher);
        return p;
    }

    protected Projectile MeeleeProjectile(Projectile p0, float nerfPartition, bool finisher)
    {
        var p = SpawnProjectile(p0, nerfPartition, finisher);
        p.Direct(User.Direction);
        return p;
    }

    protected Projectile FireAimedProjectile(Projectile p0, float nerfPartition, bool finisher)
    {
        var p = SpawnProjectile(p0, nerfPartition, finisher);
        p.Direct(User.SingleSelectedTarget.Sprite.ActionHitbox.transform.position - User.Sprite.ObjectSpawnPoint.transform.position);
        return p;
    }
}