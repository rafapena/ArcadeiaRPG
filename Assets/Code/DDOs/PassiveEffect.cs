﻿using UnityEngine;

[System.Serializable]
public struct PassiveEffect
{
    public enum TurnSequence { ActionEnd, TurnEnd }

    //public Stats StatModifiers;
    public int HPRegen;
    public int SPRegen;
    public int SPConsumeRate;
    public int ComboDifficulty;
    public int TurnEnd1;
    public int TurnEnd2;
    public TurnSequence WhenToExecute;
    public int RemoveByHitChance;
    public int CounterRate;
    public int ReflectRate;
    public int PhysicalDamageRate;
    public int MagicalDamageRate;
    public int ExtraTurns;
    public BattleMaster.ToolTypes[] DisabledToolTypes;
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;

    [HideInInspector] public int TurnsLeft;
}