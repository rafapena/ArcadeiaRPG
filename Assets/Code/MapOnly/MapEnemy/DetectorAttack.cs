using UnityEngine;

public class DetectorAttack : MonoBehaviour
{
    private MapEnemy Avatar;

    private void Awake()
    {
        Avatar = gameObject.GetComponentInParent<MapEnemy>();
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        try
        {
            if (collider.gameObject.tag != "Player") return;
            MapPlayer p = collider.gameObject.GetComponent<MapPlayer>();
            if (Physics2D.Raycast(transform.position, p.transform.position - Avatar.transform.position).collider.CompareTag("Player")) Avatar.GoAfterPlayer();
        }
        catch { }
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        OnTriggerEnter2D(collider);
    }
}