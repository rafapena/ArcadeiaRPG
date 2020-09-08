using UnityEngine;
using UnityEngine.UI;

public class MapNavigator : MonoBehaviour
{
    public MMMap MapMenu;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Button b = collision.GetComponent<Button>();
        if (b) MapMenu.TrackerOverLocation();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Button b = collision.GetComponent<Button>();
        if (b) MapMenu.TrackerDeselectedLocation();
    }
}
