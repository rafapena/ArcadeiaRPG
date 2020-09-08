using System.Collections.Generic;
using UnityEngine;

public class Item : ToolForInventory
{
    public enum UseConditions { None, MapOnly, BattleOnly, Anywhere }

    public UseConditions UseCondition;
    public bool IsKey;
    public bool Consumable = true;
    public Stats PermantentStatChanges;
    public Item TurnsInto;

    private void Start()
    {

    }

    private void Update()
    {

    }
}
