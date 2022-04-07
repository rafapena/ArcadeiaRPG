using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticTargetField : TargetField
{
    // Start is called before the first frame update
    protected override void Start()
    {
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (Player)
        {
            Vector3 pos = Player.transform.position;
            pos.x += ((RectTransform)transform).sizeDelta.x * transform.localScale.x / 2;
            transform.position = pos;
        }
    }

    public override void Activate(BattlePlayer p)
    {
        base.Activate(p);
    }
}
