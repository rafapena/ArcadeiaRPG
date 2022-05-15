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
}
