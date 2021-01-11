using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMaster
{
    public enum Locations
    {
        Test_Land_1,
        Test_Land_2
    }

    public static string SceneName { get; private set; }

    public static string CurrentLocation;
    public static int Elevation;
    public static MapEnemy EnemyEncountered;

    public static void SetScene(GameObject go)
    {
        SceneName = go.scene.name;
    }
}
