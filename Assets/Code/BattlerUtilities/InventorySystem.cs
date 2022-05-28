using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public enum ListType
    {
        None,
        Items,
        Weapons,
        Accessories
    }

    public PlayerParty Party;
    public int Gold;

    // Public for Unity Editing: Will set to private set, afterwards
    public List<Item> Items;
    public List<Weapon> Weapons;
    public List<Accessory> Accessories;

    // Weight
    [HideInInspector] public int CarryWeight;
    public int WeightCapacity = 30;

    public void Initialize()
    {
        Items = SetupToolGroup(Items);
        Weapons = SetupToolGroup(Weapons);
        Accessories = SetupToolGroup(Accessories);
    }

    public void UpdateToCurrentWeight()
    {
        CarryWeight = 0;
        CarryWeight += SetupCarryWeightForList(Items);
        CarryWeight += SetupCarryWeightForList(Weapons);
        CarryWeight += SetupCarryWeightForList(Accessories);
    }

    private int SetupCarryWeightForList<T>(List<T> tools) where T : IToolForInventory
    {
        int total = 0;
        foreach (T t in tools)
            total += t.Quantity * t.Weight;
        return total;
    }

    public List<IToolForInventory> GetItemsAndWeapons()
    {
        List<IToolForInventory> inventory = new List<IToolForInventory>();
        foreach (Item it in Items) inventory.Add(it);
        foreach (Weapon wp in Weapons) inventory.Add(wp);
        foreach (Accessory ac in Accessories) inventory.Add(ac);
        return inventory;
    }

    private List<T> SetupToolGroup<T>(List<T> toolList) where T : IToolForInventory
    {
        List<T> groupedList = new List<T>();
        for (int i = 0; i < toolList.Count; i++)
        {
            int foundTool = groupedList.FindIndex(t => t.Info.Id == toolList[i].Info.Id);
            if (foundTool < 0)
            {
                toolList[i] = (T)InitializeTool(toolList[i], Party.ItemsListDump);
                toolList[i].Quantity = 1;
                groupedList.Add(toolList[i]);
            }
            else groupedList[foundTool].Quantity++;
        }
        return groupedList;
    }

    private IToolForInventory InitializeTool<T>(T tool, Transform t) where T : IToolForInventory
    {
        if (tool is Item it) return Instantiate(it, t);
        else if (tool is Weapon wp) return Instantiate(wp, t);
        else if (tool is Accessory ac) return Instantiate(ac, t);
        return default(T);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Add/Remove Content --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public IToolForInventory Add<T>(T tool, int amount = 1) where T : IToolForInventory
    {
        if (tool is Item it) return AddInventoryTool(ref Items, it, amount);
        else if (tool is Weapon wp) return AddInventoryTool(ref Weapons, wp, amount);
        else if (tool is Accessory ac) return AddInventoryTool(ref Accessories, ac, amount);
        else return default(T);
    }

    public IToolForInventory Remove<T>(int index, int amount = 1) where T : IToolForInventory
    {
        if (typeof(T).Name.Equals("Item")) return RemoveInventoryToolByIndex(ref Items, index, amount);
        else if (typeof(T).Name.Equals("Weapon")) return RemoveInventoryToolByIndex(ref Weapons, index, amount);
        else if (typeof(T).Name.Equals("Accessory")) return RemoveInventoryToolByIndex(ref Accessories, index, amount);
        else return default(T);
    }

    public int Remove<T>(T tool, int amount = 1) where T : IToolForInventory
    {
        if (tool is Item it) return RemoveInventoryToolByValue(ref Items, it, amount);
        else if (tool is Weapon wp) return RemoveInventoryToolByValue(ref Weapons, wp, amount);
        else if (tool is Accessory ac) return RemoveInventoryToolByValue(ref Accessories, ac, amount);
        else return -1;
    }

    private T AddInventoryTool<T>(ref List<T> toolList, T newTool, int amount) where T : IToolForInventory
    {
        CarryWeight += newTool.Weight * amount;
        T tool = toolList.Find(t => t.Info.Id == newTool.Info.Id);
        if (tool == null)
        {
            tool = (T)InitializeTool(newTool, Party.ItemsListDump);
            tool.Quantity = amount;
            toolList.Add(tool);
        }
        else tool.Quantity += amount;
        return tool;
    }

    private T RemoveInventoryToolByIndex<T>(ref List<T> toolList, int index, int amount) where T : IToolForInventory
    {
        if (index < 0 || index >= toolList.Count) return default(T);
        T t = toolList[index];
        int fixedAmount = (t.Quantity - amount < 0) ? t.Quantity : amount; 
        if (t.Quantity - fixedAmount <= 0) toolList.RemoveAt(index);
        else t.Quantity -= fixedAmount;
        CarryWeight -= t.Weight * fixedAmount;
        return t;
    }

    private int RemoveInventoryToolByValue<T>(ref List<T> toolList, T tool, int amount) where T : IToolForInventory
    {
        if (tool == null) return -1;
        int index = toolList.FindIndex(t => t.Info.Id == tool.Info.Id);
        RemoveInventoryToolByIndex(ref toolList, index, amount);
        return index;
    }
}
