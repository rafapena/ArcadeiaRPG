using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public struct GameCondition
{
    public enum ConditionTypes { GoldHigherThan, LevelHigherThan }

    public ConditionTypes Type;
    public int Value;
    public bool MeetCondition;
    public UnityEvent<int> OnFailed;
}
