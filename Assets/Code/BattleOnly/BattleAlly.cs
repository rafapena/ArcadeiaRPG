using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BattleAlly : BattlerAI
{
    public bool IsSummon;

    protected new void Start()
    {
        base.Start();
        Direction = Vector3.right;
    }

    public void SetBattlePositions(VerticalPositions vp, HorizontalPositions hp)
    {
        RowPosition = vp;
        ColumnPosition = hp;
    }

    protected new void Update()
    {
        base.Update();
    }
}
