using System.Collections.Generic;
using UnityEngine;

public class Weapon : ActiveTool, IToolEquippable
{
    public Stats EquipBoosts;
    public BattleMaster.WeaponTypes WeaponType;
    public bool CollideRange;

    public BaseObject Info => this;

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

    List<BattlerClass> IToolEquippable.ClassExclusives => ClassExclusives;

    public int Index { get; set; }
}
