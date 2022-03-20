using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBox : MonoBehaviour
{
    public int Id;
    public Sprite OpenedSprite;
    private bool Opened;
    [HideInInspector] public bool CloseToPlayer;

    public GameObject UIPopup;
    private float UIPopupTimer;
    public float UIPopupTimeLimit;

    public Item[] StoredItems;
    public Weapon[] StoredWeapons;
    public Accessory[] StoredAccessories;
    private IToolForInventory[] StoredTools;

    public bool IsOpened => Opened;

    private void Start()
    {
        UIPopup.SetActive(false);
        StoredTools = MenuMaster.GroupInventoryToolsToArray(StoredItems, StoredWeapons, StoredAccessories);
    }

    private void Update()
    {
        if (Time.unscaledTime > UIPopupTimer && Opened) UIPopup.SetActive(false);
    }

    public void Open(MapPlayer opener)
    {
        if (Opened) return;
        opener.PointToDirectionOf(this);
        SetOpen();
        DisplayObtainedTools();
        foreach (var tool in StoredTools) opener.Party.Inventory.Add(tool);
    }

    public void SetOpen()
    {
        Opened = true;
        gameObject.GetComponent<SpriteRenderer>().sprite = OpenedSprite;
    }

    public void DisplayObtainedTools()
    {
        UIPopup.SetActive(true);
        UIPopupTimer = Time.unscaledTime + UIPopupTimeLimit;
        Transform list = UIPopup.transform.GetChild(0);
        int limit = System.Math.Min(StoredTools.Length, 3);
        int i = 0;
        for (; i < limit; i++)
        {
            Transform it = list.GetChild(i);
            it.GetChild(0).GetComponent<TextMeshProUGUI>().text = StoredTools[i].Info.Name;
            it.GetChild(1).GetComponent<Image>().sprite = StoredTools[i].Info.GetComponent<SpriteRenderer>().sprite;
        }
        for (; i < list.childCount; i++) list.GetChild(i).gameObject.SetActive(false);
    }
}
