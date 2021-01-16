using System;
using UnityEngine;

public class PlayerRelation
{
    public enum RelationLevels { Acquaintances, Friends, BestFriends }
    private static int[] PointMarkers = new int[] { 100, 200, 300 };
    private int MAX_RELATION_LEVEL;

    public BattlePlayer Player;

    public int Points { get; private set; }
    
    public int Level { get; private set; }

    public PlayerRelation(BattlePlayer player, RelationLevels relationLevel)
    {
        MAX_RELATION_LEVEL = Enum.GetNames(typeof(RelationLevels)).Length;
        Player = player;
        Level = (int)relationLevel + 1;
        Points = PointMarkers[Level - 1];
    }

    public int SetPoints(int points)
    {
        Points = points;
        int gain = 0;
        while (Level < MAX_RELATION_LEVEL && Points > PointMarkers[Level - 1]) gain++;
        while (Level > 1 && Points < PointMarkers[Level - 2]) gain--;
        Level += gain;
        return gain;
    }
}
