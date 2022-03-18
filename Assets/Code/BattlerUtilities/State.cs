using UnityEngine;

public class State : BaseObject
{
    public PassiveEffect Effect;
    public bool KeepAfterBattle;
    public int MaxStack = 1;
    public int ContactSpreadRate;
    public bool Stun;
    public bool Petrify;

    [HideInInspector] public int Stack;
}
