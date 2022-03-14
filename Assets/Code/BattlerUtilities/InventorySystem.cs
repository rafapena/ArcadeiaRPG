using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public enum ListType
    {
        None,
        Items,
        Weapons
    }

    public int Gold;

    // Lists are meant to be sorted by ID, automatically - Public for Unity Editing: Will set to private set, afterwards
    public List<Item> Items;
    public List<Weapon> Weapons;
    public List<Item> KeyItems;

    // Weight
    [HideInInspector] public int CarryWeight;
    public int WeightCapacity = 30;

    private void Awake()
    {
        Items = SetupToolGroup(Items);
        Weapons = SetupToolGroup(Weapons);
        KeyItems = SetupToolGroup(KeyItems);
    }

    public void UpdateToCurrentWeight()
    {
        CarryWeight = 0;
        CarryWeight += SetupCarryWeightForList(Items);
        CarryWeight += SetupCarryWeightForList(Weapons);
        CarryWeight += SetupCarryWeightForList(KeyItems);
    }

    private int SetupCarryWeightForList<T>(List<T> tools) where T : ToolForInventory
    {
        int total = 0;
        foreach (T t in tools)
            total += t.Quantity * t.Weight;
        return total;
    }

    public List<ToolForInventory> GetItemsAndWeapons()
    {
        List<ToolForInventory> inventory = new List<ToolForInventory>();
        foreach (Item it in Items) inventory.Add(it);
        foreach (Weapon wp in Weapons) inventory.Add(wp);
        foreach (Item it in KeyItems) inventory.Add(it);
        return inventory;
    }

    private List<T> SetupToolGroup<T>(List<T> toolList) where T : ToolForInventory
    {
        List<T> groupedList = new List<T>();
        for (int i = 0; i < toolList.Count; i++)
        {
            int foundTool = groupedList.FindIndex(t => t.Id == toolList[i].Id);
            if (foundTool < 0)
            {
                toolList[i] = Instantiate(toolList[i]);
                toolList[i].Quantity = 1;
                groupedList.Add(toolList[i]);
            }
            else groupedList[foundTool].Quantity++;
        }
        return groupedList;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Add/Remove Content --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Item AddItem(Item item, int amount = 1)
    {
        List<Item> itemList = item.IsKey ? KeyItems : Items;
        return AddInventoryTool(ref itemList, item, amount);
    }

    public Item RemoveItem(int index, int amount = 1, bool IsKey = false)
    {
        List<Item> itemList = IsKey ? KeyItems : Items;
        return RemoveInventoryToolByIndex(ref itemList, index, amount);
    }

    public int RemoveItem(Item item, int amount = 1)
    {
        List<Item> itemList = item.IsKey ? KeyItems : Items;
        return RemoveInventoryToolByValue(ref itemList, item, amount);
    }

    public Weapon AddWeapon(Weapon weapon, int amount = 1)
    {
        return AddInventoryTool(ref Weapons, weapon, amount);
    }

    public Weapon RemoveWeapon(int index, int amount = 1)
    {
        return RemoveInventoryToolByIndex(ref Weapons, index, amount);
    }

    public int RemoveWeapon(Weapon weapon, int amount = 1)
    {
        return RemoveInventoryToolByValue(ref Weapons, weapon, amount);
    }

    private T AddInventoryTool<T>(ref List<T> toolList, T newTool, int amount) where T : ToolForInventory
    {
        CarryWeight += newTool.Weight * amount;
        if (toolList.Find(t => t.Id == newTool.Id) == null)
        {
            MapPlayer p = gameObject.GetComponent<MapPlayer>();
            newTool = Instantiate(newTool, (p ? p.ItemsListDump : null));
            newTool.Quantity = amount;
            newTool.gameObject.GetComponent<Renderer>().enabled = false;
            toolList.Add(newTool);
        }
        else newTool.Quantity += amount;
        return newTool;
    }

    private T RemoveInventoryToolByIndex<T>(ref List<T> toolList, int index, int amount) where T : ToolForInventory
    {
        if (index < 0 || index >= toolList.Count) return null;
        T t = toolList[index];
        int fixedAmount = (t.Quantity - amount < 0) ? t.Quantity : amount; 
        if (t.Quantity - fixedAmount <= 0) toolList.RemoveAt(index);
        else t.Quantity -= fixedAmount;
        CarryWeight -= t.Weight * fixedAmount;
        return t;
    }

    private int RemoveInventoryToolByValue<T>(ref List<T> toolList, T tool, int amount) where T : ToolForInventory
    {
        if (tool == null) return -1;
        int index = toolList.FindIndex(t => t.Id == tool.Id);
        RemoveInventoryToolByIndex(ref toolList, index, amount);
        return index;
    }
}
