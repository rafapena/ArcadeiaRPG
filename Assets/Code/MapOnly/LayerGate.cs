using UnityEngine;

public class LayerGate : MonoBehaviour
{
    public int ElevationLeft = 0;
    public int ElevationRight = 0;
    public int ElevationUp = 0;
    public int ElevationDown = 0;
    private int[] Elevations;

    private void Start()
    {
        Elevations = new int[] { ElevationLeft, ElevationRight, ElevationUp, ElevationDown };
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        MapExplorer e = collision.GetComponent<MapExplorer>();
        if (!e) return;
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        float rightDiff = (e.transform.position.x - b.bounds.center.x) / b.size.x;
        float topDiff = (e.transform.position.y - b.bounds.center.y) / b.size.y;
        float[] diffs = new float[] { -rightDiff, rightDiff, topDiff, -topDiff };
        int max = 0;
        for (int i = 1; i < diffs.Length; i++)
            if (diffs[i] > diffs[max])
                max = i;
        e.GetComponent<SpriteRenderer>().sortingOrder = Elevations[max];
    }
}
