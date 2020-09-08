using System;
using UnityEngine;

public class PlayerCompanionship : MonoBehaviour
{
    public BattlePlayer Player;
    public int Points;
    public int Level;
    private int[] LevelGrowthRatesPerArea;

    private void Awake()
    {
        Level = 1;
        LevelGrowthRatesPerArea = new int[10];
    }
}
