using System.Collections.Generic;
using UnityEngine;

public interface IToolEquippable : IToolForInventory
{
    List<BattlerClass> ClassExclusives { get; }
}