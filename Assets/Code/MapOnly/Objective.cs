using System.Collections.Generic;
using UnityEngine;

public class Objective : BaseObject
{
    public enum Types { Main, Secondary, Other };

    public Types Type;
    public int EXPReward;
    public InventorySystem InventoryRewards;
    public List<SubObjective> NextObjectives;

    [HideInInspector] public bool Marked;
    [HideInInspector] public bool Cleared;

    public void CheckClear()
    {
        Cleared = AllCleared();
    }

    public bool AllCleared()
    {
        foreach (SubObjective o in NextObjectives)
            if (!AllCleared(o)) return false;
        return true;
    }

    private bool AllCleared(SubObjective o)
    {
        if (!o.Cleared) return false;
        foreach (SubObjective so0 in o.NextObjectives)
            if (!AllCleared(so0)) return false;
        return true;
    }

    public void UnMark()
    {
        Marked = false;
        foreach (SubObjective so in NextObjectives)
            UnMark(so);
    }

    private void UnMark(SubObjective so)
    {
        so.Marked = false;
        if (so.NextObjectives == null) return;
        foreach (SubObjective so0 in so.NextObjectives)
            UnMark(so0);
    }
}
