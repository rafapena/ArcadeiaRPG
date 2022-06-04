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

    public enum CombatRangeTypes { Any, Close, Medium, Far }

    public enum Elements { None, Fire, Ice, Thunder, Earth, Wind }

    public enum WeaponTypes { None, Blade, Hammer, Staff, Gun, Camera }
    
    public enum ToolTypes { None, PhysicalOffense, PhysicalDefense, MagicalOffense, MagicalDefense, GeneralOffense, GeneralDefense }
    
    public enum ToolFormulas { None, PhysicalStandard, MagicalStandard, GunStandard }
}
