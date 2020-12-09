using UnityEngine;

public class MapCamera : MonoBehaviour
{
    public MapPlayer TargetPlayer;
    [HideInInspector] public GameObject Target;

    private void Awake()
    {
        if (!Target) Target = TargetPlayer.gameObject;
    }

    private void LateUpdate()
    {
        //float lerpSpeed = 20f * Time.deltaTime;
        //float cameraX = Mathf.Lerp(transform.position.x, Target.transform.position.x, lerpSpeed);
        //float cameraY = Mathf.Lerp(transform.position.y, Target.transform.position.y, lerpSpeed);
        //transform.position = new Vector3(cameraX, cameraY, transform.position.z);
        Vector3 t = transform.position;
        t.x = Target.transform.position.x;
        t.y = Target.transform.position.y;
        transform.position = t;
    }
}
