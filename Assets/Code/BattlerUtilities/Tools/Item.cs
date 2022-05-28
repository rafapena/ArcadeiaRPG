using System.Collections.Generic;
using UnityEngine;

public class Item : ActiveTool, IToolForInventory
{
    public enum UseConditions { None, MapOnly, BattleOnly, Anywhere }

    public enum UseTypes { Standard, Offense, BigOffense }

    public UseConditions UseCondition;
    public UseTypes UseType;
    public bool IsKey;
    public bool Consumable = true;
    public Stats PermantentStatChanges;
    public Item TurnsInto;

    public DataObject Info => this;

    [SerializeField]
    private int _DefaultPrice;
    public int Price { get => _DefaultPrice; set => _DefaultPrice = value; }

    [SerializeField]
    private List<ItemOrWeaponQuantity> _RequiredTools;
    public List<ItemOrWeaponQuantity> RequiredTools { get => _RequiredTools; set => _RequiredTools = value; }

    [SerializeField]
    private int _Weight;
    public int Weight { get => _Weight; set => _Weight = value; }

    public int Quantity { get; set; }

    public bool CanRemove => Price > 0;
    public int SellPrice => Price / 2;
    public bool IsCraftable => RequiredTools.Count > 0;
}
