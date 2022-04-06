using System.Collections.Generic;
using UnityEngine;

public abstract class MapExplorer : MonoBehaviour
{
    public const int NON_COLLIDABLE_EXPLORER_LAYER = 8;
    public const int MAP_PLAYER_LAYER = 9;
    public const int MAP_ENEMY_LAYER = 10;

    [HideInInspector] public Rigidbody2D Figure;
    protected Vector3 Movement;
    public float Speed;

    private float BlinkTimer;

    protected virtual void Awake()
    {
        Figure = gameObject.GetComponent<Rigidbody2D>();
        Physics2D.IgnoreLayerCollision(MAP_PLAYER_LAYER, NON_COLLIDABLE_EXPLORER_LAYER);
        Physics2D.IgnoreLayerCollision(MAP_ENEMY_LAYER, NON_COLLIDABLE_EXPLORER_LAYER);
    }

    protected virtual void Start()
    {
        //
    }

    protected virtual void Update()
    {
        UpdateSprite();
    }

    protected virtual void UpdateSprite()
    {
        GetComponent<SpriteRenderer>().enabled = IsBlinking() ? (Time.time % 0.4f < 0.2f) : true;
    }

    protected abstract void AnimateDirection();

    public void Blink(float duration)
    {
        BlinkTimer = Time.time + duration;
        gameObject.layer = NON_COLLIDABLE_EXPLORER_LAYER;
    }

    public void DisableExplorerCollision()
    {

    }

    public bool IsBlinking()
    {
        return Time.time < BlinkTimer;
    }
}
