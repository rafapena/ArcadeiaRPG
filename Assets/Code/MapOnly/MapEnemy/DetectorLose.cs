using UnityEngine;

public class DetectorLose : MonoBehaviour
{
    private MapEnemy Avatar;

    private void Awake()
    {
        Avatar = gameObject.GetComponentInParent<MapEnemy>();
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Player") Avatar.StopGoingAfterPlayer();
    }
}
