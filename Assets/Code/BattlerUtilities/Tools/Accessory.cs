using System.Collections.Generic;
using UnityEngine;

public class Accessory : PassiveEffect, IToolEquippable
{
    public int HPMin;
    public int HPMax = 100;
    public int SPMin;
    public int SPMax = 100;
    public bool AnyState;
    public bool NoState;
    public State StateActive1;
    public State StateActive2;
    public State StateInactive1;
    public State StateInactive2;
    public int ExpGainRate;
    public int GoldGainRate;
    public int AllyCondition;
    public int FoeCondition;
    public int UserCondition;
    public List<BattlerClass> ClassExclusives;

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

    public bool CanEquipWith(BattlerClass c) => ClassExclusives.Count == 0 || ClassExclusives.Contains(c);
}
