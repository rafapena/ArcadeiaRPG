using UnityEngine;

public class EscapeCatchHitbox : MonoBehaviour
{
    public BattleMenu BattleMenu;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.GetComponent<BattlePlayer>() || collider.gameObject.GetComponent<BattleAlly>())
        {
            StartCoroutine(BattleMenu.CurrentBattle.NotifyEscapeFailure());
        }
    }
}
