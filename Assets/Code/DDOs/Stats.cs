using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEditor.PackageManager;
using UnityEngine;

[Serializable]
public struct Stats
{
    public int MaxHP;
    public int Atk;
    public int Def;
    public int Map;
    public int Mar;
    public int Rec;
    public int Spd;
    public int Tec;
    public int Acc;
    public int Eva;
    public int Crt;
    public int Cev;

    public void SetTo(Stats other)
    {
        MaxHP = other.MaxHP;
        Atk = other.Atk;
        Def = other.Def;
        Map = other.Map;
        Mar = other.Mar;
        Rec = other.Rec;
        Spd = other.Spd;
        Tec = other.Tec;
        Acc = other.Acc;
        Crt = other.Crt;
        Eva = other.Eva;
        Cev = other.Cev;
    }

    public void SetToZero(int accMod = 0)
    {
        MaxHP = 0;
        Atk = 0;
        Def = 0;
        Map = 0;
        Mar = 0;
        Rec = 0;
        Spd = 0;
        Tec = 0;
        Acc = accMod;
        Eva = accMod;
        Crt = accMod;
        Cev = accMod;
    }

    public void Print()
    {
        Debug.Log("HP: " + MaxHP);
        Debug.Log("ATK: " + Atk);
        Debug.Log("DEF: " + Def);
        Debug.Log("MAP: " + Map);
        Debug.Log("MAR: " + Mar);
        Debug.Log("Rec: " + Rec);
        Debug.Log("SPD: " + Spd);
        Debug.Log("TEC: " + Tec);
        Debug.Log("ACC: " + Acc);
        Debug.Log("EVA: " + Eva);
        Debug.Log("CRT: " + Crt);
        Debug.Log("CEV: " + Cev);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Stat conversions --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static int[] MinMHP = { 10, 19, 22, 25, 28, 31, 37, 40, 49 };
    private static int[] MaxMHPLow = { 45, 275, 375, 475, 600, 750, 950, 1100, 1500 };
    private static int[] MaxMHPHigh = { 55, 325, 425, 525, 700, 850, 1050, 1300, 1700 };

    public static int[] MinStat = { 2, 4, 4, 5, 6, 6, 8, 8, 10 };
    public static int[] MaxStatLow = { 45, 90, 115, 140, 175, 210, 240, 270, 320 };
    public static int[] MaxStatHigh = { 55, 110, 135, 160, 195, 230, 260, 290, 340 };

    public void ConvertFromBaseToActual(int level)
    {
        MaxHP = SetMHPNorms(level, MaxHP, 0);
        Atk = SetStatNorms(level, Atk, 0);
        Def = SetStatNorms(level, Def, 0);
        Map = SetStatNorms(level, Map, 0);
        Mar = SetStatNorms(level, Mar, 0);
        Rec = SetStatNorms(level, Rec, 0);
        Spd = SetStatNorms(level, Spd, 0);
        Tec = SetStatNorms(level, Tec, 0);
        Acc = SetOtherStatNorms(Acc, 100);
        Eva = SetOtherStatNorms(Eva, 100);
        Crt = SetOtherStatNorms(Crt, 100);
        Cev = SetOtherStatNorms(Cev, 100);
    }

    public void ConvertFromBaseToActual(int level, Stats modifiers)
    {
        MaxHP = SetMHPNorms(level, MaxHP, modifiers.MaxHP);
        Atk = SetStatNorms(level, Atk, modifiers.Atk);
        Def = SetStatNorms(level, Def, modifiers.Def);
        Map = SetStatNorms(level, Map, modifiers.Map);
        Mar = SetStatNorms(level, Mar, modifiers.Mar);
        Rec = SetStatNorms(level, Rec, modifiers.Rec);
        Spd = SetStatNorms(level, Spd, modifiers.Spd);
        Tec = SetStatNorms(level, Tec, modifiers.Tec);
        Acc = SetOtherStatNorms(Acc, modifiers.Acc);
        Eva = SetOtherStatNorms(Eva, modifiers.Eva);
        Crt = SetOtherStatNorms(Crt, modifiers.Crt);
        Cev = SetOtherStatNorms(Cev, modifiers.Cev);
    }

    private int SetMHPNorms(int level, int baseHPStat, int natMod)
    {
        float actualBase = baseHPStat / 10.0f;
        int flooredBase = (int)actualBase;
        int min = MinMHP[flooredBase] - natMod * 4;
        int maxMhpLow = MaxMHPLow[flooredBase];
        int maxMhpHigh = MaxMHPHigh[flooredBase];
        float max = maxMhpLow + (actualBase - flooredBase) * (maxMhpHigh - maxMhpLow) + natMod * 40;
        float result = min + (level - 1) * ((int)max - min) / 99;
        return (int)(Math.Round(result));
    }

    private int SetStatNorms(int level, int baseStat, int natMod)
    {
        float actualBase = baseStat / 10.0f;
        int flooredBase = (int)actualBase;
        int min = MinStat[flooredBase] - natMod * 2;
        int maxStatLow = MaxStatLow[flooredBase];
        int maxStatHigh = MaxStatHigh[flooredBase];
        double max = maxStatLow + (actualBase - flooredBase) * (maxStatHigh - maxStatLow) + natMod * 20;
        double result = min + (level - 1) * ((int)max - min) / 99;
        return (int)(Math.Round(result));
    }

    private int SetOtherStatNorms(int baseStat, int natMod)
    {
        return baseStat * natMod / 100;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Operations --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private delegate int DoOperation(int a, int b);

    private void ExecuteOperation(Stats other, DoOperation opFunc)
    {
        MaxHP = opFunc(MaxHP, other.MaxHP);
        Atk = opFunc(Atk, other.Atk);
        Def = opFunc(Def, other.Def);
        Map = opFunc(Map, other.Map);
        Mar = opFunc(Mar, other.Mar);
        Rec = opFunc(Rec, other.Rec);
        Spd = opFunc(Spd, other.Spd);
        Tec = opFunc(Tec, other.Tec);
        Acc = opFunc(Acc, other.Acc);
        Crt = opFunc(Crt, other.Crt);
        Eva = opFunc(Eva, other.Eva);
        Cev = opFunc(Cev, other.Cev);
    }

    private int Add(int a, int b)
    {
        return a + b;
    }

    private int Subtract(int a, int b)
    {
        return a - b;
    }

    private int Multiply(int a, int b)
    {
        return a * b;
    }

    private int Divide(int a, int b)
    {
        return a / b;
    }

    public void Add(Stats other)
    {
        ExecuteOperation(other, Add);
    }

    public void Subtract(Stats other)
    {
        ExecuteOperation(other, Subtract);
    }

    public void Multiply(Stats other)
    {
        ExecuteOperation(other, Multiply);
    }

    public void Divide(Stats other)
    {
        ExecuteOperation(other, Divide);
    }
}
