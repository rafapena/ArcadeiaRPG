using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicTargetField : TargetField
{
    protected Vector3 Movement;
    private float Speed;

    [SerializeField]
    public float DefaultSpeed;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        Movement = InputMaster.GetCustomMovementControls(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow);
        Figure.velocity = Movement * Speed;
    }

    public void AimAt(Battler target, bool movable)
    {
        Speed = movable ? DefaultSpeed : 0;
        transform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z - 1);
    }
}
