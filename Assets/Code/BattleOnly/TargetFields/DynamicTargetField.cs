﻿using UnityEngine;

public class DynamicTargetField : TargetField
{
    protected Vector3 Movement;
    private float Speed;

    [SerializeField]
    private float DefaultSpeed;

    // Update is called once per frame
    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        Movement = InputMaster.GetCustomMovementControls(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow);
        Figure.velocity = Movement * Speed;
    }

    public void AimAt(Battler target, bool movable)
    {
        if (target.Properties.ScopeHitBox == null) return;
        Speed = movable ? DefaultSpeed : 0;
        Vector3 pos = target.Properties.ScopeHitBox.transform.position;
        transform.position = new Vector3(pos.x, pos.y, target.transform.position.z - 1);
    }
}
