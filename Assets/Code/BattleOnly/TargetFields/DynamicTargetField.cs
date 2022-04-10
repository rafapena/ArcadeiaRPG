using UnityEngine;

public class DynamicTargetField : TargetField
{
    protected Vector3 Movement;
    private float Speed;

    public Transform ApproachPointLeft;
    public Transform ApproachPointRight;

    [SerializeField]
    private float DefaultSpeed;

    // Update is called once per frame
    protected override void Update()
    {
        if (!gameObject.activeSelf) return;
        Movement = InputMaster.GetCustomMovementControls(KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow);
        Figure.velocity = Movement * Speed;
    }

    public override bool HasApproachPoints() => ApproachPointLeft != null && ApproachPointRight != null;

    public void AimAt(Battler target, bool movable)
    {
        if (target.HUDProperties.ScopeHitBox == null) return;
        Speed = movable ? DefaultSpeed : 0;
        Vector3 pos = target.HUDProperties.ScopeHitBox.transform.position;
        transform.position = new Vector3(pos.x, pos.y, target.transform.position.z - 1);
    }
}
