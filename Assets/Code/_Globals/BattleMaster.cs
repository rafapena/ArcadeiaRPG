using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleMaster
{
    public const int MAX_NUMBER_OF_ITEMS = 6;
    public const int MAX_NUMBER_OF_WEAPONS = 3;
    public const int DEFAULT_RATE = 100;
    public const int SP_CAP = 100;
    public const int BASE_ACCURACY = 95;

    public enum Elements { None, Fire, Ice, Thunder, Earth, Wind, Light, Dark }

    public enum WeaponTypes { None, Blade, Hammer, Charm, Gun, Tools, Camera }
    
    public enum ToolTypes { None, PhysicalOffense, PhysicalDefense, MagicalOffense, MagicalDefense, GeneralOffense, GeneralDefense }
    
    public enum ToolFormulas { None, PhysicalStandard, MagicalStandard, PhysicalGun, MagicalGun }

    public static string[] CompanionshipLevels = new string[] { "Allies", "Supporters", "Battle Buddies", "Super Team", "Elite Duo", "Perfect Unison" };
    public static int[] CompanionshipPoints = new int[] { 100, 300, 600, 1000, 1500 };

    // Variables used to transition between scenes
    public static Environment EnvironmentSettings;
    public static PlayerParty PlayerParty;
    public static EnemyParty EnemyParty;

    public static void Setup(Environment environmentSettings, PlayerParty playerParty, EnemyParty enemyParty)
    {
        EnvironmentSettings = environmentSettings;
        PlayerParty = playerParty;
        EnemyParty = enemyParty;
    }

    public static void Reset()
    {
        EnvironmentSettings = null;
        PlayerParty = null;
        EnemyParty = null;
    }
}
