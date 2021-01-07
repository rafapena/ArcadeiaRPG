using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public int Id;
    public ToolForInventory[] StoredTools;
    public Sprite OpenedSprite;
    private bool Opened;

    public GameObject UIPopup;
    private float UIPopupTimer;
    public float UIPopupTimeLimit;

    private void Start()
    {
        UIPopup.SetActive(false);
    }

    private void Update()
    {
        if (Time.unscaledTime > UIPopupTimer && Opened) UIPopup.SetActive(false);
    }

    public void Open(MapPlayer opener)
    {
        if (Opened) return;
        Opened = true;
        UIPopup.SetActive(true);
        UIPopupTimer = Time.unscaledTime + UIPopupTimeLimit;
        gameObject.GetComponent<SpriteRenderer>().sprite = OpenedSprite;
        foreach (ToolForInventory t in StoredTools)
        {
            Item it = t as Item;
            if (it) opener.Party.Inventory.AddItem(it);
            Weapon wp = t as Weapon;
            if (wp) opener.Party.Inventory.AddWeapon(wp);
        }
    }

    public void SetOpen()
    {
        Opened = true;
    }
}
