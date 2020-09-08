using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;

public class PlayerParty : MonoBehaviour
{
    // Constants
    public readonly int MAX_NUMBER_OF_ACTIVE_PLAYERS = 4;

    // General
    public int Level;
    [HideInInspector] public int EXP;
    public int LastEXPToNext;
    public int EXPToNext;
    private int[] LevelCurves;

    // Party formation
    public List<BattlePlayer> Players;      // Active players in battle
    public List<BattlePlayer> AllPlayers;   // Includes reserve players
    public List<BattleAlly> Allies;

    // Menu Items
    public InventorySystem Inventory;
    public List<Objective> LoggedObjectives;

    [HideInInspector] public float PreemptiveAttackChance;
    

    private void Start()
    {
        UpdateActivePlayers();
        SetupExpCurve();
        SetupCompanionships();
    }
   
    public void UpdateActivePlayers()
    {
        Players = new List<BattlePlayer>();
        int inActivePartyCount = AllPlayers.Count < MAX_NUMBER_OF_ACTIVE_PLAYERS ? AllPlayers.Count : MAX_NUMBER_OF_ACTIVE_PLAYERS;
        for (int i = 0; i < inActivePartyCount; i++)
            Players.Add(AllPlayers[i]);
    }

    public void SetupExpCurve()
    {
        LevelCurves = new int[100];
        LevelCurves[0] = 0;
        for (int i = 1; i < LevelCurves.Length; i++)
            LevelCurves[i] = LevelCurves[i - 1] + i * 100;
        LastEXPToNext = Level <= 1 ? 0 : LevelCurves[Level - 1];
        EXPToNext = LevelCurves[Level];
        EXP = LastEXPToNext + 10;
    }

    private void SetupCompanionships()
    {
        for (int i = 0; i < AllPlayers.Count; i++)
        {
            for (int j = 0; j < AllPlayers.Count; j++)
            {
                if (i == j)
                {
                    AllPlayers[i].Relations.Add(null);
                    continue;
                }
                PlayerCompanionship pc = gameObject.AddComponent<PlayerCompanionship>();
                pc.Player = AllPlayers[j];
                AllPlayers[i].Relations.Add(pc);
            }
        }  
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Level Up --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void LevelUp()
    {
        Level++;
        LastEXPToNext = EXPToNext;
        EXPToNext = LevelCurves[Level];
        LevelUpStatBoosts(ref AllPlayers);
        LevelUpLearnedSkills(ref AllPlayers);
    }

    private void LevelUpStatBoosts(ref List<BattlePlayer> players)
    {
        foreach (BattlePlayer p in players)
        {
            p.Level++;
        }
    }
    
    private void LevelUpLearnedSkills(ref List<BattlePlayer> players)
    {

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Companionship Info --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public PlayerCompanionship GetCompanionshipInfo(BattlePlayer p1, BattlePlayer p2)
    {
        foreach (PlayerCompanionship pc in p1.Relations)
            if (pc.Player.Id == p2.Id)
                return pc;
        return null;    // Function should never actually go here: Indicates a malformed setup, otherwise
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Combining Lists --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public List<Battler> GetBattlingParty()
    {
        List<Battler> battlingParty = new List<Battler>();
        battlingParty.AddRange(Players);
        battlingParty.AddRange(Allies);
        return battlingParty;
    }

    public List<Battler> GetWholeParty()
    {
        List<Battler> wholeParty = new List<Battler>();
        foreach (Battler b in AllPlayers) wholeParty.Add(b);
        foreach (Battler a in Allies) wholeParty.Add(a);
        return wholeParty;
    }

    public void UpdateAll(List<Battler> all)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (i < AllPlayers.Count) AllPlayers[i] = all[i] as BattlePlayer;
            else Allies[i - AllPlayers.Count] = all[i] as BattleAlly;
        }
    }
}
