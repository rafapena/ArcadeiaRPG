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

    public SaveData(int file)
    {
        File = file;
        FileDataExists = PlayerPrefs.GetInt("File_" + File) > 0;
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
        GameplayMaster.SetLastManagedFile(File);
        PlayerPrefs.SetInt("File_" + File, File);
        PlayerPrefs.SetString("PlayerName_" + File, MenuMaster.PartyInfo.AllPlayers[0].Name);
        PlayerPrefs.SetInt("Chapter_" + File, GameplayMaster.Chapter);
        PlayerPrefs.SetInt("Level_" + File, MenuMaster.PartyInfo.Level);
        PlayerPrefs.SetInt("Difficulty_" + File, (int)GameplayMaster.Difficulty);
        PlayerPrefs.SetInt("TotalPlayTime_" + File, (int)GameplayMaster.TotalPlayTime);
        PlayerPrefs.SetString("Location_" + File, MapMaster.CurrentLocation);
        PlayerPrefs.SetInt("Gold_" + File, MenuMaster.PartyInfo.Inventory.Gold);
        //SaveParty();
        PlayerPrefs.SetString("SceneName", MapMaster.SceneName);
    }

    public void LoadGame()
    {
        GameplayMaster.SetLastManagedFile(File);
        GameplayMaster.Chapter = Chapter;
        GameplayMaster.Difficulty = (GameplayMaster.Difficulties)Difficulty;
        GameplayMaster.TotalPlayTime = TotalPlayTime;
        LoadParty();
        SceneMaster.ChangeScene(PlayerPrefs.GetString("SceneName"), 2f);
    }

    public void DeleteGame()
    {
        PlayerPrefs.DeleteKey("File_" + File);
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

    private void SaveParty()
    {
        PlayerParty p = MenuMaster.PartyInfo;
        PlayerPrefs.SetInt("PartyEXP_" + File, p.EXP);
        PlayerPrefs.SetInt("PartyLastEXPToNext_" + File, p.LastEXPToNext);
        PlayerPrefs.SetInt("PartyEXPToNext_" + File, p.EXPToNext);
        int i = 0;
        foreach (BattlePlayer bp in p.AllPlayers) SavePlayer(i++, bp);
        foreach (BattleAlly ally in p.Allies) SavePlayer(i++, ally);
    }

    private void LoadParty()
    {
        //PlayerParty p = MenuMaster.PartyInfo;
        //p.EXP = PlayerPrefs.GetInt("");
    }

    private void SavePlayer(int index, Battler b)
    {
        string bt = "Battler" + index;
        PlayerPrefs.SetInt(bt + "Id_" + File, b.Id);
        PlayerPrefs.SetInt(bt + "HP_" + File, b.HP);
        PlayerPrefs.SetInt(bt + "SP_" + File, b.SP);
        SavePlayerAttributesList(b.SoloSkills, bt);
        SavePlayerAttributesList(b.TeamSkills, bt);
        SavePlayerAttributesList(b.Weapons, bt);
        SavePlayerAttributesList(b.Items, bt);
        SavePlayerAttributesList(b.PassiveSkills, bt);
        SavePlayerAttributesList(b.States, bt);
        BattlePlayer p = b as BattlePlayer;
        if (!p) return;
        for (int i = 0; i < p.Relations.Count; i++)
        {
            int r = p.Relations[i] ? p.Relations[i].Points : -1;
            PlayerPrefs.SetInt(bt + "Relation" + i + "_" + File, r);
        }
        SaveInventory();
        SaveObjectives();
    }

    private void LoadPlayer()
    {
        //
    }

    private void SavePlayerAttributesList<T>(List<T> list, string pre) where T : BaseObject
    {
        int i = 0;
        foreach (T entry in list) PlayerPrefs.SetInt(pre + typeof(T).Name + i + "_" + File, entry.Id);
    }

    private void LoadPlayerAttributesList()
    {
        //
    }

    private void SaveInventory()
    {
        string inv = "Inventory";
        PlayerPrefs.SetInt("InventoryCapacity_" + File, MenuMaster.PartyInfo.Inventory.WeightCapacity);
        SaveToolsList(MenuMaster.PartyInfo.Inventory.Items, inv);
        SaveToolsList(MenuMaster.PartyInfo.Inventory.Weapons, inv);
        SaveToolsList(MenuMaster.PartyInfo.Inventory.KeyItems, inv);
    }

    private void LoadInventory()
    {
        //
    }

    private void SaveToolsList<T>(List<T> list, string pre) where T : ToolForInventory
    {
        foreach (T entry in list) PlayerPrefs.SetInt(pre + typeof(T).Name + entry.Id + "_" + File, entry.Quantity);
    }

    private void LoadToolsList()
    {
        //
    }

    private void SaveObjectives()
    {
        int markedObjective = 0;
        int markedSubObjective = 0;
        foreach (Objective o in MenuMaster.PartyInfo.LoggedObjectives)
        {
            if (o.Marked) markedObjective = o.Id;
            PlayerPrefs.SetInt("Objective" + o.Id + "_" + File, o.Cleared ? 1 : 0);
            foreach (SubObjective so in o.NextObjectives)
            {
                if (so.Marked) markedSubObjective = so.Id;
                int soIndex = 0;
                if (so.Cleared) soIndex = 1;
                else if (so.Hidden) soIndex = 2;
                PlayerPrefs.SetInt("SubObjective" + so.Id + "_" + o.Id + "_" + File, soIndex);
            }
        }
        PlayerPrefs.SetInt("MarkedObjective_" + File, markedObjective);
        PlayerPrefs.SetFloat("MarkedSubObjective_" + File, markedSubObjective);
    }

    private void LoadObjectives()
    {
        //
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Map --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}