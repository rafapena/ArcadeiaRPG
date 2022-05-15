using UnityEngine;

public class BattlerHitbox : MonoBehaviour
{
    public Battler Battler { get; private set; }

    public void SetBattler(Battler b)
    {
        Battler = b;
    }
}
