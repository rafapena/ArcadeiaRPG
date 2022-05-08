using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct AIAction
{
    public ActiveTool Action;
    public int ClassSkillId;
    public int Priority;
    public int PriorityOnFail;
    public AIStandardCondition UserCondition;
    public AITargetCondition TargetCondition;

    [HideInInspector] public int CurrentPriority;
    [HideInInspector] public List<Battler> PossibleTargets;

    public bool Unusable() => ClassSkillId < 0;

    public void SetPriority(int value) => CurrentPriority = value;

    public void SetPossibleTargets(IEnumerable<Battler> targets) => PossibleTargets = targets.ToList();

    public bool MetRequirements(Battler user, IEnumerable<Battler> targets)
    {
        if (targets == null || !MetUserRequirements(user)) return false;

        PossibleTargets = new List<Battler>();
        foreach (var t in targets)
            if (MetTargetRequirements(t))
                PossibleTargets.Add(t);
        
        return PossibleTargets.Count > 0;
    }

    private bool MetUserRequirements(Battler user) => MetStandardConditions(UserCondition, user);

    private bool MetTargetRequirements(Battler target)
    {
        bool metRangeConditions = (target.CombatRangeType == TargetCondition.RangeType || target.CombatRangeType == ToolUser.CombatRangeTypes.Any || TargetCondition.RangeType == ToolUser.CombatRangeTypes.Any);
        return MetStandardConditions(TargetCondition.Condition, target) && metRangeConditions;
    }

    private bool MetStandardConditions(AIStandardCondition c, Battler b)
    {
        int HPPercent = 100 * b.HP / b.MaxHP;
        bool hpReq = c.HPHigh == 0 || c.HPLow <= HPPercent && HPPercent <= c.HPHigh;
        bool spReq = c.SPHigh == 0 || c.SPLow <= b.SP && b.SP <= c.SPHigh;
        bool activeStatesReq = c.AnyActiveStates.Length == 0 || b.States.Any(x => c.AnyActiveStates.Contains(x));
        bool inactiveStatesReq = c.AllInactiveStates.Length == 0 || !b.States.Any(x => c.AllInactiveStates.Contains(x));
        return hpReq && spReq && activeStatesReq && inactiveStatesReq;
    }
}


[System.Serializable]
public struct AIStandardCondition
{
    public int HPLow;
    public int HPHigh;
    public int SPLow;
    public int SPHigh;
    public State[] AnyActiveStates;
    public State[] AllInactiveStates;
}


[System.Serializable]
public struct AITargetCondition
{
    public AIStandardCondition Condition;
    public ToolUser.CombatRangeTypes RangeType;
}