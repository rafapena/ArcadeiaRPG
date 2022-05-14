using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class Battle : MonoBehaviour
{
    // Collision detection
    public const int BASE_HITBOX_LAYER = 11;
    public const int ACTION_HITBOX_LAYER = 12;
    public const int MOVING_SCOPE_HITBOX_LAYER = 13;
    public GameObject[] Boundaries;

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
    private static readonly float uY = 1f;
    private readonly Vector3[][] Positions = new Vector3[][]
    {
        new Vector3[] { new Vector3(-uX, uY), new Vector3(0, uY), new Vector3(uX, uY) },
        new Vector3[] { new Vector3(-uX, 0), Vector3.zero, new Vector3(uX, 0) },
        new Vector3[] { new Vector3(-uX, -uY), new Vector3(0, -uY), new Vector3(uX, -uY) },
    };

    // Battle state tracking
    public int Turn { get; private set; }
    private bool LastActionOfTurn;

    // Grouping battlers
    public Transform PlayerPartyDump;
    public Transform EnemyPartyDump;
    private List<Battler> Battlers = new List<Battler>();
    private List<Battler> BattlersByColumn = new List<Battler>();
    public Battler ActingBattler { get; private set; }
    public Battler NextActingBattler { get; private set; }
    public IEnumerable<Battler> AllBattlers => Battlers;
    public IEnumerable<Battler> FightingPlayerParty => PlayerParty.Players.Cast<Battler>().Concat(PlayerParty.Allies);

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
        // Hide overworld
        SceneMaster.DeactivateStoredGameObjects();

        // Setup player party
        PlayerParty = BattleMaster.PlayerParty;
        PlayerParty = BattleMaster.PlayerParty;
        PlayerParty.Players = SetupPlayerPositions(PlayerParty.Players);
        PlayerParty.Allies = SetupAllyPositions(PlayerParty.Allies);
        PlayerParty.Players = SetupBattlers(PlayerParty.Players, PlayerPartyDump);
        PlayerParty.Allies = SetupBattlers(PlayerParty.Allies, PlayerPartyDump);

        // Setup enemy party
        EnemyParty = Instantiate(BattleMaster.EnemyParty, gameObject.transform);
        EnemyParty.gameObject.SetActive(false);
        EnemyParty.Enemies = SetupBattlers(EnemyParty.Enemies, EnemyPartyDump);

        // Group into battlers list
        foreach (BattlePlayer p in PlayerParty.Players) Battlers.Add(p);
        foreach (BattleAlly a in PlayerParty.Allies) Battlers.Add(a);
        foreach (BattleEnemy e in EnemyParty.Enemies) Battlers.Add(e);
        SortBattlersInOrderLayer();

        // Start battle
        StartCoroutine(ProcessFirstTurn());
    }

    private List<T> SetupBattlers<T>(List<T> list, Transform partyGameObject) where T : Battler
    {
        List<T> result = new List<T>();
        foreach (Battler b0 in list)
        {
            Vector3 bpPos = partyGameObject.position + Positions[(int)b0.RowPosition][(int)b0.ColumnPosition];
            T b = (T)InstantiateBattler(b0, bpPos);
            result.Add(b);
        }
        return result;
    }

    private List<BattlePlayer> SetupPlayerPositions(List<BattlePlayer> party)
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

    private List<BattleAlly> SetupAllyPositions(List<BattleAlly> allies)
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

    public T InstantiateBattler<T>(T newBattler, Vector3 position) where T : Battler
    {
        T b = Instantiate(newBattler, position, Quaternion.identity, (newBattler is BattleEnemy ? EnemyPartyDump : PlayerPartyDump));

        if (b.Class)
        {
            b.Class = Instantiate(b.Class, b.transform);
            b.Class.SetBattle(this);
        }

        if (b is BattleEnemy) b.StatConversion();

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
        int minSpeed = Battlers[Battlers.Count - 1].Spd;
        foreach (var b in Battlers)
        {
            b.SpriteSpeed = ((b.Spd - minSpeed) / (float)(maxSpeed - minSpeed)) * 4 + 3;
        }
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
            yield return BattleMenu.SelectedEscape ? Escape() : ExecuteAction();
        }
        else  // Battler AI
        {
            BattleMenu.RefreshPartyFrames();
            yield return new WaitForSeconds(1);
            if (ActingBattler is BattleAlly ally) ally.MakeDecision(FightingPlayerParty.ToList(), EnemyParty.Enemies);
            else if (ActingBattler is BattleEnemy enemy) enemy.MakeDecision(EnemyParty.Enemies, FightingPlayerParty.ToList());
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
        
        ActingBattler.Sprite.HandleTurnIndicators(true, false);
        NextActingBattler.Sprite.HandleTurnIndicators(false, true);
        return true;
    }

    private void ClearAllTurnIndicators()
    {
        foreach (var b in Battlers) b.Sprite.HandleTurnIndicators(false, false);
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
        if (ActingBattler.SelectedSingleMeeleeTarget)
        {
            RestrictBattlerWallCollision(false);
            var sp = ActingBattler.SelectedSingleMeeleeTarget.Sprite;
            ActingBattler.ApproachTarget(sp.ApproachPointLeft.position, sp.ApproachPointRight.position);
            yield return new WaitUntil(ActingBattler.HasApproachedTarget);
        }

        // Display action usage popup
        Skill skill = ActingBattler.SelectedAction as Skill;
        if (!skill?.Basic ?? true) StartCoroutine(BattleMenu.DisplayUsedAction(ActingBattler, ActingBattler.SelectedAction?.Name ?? string.Empty));

        // Use action
        ActingBattler.Sprite.Animation.SetTrigger(Battler.AnimParams.DoAction.ToString());
        if (ActingBattler.UsingBasicAttack)
        {
            if (ActingBattler.Class) ActingBattler.Class.UseBasicAttack(ActingBattler.SelectedWeapon);
            else ActingBattler.UseBasicAttack();
        }
        else if (skill)
        {
            skill.StartCharge();
            if (skill.ClassSkill) ActingBattler.Class.UseSkill();
            else ActingBattler.UseSkill();
        }
        else if (ActingBattler.SelectedAction is Item item)
        {
            if (ActingBattler.Class) ActingBattler.Class.UseItem(item);
            else ActingBattler.UseItem(item);
        }
    }

    public IEnumerator NotifyActionCompletion()
    {
        ActingBattler.ApproachForNextTurn();
        yield return new WaitUntil(ActingBattler.HasApproachedNextTurnDestination);
        ActionEnd();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of Action --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ActionEnd()
    {
        ActingBattler.ExecutedAction = true;
        (ActingBattler.SelectedAction as Skill)?.Charge1Turn();
        ResetSelectedTargets();
        RestrictBattlerWallCollision(true);
        FinishActionUsage();
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
            b.SelectedSingleMeeleeTarget = null;
            b.Select(false);
            b.LockSelectTrigger = false;
        }
    }

    private void FinishActionUsage()
    {
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
            BattleMenu.RefreshPartyFrames();
            if (EnemyParty.PartyMode != EnemyParty.EnemyPartyModes.FinalBoss) StartCoroutine(DeclareWin());
            else SceneMaster.EndBattle(PlayerParty);
            return true;
        }
        else if (PlayerPartyDefeated)
        {
            if (EnemyParty.GameOverOnLose) StartCoroutine(DeclareGameOver());
            else SceneMaster.EndBattle(PlayerParty);
            return true;
        }
        return false;
    }

    private bool EnemyPartyDefeated => EnemyParty.Enemies.All(x => x.KOd);
    private bool PlayerPartyDefeated => PlayerParty.Players.All(x => x.KOd) && (PlayerParty.Allies.Count == 0 || PlayerParty.Allies.All(x => x.KOd));

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- End of Battle --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public IEnumerator Escape()
    {
        ClearAll();
        RestrictBattlerWallCollision(false);
        yield return new WaitForSeconds(1);
        foreach (var b in Battlers) b.SetToEscapeMode(true, b is not BattleEnemy);
        yield return new WaitForSeconds(3);
        foreach (var e in EnemyParty.Enemies) e.SetToEscapeMode(false, false);
        yield return new WaitForSeconds(1);
        SceneMaster.EndBattle(PlayerParty);
    }

    private IEnumerator DeclareWin()
    {
        ClearAll();
        foreach (var b in FightingPlayerParty) b.Sprite.Animation.SetInteger(Battler.AnimParams.Victory.ToString(), 1);
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
        SceneMaster.ActivateStoredGameObjects();    // Return overworld
    }
}