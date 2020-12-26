﻿using System.Collections;
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

    public const string MAP_MENU_SCENE = "MapMenu";
    public const string FILE_SELECT_MENU_SCENE = "FileSelect";
    public const string BATTLE_SCENE = "Battle";
    public const string SCREEN_TRANSITION_SCENE = "ScreenTransition";
    public const float BATTLE_TRANSITION_TIME = 1f;

    public static void ChangeScene(string sceneName, float blackScreenTime,
        ScreenTransitioner.SceneChangeModes changeMode = ScreenTransitioner.SceneChangeModes.Change,
        ScreenTransitioner.TransitionModes transitionMode = ScreenTransitioner.TransitionModes.BlackScreen)
    {
        string oldScene = SceneManager.GetActiveScene().name;
        string newScene = sceneName;
        ScreenTransitioner.SetupComponents(oldScene, newScene, blackScreenTime, changeMode, transitionMode);
        SceneManager.LoadScene(SCREEN_TRANSITION_SCENE, LoadSceneMode.Additive);
    }

    public static void StartBattle(PlayerParty playerParty, EnemyParty enemyParty)
    {
        BattleMaster.Setup(null, playerParty, enemyParty);
        StoreGameObjects();
        ChangeScene(BATTLE_SCENE, BATTLE_TRANSITION_TIME, ScreenTransitioner.SceneChangeModes.Add, ScreenTransitioner.TransitionModes.Batte);
    }

    public static void EndBattle(PlayerParty playerParty)
    {
        ChangeScene(BATTLE_SCENE, BATTLE_TRANSITION_TIME / 2, ScreenTransitioner.SceneChangeModes.Remove, ScreenTransitioner.TransitionModes.BlackScreen);
        MapMaster.EnemyEncountered.DeclareDefeated();
    }

    public static void OpenMenu(PlayerParty playerParty)
    {
        MenuMaster.PartyInfo = playerParty;
        SceneManager.LoadScene(MAP_MENU_SCENE, LoadSceneMode.Additive);
        InMapMenu = true;
        Time.timeScale = 0;
    }

    public static void CloseMenu(PlayerParty playerParty)
    {
        MenuMaster.PartyInfo = playerParty;
        SceneManager.UnloadSceneAsync(MAP_MENU_SCENE);
        InMapMenu = false;
        Time.timeScale = 1;
    }

    public static void OpenFileSelect(FileSelect.FileMode fileMode, PlayerParty playerParty = null)
    {
        if (MenuMaster.PartyInfo) MenuMaster.PartyInfo = playerParty;
        FileSelect.FileSelectMode = fileMode;
        SceneManager.LoadScene(FILE_SELECT_MENU_SCENE, LoadSceneMode.Additive);
        Time.timeScale = 0;
    }

    public static void CloseFileSelect()
    {
        SceneManager.UnloadSceneAsync(FILE_SELECT_MENU_SCENE);
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
