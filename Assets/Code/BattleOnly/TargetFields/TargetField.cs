using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TargetField : MonoBehaviour
{
    [HideInInspector] public Rigidbody2D Figure;
    public bool DisposeOnDeactivate;
    protected BattlePlayer AimingPlayer;

    private Color SetupColor = Color.blue;
    private Color MeeleeColor = Color.red;
    private Color RangeColor = Color.yellow;

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

    public abstract bool HasApproachPoints();

    public virtual void Activate(BattlePlayer p, bool isSetup = false)
    {
        AimingPlayer = p;
        gameObject.SetActive(true);
        if (p.SelectedTool == null) return;
        else if (isSetup) GetComponent<SpriteRenderer>().color = SetupColor;
        else if (p.SelectedTool.Ranged) GetComponent<SpriteRenderer>().color = RangeColor;
        else GetComponent<SpriteRenderer>().color = MeeleeColor;
    }

    public void Deactivate()
    {
        if (DisposeOnDeactivate) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    public void NotifyTriggerEnter(Collider2D collision)
    {
        ScopeHitBox bhb = collision.gameObject.GetComponent<ScopeHitBox>();
        if (!bhb || AimingPlayer == null) return;

        Battler b = bhb.Battler;
        if (b.LockSelectTrigger) return;

        if (AimingPlayer.AimingForEnemies() && b is BattleEnemy ||
            AimingPlayer.AimingForTeammates() && (b is BattlePlayer || b is BattleAlly) ||
            AimingPlayer.SelectedTool.Scope == ActiveTool.ScopeType.EveryoneButSelf && b is BattlePlayer && b.Id == AimingPlayer.Id ||
            AimingPlayer.SelectedTool.Scope == ActiveTool.ScopeType.Everyone)
        {
            b.Select(true);
        }
    }

    public void NotifyTriggerExit(Collider2D collision)
    {
        ScopeHitBox hb = collision.gameObject.GetComponent<ScopeHitBox>();
        if (hb && !hb.Battler.LockSelectTrigger && AimingPlayer != null && AimingPlayer.Phase == Battler.Phases.DecidingAction) hb.Battler.Select(false);
    }
}
