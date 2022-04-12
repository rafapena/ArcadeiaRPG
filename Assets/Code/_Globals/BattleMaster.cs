using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleMaster
{
    public const int MAX_NUMBER_OF_EQUIPS = 4;
    public const int DEFAULT_RATE = 100;
    public const int SP_CAP = 100;

    public enum Elements { None, Fire, Ice, Thunder, Earth, Wind, Light, Dark }

    public enum WeaponTypes { None, Blade, Hammer, Charm, Gun, Tools, Camera }
    
    public enum ToolTypes { None, PhysicalOffense, PhysicalDefense, MagicalOffense, MagicalDefense, GeneralOffense, GeneralDefense }
    
    public enum ToolFormulas { None, PhysicalStandard, MagicalStandard, PhysicalGun, MagicalGun }

    // Variables used to transitioning between scenes
    public static Environment EnvironmentSettings { get; private set; }
    public static PlayerParty PlayerParty { get; private set; }
    public static EnemyParty EnemyParty { get; private set; }

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
