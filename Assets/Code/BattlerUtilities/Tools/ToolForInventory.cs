using System.Collections.Generic;
using UnityEngine;

public abstract class ToolForInventory : Tool
{
    public int DefaultPrice;
    public List<ToolQuantity> RequiredTools;
    public int Quantity;
    public int Weight;

    public bool IsCraftable()
    {
        return RequiredTools.Count > 0;
    }
}