using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class Battle : MonoBehaviour
{
    public enum BattleEndTypes { None, Win, BossWin, FinalWin, Lose, GameOver, Cut }

    public enum BattleStates { None, Menu, Action, Won, GameOver }

    // UI
    public BattleMenu BattleMenu;
    public BattleWin BattleWinMenu;

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
    private int ActingBattlerIndex;
    

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void Start()
    {
        SetupBackground();
        SetupPlayerParty();
        SetupEnemyParty();
        Battlers = new List<Battler>();
        foreach (BattlePlayer p in PlayerParty.Players) Battlers.Add(p);
        foreach (BattleAlly a in PlayerParty.Allies) Battlers.Add(a);
        foreach (BattleEnemy e in EnemyParty.Enemies) Battlers.Add(e);
    }

    void SetupBackground()
    {
        //
    }

    void SetupPlayerParty()
    {
        Transform playerPartySquare = GameObject.Find("/PlayerParty").transform;
        PlayerParty = Instantiate(BattleMaster.PlayerParty, gameObject.transform);
        PlayerParty.Players = SetupPlayerPositions(PlayerParty.Players);
        for (int i = 0; i < PlayerParty.Players.Count; i++)
        {
            BattlePlayer bpO = PlayerParty.Players[i];
            Vector3 bpPos = playerPartySquare.position + Positions[(int)bpO.RowPosition][(int)bpO.ColumnPosition];
            BattlePlayer bp = Instantiate(bpO, bpPos, Quaternion.identity, playerPartySquare);
            bp.AttackSkill = Instantiate(bp.AttackSkill, bp.transform);
            bp = InstantiateContents(bp);
            bp.transform.localScale = Vector3.one * 0.5f;
            bp.gameObject.SetActive(true);
            PlayerParty.Players[i] = bp;
        }
        PlayerParty.Allies = SetupAllyPositions(PlayerParty.Allies);
        for (int i = 0; i < PlayerParty.Allies.Count; i++)
        {
            BattleAlly baO = PlayerParty.Allies[i];
            Vector3 baPos = playerPartySquare.position + Positions[(int)baO.RowPosition][(int)baO.ColumnPosition];
            BattleAlly ba = Instantiate(baO, baPos, Quaternion.identity, playerPartySquare);
            ba = InstantiateContents(ba);
            ba.transform.position = new Vector3(ba.transform.position.x, ba.transform.position.y + 1.5f);
            ba.transform.localScale = Vector3.one * 0.5f;
            ba.gameObject.SetActive(true);
            ba.RemoveChoiceUI();
            PlayerParty.Allies[i] = ba;
        }
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

    void SetupEnemyParty()
    {
        Transform enemyPartySquare = GameObject.Find("/EnemyParty").transform;
        EnemyParty = Instantiate(BattleMaster.EnemyParty, gameObject.transform);
        EnemyParty.gameObject.SetActive(false);
        for (int i = 0; i < EnemyParty.Enemies.Count; i++)
        {
            BattleEnemy beO = EnemyParty.Enemies[i];
            Vector3 bePos = enemyPartySquare.position + Positions[(int)beO.RowPosition][(int)beO.ColumnPosition];
            BattleEnemy be = Instantiate(beO, bePos, Quaternion.identity, enemyPartySquare);
            be = InstantiateContents(be);
            be.transform.position = new Vector3(be.transform.position.x, be.transform.position.y + 1.5f);
            be.transform.localScale = Vector3.one * 0.5f;
            be.RemoveChoiceUI();
            EnemyParty.Enemies[i] = be;
        }
    }

    private T InstantiateContents<T>(T b) where T : Battler
    {
        for (int i = 0; i < b.SoloSkills.Count; i++)
        {
            b.SoloSkills[i] = Instantiate(b.SoloSkills[i], b.transform);
            b.SoloSkills[i].DisableForWarmup();
        }
        for (int i = 0; i < b.TeamSkills.Count; i++)
        {
            b.TeamSkills[i] = Instantiate(b.TeamSkills[i], b.transform);
            b.TeamSkills[i].DisableForWarmup();
        }
        for (int i = 0; i < b.Weapons.Count; i++) b.Weapons[i] = Instantiate(b.Weapons[i], b.transform);
        for (int i = 0; i < b.Items.Count; i++) b.Items[i] = Instantiate(b.Items[i], b.transform);
        for (int i = 0; i < b.PassiveSkills.Count; i++) b.PassiveSkills[i] = Instantiate(b.PassiveSkills[i], b.transform);
        for (int i = 0; i < b.States.Count; i++) b.States[i] = Instantiate(b.States[i], b.transform);
        b.StatBoosts = gameObject.AddComponent<Stats>(); // TEMP
        b.StatBoosts.transform.SetParent(b.transform);   // TEMP
        b.StatBoosts.SetToZero();                        // TEMP
        b.StatModifiers = gameObject.AddComponent<Stats>();
        b.StatModifiers.transform.SetParent(b.transform);
        return b;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Update()
    {
        if (Time.time <= BattleStateTime) return;
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
                if (ActingBattler.GetType().Name == "BattleEnemy") ActingBattler.ExecuteAction(EnemyPartyMembers, PlayerPartyMembers);
                else ActingBattler.ExecuteAction(PlayerPartyMembers, EnemyPartyMembers);
                Tool abt = ActingBattler.SelectedToolMove;
                if (abt)
                    BattleStateTime = Time.time + abt.ActionTime;
                break;

            case BattleStates.Won:
                BattleWinMenu.Setup();
                BattleState = BattleStates.None;
                break;

            case BattleStates.GameOver:
                //SetupGameOver();
                BattleState = BattleStates.None;
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Before action execution --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void TurnStartSetup()
    {
        ActingBattlerIndex = 0;
        Battlers = SortBattlersBySpeed(Battlers);
        ActingBattler = Battlers[ActingBattlerIndex];
        EndingAction = false;
        EndingTurn = false;
    }

    // Insertion sort: best algorithm for this, since it order very rarely changes after the first setup, giving an overall O(N) runtime.
    private List<Battler> SortBattlersBySpeed(List<Battler> battlers)
    {
        for (int i = ActingBattlerIndex; i < battlers.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int bj1 = battlers[j - 1].Spd();
                int bj2 = battlers[j].Spd();
                if (bj1 > bj2 || 
                    battlers[j - 1].ExecutedAction ||
                    bj1 == bj2 && Random.Range(0, 100) < 50)
                    continue;
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
            if (EnemyParty.BattleEndType == BattleEndTypes.GameOver) DeclareGameOver();
            //else if (EnemyParty.BattleEndType == BattleEndTypes.Lose) ;
            return true;
        }
        return false;
    }

    private bool EnemyPartyDefeated()
    {
        foreach (BattleEnemy e in EnemyParty.Enemies)
            if (!e.Unconscious) return false;
        return true;
    }

    private bool PlayerPartyDefeated()
    {
        foreach (BattlePlayer p in PlayerParty.Players)
            if (!p.Unconscious) return false;
        foreach (BattleAlly a in PlayerParty.Allies)
            if (!a.Unconscious) return false;
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
        BattleStateTime = Time.time + 1f;
    }

    private void DeclareGameOver()
    {
        BattleState = BattleStates.GameOver;
        BattleStateTime = Time.time + 0.5f;
        Debug.Log("YOU LOSE");
    }
}