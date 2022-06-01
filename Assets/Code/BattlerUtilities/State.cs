using UnityEngine;

public class State : PassiveEffect
{
    public bool KeepAfterBattle;
    public int MaxStack = 1;
    public int ContactSpreadRate;
    public bool Stun;
    public bool Petrify;
    public ParticleSystem SurroundingEffect;

    public int TurnsLeft { get; private set; }

    public int Stack { get; private set; }

    protected override void Awake()
    {
        TurnsLeft = Random.Range(TurnEnd1, TurnEnd2 + 1);
    }

    public void StackState(int i)
    {
        Stack += i;
        if (Stack > MaxStack) Stack = MaxStack;
    }
}
