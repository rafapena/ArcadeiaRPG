using UnityEngine;

public class EscapeCatchHitbox : MonoBehaviour
{
    public Battle CurrentBattle;
    [HideInInspector] public Battler Followee;

    private void Update()
    {
        if (Followee) transform.position = Followee.Position;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag(Battle.SCOPE_HITBOX_TAG) && collider.gameObject.GetComponent<BattlerHitbox>()?.Battler is BattleEnemy)
        {
            CurrentBattle.NotifyEscapeFailure();
        }
    }
}
