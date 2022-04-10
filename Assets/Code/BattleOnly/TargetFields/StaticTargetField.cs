using UnityEngine;

public class StaticTargetField : TargetField
{
    // Update is called once per frame
    protected override void Update()
    {
        if (AimingPlayer)
        {
            Vector3 pos = AimingPlayer.transform.position;
            pos.x += ((RectTransform)transform).sizeDelta.x * transform.localScale.x / 2;
            transform.position = pos;
        }
    }

    public override bool HasApproachPoints() => false;
}
