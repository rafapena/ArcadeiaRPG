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

    protected override void Start()
    {
        base.Start();
        TargetPlayer = GameObject.FindGameObjectWithTag("Player").GetComponent<MapPlayer>();
        Physics2D.queriesStartInColliders = false;
    }

    protected override void Update()
    {
        base.Update();
        if (DetectTime > Time.time) return;
        else if (ChasingPlayer) ChasePlayer();
        else WanderAround();
        transform.position += Movement * Time.deltaTime * Speed;
    }

    protected override void AnimateDirection()
    {
        //
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<MapPlayer>())
            SceneMaster.StartBattle(TargetPlayer.Party, EnemyPartyOnContact);
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
        DetectTime = Time.time + 1f;
        ChasingPlayer = true;
    }

    public void StopGoingAfterPlayer()
    {
        if (!ChasingPlayer) return;
        ChasingPlayer = false;
    }
}