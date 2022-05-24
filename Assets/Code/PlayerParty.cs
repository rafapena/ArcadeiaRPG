using System.Collections.Generic;
using System.Linq;
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
    public List<BattlePlayer> AllPlayers;
    public List<BattleAlly> Allies;

    // Menu Items
    public InventorySystem Inventory;
    public InventorySystem Storage;
    public List<Objective> LoggedObjectives;

    // Collected craftable items/weapons
    public List<Item> CraftableItems;
    public List<Weapon> CraftableWeapons;
    public List<Accessory> CraftableAccessories;

    // Relations
    [HideInInspector] public List<PlayerRelation> Relations = new List<PlayerRelation>();
    public List<PlayerRelation> PreExistingRelations;

    public List<BattlePlayer> Players => AllPlayers.Take(MAX_NUMBER_OF_PLAYABLE_BATTLERS).ToList();

    public List<Battler> BattlingParty => Players.Cast<Battler>().Concat(Allies).ToList();

    public List<Battler> WholeParty => AllPlayers.Cast<Battler>().Concat(Allies).ToList();

    private void Start()
    {
        //
    }

    public void Setup()
    {
        SetupExpCurve();
        SetupRelations();
    }
   
    /*public void UpdateActivePlayers()
    {
        Players = new List<BattlePlayer>();
        int inActivePartyCount = AllPlayers.Count < MAX_NUMBER_OF_PLAYABLE_BATTLERS ? AllPlayers.Count : MAX_NUMBER_OF_PLAYABLE_BATTLERS;
        for (int i = 0; i < inActivePartyCount; i++)
            Players.Add(AllPlayers[i]);
    }*/

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
        Relations.Clear();
        for (int i = 0; i < AllPlayers.Count; i++)
        {
            for (int j = i + 1; j < AllPlayers.Count; j++) Relations.Add(new PlayerRelation(AllPlayers[i], AllPlayers[j]));
        }
        foreach (PlayerRelation per in PreExistingRelations) GetRelation(per.Player1, per.Player2).SetLevel(per.Level);
    }

    public PlayerRelation GetRelation(BattlePlayer p1, BattlePlayer p2)
    {
        IEnumerable<PlayerRelation> r = Relations.Where(x => x.PlayerInRelation(p1) && x.PlayerInRelation(p2));
        return r.Any() ? r.First() : default;
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
        Allies = new List<BattleAlly>();
        int playerIndexCap = PlayerPrefs.GetInt("NumberOfPlayers");
        int allyIndexCap = PlayerPrefs.GetInt("NumberOfAllies");
        for (int i = 0; i < playerIndexCap; i++)
        {
            BattlePlayer b = LoadBattler(file, mp, ResourcesMaster.Players, i);
            AllPlayers.Add(b);
        }
        for (int i = playerIndexCap; i < allyIndexCap; i++) LoadBattler(file, mp, ResourcesMaster.Allies, i);
        SetupExpCurve();
        LoadRelations(file);
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
        if (b is BattlePlayer p)
        {
            p.PermanentStatsBoosts.SetFromString(PlayerPrefs.GetString(bt + "PStats_" + file));
            int sw = PlayerPrefs.GetInt(bt + "SelectedWeapon_" + file);
            p.SelectedWeapon = sw >= 0 ? p.Weapons.Find(x => x.Id == sw) : null;
            p.Weapons = LoadBattlersList(file, b, ResourcesMaster.Weapons, bt);
        }
        b.StatConversion();
        b.AddHP(PlayerPrefs.GetInt(bt + "HP_" + file) - b.MaxHP);
        b.AddSP(PlayerPrefs.GetInt(bt + "SP_" + file) - BattleMaster.SP_CAP);
        b.States = LoadBattlersList(file, b, ResourcesMaster.States, bt);
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

    private void LoadRelations(int file)
    {
        Relations = new List<PlayerRelation>(PlayerPrefs.GetInt("RelationsCount_" + file));
        for (int i = 0; i < Relations.Count; i++)
        {
            string[] pString = PlayerPrefs.GetString("Relation" + i + "_" + file).Split('_');
            BattlePlayer p1 = ResourcesMaster.Players.Where(x => x.Id == int.Parse(pString[0])).Single();
            BattlePlayer p2 = ResourcesMaster.Players.Where(x => x.Id == int.Parse(pString[1])).Single();
            Relations[i] = new PlayerRelation(p1, p2);
            Relations[i].SetPoints(int.Parse(pString[2]));
        }
    }

    private void LoadInventory(int file)
    {
        string inv = "Inventory";
        Inventory.WeightCapacity = PlayerPrefs.GetInt("InventoryCapacity_" + file);
        LoadToolsList(file, ResourcesMaster.Items, inv, Inventory);
        LoadToolsList(file, ResourcesMaster.Weapons, inv, Inventory);
        LoadToolsList(file, ResourcesMaster.Accessories, inv, Inventory);
    }

    private void LoadStorage(int file)
    {
        string str = "Storage";
        LoadToolsList(file, ResourcesMaster.Items, str, Storage);
        LoadToolsList(file, ResourcesMaster.Weapons, str, Storage);
        LoadToolsList(file, ResourcesMaster.Accessories, str, Storage);
    }

    private void LoadToolsList<T>(int file, T[] list, string pre, InventorySystem system) where T : IToolForInventory
    {
        for (int i = 0; i < list.Length; i++)
        {
            int quantity = PlayerPrefs.GetInt(pre + typeof(T).Name + list[i].Info.Id + "_" + file);
            Item it = list[i] as Item;
            Weapon wp = list[i] as Weapon;
            if (it) system.Add(it, quantity);
            else if (wp) system.Add(wp, quantity);
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
        foreach (BattlePlayer p in AllPlayers)
        {
            p.Level++;
            p.StatConversion();
            p.Skills.Clear();
            p.AddLearnedSkills();
        }
    }
}
