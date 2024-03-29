﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    private Battler Shooter;
    private List<Battler> Targets = new List<Battler>();
    private ActiveTool ToolEffect;

    private Vector3 ShootDir;
    public bool PointToDirection;
    public float MoveSpeed;
    public float Duration;
    public float DurationAfterCollision;
    public UnityEvent OnExplode;

    public bool Finisher { get; private set; }
    public float NerfPartition { get; private set; }

    public void Direct(Vector3 shootDir)
    {
        ShootDir = shootDir.normalized;
        if (PointToDirection) transform.eulerAngles = new Vector3(0, 0, GetAngleFromVectorFloat(shootDir));
        if (Duration > 0) Destroy(gameObject, Duration);
    }

    public void SetBattleInfo(Battler user, ActiveTool activeTool, float nerfPartition, bool finisher)
    {
        Shooter = user;
        ToolEffect = activeTool;
        NerfPartition = nerfPartition;
        Finisher = finisher;
        GetComponent<SpriteRenderer>().sortingOrder += SpriteProperties.SPRITE_LAYER_DISTANCE * user.ColumnOverlapRank;
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

    // Handles action-to-player interaction
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag(Battle.ACTION_HITBOX_TAG))
        {
            BattlerHitbox hb = collider.gameObject.GetComponent<BattlerHitbox>();
            if (hb && hb.Battler.IsSelected && !Targets.Contains(hb.Battler))
            {
                Targets.Add(hb.Battler);
                hb.Battler.ReceiveToolEffects(Shooter, ToolEffect, this);
                Explode();
            }
        }
    }

    private void Explode()
    {
        Destroy(gameObject, DurationAfterCollision);
        OnExplode?.Invoke();
    }
}