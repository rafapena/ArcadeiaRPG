using System.Collections.Generic;
using UnityEngine;

public abstract class ToolForInventory : Tool
{
    public int DefaultPrice;
    public List<ToolForInventory> RequiredTools;
    public int Quantity;

    public bool IsCraftable()
    {
        return RequiredTools.Count > 0;
    }
}