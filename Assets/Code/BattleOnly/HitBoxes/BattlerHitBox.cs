using UnityEngine;

public abstract class BattlerHitBox : MonoBehaviour
{
    public Battler Battler { get; private set; }

    public void SetBattler(Battler b)
    {
        Battler = b;
    }
}
