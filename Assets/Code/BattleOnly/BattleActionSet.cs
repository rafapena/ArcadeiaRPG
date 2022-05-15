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

    public Projectile SpawnProjectile(Projectile p0, float nerfPartition = 1f)
    {
        Projectile p = Instantiate(p0, User.transform);
        p.transform.position = User.transform.position;
        p.SetBattleInfo(User, User.SelectedAction, nerfPartition);
        p.Direct(User.Direction);
        return p;
    }

    public void SummonBattler(BattlerAI b)
    {
        if (b is BattleAlly a0)
        {
            var a = User.CurrentBattle.InstantiateBattler(a0, User.TargetDestination);
            a.StatConversion();
            User.CurrentBattle.PlayerParty.Allies.Add(a);
            a.IsSummon = true;
        }
        else if (b is BattleEnemy e0)
        {
            var e = User.CurrentBattle.InstantiateBattler(e0, User.TargetDestination);
            User.CurrentBattle.EnemyParty.Enemies.Add(e);
            e.IsSummon = true;
        }
    }
}