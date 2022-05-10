using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class Battle : MonoBehaviour
{
    public enum BattlePhases { None, DecidingAction, PreAction, Action, RunningAway, Won, GameOver }

    // Collision detection
    public const int BASE_HITBOX_LAYER = 11;
    public const int ACTION_HITBOX_LAYER = 12;
    public const int MOVING_SCOPE_HITBOX_LAYER = 13;
    public GameObject[] Boundaries;

    // Loading
    public bool Waiting => Time.unscaledTime < AwaitingTime;
    private float AwaitingTime;

    // UI
    public BattleCamera BattleCamera;
    public BattleMenu BattleMenu;
    public BattleWin BattleWinMenu;

    // Data transferred from Map
    [HideInInspector] public Environment Enviornment;
    [HideInInspector] public PlayerParty PlayerParty;
    [HideInInspector] public EnemyParty EnemyParty;

    // Raw locations based on battler's horizontal/vertical positions
    private static readonly float uX = 1.5f;
    private static readonly float uY = 1.6f;
    private readonly Vector3[][] Positions = new Vector3[][]
    {
        new Vector3[] { new Vector3(-uX, uY), new Vector3(0, uY), new Vector3(uX, uY) },
        new Vector3[] { new Vector3(-uX, 0), Vector3.zero, new Vector3(uX, 0) },
        new Vector3[] { new Vector3(-uX, -uY), new Vector3(0, -uY), new Vector3(uX, -uY) },
    };

    // Battle state tracking
    public BattlePhases Phase { get; private set; }
    private bool StartingBattle;
    private bool StartingAction;
    private bool NotifySwitchInBattlePhase;
    private bool LastActionOfTurn;
    [HideInInspector] public int Turn;

    // Manage the battlers themselves
    private List<Battler> Battlers = new List<Battler>();
    private List<Battler> BattlersByColumn = new List<Battler>();
    public Battler ActingBattler { get; private set; }
    public Battler NextActingBattler { get; private set; }
    public IEnumerable<Battler> AllBattlers => Battlers;
    public IEnumerable<Battler> FightingPlayerParty => PlayerParty.Players.Cast<Battler>().Concat(PlayerParty.Allies);

    // Lists dump
    public Transform PlayerPartyDump;
    public Transform EnemyPartyDump;

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
        SceneMaster.DeactivateStoredGameObjects();      // Hide overworld
        //StartCoroutine(SetupContents());
        SetupBackground();
        SetupPlayerParty();
        SetupEnemyParty();
        foreach (BattlePlayer p in PlayerParty.Players) Battlers.Add(p);
        foreach (BattleAlly a in PlayerParty.Allies) Battlers.Add(a);
        foreach (BattleEnemy e in EnemyParty.Enemies) Battlers.Add(e);
        ClearAllTurnIndicators();
        SortBattlersInOrderLayer();
        StartingBattle = true;
        Await(2);
    }

    IEnumerable SetupContents()
    {
        yield return null;
    }

    public void RestrictBattlerWallCollision(bool restrict)
    {
        foreach (var b in Boundaries) b.gameObject.SetActive(restrict);
    }

    private void SetupBackground()
    {
        //
    }

    private void SetupPlayerParty()
    {
        PlayerParty = BattleMaster.PlayerParty;
        PlayerParty.Players = SetupPlayerPositions(PlayerParty.Players);
        PlayerParty.Allies = SetupAllyPositions(PlayerParty.Allies);
        PlayerParty.Players = SetupBattlers(PlayerParty.Players, PlayerPartyDump);
        PlayerParty.Allies = SetupBattlers(PlayerParty.Allies, PlayerPartyDump);
    }

    private void SetupEnemyParty()
    {
        EnemyParty = Instantiate(BattleMaster.EnemyParty, gameObject.transform);
        EnemyParty.gameObject.SetActive(false);
        EnemyParty.Enemies = SetupBattlers(EnemyParty.Enemies, EnemyPartyDump);
    }

    List<T> SetupBattlers<T>(List<T> list, Transform partyGameObject) where T : Battler
    {
        List<T> result = new List<T>();
        int i = 0;
        foreach (Battler b0 in list)
        {
            Vector3 bpPos = partyGameObject.position + Positions[(int)b0.RowPosition][(int)b0.ColumnPosition];
            T b = (T)InstantiateBattler(b0, bpPos, i++);
            result.Add(b);
        }
        return result;
    }

    List<BattlePlayer> SetupPlayerPositions(List<BattlePlayer> party)
    {
        switch (party.Count)
        {
            case 1:
                party[0].SetBattlePositions(Battler.VerticalPositions.Center, Battler.HorizontalPositions.Right);
                break;
            case 2:
                party[0].SetBattlePositions(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Right);
                party[1].SetBattlePositions(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Right);
                break;
            case 3:
                party[0].SetBattlePositions(Battler.VerticalPositions.Center, Battler.HorizontalPositions.Right);
                party[1].SetBattlePositions(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Left);
                party[2].SetBattlePositions(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Left);
                break;
            case 4:
                party[0].SetBattlePositions(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Right);
                party[1].SetBattlePositions(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Right);
                party[2].SetBattlePositions(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Left);
                party[3].SetBattlePositions(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Left);
                break;
        }
        return party;
    }

    List<BattleAlly> SetupAllyPositions(List<BattleAlly> allies)
    {
        switch (allies.Count)
        {
            case 1:
                allies[0].SetBattlePositions(Battler.VerticalPositions.Center, Battler.HorizontalPositions.Center);
                break;
            case 2:
                allies[0].SetBattlePositions(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Center);
                allies[1].SetBattlePositions(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Center);
                break;
        }
        return allies;
    }

    public T InstantiateBattler<T>(T newBattler, Vector3 position, int index) where T : Battler
    {
        T b = Instantiate(newBattler, position, Quaternion.identity, (newBattler is BattleEnemy ? EnemyPartyDump : PlayerPartyDump));

        if (b.Class)
        {
            b.Class = Instantiate(b.Class, b.transform);
            b.Class.SetBattle(this);
        }
        
        b.BasicAttackSkill = Instantiate(b.BasicAttackSkill, b.transform);
        for (int i = 0; i < b.States.Count; i++) b.States[i] = Instantiate(b.States[i], b.transform);
        b.StatBoosts.SetToZero();
        
        b.transform.localScale = Vector3.one * 0.7f;
        b.gameObject.SetActive(true);
        b.SetBattle(this);
        return b;
    }

    private void SortBattlersInOrderLayer()
    {
        BattlersByColumn.AddRange(Battlers);
        BattlersByColumn = BattlersByColumn.OrderByDescending(x => x.Position.y).ToList();
        int i = 0;
        foreach (var b in BattlersByColumn) b.SetColumnOverlapRank(i++);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Update()
    {
        if (Waiting)
        {
            return;
        }
        else if (StartingBattle)
        {
            StartingBattle = false;
            TurnStart();
            return;
        }
        else if (StartingAction)
        {
            BattleMenu.RefreshPartyFrames();
            StartingAction = false;
            ActionStart();
            return;
        }
        
        UpdateActingBattlerLayerOrder();

        switch (Phase)
        {
            case BattlePhases.DecidingAction:
                if (NotifiedToSwitchInBattlePhase()) PrepareForAction();
                break;

            case BattlePhases.PreAction:
                if (NotifiedToSwitchInBattlePhase()) ExecuteAction();
                break;

            case BattlePhases.Action:
                if (NotifiedToSwitchInBattlePhase()) ActionEnd();
                break;

            case BattlePhases.RunningAway:
                break;

            case BattlePhases.Won:
                BattleWinMenu.Setup();
                Phase = BattlePhases.None;
                break;

            case BattlePhases.GameOver:
                SetupGameOver();
                Phase = BattlePhases.None;
                break;
        }
    }

    public void Await(float m = 1)
    {
        if (Waiting) AwaitingTime += m;
        else AwaitingTime = Time.unscaledTime + m;
    }

    public void NotifyToSwitchInBattlePhase()
    {
        NotifySwitchInBattlePhase = true;
    }

    private bool NotifiedToSwitchInBattlePhase()
    {
        if (NotifySwitchInBattlePhase)
        {
            NotifySwitchInBattlePhase = false;
            return true;
        }
        return false;
    }

    private void UpdateActingBattlerLayerOrder()
    {
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
    /// -- Turn Start --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void TurnStart()
    {
        Turn++;
        LastActionOfTurn = false;
        Battlers = SortBattlersBySpeed(Battlers, -5, 6);
        ActionStartSetup();
    }

    public void ActionStartSetup()
    {
        Await(1);
        ClearAllTurnIndicators();
        GetNextFastestAvailableBattlers();
        ActingBattler.Sprite.HandleTurnIndicators(true, false);
        NextActingBattler.Sprite.HandleTurnIndicators(false, true);
        StartingAction = true;
    }

    private void ActionStart()
    {
        Phase = BattlePhases.DecidingAction;
        if (ActingBattler.CanDoAction)
        {
            if (ActingBattler is BattlePlayer p)
            {
                BattleMenu.Setup(p);
            }
            else  // Battler AI
            {
                BattleMenu.Hide();
                if (ActingBattler is BattleAlly ally) ally.MakeDecision(FightingPlayerParty.ToList(), EnemyParty.Enemies);
                else if (ActingBattler is BattleEnemy enemy) enemy.MakeDecision(EnemyParty.Enemies, FightingPlayerParty.ToList());
                PrepareForAction();
            }
        }
        else ActionEnd();
    }

    private List<Battler> SortBattlersBySpeed(List<Battler> battlers, int speedRandomLow = 0, int speedRandomHigh = 0)
    {
        for (int i = 0; i < battlers.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                if (battlers[j - 1].Spd + Random.Range(speedRandomLow, speedRandomHigh) > battlers[j].Spd + Random.Range(speedRandomLow, speedRandomHigh)) continue;
                Battler temp = battlers[j - 1];
                battlers[j - 1] = battlers[j];
                battlers[j] = temp;
            }
        }
        return battlers;
    }

    private void GetNextFastestAvailableBattlers()
    {
        int actingBattlerSet = 0;
        foreach (Battler b in Battlers)
        {
            if (b.ExecutedAction || b.KOd) continue;
            else if (actingBattlerSet == 0) ActingBattler = b;
            else if (actingBattlerSet == 1) NextActingBattler = b;
            actingBattlerSet++;
        }
        if (actingBattlerSet == 1)
        {
            LastActionOfTurn = true;
            foreach (Battler b in Battlers)
            {
                if (ActingBattler == b || b.KOd) continue;
                NextActingBattler = b;
                break;
            }
        }
    }

    public void ResetSelectedTargets()
    {
        foreach (Battler b in Battlers)
        {
            b.SelectedSingleMeeleeTarget = null;
            b.Select(false);
            b.LockSelectTrigger = false;
        }
    }

    private void ClearAllTurnIndicators()
    {
        foreach (var b in Battlers) b.Sprite.HandleTurnIndicators(false, false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action execution --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void PrepareForAction()
    {
        if (ActingBattler.SelectedAction == null)
        {
            ActionEnd();
        }
        else if (ActingBattler.SelectedSingleMeeleeTarget)
        {
            Phase = BattlePhases.PreAction;
            RestrictBattlerWallCollision(false);
            var sp = ActingBattler.SelectedSingleMeeleeTarget.Sprite;
            ActingBattler.ApproachTarget(sp.ApproachPointLeft.position, sp.ApproachPointRight.position);
        }
        else ExecuteAction();
    }

    private bool IsChargingSkill(Skill skill)
    {
        if (!skill) return false;
        skill.StartCharge();
        if (skill.ChargeCount == 0) return false;
        //BattleState = BattleStates.
        //ActingBattler.Phase = Battler.Phases.UsingAction;
        skill.Charge1Turn();
        ActingBattler.AnimatingCharging();
        return true;
    }

    public void ExecuteAction()
    {
        Skill skill = ActingBattler.SelectedAction as Skill;
        if (IsChargingSkill(skill)) return;

        Phase = BattlePhases.Action;
        if (!skill || !skill.Basic) BattleMenu.DisplayUsedAction(ActingBattler, ActingBattler.SelectedAction);

        if (ActingBattler.UsingBasicAttack)
        {
            if (ActingBattler.Class) ActingBattler.Class.UseBasicAttack(ActingBattler.SelectedWeapon);
            else ActingBattler.UseBasicAttack();
        }
        else if (skill)
        {
            if (skill.ClassSkill) ActingBattler.Class.UseSkill();
            else ActingBattler.UseSkill();
            skill.DisableForCooldown();
        }
        else if (ActingBattler.SelectedAction is Item item)
        {
            if (ActingBattler.Class) ActingBattler.Class.UseItem(item);
            else ActingBattler.UseItem(item);
        }
    }

    public void RunAway()
    {
        Phase = BattlePhases.RunningAway;
    }

    public void RunFailed()
    {
        BattleMenu.RunFailed();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of Action --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ActionEnd()
    {
        ActingBattler.ExecutedAction = true;
        ResetSelectedTargets();
        RestrictBattlerWallCollision(true);
        FinishActionUsage();
        if (CheckBattleEndCondition()) return;
        else if (LastActionOfTurn) TurnEnd();
        else ActionStartSetup();
    }

    private void FinishActionUsage()
    {
        if (ActingBattler.Class) ActingBattler.Class.ResetActionExecution();
        else ActingBattler.ResetActionExecution();

        if (ActingBattler.SelectedAction is Skill sk)
        {
            sk.DisableForCooldown();
        }
        else if (ActingBattler.SelectedAction is Item it)
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
            switch (EnemyParty.PartyMode)
            {
                case EnemyParty.EnemyPartyModes.Regular:
                    DeclareWin();
                    break;
                case EnemyParty.EnemyPartyModes.Boss:
                    break;
                case EnemyParty.EnemyPartyModes.FinalBoss:
                    break;
            }
            return true;
        }
        else if (PlayerPartyDefeated)
        {
            if (EnemyParty.GameOverOnLose) DeclareGameOver();
            else SceneMaster.EndBattle(PlayerParty);
            return true;
        }
        return false;
    }

    private bool EnemyPartyDefeated => EnemyParty.Enemies.All(x => x.KOd);
    private bool PlayerPartyDefeated => PlayerParty.Players.All(x => x.KOd) && PlayerParty.Allies.All(x => x.KOd);

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of turn --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void TurnEnd()
    {
        ResetBattlerActions();
        BattleMenu.EndTurn();
        TurnStart();
    }

    private void ResetBattlerActions()
    {
        foreach (Battler b in AllBattlers)
        {
            b.ResetAction();
            b.Select(false);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of battle --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ClearAll()
    {
        ClearAllTurnIndicators();
        ResetBattlerActions();
    }

    private void DeclareWin()
    {
        Phase = BattlePhases.Won;
        ClearAll();
    }

    private void DeclareGameOver()
    {
        Phase = BattlePhases.GameOver;
        ClearAll();
    }

    private void SetupGameOver()
    {
        SceneMaster.OpenGameOver();
    }

    private void OnDestroy()
    {
        SceneMaster.ActivateStoredGameObjects();      // Return overworld
    }
}