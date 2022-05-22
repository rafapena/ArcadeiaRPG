using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

public class MapEnemy : MapExplorer
{
    private MapPlayer TargetPlayer;
    private float TurnCounter;
    private float DetectTime;

    public EnemyParty EnemyPartyOnContact;
    [HideInInspector] public bool ChasingPlayer;
    private bool Defeated;

    protected override void Start()
    {
        base.Start();
        TargetPlayer = GameObject.FindGameObjectWithTag("Player").GetComponent<MapPlayer>();
        Physics2D.queriesStartInColliders = false;
    }

    protected override void Update()
    {
        if (Defeated)
        {
            UpdateSprite();
            return;
        }
        base.Update();
        if (gameObject.layer == NON_COLLIDABLE_EXPLORER_LAYER && !IsBlinking()) gameObject.layer = MAP_ENEMY_LAYER;
        if (SceneMaster.InCutscene && ChasingPlayer)
        {
            StopGoingAfterPlayer();
        }
        else if (DetectTime < Time.time)
        {
            if (ChasingPlayer) ChasePlayer();
            else WanderAround();
        }
        Figure.velocity = Movement * Speed;
    }

    protected override void AnimateDirection()
    {
        //
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<MapPlayer>() && !SceneMaster.InCutscene)
        {
            SceneMaster.StartBattle(TargetPlayer.Party, EnemyPartyOnContact);
            MapMaster.EnemyEncountered = this;
        }
    }

    public void WanderAround()
    {
        TurnCounter += Mathf.Deg2Rad;
        Movement = new Vector3(Mathf.Sin(TurnCounter), Mathf.Cos(TurnCounter));
    }

    public void ChasePlayer()
    {
        float ax = 0;
        float ay = 0;
        float xLimit = Speed / 8f;
        float yLimit = Speed / 8f;
        if (TargetPlayer.transform.position.x > transform.position.x + xLimit) ax = 1;
        else if (TargetPlayer.transform.position.x < transform.position.x - xLimit) ax = -1;
        if (TargetPlayer.transform.position.y > transform.position.y + yLimit) ay = 1;
        else if (TargetPlayer.transform.position.y < transform.position.y - yLimit) ay = -1;
        Movement = new Vector3(ax, ay);
    }

    public void GoAfterPlayer()
    {
        if (ChasingPlayer) return;
        Movement = Vector3.zero;
        DetectTime = Time.time + 1f;
        ChasingPlayer = true;
    }

    public void StopGoingAfterPlayer()
    {
        if (!ChasingPlayer) return;
        ChasingPlayer = false;
    }

    public void DropItems()
    {
        //
    }

    public void DeclareDefeated()
    {
        Defeated = true;
        DropItems();
        MapMaster.EnemyEncountered.Blink(1.5f);
        Destroy(MapMaster.EnemyEncountered.gameObject, 1.5f);
    }
}