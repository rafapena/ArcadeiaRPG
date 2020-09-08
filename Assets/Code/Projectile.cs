using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [HideInInspector] private Battler Shooter;
    [HideInInspector] private List<Battler> Targets;
    [HideInInspector] private Tool ToolEffect;

    private Vector3 ShootDir;
    public float MoveSpeed;

    public void Setup(float moveSpeed, Vector3 shootDir)
    {
        MoveSpeed = moveSpeed;
        ShootDir = shootDir;
        transform.eulerAngles = new Vector3(0, 0, GetAngleFromVectorFloat(shootDir));
        Destroy(gameObject, 12f);
    }

    public void GetBattleInfo(Battler user, List<Battler> targets, Tool tool)
    {
        Shooter = user;
        Targets = targets;
        ToolEffect = tool;
    }

    protected void Update()
    {
        transform.position += ShootDir * MoveSpeed * Time.deltaTime;
    }

    // Keeps the bullet sprite pointed to the right direction
    private float GetAngleFromVectorFloat(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        if (n < 0) n += 360;
        return n;
    }

    // BulletHurtBox manages player damage: This ensures that specific hitboxes can be ignored
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Invisible")) return;
        Battler b = collider.gameObject.GetComponent<Battler>();
        if (b && Targets.Contains(b))
        {
            b.ReceiveToolEffects(Shooter, ToolEffect);
            Destroy(gameObject);
        }
    }
}