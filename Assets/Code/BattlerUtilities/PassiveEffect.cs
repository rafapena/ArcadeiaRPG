using UnityEngine;

[System.Serializable]
public abstract class PassiveEffect : DataObject
{
    public enum TurnSequence { ActionStart, ActionEnd, TurnEnd }

    public Stats StatModifiers;
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
    public int ExpGainRate;
    public int GoldGainRate;
    public BattleMaster.ToolTypes[] DisabledToolTypes;
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;
}
