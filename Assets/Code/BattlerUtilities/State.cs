using UnityEngine;

public class State : PassiveEffect
{
    public bool KeepAfterBattle;
    public int MaxStack = 1;
    public int ContactSpreadRate;
    public bool Stun;
    public bool Petrify;

    public int Stack { get; private set; }

    public void StackState(int i)
    {
        Stack += i;
        if (Stack > MaxStack) Stack = MaxStack;
    }
}
