using UnityEngine;

public class DetectorLose : MonoBehaviour
{
    void OnTriggerExit2D(Collider2D collider)
    {
        MapEnemy e = gameObject.GetComponentInParent<MapEnemy>();
        MapPlayer p = collider.gameObject.GetComponent<MapPlayer>();
        if (e && p) e.StopGoingAfterPlayer();
    }
}
