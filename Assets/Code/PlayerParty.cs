using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;

public class PlayerParty : MonoBehaviour
{
    // Constants
    public readonly int MAX_NUMBER_OF_PLAYABLE_BATTLERS = 4;

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
    public InventorySystem Storage;
    public List<Objective> LoggedObjectives;

    [HideInInspector] public float PreemptiveAttackChance;
    

    private void Start()
    {
        //
    }

    public void Setup()
    {
        UpdateActivePlayers();
        SetupExpCurve();
        SetupRelations();
    }
   
    public void UpdateActivePlayers()
    {
        Players = new List<BattlePlayer>();
        int inActivePartyCount = AllPlayers.Count < MAX_NUMBER_OF_PLAYABLE_BATTLERS ? AllPlayers.Count : MAX_NUMBER_OF_PLAYABLE_BATTLERS;
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
        EXP = LastEXPToNext;
    }

    private void SetupRelations()
    {
        bool[] playerInParty = new bool[ResourcesMaster.Players.Length];
        foreach (BattlePlayer p in AllPlayers)
        {
            playerInParty[p.Id] = true;
        }
        foreach (BattlePlayer p in AllPlayers)
        {
            p.Relations = new List<PlayerRelation>();
            for (int j = 0; j < ResourcesMaster.Players.Length; j++) p.Relations.Add(null);
            foreach (PreExistingRelation pr in p.PreExistingRelations)
            {
                if (playerInParty[pr.Player.Id]) p.Relations[pr.Player.Id] = new PlayerRelation(pr.Player, pr.RelationLevel);
            }
        }  
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Loading File --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void LoadFromFile(int file, MapPlayer mp)
    {
        EXP = PlayerPrefs.GetInt("PartyEXP_" + file);
        LastEXPToNext = PlayerPrefs.GetInt("PartyLastEXPToNext_" + file);
        EXPToNext = PlayerPrefs.GetInt("PartyEXPToNext_" + file);
        AllPlayers = new List<BattlePlayer>();
        Players = new List<BattlePlayer>();
        Allies = new List<BattleAlly>();
        int playerIndexCap = PlayerPrefs.GetInt("NumberOfPlayers");
        int allyIndexCap = PlayerPrefs.GetInt("NumberOfAllies");
        for (int i = 0; i < playerIndexCap; i++)
        {
            BattlePlayer b = LoadBattler(file, mp, ResourcesMaster.Players, i);
            if (i < MAX_NUMBER_OF_PLAYABLE_BATTLERS) Players.Add(b);
            AllPlayers.Add(b);
        }
        for (int i = playerIndexCap; i < allyIndexCap; i++) LoadBattler(file, mp, ResourcesMaster.Allies, i);
        UpdateAll(GetWholeParty());
        LoadInventory(file);
        LoadStorage(file);
        LoadObjectives(file, mp);
        GameplayMaster.DeclareContentLoaded();
    }

    private T LoadBattler<T>(int file, MapPlayer mp, T[] list, int index) where T : Battler
    {
        string bt = "Battler" + index;
        Battler b = Instantiate(list[PlayerPrefs.GetInt(bt + "Id_" + file)], mp.BattlersListDump);
        b.Level = Level;
        b.StatConversion();
        b.HP = PlayerPrefs.GetInt(bt + "HP_" + file);
        b.SP = PlayerPrefs.GetInt(bt + "SP_" + file);
        b.SoloSkills = LoadBattlersList(file, b, ResourcesMaster.SoloSkills, bt);
        b.TeamSkills = LoadBattlersList(file, b, ResourcesMaster.TeamSkills, bt);
        b.Weapons = LoadBattlersList(file, b, ResourcesMaster.Weapons, bt);
        b.Items = LoadBattlersList(file, b, ResourcesMaster.Items, bt);
        b.PassiveSkills = LoadBattlersList(file, b, ResourcesMaster.PassiveSkills, bt);
        b.States = LoadBattlersList(file, b, ResourcesMaster.States, bt);
        string sw = bt + "SelectedWeapon_" + file;
        if (PlayerPrefs.HasKey(sw)) b.SelectedWeapon = b.Weapons.Find(x => x.Id == PlayerPrefs.GetInt(sw));
        BattlePlayer p = b as BattlePlayer;
        if (p)
        {
            p.Relations = new List<PlayerRelation>(ResourcesMaster.Players.Length);
            for (int i = 0; i < p.Relations.Count; i++)
            {
                string str = bt + "Relation" + i + "_" + file;
                if (PlayerPrefs.HasKey(str)) p.Relations[i].SetPoints(PlayerPrefs.GetInt(str));
            }
        }
        b.gameObject.SetActive(false);
        return b as T;
    }

    private List<T> LoadBattlersList<T>(int file, Battler b, T[] list, string pre) where T : BaseObject
    {
        List<T> destList = new List<T>();
        int listSize = PlayerPrefs.GetInt(pre + typeof(T).Name + "Count_" + file);
        for (int i = 0; i < listSize; i++)
        {
            int id = PlayerPrefs.GetInt(pre + typeof(T).Name + i + "_" + file);
            T obj = Instantiate(list[id], b.transform);
            destList.Add(obj);
        }
        return destList;
    }

    private void LoadInventory(int file)
    {
        string inv = "Inventory";
        Inventory.WeightCapacity = PlayerPrefs.GetInt("InventoryCapacity_" + file);
        LoadToolsList(file, ResourcesMaster.Items, inv, Inventory);
        LoadToolsList(file, ResourcesMaster.Weapons, inv, Inventory);
    }

    private void LoadStorage(int file)
    {
        string str = "Storage";
        LoadToolsList(file, ResourcesMaster.Items, str, Storage);
        LoadToolsList(file, ResourcesMaster.Weapons, str, Storage);
    }

    private void LoadToolsList<T>(int file, T[] list, string pre, InventorySystem system) where T : ToolForInventory
    {
        for (int i = 0; i < list.Length; i++)
        {
            int quantity = PlayerPrefs.GetInt(pre + typeof(T).Name + list[i].Id + "_" + file);
            Item it = list[i] as Item;
            Weapon wp = list[i] as Weapon;
            if (it)
            {
                for (int j = 0; j < quantity; j++) system.AddItem(it);
            }
            else if (wp)
            {
                for (int j = 0; j < quantity; j++) system.AddWeapon(wp);
            }
        }
    }

    private void LoadObjectives(int file, MapPlayer mp)
    {
        int markedObjective = PlayerPrefs.GetInt("MarkedObjective_" + file);
        int markedSubObjective = PlayerPrefs.GetInt("MarkedSubObjective_" + file);
        foreach (Objective obj in ResourcesMaster.Objectives)
        {
            int x = PlayerPrefs.GetInt("Objective" + obj.Id + "_" + file);
            if (x <= 0) continue;
            Objective o = Instantiate(obj, mp.ObjectivesListDump);
            if (x == 2) o.Cleared = true;
            else if (o.Id == markedObjective) o.Marked = true;
            foreach (SubObjective so in o.NextObjectives)
            {
                int y = PlayerPrefs.GetInt("SubObjective" + so.Id + "_" + o.Id + "_" + file);
                if (y == 2) so.Cleared = true;
                else if (y == 3) so.Hidden = true;
                if (o.Id == markedObjective && so.Id == markedSubObjective) so.Marked = true;
            }
            LoggedObjectives.Add(o);
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

    public PlayerRelation GetCompanionshipInfo(BattlePlayer p1, BattlePlayer p2)
    {
        foreach (PlayerRelation pc in p1.Relations)
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
        UpdateActivePlayers();
        SetupExpCurve();
        SetupRelations();
    }
}
