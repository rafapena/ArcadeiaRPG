using UnityEngine;

public abstract class PassiveEffect : BaseObject
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
    public int PhysicalDamageRate = 100;
    public int MagicalDamageRate = 100;
    public int ExtraTurns;
    public BattleMaster.ToolTypes[] DisabledToolTypes;
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;

    [HideInInspector] public int TurnsLeft;
    private int[] ElementRates;
    private int[] StateRates;
}
