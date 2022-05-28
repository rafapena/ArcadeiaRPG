using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class Battle : MonoBehaviour
{
    // Collision detection
    public const string ACTION_HITBOX_TAG = "ActionHitbox";
    public const string SCOPE_HITBOX_TAG = "ScopeHitbox";
    public const int BASE_HITBOX_LAYER = 11;
    public const int ACTION_HITBOX_LAYER = 12;
    public const int MOVING_SCOPE_HITBOX_LAYER = 13;
    public GameObject[] Boundaries;

    // UI
    public BattleCamera BattleCamera;
    public BattleMenu BattleMenu;
    public BattleWin BattleWinMenu;

    // Positioning
    public Transform PlayerPartyField;
    public Transform EnemyPartyField;
    private const float X_POSITION_DISTANCE = 2.5f;
    private const float Y_POSITION_DISTANCE = 0.8f;

    // Constants
    public Skill BasicAttack;
    public Projectile BasicWeaponlessProjectile;
    public ParticleSystem PlayerKOParticles;
    public ParticleSystem[] EnemyKOParticles;

    // Data transferred from Map
    [HideInInspector] public Surrounding Enviornment;
    [HideInInspector] public PlayerParty PlayerParty;
    [HideInInspector] public EnemyParty EnemyParty;

    // Battle state tracking
    public int Turn { get; private set; }
    private bool LastActionOfTurn;
    
    // Grouping battle surroundings
    public Transform ActiveProjectiles;
    public Transform ActiveHazards;
    public Transform ActiveFields;
    public Transform ActivePopups;

    // Grouping battlers
    private List<Battler> Battlers = new List<Battler>();
    private List<Battler> BattlersByColumn = new List<Battler>();
    public Battler ActingBattler { get; private set; }
    public Battler NextActingBattler { get; private set; }
    public IEnumerable<Battler> AllBattlers => Battlers;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Awake()
    {
        Physics2D.IgnoreLayerCollision(BASE_HITBOX_LAYER, BASE_HITBOX_LAYER);
        Physics2D.IgnoreLayerCollision(MOVING_SCOPE_HITBOX_LAYER, MOVING_SCOPE_HITBOX_LAYER);
        Physics2D.IgnoreLayerCollision(BASE_HITBOX_LAYER, MOVING_SCOPE_HITBOX_LAYER);
    }

    private void Start()
    {
        // Scene update
        SceneMaster.ApplyBattleStartChanges();

        // Setup player party
        PlayerParty = GameplayMaster.Party;
        var teammates = PlayerParty.BattlingParty;
        ActivateBattlers(ref teammates);
        SetupStartingPositions(ref teammates, PlayerPartyField, 1);

        // Setup enemy party
        EnemyParty = Instantiate(GameplayMaster.EnemyGroup);
        ActivateBattlers(ref EnemyParty.Enemies);
        SetupStartingPositions(ref EnemyParty.Enemies, EnemyPartyField, -1);
        EnemyParty.Setup();

        // Group into battlers list
        foreach (Battler b in teammates) Battlers.Add(b);
        foreach (BattleEnemy e in EnemyParty.Enemies) Battlers.Add(e);
        SortBattlersInOrderLayer();

        // Start battle
        StartCoroutine(ProcessFirstTurn());
    }

    private void ActivateBattlers<T>(ref List<T> party) where T : Battler
    {
        foreach (var b in party)
        {
            b.transform.gameObject.SetActive(true);
            b.SetBattle(this);
        }
    }

    private void SetupStartingPositions<T>(ref List<T> party, Transform battlerPartyField, int mult) where T : Battler
    {
        if (party.Count >= 9) party = party.Take(9).ToList();
        var dists = new List<Battler>[] { new List<Battler>(), new List<Battler>(), new List<Battler>() };
        foreach (var b in party)
        {
            // x-position
            int range = (int)(b.Class ? b.Class.CombatRangeType : b.CombatRangeType) - 1;
            while (dists[range].Count >= 3) range = (range == 1) ? (dists[0].Count >= 3 ? 2 : 0) : 1;
            float xPos = 1 * (range - 1) * X_POSITION_DISTANCE * mult + (mult >= 0 ? 0 : (b.transform.localScale.x / 2f));
            Vector3 bPos = Vector3.left * xPos;

            // y-position
            var col = dists[range];
            if (col.Count > 0) col[0].transform.position += Vector3.up * Y_POSITION_DISTANCE;
            if (col.Count == 1) bPos += Vector3.down * Y_POSITION_DISTANCE;
            else if (col.Count == 2) col[1].transform.position += Vector3.down * Y_POSITION_DISTANCE;

            // Set position
            col.Add(b);
            b.SetPosition(battlerPartyField.position + bPos);
        }
    }

    private void SortBattlersInOrderLayer()
    {
        BattlersByColumn.AddRange(Battlers);
        BattlersByColumn = BattlersByColumn.OrderByDescending(x => x.Position.y).ToList();
        int i = 0;
        foreach (var b in BattlersByColumn) b.SetColumnOverlapRank(i++);
    }

    public void RestrictBattlerWallCollision(bool restrict)
    {
        foreach (var b in Boundaries) b.gameObject.SetActive(restrict);
    }

    private IEnumerator ProcessFirstTurn()
    {
        yield return new WaitForSeconds(1);
        TurnReset();
        StartCoroutine(ActionStart());
    }    

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Update()
    {
        UpdateActingBattlerLayerOrder();
    }

    private void UpdateActingBattlerLayerOrder()
    {
        if (!ActingBattler) return;
        int i = ActingBattler.ColumnOverlapRank;
        var bbc = BattlersByColumn;
        if (i > 0 && bbc[i].Position.y > bbc[i - 1].Position.y) UpdateActingBattlerLayerOrder(i--, -1);
        if (i < bbc.Count - 1 && bbc[i].Position.y < bbc[i + 1].Position.y)  UpdateActingBattlerLayerOrder(i++, 1);
    }

    private void UpdateActingBattlerLayerOrder(int i, int inc)
    {
        int i1 = i + inc;
        BattlersByColumn[i].SetColumnOverlapRank(i1);
        BattlersByColumn[i1].SetColumnOverlapRank(i);
        var temp = BattlersByColumn[i];
        BattlersByColumn[i] = BattlersByColumn[i1];
        BattlersByColumn[i1] = temp;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Turn Reset --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void TurnReset()
    {
        Turn++;
        LastActionOfTurn = false;
        ResetBattlerActions();
        SortBattlersBySpeed(-5, 6);
        UpdateBattlersSpriteSpeed();
    }

    private void ResetBattlerActions()
    {
        foreach (Battler b in AllBattlers)
        {
            b.ResetAction();
            b.Select(false);
        }
    }

    private void SortBattlersBySpeed(int speedRandomLow = 0, int speedRandomHigh = 0)
    {
        for (int i = 0; i < Battlers.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                if (Battlers[j - 1].Spd + Random.Range(speedRandomLow, speedRandomHigh) > Battlers[j].Spd + Random.Range(speedRandomLow, speedRandomHigh)) continue;
                Battler temp = Battlers[j - 1];
                Battlers[j - 1] = Battlers[j];
                Battlers[j] = temp;
            }
        }
    }

    private void UpdateBattlersSpriteSpeed()
    {
        int maxSpeed = Battlers[0].Spd;
        int minSpeed = Battlers[0].Spd;
        foreach (var b in Battlers.Skip(1))
        {
            if (b.Spd > maxSpeed) maxSpeed = b.Spd;
            else if (b.Spd < minSpeed) minSpeed = b.Spd;
        }
        foreach (var b in Battlers) b.SpriteSpeed = ((b.Spd - minSpeed) / (float)(maxSpeed - minSpeed)) * 4 + 3;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action Start --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private IEnumerator ActionStart()
    {
        if (!DeclareActingBattlers())
        {
            TurnReset();
            StartCoroutine(ActionStart());
        }
        else if (!ActingBattler.CanDoAction)
        {
            BattleMenu.RefreshPartyFrames();
            yield return new WaitForSeconds(2.5f);
            ActionEnd();
        }
        else if (ActingBattler is BattlePlayer p)
        {
            yield return new WaitForSeconds(1);
            BattleMenu.RefreshPartyFrames();
            BattleMenu.Setup(p);
            yield return new WaitUntil(BattleMenu.SelectedAction);
            yield return BattleMenu.Escaping ? AttemptEscape() : ExecuteAction();
        }
        else  // Battler AI
        {
            BattleMenu.RefreshPartyFrames();
            yield return new WaitForSeconds(1);
            if (ActingBattler is BattleAlly ally) ally.MakeDecision(PlayerParty.BattlingParty, EnemyParty.Enemies);
            else if (ActingBattler is BattleEnemy enemy) enemy.MakeDecision(EnemyParty.Enemies, PlayerParty.BattlingParty);
            yield return ExecuteAction();
        }
    }

    private bool DeclareActingBattlers()
    {
        ClearAllTurnIndicators();

        ActingBattler = Battlers.FirstOrDefault(x => !x.ExecutedAction && !x.KOd);
        if (!ActingBattler) return false;

        NextActingBattler = Battlers.FirstOrDefault(x => x != ActingBattler && !x.ExecutedAction && !x.KOd);
        if (!NextActingBattler)
        {
            LastActionOfTurn = true;
            NextActingBattler = Battlers.FirstOrDefault(x => x != ActingBattler && !x.KOd);
        }
        
        ActingBattler.SpriteInfo.HandleTurnIndicators(true, false);
        NextActingBattler.SpriteInfo.HandleTurnIndicators(false, true);
        return true;
    }

    private void ClearAllTurnIndicators()
    {
        foreach (var b in Battlers) b.SpriteInfo.HandleTurnIndicators(false, false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action Execution --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private IEnumerator ExecuteAction()
    {
        // Skip null actions
        if (!ActingBattler.SelectedAction)
        {
            yield return new WaitForSeconds(1);
            ActionEnd();
            yield break;
        }

        // Approach target for meelee attacks
        if (ActingBattler.SingleSelectedTarget && !ActingBattler.SelectedAction.Ranged)
        {
            RestrictBattlerWallCollision(false);
            var sp = ActingBattler.SingleSelectedTarget.SpriteInfo;
            ActingBattler.ApproachTarget(sp.ApproachPointLeft.position, sp.ApproachPointRight.position);
            yield return new WaitUntil(ActingBattler.HasApproachedTarget);
        }

        // Display action usage popup
        Skill skill = ActingBattler.SelectedAction as Skill;
        if (!skill?.Basic ?? true) StartCoroutine(BattleMenu.DisplayUsedAction(ActingBattler, ActingBattler.SelectedAction?.Name ?? string.Empty));

        // Use action
        ActingBattler.SpriteInfo.Animation.SetTrigger(Battler.AnimParams.DoAction.ToString());
        if (ActingBattler.UsingBasicAttack) ActingBattler.UseBasicAttack(ActingBattler.SelectedWeapon);
        else if (skill) ActingBattler.UseSkill(skill);
        else if (ActingBattler.SelectedAction is Item item) ActingBattler.UseItem(item);
        yield return new WaitUntil(ActingBattler.ActionAnimationCompleted);

        // Complete action
        ActingBattler.SpriteInfo.Animation.SetTrigger(Battler.AnimParams.DoneAction.ToString());
        ActingBattler.ApproachForNextTurn();
        yield return new WaitUntil(ActingBattler.HasApproachedNextTurnDestination);
        yield return new WaitUntil(() => ActiveProjectiles.childCount == 0);
        ActionEnd();
    }

    private IEnumerator AttemptEscape()
    {
        BattleMenu.RemovePartyFrames();
        RestrictBattlerWallCollision(false);
        BattleMenu.AddEscapeHitboxes();
        yield return new WaitForSeconds(1);
        foreach (var b in Battlers) b.SetToEscapeMode(true, b is not BattleEnemy);
        yield return new WaitForSeconds(3);
        if (BattleMenu.Escaping) yield return Escape();
    }

    public void NotifyEscapeFailure()
    {
        BattleMenu.RemoveEscapeHitboxes();
        StartCoroutine(BattleMenu.DisplayUsedAction(ActingBattler, "COULD NOT ESCAPE"));
        StartCoroutine(EscapeFailureSequence());
    }

    private IEnumerator EscapeFailureSequence()
    {
        foreach (var b in Battlers) b.SetToEscapeMode(false, b is not BattleEnemy);
        yield return new WaitForSeconds(2);
        foreach (var b in Battlers)
        {
            b.ApproachForNextTurn();
            yield return new WaitUntil(b.HasApproachedNextTurnDestination);
        }
        ActionEnd();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of Action --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ActionEnd()
    {
        ActingBattler.ExecutedAction = true;
        if (ActingBattler.SelectedAction is Skill skill) skill.ApplyActionEndEffects();
        ResetSelectedTargets();
        RestrictBattlerWallCollision(true);
        if (!CheckBattleEndCondition())
        {
            if (LastActionOfTurn) TurnReset();
            StartCoroutine(ActionStart());
        }
    }

    public void ResetSelectedTargets()
    {
        foreach (Battler b in Battlers)
        {
            b.SingleSelectedTarget = null;
            b.Select(false);
            b.LockSelectTrigger = false;
        }
    }

    private void FinishActionUsage()
    {
        if (ActingBattler.SelectedAction is Item it)
        {
            //Stats.Add(item.PermantentStatChanges);
            //if (it.TurnsInto) Items[Items.FindIndex(x => x.Id == it.Id)] = Instantiate(it.TurnsInto, gameObject.transform);
            //else if (it.Consumable) Items.Remove(it);
        }
    }

    private bool CheckBattleEndCondition()
    {
        if (EnemyPartyDefeated)
        {
            BattleMenu.RefreshPartyFrames();
            if (EnemyParty.ShowVictoryWhenDefeated) StartCoroutine(DeclareWin());
            else SceneMaster.EndBattle();
            return true;
        }
        else if (PlayerPartyDefeated)
        {
            if (EnemyParty.HasGameOverScreen) StartCoroutine(DeclareGameOver());
            else SceneMaster.EndBattle();
            return true;
        }
        return false;
    }

    private bool EnemyPartyDefeated => EnemyParty.Enemies.All(x => x.KOd);
    private bool PlayerPartyDefeated => PlayerParty.Players.All(x => x.KOd) && (PlayerParty.Allies.Count == 0 || PlayerParty.Allies.All(x => x.KOd));

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of Battle --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private IEnumerator Escape()
    {
        ClearAll();
        foreach (var e in EnemyParty.Enemies) e.SetToEscapeMode(false, false);
        StartCoroutine(BattleMenu.DisplayUsedAction(ActingBattler, "ESCAPED!"));
        yield return new WaitForSeconds(2);
        SceneMaster.EndBattle();
    }

    private IEnumerator DeclareWin()
    {
        ClearAll();
        foreach (var b in PlayerParty.BattlingParty) b.SpriteInfo.Animation.SetInteger(Battler.AnimParams.Victory.ToString(), 1);
        yield return new WaitForSeconds(2);
        BattleWinMenu.Setup();
    }

    private IEnumerator DeclareGameOver()
    {
        ClearAll();
        yield return new WaitForSeconds(3);
        SceneMaster.OpenGameOver();
    }

    private void ClearAll()
    {
        ClearAllTurnIndicators();
        ResetBattlerActions();
        BattleMenu.RemovePartyFrames();
    }

    private void OnDestroy()
    {
        foreach (var b in PlayerParty.BattlingParty) b.SetBattle(null);
        SceneMaster.ApplyBattleEndChanges();
    }
}