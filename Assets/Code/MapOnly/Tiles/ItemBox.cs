using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemBox : MonoBehaviour
{
    public int Id;
    public ToolForInventory[] StoredTools;
    public Sprite OpenedSprite;
    private bool Opened;
    [HideInInspector] public MapPlayer ClosingPlayer;

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
        SetOpen();
        DisplayObtainedTools();
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
        gameObject.GetComponent<SpriteRenderer>().sprite = OpenedSprite;
    }

    public void DisplayObtainedTools()
    {
        UIPopup.SetActive(true);
        UIPopupTimer = Time.unscaledTime + UIPopupTimeLimit;
        Transform list = UIPopup.transform.GetChild(0);
        int limit = Math.Min(StoredTools.Length, 3);
        int i = 0;
        for (; i < limit; i++)
        {
            Transform it = list.GetChild(i);
            it.GetChild(0).GetComponent<TextMeshProUGUI>().text = StoredTools[i].Name;
            it.GetChild(1).GetComponent<Image>().sprite = StoredTools[i].GetComponent<SpriteRenderer>().sprite;
        }
        for (; i < list.childCount; i++) list.GetChild(i).gameObject.SetActive(false);
    }
}
