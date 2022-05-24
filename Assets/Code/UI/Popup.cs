using UnityEngine;

public class Popup : MonoBehaviour
{
    public float Duration;

    void Start()
    {
        gameObject.GetComponent<MeshRenderer>().sortingLayerName = "Popup";
        Destroy(gameObject, Duration);
    }

    void Update()
    {
        gameObject.transform.position -= new Vector3(0, -0.015f, 0);
    }

    public void Show(string displayText)
    {
        gameObject.GetComponent<TextMesh>().text = displayText;
    }
}
