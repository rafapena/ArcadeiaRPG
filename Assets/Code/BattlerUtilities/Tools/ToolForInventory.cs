using System.Collections.Generic;
using UnityEngine;

public abstract class ToolForInventory : Tool
{
    public int DefaultPrice;
    public List<ToolQuantity> RequiredTools;
    public int Quantity;
    public int Weight;

    public bool CanRemove => DefaultPrice > 0;

    public int DefaultSellPrice => DefaultPrice / 2;

    public bool IsCraftable()
    {
        return RequiredTools.Count > 0;
    }
}