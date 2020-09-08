using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public enum ListType
    {
        None,
        Items,
        Weapons,
        KeyItems
    }

    public int Gold;

    // Lists are meant to be sorted by ID, automatically - Public for Unity Editing: Will set to private set, afterwards
    public List<Item> Items;
    public List<Weapon> Weapons;
    public List<Item> KeyItems;

    [HideInInspector] public int NumberOfTools;
    public int ToolCapacity = 30;

    private void Awake()
    {
        Items = SetupToolGroup(Items);
        Weapons = SetupToolGroup(Weapons);
        KeyItems = SetupToolGroup(KeyItems);
    }

    public void UpdateNumberOfTools()
    {
        NumberOfTools = 0;
        NumberOfTools += SetupCarryWeightForList(Items);
        NumberOfTools += SetupCarryWeightForList(Weapons);
        NumberOfTools += SetupCarryWeightForList(KeyItems);
    }

    private int SetupCarryWeightForList<T>(List<T> tools) where T : ToolForInventory
    {
        int total = 0;
        foreach (T t in tools)
            total += t.Quantity;
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
        List<T> ClonedList = new List<T>();
        for (int i = 0; i < toolList.Count; i++)
        {
            bool itemAlreadyInList = false;
            for (int j = 0; j < ClonedList.Count; j++)
            {
                if (toolList[i].Id != ClonedList[j].Id) continue;
                ClonedList[j].Quantity++;
                itemAlreadyInList = true;
                break;
            }
            if (itemAlreadyInList) continue;
            toolList[i] = Instantiate(toolList[i]);
            toolList[i].Quantity = 1;
            ClonedList.Add(toolList[i]);
        }
        return ClonedList;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Add/Remove Content --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public Item AddItem(Item item)
    {
        List<Item> itemList = item.IsKey ? KeyItems : Items;
        return AddInventoryTool(ref itemList, item);
    }

    public Item RemoveItem(int index, bool IsKey = false)
    {
        List<Item> itemList = IsKey ? KeyItems : Items;
        return RemoveInventoryToolByIndex(ref itemList, index);
    }

    public int RemoveItem(Item item)
    {
        List<Item> itemList = item.IsKey ? KeyItems : Items;
        return RemoveInventoryToolByValue(ref itemList, item);
    }

    public Weapon AddWeapon(Weapon weapon)
    {
        return AddInventoryTool(ref Weapons, weapon);
    }

    public Weapon RemoveWeapon(int index)
    {
        return RemoveInventoryToolByIndex(ref Weapons, index);
    }

    public int RemoveWeapon(Weapon weapon)
    {
        return RemoveInventoryToolByValue(ref Weapons, weapon);
    }

    private T AddInventoryTool<T>(ref List<T> toolList, T newTool) where T : ToolForInventory
    {
        if (NumberOfTools + 1 > ToolCapacity) return null;
        NumberOfTools++;

        newTool.gameObject.GetComponent<Renderer>().enabled = false;
        bool toolFound = false;
        foreach (T t in toolList)
        {
            if (newTool.Id < t.Id) break;   // Ensure ID sorting
            else if (newTool.Id == t.Id)
            {
                t.Quantity++;
                toolFound = true;
                break;
            }
        }
        if (!toolFound)
        {
            newTool = Instantiate(newTool, gameObject.transform);
            newTool.Quantity = 1;
            toolList.Add(newTool);
        }
        return newTool;
    }

    private T RemoveInventoryToolByIndex<T>(ref List<T> toolList, int index) where T : ToolForInventory
    {
        if (index < 0 || index >= toolList.Count) return null;
        T t = toolList[index];
        if (t.Quantity <= 1)
        {
            Destroy(toolList[index]);
            toolList.RemoveAt(index);
        }
        else t.Quantity--;
        NumberOfTools--;
        return t;
    }

    private int RemoveInventoryToolByValue<T>(ref List<T> toolList, T tool) where T : ToolForInventory
    {
        int i = toolList.Count - 1;
        for (; i >= 0; i--)
        {
            T t = toolList[i];
            if (tool.Id != t.Id) continue;
            if (t.Quantity <= 1)
            {
                Destroy(toolList[i]);
                toolList.RemoveAt(i);
            }
            else t.Quantity--;
            NumberOfTools--;
            break;
        }
        return i;
    }
}
