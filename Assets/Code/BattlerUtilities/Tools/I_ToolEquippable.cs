using System.Collections.Generic;
using UnityEngine;

public interface IToolEquippable : IToolForInventory
{
    bool CanEquipWith(BattlerClass c);
}