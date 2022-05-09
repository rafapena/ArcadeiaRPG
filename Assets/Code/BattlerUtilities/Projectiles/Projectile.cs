using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    private Battler Shooter;
    private List<Battler> Targets = new List<Battler>();
    private ActiveTool ToolEffect;
    private float NerfPartition;

    private Vector3 ShootDir;
    public bool PointToDirection;
    public float MoveSpeed;
    public float Duration;
    public float DurationAfterCollision;
    public UnityEvent OnExplode;

    public void Direct(Vector3 shootDir)
    {
        ShootDir = shootDir;
        if (PointToDirection) transform.eulerAngles = new Vector3(0, 0, GetAngleFromVectorFloat(shootDir));
        if (Duration > 0) Destroy(gameObject, Duration);
    }

    public void SetBattleInfo(Battler user, ActiveTool activeTool, float nerfPartition = 1f)
    {
        Shooter = user;
        ToolEffect = activeTool;
        NerfPartition = nerfPartition;
    }

    protected void Update()
    {
        transform.position += ShootDir * MoveSpeed * Time.deltaTime;
    }

    // Keeps the bullet sprite pointed to the right direction
    private float GetAngleFromVectorFloat(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg + 90;
        if (n < 0) n += 360;
        return n;
    }

    // BulletHurtBox manages player damage: This ensures that specific hitboxes can be ignored
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Invisible")) return;

        ActionHitBox hb = collider.gameObject.GetComponent<ActionHitBox>();
        if (hb && hb.Battler.IsSelected && !Targets.Contains(hb.Battler))
        {
            Targets.Add(hb.Battler);
            hb.Battler.ReceiveToolEffects(Shooter, ToolEffect, NerfPartition);
            if (DurationAfterCollision > 0) Explode();
        }
    }

    private void Explode()
    {
        MoveSpeed = 0;
        Destroy(gameObject, DurationAfterCollision);
        OnExplode?.Invoke();
    }
}