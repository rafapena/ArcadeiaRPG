using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class Battle : MonoBehaviour
{
    public enum BattleStates { None, Menu, PreAction, Action, Running, Won, GameOver }

    // Collision detection
    public const int BASE_HITBOX_LAYER = 11;
    public const int ACTION_HITBOX_LAYER = 12;
    public const int MOVING_SCOPE_HITBOX_LAYER = 13;

    // Loading
    public bool Waiting => Time.unscaledTime < AwaitingTime;
    private float AwaitingTime;
    private const float AWAITING_TIME_BEFORE_ACTION_START = 0.5f;

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
    private BattleStates BattleState;
    private float BattleStateTime;
    private bool LastActionOfTurn;
    [HideInInspector] public int Turn;

    // Manage the battlers themselves
    private List<Battler> Battlers = new List<Battler>();
    public Battler ActingBattler { get; private set; }
    public Battler NextActingBattler { get; private set; }
    public IEnumerable<Battler> AllBattlers => Battlers;
    public IEnumerable<Battler> FightingPlayerParty => PlayerParty.Players.Cast<Battler>().Concat(PlayerParty.Allies);

    // Lists dump
    public Transform PlayerPartyDump;
    public Transform EnemyPartyDump;
    public Transform ClassControlDump;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Awake()
    {
        Physics2D.IgnoreLayerCollision(BASE_HITBOX_LAYER, BASE_HITBOX_LAYER);
        Physics2D.IgnoreLayerCollision(MOVING_SCOPE_HITBOX_LAYER, MOVING_SCOPE_HITBOX_LAYER);
        Physics2D.IgnoreLayerCollision(BASE_HITBOX_LAYER, MOVING_SCOPE_HITBOX_LAYER);
    }

    void Start()
    {
        SceneMaster.DeactivateStoredGameObjects();      // Hide overworld
        SetupBackground();
        SetupPlayerParty();
        SetupEnemyParty();
        BattleMenu.ClearAllNextLabels();
        foreach (BattlePlayer p in PlayerParty.Players) Battlers.Add(p);
        foreach (BattleAlly a in PlayerParty.Allies) Battlers.Add(a);
        foreach (BattleEnemy e in EnemyParty.Enemies) Battlers.Add(e);
        Await(4);
        TurnStart();
    }

    void SetupBackground()
    {
        //
    }

    void SetupPlayerParty()
    {
        PlayerParty = BattleMaster.PlayerParty;
        PlayerParty.Players = SetupPlayerPositions(PlayerParty.Players);
        PlayerParty.Allies = SetupAllyPositions(PlayerParty.Allies);
        PlayerParty.Players = SetupBattlers(PlayerParty.Players, PlayerPartyDump);
        PlayerParty.Allies = SetupBattlers(PlayerParty.Allies, PlayerPartyDump);
    }

    void SetupEnemyParty()
    {
        EnemyParty = Instantiate(BattleMaster.EnemyParty, gameObject.transform);
        EnemyParty.gameObject.SetActive(false);
        EnemyParty.Enemies = SetupBattlers(EnemyParty.Enemies, EnemyPartyDump);
    }

    List<T> SetupBattlers<T>(List<T> list, Transform partyGameObject) where T : Battler
    {
        List<T> result = new List<T>();
        foreach (Battler b0 in list)
        {
            Vector3 bpPos = partyGameObject.position + Positions[(int)b0.RowPosition][(int)b0.ColumnPosition];
            T b = (T)Instantiate(b0, bpPos, Quaternion.identity, partyGameObject);
            if (b is BattlePlayer p) p.BasicAttackSkill = Instantiate(p.BasicAttackSkill, b.transform);
            b = InstantiateContents(b);
            b.transform.localScale = Vector3.one * 0.5f;
            b.gameObject.SetActive(true);
            b.SetBattle(this);
            if (b.Class)
            {
                b.Class = Instantiate(b.Class, bpPos, Quaternion.identity, ClassControlDump);
                b.Class.SetBattle(this);
            }
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

    private T InstantiateContents<T>(T b) where T : Battler
    {
        for (int i = 0; i < b.Skills.Count; i++)
        {
            b.Skills[i] = Instantiate(b.Skills[i], b.transform);
            b.Skills[i].DisableForWarmup();
        }
        for (int i = 0; i < b.Weapons.Count; i++) b.Weapons[i] = Instantiate(b.Weapons[i], b.transform);
        for (int i = 0; i < b.Accessories.Count; i++) b.Accessories[i] = Instantiate(b.Accessories[i], b.transform);
        for (int i = 0; i < b.States.Count; i++) b.States[i] = Instantiate(b.States[i], b.transform);
        b.StatBoosts.SetToZero();
        return b;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Update()
    {
        if (Waiting) return;
        switch (BattleState)
        {
            case BattleStates.Menu:
                return;

            case BattleStates.PreAction:
                if (ActingBattler.Phase == Battler.Phases.UsingAction) ExecuteAction();
                return;

            case BattleStates.Action:
                if (Time.time > BattleStateTime) ActionEnd();
                break;

            case BattleStates.Running:
                break;

            case BattleStates.Won:
                BattleWinMenu.Setup();
                BattleState = BattleStates.None;
                break;

            case BattleStates.GameOver:
                SetupGameOver();
                BattleState = BattleStates.None;
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Turn Start --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Await(float m = 1)
    {
        AwaitingTime = Time.unscaledTime + AWAITING_TIME_BEFORE_ACTION_START * m;
    }

    public void TurnStart()
    {
        Turn++;
        LastActionOfTurn = false;
        ActionStart();
    }

    private void ActionStart()
    {
        ResetSelectedTargets();
        Battlers = SortBattlersBySpeed(Battlers);
        GetNextFastestAvailableBattlers();
        ActingBattler.Phase = Battler.Phases.DecidingAction;

        if (ActingBattler is BattlePlayer p)
        {
            BattleState = BattleStates.Menu;
            BattleMenu.Setup(p);
        }
        else // Ally or enemy
        {
            BattleMenu.Hide();
            if (ActingBattler is BattleAlly ally) ally.MakeDecision(FightingPlayerParty.ToList(), EnemyParty.Enemies);
            else if (ActingBattler is BattleEnemy enemy) enemy.MakeDecision(EnemyParty.Enemies, FightingPlayerParty.ToList());
            PrepareForAction();
        }
        BattleMenu.DeclareNext(NextActingBattler);
    }

    private List<Battler> SortBattlersBySpeed(List<Battler> battlers)
    {
        for (int i = 0; i < battlers.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                if (battlers[j - 1].Spd + Random.Range(-5, 6) > battlers[j].Spd + Random.Range(-5, 6)) continue;
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
            if (b.Phase == Battler.Phases.ExecutedAction) continue;
            else if (actingBattlerSet == 0) ActingBattler = b;
            else if (actingBattlerSet == 1) NextActingBattler = b;
            actingBattlerSet++;
        }
        if (actingBattlerSet == 1)
        {
            NextActingBattler = Battlers[0];
            LastActionOfTurn = true;
        }
    }

    public void ResetSelectedTargets()
    {
        foreach (Battler b in Battlers)
        {
            b.Select(false);
            b.LockSelectTrigger = false;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action execution --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void PrepareForAction()
    {
        BattleState = BattleStates.PreAction;
        if (!ActingBattler.SelectedTool.Ranged && ActingBattler.SelectedTool.Scope == ActiveTool.ScopeType.OneEnemy)
        {
            ActingBattler.Phase = Battler.Phases.PreparingAction;
            ActingBattler.ApproachTarget(BattleMenu.TargetFields.Single.ApproachPointLeft, BattleMenu.TargetFields.Single.ApproachPointRight);
        }
        else ActingBattler.Phase = Battler.Phases.UsingAction;
    }

    public void ExecuteAction()
    {
        BattleState = BattleStates.Action;
        ActingBattler.Phase = Battler.Phases.UsingAction;
        if (ActingBattler.UsingBasicAttack)
        {
            if (ActingBattler.Class) ActingBattler.Class.UseBasicAttack(ActingBattler.SelectedWeapon);
            else ActingBattler.UseBasicAttack();
        }
        else ActingBattler.SelectedTool.ExecuteAction?.Invoke();
        BattleStateTime = Time.time + (ActingBattler.SelectedTool?.ActionTime ?? 1);
    }

    public void RunAway()
    {
        BattleState = BattleStates.Running;
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
        ActingBattler.Phase = Battler.Phases.ExecutedAction;
        FinishSkillUsage();
        if (CheckBattleEndCondition()) return;
        else if (LastActionOfTurn) TurnEnd();
        else ActionStart();
    }

    private void FinishSkillUsage()
    {
        if (ActingBattler.Class) ActingBattler.Class.ClearSkillExecution();
        else ActingBattler.ClearSkillExecution();
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
        BattleMenu.ClearAllNextLabels();
        ResetBattlerActions();
    }

    private void DeclareWin()
    {
        BattleState = BattleStates.Won;
        ClearAll();
    }

    private void DeclareGameOver()
    {
        BattleState = BattleStates.GameOver;
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