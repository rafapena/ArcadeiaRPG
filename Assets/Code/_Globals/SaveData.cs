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
    public int FileNumber;

    public int Chapter;
    public int Level;
    public int Difficulty;
    public int TotalPlaytimeHours;
    public int TotalPlaytimeMinutes;
    public float TotalPlaytimeSeconds;
    public string Location;
    public int Gold;

    public int[] GameVars;

    public void SaveGame()
    {

    }

    public void LoadGame()
    {

    }
}