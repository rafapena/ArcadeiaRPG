using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetField : MonoBehaviour
{
    [HideInInspector] public Rigidbody2D Figure;
    public bool DisposeOnDeactivate;
    protected BattlePlayer AimingPlayer;

    private Color SetupColor = Color.blue;
    private Color AidColor = Color.green;
    private Color RangeColor = Color.yellow;
    private Color MeeleeColor = Color.red;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        Figure = gameObject.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        //
    }

    public virtual void Activate(BattlePlayer p, bool isSetup = false)
    {
        AimingPlayer = p;
        gameObject.SetActive(true);
        if (p.SelectedAction == null) return;
        else if (isSetup) GetComponent<SpriteRenderer>().color = SetupColor;
        else if (p.AimingForTeammates()) GetComponent<SpriteRenderer>().color = AidColor;
        else if (p.SelectedAction.Ranged) GetComponent<SpriteRenderer>().color = RangeColor;
        else GetComponent<SpriteRenderer>().color = MeeleeColor;
    }

    public void Deactivate()
    {
        if (DisposeOnDeactivate) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    public void NotifyTriggerEnter(Collider2D collision)
    {
        if (collision.gameObject.CompareTag(Battle.SCOPE_HITBOX_TAG) && AimingPlayer)
        {
            Battler b = collision.gameObject.GetComponent<BattlerHitbox>().Battler;
            if (AimingPlayer.AimingForEnemies() && b is BattleEnemy ||
            AimingPlayer.AimingForTeammates() && (b is BattlePlayer || b is BattleAlly) ||
            AimingPlayer.SelectedAction.Scope == ActiveTool.ScopeType.EveryoneButSelf && b is BattlePlayer && b.Id == AimingPlayer.Id ||
            AimingPlayer.SelectedAction.Scope == ActiveTool.ScopeType.Everyone)
            {
                SelectBattler(b);
            }
        }
    }

    public void NotifyTriggerExit(Collider2D collision)
    {
        Battler b = collision.gameObject.CompareTag(Battle.SCOPE_HITBOX_TAG) ? collision.gameObject.GetComponent<BattlerHitbox>()?.Battler : null;
        if (b && !b.LockSelectTrigger && AimingPlayer != null && AimingPlayer.IsDecidingAction) b.Select(false);
    }

    protected virtual void SelectBattler(Battler b) => b.Select(true);
}
