using System.Collections.Generic;
using UnityEngine;

public interface IToolForInventory
{
    DataObject Info { get; }


    int Price { get; set; }

    List<ItemOrWeaponQuantity> RequiredTools { get; set; }

    int Weight { get; set; }

    int Quantity { get; set; }


    bool CanRemove { get; }

    int SellPrice { get; }

    bool IsCraftable { get; }
}