using System.Collections.Generic;
using UnityEngine;

public class EnemyParty : BaseObject
{
    public enum EnemyPartyModes { Regular, Boss, FinalBoss }

    public List<BattleEnemy> Enemies;
    public List<Item> ItemInventory;
    public bool RunDisabled;
    public EnemyPartyModes PartyMode;
    public bool GameOverOnLose = true;

    [HideInInspector] public int[][] IndexMapping;

    public void Start()
    {
        LogEnemyPositions();
    }

    public void LogEnemyPositions()
    {
        IndexMapping = new int[System.Enum.GetNames(typeof(Battler.HorizontalPositions)).Length][];
        for (int i = 0; i < IndexMapping.Length; i++)
        {
            IndexMapping[i] = new int[System.Enum.GetNames(typeof(Battler.VerticalPositions)).Length];
            for (int j = 0; j < IndexMapping[i].Length; j++)
                IndexMapping[i][j] = -1;
        }
        for (int i = 0; i < Enemies.Count; i++)
            IndexMapping[(int)Enemies[i].RowPosition][(int)Enemies[i].ColumnPosition] = i;
    }

    /*public void ApplySmartLetterUI_IndividualTarget()
    {
        if (IndexMapping[1][0] >= 0)
        {
            Enemies[IndexMapping[1][0]].TargetLetterCommand = "A";
        }
    }

    private void FillUIColumn(int c, string s1, string s2, string s3)
    {
        if (IndexMapping[1][c])
    }

    public void ApplySmartLetterUI_IndividualRows()
    {

    }

    public void ApplySmartLetterUI_IndividualColumns()
    {

    }*/

    public List<Battler> ConvertToGeneric()
    {
        List<Battler> enemies = new List<Battler>();
        foreach (BattleEnemy e in Enemies) enemies.Add(e);
        return enemies;
    }
}
