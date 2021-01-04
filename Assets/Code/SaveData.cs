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
    public bool FileExists { get; private set; }

    public int FileNumber;
    public string PlayerName;

    public int Chapter;
    public int Level;
    public int Difficulty;
    public int TotalPlaytimeHours;
    public int TotalPlaytimeMinutes;
    public float TotalPlaytimeSeconds;
    public string Location;
    public int Gold;

    public int[] GameVars;

    public SaveData(int i)
    {
        FileNumber = PlayerPrefs.GetInt("File" + i);
        FileExists = FileNumber > 0;
        if (!FileExists) return;
        PlayerName = PlayerPrefs.GetString("PlayerName" + i);
        Chapter = PlayerPrefs.GetInt("Chapter" + i);
        Level = PlayerPrefs.GetInt("Level" + i);
        Difficulty = PlayerPrefs.GetInt("Difficulty" + i);
        TotalPlaytimeHours = PlayerPrefs.GetInt("TotalPlaytimeHours" + i);
        TotalPlaytimeMinutes = PlayerPrefs.GetInt("TotalPlaytimeMinutes" + i);
        TotalPlaytimeSeconds = PlayerPrefs.GetFloat("TotalPlaytimeSeconds" + i);
        Location = PlayerPrefs.GetString("Location" + i);
        Gold = PlayerPrefs.GetInt("Gold" + i);
    }

    public void SaveGame()
    {

    }

    public void LoadGame()
    {

    }
}