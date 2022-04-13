[System.Serializable]
public struct AIAction
{
    public ActiveTool Action;
    public int ClassSkillId;
    public int Priority;
    public AIStandardCondition UserCondition;
    public AITargetCondition TargetCondition;
}

[System.Serializable]
public struct AIStandardCondition
{
    public int HPLow;
    public int HPHigh;
    public int SPLow;
    public int SPHigh;
    public State[] AnyActiveStates;
    public State[] AnyInactiveStates;
}

[System.Serializable]
public struct AITargetCondition
{
    public AIStandardCondition Condition;
    public ElementRate[] ElementRates;
    public StateRate[] StateRates;
    public ToolUser.CombatRangeTypes TargetRange;
}