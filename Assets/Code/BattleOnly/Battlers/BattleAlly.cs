using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleAlly : BattlerAI
{
    protected new void Start()
    {
        base.Start();
        Direction = Vector3.right;
    }

    protected new void Update()
    {
        base.Update();
    }

    protected override void GetKOd()
    {
        base.GetKOd();
        var ps = CurrentBattle.PlayerKOParticles;
        StartCoroutine(IsSummon ? ApplyKOEffect(ps, 1f, false) : ApplyKOEffect(ps, 0.5f, true));
    }
}
