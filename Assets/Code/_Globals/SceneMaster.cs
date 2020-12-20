using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMaster : MonoBehaviour
{
    public static bool InMapMenu = false;
    public static bool InBattle = false;
    public static List<GameObject> StoredMapScene;

    public const string SCREEN_TRANSITION_SCENE = "ScreenTransition";
    public const float BATTLE_TRANSITION_TIME = 1f;

    public static void ChangeScene(string sceneName, float blackScreenTime,
        ScreenTransitioner.SceneChangeModes changeMode = ScreenTransitioner.SceneChangeModes.Change,
        ScreenTransitioner.TransitionModes transitionMode = ScreenTransitioner.TransitionModes.BlackScreen)
    {
        ScreenTransitioner.SetupComponents(sceneName, blackScreenTime, changeMode, transitionMode);
        SceneManager.LoadScene(SCREEN_TRANSITION_SCENE, LoadSceneMode.Additive);
    }

    public static void StartBattle(PlayerParty playerParty, EnemyParty enemyParty)
    {
        BattleMaster.Setup(null, playerParty, enemyParty);
        StoreGameObjects();
        ChangeScene("Battle", BATTLE_TRANSITION_TIME, ScreenTransitioner.SceneChangeModes.Add, ScreenTransitioner.TransitionModes.Batte);
    }

    public static void EndBattle(PlayerParty playerParty)
    {
        ChangeScene("Battle", BATTLE_TRANSITION_TIME / 2, ScreenTransitioner.SceneChangeModes.Remove, ScreenTransitioner.TransitionModes.BlackScreen);
        MapMaster.EnemyEncountered.DeclareDefeated();
    }

    public static void OpenMenu(PlayerParty playerParty)
    {
        MenuMaster.PartyInfo = playerParty;
        SceneManager.LoadScene("MapMenu", LoadSceneMode.Additive);
        InMapMenu = true;
        Time.timeScale = 0;
    }

    public static void CloseMenu(PlayerParty playerParty)
    {
        MenuMaster.PartyInfo = playerParty;
        SceneManager.UnloadSceneAsync("MapMenu");
        InMapMenu = false;
        Time.timeScale = 1;
    }

    public static void StoreGameObjects()
    {
        StoredMapScene = new List<GameObject>();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject g in allObjects) StoredMapScene.Add(g);
    }

    public static void ActivateStoredGameObjects()
    {
        foreach (GameObject g in StoredMapScene)
            if (g) g.SetActive(true);
    }

    public static void DeactivateStoredGameObjects()
    {
        foreach (GameObject g in StoredMapScene)
            if (g) g.SetActive(false);
    }
}
