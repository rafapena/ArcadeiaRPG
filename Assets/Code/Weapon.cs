using System.Collections.Generic;
using UnityEngine;

public class Weapon : ToolForInventory
{
    public Stats EquipBoosts;
    public BattleMaster.WeaponTypes WeaponType;
    public int Range = 3;
    public bool CollideRange;
}
