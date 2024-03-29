﻿using UnityEngine;

[System.Serializable]
public abstract class PassiveEffect : DataObject
{
    public Stats StatModifiers;
    public int HPRegen;
    public int SPRegen;
    public int SPConsumeRate;
    public int TurnEnd1;
    public int TurnEnd2;
    public int RemoveByHitChance;
    public bool Counter;
    public bool Reflect;
    public int ExpGainRate;
    public int GoldGainRate;
    public BattleMaster.ToolTypes[] DisabledToolTypes;
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;
}
