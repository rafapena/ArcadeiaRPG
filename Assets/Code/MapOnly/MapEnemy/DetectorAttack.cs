using UnityEngine;

public class DetectorAttack : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        MapEnemy e = gameObject.GetComponentInParent<MapEnemy>();
        MapPlayer p = collider.gameObject.GetComponent<MapPlayer>();
        try
        {
            if (e && p && Physics2D.Raycast(transform.position, p.transform.position - e.transform.position).collider.CompareTag("Player"))
                e.GoAfterPlayer();
        }
        catch { }
        
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        OnTriggerEnter2D(collider);
    }
}