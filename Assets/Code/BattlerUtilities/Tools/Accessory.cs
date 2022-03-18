using System.Collections.Generic;
using UnityEngine;

public class Accessory : ToolForInventory
{
    public PassiveEffect Effect;
    public int HPMin;
    public int HPMax = 100;
    public int SPMin;
    public int SPMax = 100;
    public bool AnyState;
    public bool NoState;
    //public State StateActive1;
    //public State StateActive2;
    //public State StateInactive1;
    //public State StateInactive2;
    public int ExpGainRate;
    public int GoldGainRate;
    public int AllyCondition;
    public int FoeCondition;
    public int UserCondition;
}
