using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveData
{
    public bool FileDataExists { get; private set; }

    public int File;
    public string PlayerName;

    public int Chapter;
    public int Level;
    public int Difficulty;
    public int TotalPlayTime;
    public string Location;
    public int Gold;

    public bool CurrentlyWritingData { get; private set; }

    public SaveData(int file)
    {
        File = file;
        FileDataExists = PlayerPrefs.HasKey("File_" + File);
        if (!FileDataExists) return;
        PlayerName = PlayerPrefs.GetString("PlayerName_" + File);
        Chapter = PlayerPrefs.GetInt("Chapter_" + File);
        Level = PlayerPrefs.GetInt("Level_" + File);
        Difficulty = PlayerPrefs.GetInt("Difficulty_" + File);
        TotalPlayTime = PlayerPrefs.GetInt("TotalPlayTime_" + File);
        Location = PlayerPrefs.GetString("Location_" + File);
        Gold = PlayerPrefs.GetInt("Gold_" + File);
    }

    public void SaveGame()
    {
        CurrentlyWritingData = true;
        GameplayMaster.SetLastManagedFile(File);
        DeleteParty();
        DeleteMapContent();
        PlayerPrefs.SetInt("File_" + File, File);
        PlayerPrefs.SetString("PlayerName_" + File, GameplayMaster.Party.AllPlayers[0].Name);
        PlayerPrefs.SetInt("Chapter_" + File, GameplayMaster.Chapter);
        PlayerPrefs.SetInt("Level_" + File, GameplayMaster.Party.Level);
        PlayerPrefs.SetInt("Difficulty_" + File, (int)GameplayMaster.Difficulty);
        PlayerPrefs.SetInt("TotalPlayTime_" + File, (int)GameplayMaster.TotalPlayTime);
        PlayerPrefs.SetString("Location_" + File, MapMaster.CurrentLocation);
        PlayerPrefs.SetInt("Gold_" + File, GameplayMaster.Party.Inventory.Gold);
        SaveParty();
        SaveMapContent();
        PlayerPrefs.SetString("SceneName", MapMaster.SceneName);
        CurrentlyWritingData = false;
    }

    public void LoadGame()
    {
        GameplayMaster.ResetLoad();
        GameplayMaster.SetLastManagedFile(File);
        GameplayMaster.Chapter = Chapter;
        GameplayMaster.Difficulty = (GameplayMaster.Difficulties)Difficulty;
        GameplayMaster.TotalPlayTime = TotalPlayTime;
        SceneMaster.ChangeScene(PlayerPrefs.GetString("SceneName"), 2f);
    }

    public void DeleteGame()
    {
        CurrentlyWritingData = true;
        PlayerPrefs.DeleteKey("File_" + File);
        DeleteParty();
        CurrentlyWritingData = false;
    }

    public static void SetupForNewGame()
    {
        GameplayMaster.Chapter = 0;
        GameplayMaster.TotalPlayTime = 0;
        GameplayMaster.SetInGameTime(20, 0);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Party --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Loading can be found in MapPlayer.cs
    private void SaveParty()
    {
        PlayerParty p = GameplayMaster.Party;
        PlayerPrefs.SetInt("PartyEXP_" + File, p.EXP);
        PlayerPrefs.SetInt("PartyLastEXPToNext_" + File, p.LastEXPToNext);
        PlayerPrefs.SetInt("PartyEXPToNext_" + File, p.EXPToNext);
        int i = 0;
        foreach (BattlePlayer bp in p.AllPlayers) SaveBattler(i++, bp);
        int numberOfPlayers = i;
        PlayerPrefs.SetInt("NumberOfPlayers", numberOfPlayers);
        foreach (BattleAlly ally in p.Allies) SaveBattler(i++, ally);
        PlayerPrefs.SetInt("NumberOfAllies", i - numberOfPlayers);
        SaveRelations();
        SaveInventory();
        SaveObjectives();
    }

    private void SaveBattler(int index, Battler b)
    {
        string bt = "Battler" + index;
        PlayerPrefs.SetInt(bt + "Id_" + File, b.Id);
        if (b is BattlePlayer p)
        {
            PlayerPrefs.SetString(bt + "PStats_" + File, p.PermanentStatsBoosts.GetToString());
            SaveBattlersList(p.Weapons, bt);
            if (p.Weapons.Count > 0) PlayerPrefs.SetInt(bt + "SelectedWeapon_" + File, p.SelectedWeapon?.Id ?? -1);
        }
        PlayerPrefs.SetInt(bt + "HP_" + File, b.HP);
        PlayerPrefs.SetInt(bt + "SP_" + File, b.SP);
        SaveBattlersList(b.Accessories, bt);
        SaveBattlersList(b.States, bt);
    }

    private void SaveBattlersList<T>(List<T> list, string pre) where T : BaseObject
    {
        int i = 0;
        foreach (T entry in list) PlayerPrefs.SetInt(pre + typeof(T).Name + i++ + "_" + File, entry.Id);
        PlayerPrefs.SetInt(pre + typeof(T).Name + "Count_" + File, i);
    }

    private void SaveRelations()
    {
        int i = 0;
        foreach (PlayerRelation pr in GameplayMaster.Party.Relations) PlayerPrefs.SetString("Relation" + i++ + "_" + File, pr.Player1.Id + "_" + pr.Player2.Id + "_" + pr.Points);
        PlayerPrefs.SetInt("RelationsCount" + "_" + File, GameplayMaster.Party.Relations.Count);
    }

    private void SaveInventory()
    {
        string inv = "Inventory";
        string str = "Storage";
        PlayerPrefs.SetInt("InventoryCapacity_" + File, GameplayMaster.Party.Inventory.WeightCapacity);
        SaveToolsList(GameplayMaster.Party.Inventory.Items, inv);
        SaveToolsList(GameplayMaster.Party.Inventory.Weapons, inv);
        SaveToolsList(GameplayMaster.Party.Inventory.Accessories, inv);
        SaveToolsList(GameplayMaster.Party.Storage.Items, str);
        SaveToolsList(GameplayMaster.Party.Storage.Weapons, str);
        SaveToolsList(GameplayMaster.Party.Storage.Accessories, str);
    }

    private void SaveToolsList<T>(List<T> list, string pre) where T : IToolForInventory
    {
        foreach (T entry in list) PlayerPrefs.SetInt(pre + typeof(T).Name + entry.Info.Id + "_" + File, entry.Quantity);
    }

    private void SaveObjectives()
    {
        int markedObjective = 0;
        int markedSubObjective = 0;
        foreach (Objective o in GameplayMaster.Party.LoggedObjectives)
        {
            if (o.Marked) markedObjective = o.Id;
            PlayerPrefs.SetInt("Objective" + o.Id + "_" + File, o.Cleared ? 2 : 1);
            foreach (SubObjective so in o.NextObjectives)
            {
                if (so.Marked) markedSubObjective = so.Id;
                int soIndex = 1;
                if (so.Cleared) soIndex = 2;
                else if (so.Hidden) soIndex = 3;
                PlayerPrefs.SetInt("SubObjective" + so.Id + "_" + o.Id + "_" + File, soIndex);
            }
        }
        PlayerPrefs.SetInt("MarkedObjective_" + File, markedObjective);
        PlayerPrefs.SetInt("MarkedSubObjective_" + File, markedSubObjective);
    }

    private void DeleteParty()
    {
        PlayerPrefs.DeleteKey("NumberOfPlayers");
        PlayerPrefs.DeleteKey("NumberOfAllies");
        for (int i = 0; i < ResourcesMaster.Players.Length; i++) DeleteBattler(i);
        for (int i = 0; i < ResourcesMaster.Allies.Length; i++) DeleteBattler(i);
        DeleteInventory();
        DeleteStorage();
        DeleteObjectives();
    }

    private void DeleteBattler(int index)
    {
        string bt = "Battler" + index;
        PlayerPrefs.DeleteKey(bt + "Id_" + File);
        PlayerPrefs.DeleteKey(bt + "HP_" + File);
        PlayerPrefs.DeleteKey(bt + "SP_" + File);
        DeleteBattlerList<Skill>(bt);
        DeleteBattlerList<Weapon>(bt);
        DeleteBattlerList<Accessory>(bt);
        DeleteBattlerList<Item>(bt);
        DeleteBattlerList<State>(bt);
        for (int i = 0; i < ResourcesMaster.Players.Length; i++) PlayerPrefs.DeleteKey(bt + "Relation" + i + "_" + File);
    }

    private void DeleteBattlerList<T>(string pre)
    {
        PlayerPrefs.DeleteKey(pre + typeof(T).Name + "Count_" + File);
        for (int i = 0; i < int.MaxValue; i++)
        {
            string str = pre + typeof(T).Name + i++ + "_" + File;
            if (!PlayerPrefs.HasKey(str)) break;
            PlayerPrefs.DeleteKey(str);
        }
    }

    private void DeleteInventory()
    {
        string inv = "Inventory";
        PlayerPrefs.DeleteKey("InventoryCapacity_" + File);
        DeleteToolList(ResourcesMaster.Items, inv);
        DeleteToolList(ResourcesMaster.Weapons, inv);
    }

    private void DeleteStorage()
    {
        string str = "Storage";
        DeleteToolList(ResourcesMaster.Items, str);
        DeleteToolList(ResourcesMaster.Weapons, str);
    }

    private void DeleteToolList<T>(T[] list, string pre) where T : IToolForInventory
    {
        foreach (T entry in list) PlayerPrefs.DeleteKey(pre + typeof(T).Name + entry.Info.Id + "_" + File);
    }

    private void DeleteObjectives()
    {
        PlayerPrefs.DeleteKey("MarkedObjective_" + File);
        PlayerPrefs.DeleteKey("MarkedSubObjective_" + File);
        foreach (Objective o in ResourcesMaster.Objectives)
        {
            PlayerPrefs.DeleteKey("Objective" + o.Id + "_" + File);
            foreach (SubObjective so in o.NextObjectives) PlayerPrefs.DeleteKey("SubObjective" + so.Id + "_" + o.Id + "_" + File);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Map --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SaveMapContent()
    {
        //
    }

    private void DeleteMapContent()
    {
        //
    }

    private void LoadMapContent()
    {
        //
    }
}