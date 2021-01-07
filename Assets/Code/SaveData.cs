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

    public int FileNumber;
    public string PlayerName;

    public int Chapter;
    public int Level;
    public int Difficulty;
    public int TotalPlayTime;
    public string Location;
    public int Gold;

    public int InGameDay;
    public int InGameTime;
    public int[] GameVars;

    public SaveData(int file)
    {
        FileNumber = file;
        FileDataExists = PlayerPrefs.GetInt("File_" + file) > 0;
        if (!FileDataExists) return;
        PlayerName = PlayerPrefs.GetString("PlayerName_" + file);
        Chapter = PlayerPrefs.GetInt("Chapter_" + file);
        Level = PlayerPrefs.GetInt("Level_" + file);
        Difficulty = PlayerPrefs.GetInt("Difficulty_" + file);
        TotalPlayTime = PlayerPrefs.GetInt("TotalPlayTime_" + file);
        Location = PlayerPrefs.GetString("Location_" + file);
        Gold = PlayerPrefs.GetInt("Gold_" + file);
    }

    public void SaveGame()
    {
        int file = FileNumber;
        GameplayMaster.SetLastManagedFile(file);
        PlayerPrefs.SetInt("File_" + file, file);
        PlayerPrefs.SetString("PlayerName_" + file, MenuMaster.PartyInfo.AllPlayers[0].Name);
        PlayerPrefs.SetInt("Chapter_" + file, GameplayMaster.Chapter);
        PlayerPrefs.SetInt("Level_" + file, MenuMaster.PartyInfo.Level);
        PlayerPrefs.SetInt("Difficulty_" + file, (int)GameplayMaster.Difficulty);
        PlayerPrefs.SetInt("TotalPlayTime_" + file, (int)GameplayMaster.TotalPlayTime);
        PlayerPrefs.SetString("Location_" + file, MapMaster.CurrentLocation);
        PlayerPrefs.SetInt("Gold_" + file, MenuMaster.PartyInfo.Inventory.Gold);
    }

    public void LoadGame()
    {
        GameplayMaster.SetLastManagedFile(FileNumber);
        GameplayMaster.Chapter = Chapter;
        GameplayMaster.Difficulty = (GameplayMaster.Difficulties)Difficulty;
        GameplayMaster.TotalPlayTime = TotalPlayTime;
        SceneMaster.ChangeScene(Location.Replace(" ", "_"), 2f);
    }

    public void DeleteGame()
    {
        PlayerPrefs.DeleteKey("File_" + FileNumber);
    }

    public static void SetupForNewGame()
    {
        GameplayMaster.Chapter = 0;
        GameplayMaster.TotalPlayTime = 0;
        GameplayMaster.SetInGameTime(20, 0);
    }
}