using System;
using UnityEngine;

[Serializable]
public class PlayerRelation
{
    public static int[] PointMarkers = new int[] { 100, 200, 350, 500, 800, 1200 };

    public int MAX_RELATION_LEVEL { get; private set; }

    public BattlePlayer Player1;
    public BattlePlayer Player2;

    public int Points { get; private set; }

    [SerializeField]
    private int _Level;
    public int Level { get => _Level; private set => _Level = value; }

    public PlayerRelation(BattlePlayer player1, BattlePlayer player2)
    {
        MAX_RELATION_LEVEL = PointMarkers.Length;
        Player1 = player1;
        Player2 = player2;
        Points = 0;
        _Level = 0;
    }

    public void SetPoints(int points)
    {
        Points = points;
        int i = 0;
        while (Level != MAX_RELATION_LEVEL && points >= PointMarkers[i++]) Level++;
        while (Level != 0 && points < PointMarkers[i--]) Level--;
    }

    public void SetLevel(int level)
    {
        if (level <= 0)
        {
            Points = 0;
            Level = 0;
        }
        else if (level > PointMarkers.Length)
        {
            Points = PointMarkers[PointMarkers.Length - 1];
            Level = PointMarkers.Length;
        }
        else // Normal settings
        {
            Points = PointMarkers[level - 1];
            Level = level;
        }
    }

    public bool PlayerInRelation(BattlePlayer p)
    {
        return p.Id == Player1.Id || p.Id == Player2.Id;
    }

    public BattlePlayer GetOtherPlayerInRelationWith(BattlePlayer p)
    {
        return PlayerInRelation(p) ? (p.Id == Player1.Id ? Player2 : Player1) : null;
    }

    public void AddPoints(int points)
    {
        if (Level == MAX_RELATION_LEVEL) return;
        Points += points;
        if (Points > PointMarkers[Level]) Points = PointMarkers[Level];
    }

    public void LevelUp()
    {
        Points += 10;
        Level++;
    }
}
