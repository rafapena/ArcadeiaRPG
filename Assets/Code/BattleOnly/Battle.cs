using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class Battle : MonoBehaviour
{
    public enum BattleStates { None, Menu, Action, Won, GameOver }

    // Collision detection
    public const int BATTLER_LAYER = 11;
    public const int BATTLE_WALL_LAYER = 12;
    public const int SCOPE_THROUGH_LAYER = 13;

    // Loading
    public bool Waiting => Time.unscaledTime < AwaitingTime;
    private float AwaitingTime;
    private const float AWAITING_TIME_BEFORE_ACTION_START = 0.5f;

    // UI
    public BattleCamera BattleCamera;
    public BattleMenu BattleMenu;
    public BattleWin BattleWinMenu;

    // Target fields
    [System.Serializable]
    public struct TargetFieldsGroup
    {
        public Transform FieldsList;
        public DynamicTargetField Single;
        public DynamicTargetField SplashRange;
        public StaticTargetField SplashMeelee;
        public StaticTargetField StraightThrough;
        public StaticTargetField Widespread;
    }
    public TargetFieldsGroup TargetFields;

    // Data transferred from Map
    [HideInInspector] public Environment Enviornment;
    [HideInInspector] public PlayerParty PlayerParty;
    [HideInInspector] public EnemyParty EnemyParty;

    // Helpers for their respective party lists for flexible target handling
    [HideInInspector] public List<Battler> PlayerPartyMembers;  
    [HideInInspector] public List<Battler> EnemyPartyMembers;

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

    // Turn tracking
    [HideInInspector] public int Turn;
    private bool EndingAction;
    private bool EndingTurn;

    // Manage the battlers themselves
    private List<Battler> Battlers;
    private Battler ActingBattler;
    private Battler NextActingBattler;
    private int ActingBattlerIndex;

    public IEnumerable<Battler> AllBattlers => Battlers;


    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Awake()
    {
        Physics2D.IgnoreLayerCollision(BATTLER_LAYER, BATTLER_LAYER);
        Physics2D.IgnoreLayerCollision(SCOPE_THROUGH_LAYER, BATTLER_LAYER);
        Physics2D.IgnoreLayerCollision(SCOPE_THROUGH_LAYER, BATTLE_WALL_LAYER);
        BattleMenu.ClearScope();
    }

    void Start()
    {
        SceneMaster.DeactivateStoredGameObjects();      // Hide overworld
        SetupBackground();
        SetupPlayerParty();
        SetupEnemyParty();
        Battlers = new List<Battler>();
        foreach (BattlePlayer p in PlayerParty.Players) Battlers.Add(p);
        foreach (BattleAlly a in PlayerParty.Allies) Battlers.Add(a);
        foreach (BattleEnemy e in EnemyParty.Enemies) Battlers.Add(e);
        TurnStart();
    }

    void SetupBackground()
    {
        //
    }

    void SetupPlayerParty()
    {
        Transform playerPartyGameObject = GameObject.Find("/PlayerParty").transform;
        PlayerParty = BattleMaster.PlayerParty;
        PlayerParty.Players = SetupPlayerPositions(PlayerParty.Players);
        PlayerParty.Allies = SetupAllyPositions(PlayerParty.Allies);
        PlayerParty.Players = SetupBattlers(PlayerParty.Players, playerPartyGameObject);
        PlayerParty.Allies = SetupBattlers(PlayerParty.Allies, playerPartyGameObject);
    }

    void SetupEnemyParty()
    {
        Transform enemyPartySquare = GameObject.Find("/EnemyParty").transform;
        EnemyParty = Instantiate(BattleMaster.EnemyParty, gameObject.transform);
        EnemyParty.gameObject.SetActive(false);
        EnemyParty.Enemies = SetupBattlers(EnemyParty.Enemies, enemyPartySquare);
    }

    List<T> SetupBattlers<T>(List<T> list, Transform playerPartySquare) where T : Battler
    {
        List<T> result = new List<T>();
        foreach (Battler b0 in list)
        {
            Vector3 bpPos = playerPartySquare.position + Positions[(int)b0.RowPosition][(int)b0.ColumnPosition];
            T b = (T)Instantiate(b0, bpPos, Quaternion.identity, playerPartySquare);
            if (b is BattlePlayer p) p.BasicAttackSkill = Instantiate(p.BasicAttackSkill, b.transform);
            b = InstantiateContents(b);
            b.transform.localScale = Vector3.one * 0.5f;
            b.gameObject.SetActive(true);
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
            case BattleStates.Action:
                if (ActingBattlerIndex >= Battlers.Count)
                {
                    TurnEnd();
                    return;
                }
                else if (!EndingAction)
                {
                    EndingAction = true;
                }
                else
                {
                    ActionEnd();
                    EndingAction = false;
                    return;
                }
                if (ActingBattler is BattleEnemy) ActingBattler.ExecuteAction(EnemyPartyMembers, PlayerPartyMembers);
                else ActingBattler.ExecuteAction(PlayerPartyMembers, EnemyPartyMembers);
                BattleStateTime = Time.time + (ActingBattler.SelectedTool?.ActionTime ?? 0);
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
        Await(4);
        ActingBattlerIndex = 0;
        EndingAction = false;
        EndingTurn = false;
        ActionStart();
    }

    private void ActionStart()
    {
        Battlers = SortBattlersBySpeed(Battlers);
        ActingBattler = Battlers[ActingBattlerIndex];
        NextActingBattler = Battlers[(ActingBattlerIndex + 1) % Battlers.Count];

        if (ActingBattler is BattlePlayer p)
        {
            BattleState = BattleStates.Menu;
            BattleMenu.Setup(p);
        }
        else // Ally or enemy
        {
            BattleState = BattleStates.Action;
            BattleMenu.Hide();
            ActingBattler.EnableMoving();
        }
        if (NextActingBattler is BattlePlayer p0) BattleMenu.DeclareNext(p0);
        else (NextActingBattler as BattlerAI).DeclareNext();
    }

    // Insertion sort: Order very rarely changes after the first setup, giving an overall O(N) runtime.
    private List<Battler> SortBattlersBySpeed(List<Battler> battlers)
    {
        for (int i = ActingBattlerIndex; i < battlers.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int bj1 = battlers[j - 1].Spd();
                int bj2 = battlers[j].Spd();
                if (bj1 > bj2 || battlers[j - 1].ExecutedAction || bj1 == bj2 && Random.Range(0, 100) < 50) continue;
                Battler temp = battlers[j - 1];
                battlers[j - 1] = battlers[j];
                battlers[j] = temp;
            }
        }
        return battlers;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action execution --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ExecuteTurn(List<Battler> playerParty, List<Battler> enemyParty)
    {
        foreach (BattlePlayer p in PlayerParty.Players) p.ExecutedAction = false;
        foreach (BattleAlly a in PlayerParty.Allies) a.MakeDecision(playerParty, enemyParty);
        foreach (BattleEnemy e in EnemyParty.Enemies) e.MakeDecision(enemyParty, playerParty);
        PlayerPartyMembers = playerParty;
        EnemyPartyMembers = enemyParty;
        BattleState = BattleStates.Action;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of Action --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ActionEnd()
    {
        ActingBattler.ExecutedAction = true;
        if (CheckBattleEndCondition()) return;
        ActingBattlerIndex++;
        if (ActingBattlerIndex >= Battlers.Count) return;
        Battlers = SortBattlersBySpeed(Battlers);
        ActingBattler = Battlers[ActingBattlerIndex];
    }

    private bool CheckBattleEndCondition()
    {
        if (EnemyPartyDefeated())
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
        else if (PlayerPartyDefeated())
        {
            if (EnemyParty.GameOverOnLose) DeclareGameOver();
            else SceneMaster.EndBattle(PlayerParty);
            return true;
        }
        return false;
    }

    private bool EnemyPartyDefeated()
    {
        foreach (BattleEnemy e in EnemyParty.Enemies)
            if (!e.KOd) return false;
        return true;
    }

    private bool PlayerPartyDefeated()
    {
        foreach (BattlePlayer p in PlayerParty.Players)
            if (!p.KOd) return false;
        foreach (BattleAlly a in PlayerParty.Allies)
            if (!a.KOd) return false;
        return true;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of turn --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void TurnEnd()
    {
        BattleState = BattleStates.Menu;
        EndingTurn = true;
        Turn++;
        BattleMenu.EndTurn();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of battle --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    private void DeclareWin()
    {
        BattleState = BattleStates.Won;
        BattleStateTime = Time.time + 0.5f;
    }

    private void DeclareGameOver()
    {
        BattleState = BattleStates.GameOver;
        BattleStateTime = Time.time + 0.5f;
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