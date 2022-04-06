using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicTargetField : TargetField
{
    protected Vector3 Movement;
    public float Speed;

    public float DefaultSpeed { get; private set; }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        DefaultSpeed = Speed;
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        Movement = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Figure.velocity = Movement * Speed;
    }

    public void AimAt(Battler target, bool movable)
    {
        Speed = movable ? DefaultSpeed : 0;
        transform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z - 1);
    }
}
