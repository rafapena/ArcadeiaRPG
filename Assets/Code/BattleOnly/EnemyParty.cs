using System.Collections.Generic;
using UnityEngine;

public class EnemyParty : MonoBehaviour
{
    public List<BattleEnemy> Enemies;
    public List<Item> ItemInventory;
    public bool RunDisabled;
    public bool ShowVictoryWhenDefeated = true;
    public bool HasGameOverScreen = true;

    [HideInInspector] public int[][] IndexMapping;

    public void Start()
    {
        //
    }
}
