using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMaster : MonoBehaviour
{
    private static bool InMapMenu = false;
    private static bool InPauseMenu = false;
    private static bool InFileSelectMenu = false;
    private static int InShopMenu = 0;
    private static bool InShop = false;
    private static bool InStorageMenu = false;

    public static bool InMenu => InMapMenu || InPauseMenu || InFileSelectMenu || InShop || InStorageMenu;

    public static bool BuyingInShop => InShopMenu == 1;

    public static bool InCutscene { get; private set; } = false;

    public static bool InBattle { get; private set; } = false;

    public static List<GameObject> StoredMapScene { get; private set; }

    public const string TITLE_SCREEN_SCENE = "Title";
    public const string MAP_MENU_SCENE = "MapMenu";
    public const string PAUSE_MENU_SCENE = "PauseMenu";
    public const string FILE_SELECT_MENU_SCENE = "FileSelect";
    public const string SHOP_SCENE = "Shop";
    public const string STORAGE_SCENE = "Storage";
    public const string CHANGE_CLASS_SCENE = "ChangeClass";
    public const string BATTLE_SCENE = "Battle";
    public const string GAME_OVER_SCENE = "GameOver";
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

    public static void OpenGameOver()
    {
        InBattle = false;
        SceneManager.LoadScene(GAME_OVER_SCENE, LoadSceneMode.Additive);
        Time.timeScale = 0;
    }

    public static void CloseGameOver()
    {
        Destroy(MapMaster.EnemyEncountered.gameObject);
        SceneManager.UnloadSceneAsync(BATTLE_SCENE);
        SceneManager.UnloadSceneAsync(GAME_OVER_SCENE);
        Time.timeScale = 1;
    }

    public static void StartBattle(EnemyParty enemyParty)
    {
        InBattle = true;
        GameplayMaster.EnemyGroup = enemyParty;
        StoreGameObjects();
        ChangeScene(BATTLE_SCENE, BATTLE_TRANSITION_TIME, ScreenTransitioner.SceneChangeModes.Add, ScreenTransitioner.TransitionModes.Battle);
    }

    public static void EndBattle()
    {
        MenuMaster.SetupSelectionBufferInGameplay(0.5f);
        ChangeScene(BATTLE_SCENE, BATTLE_TRANSITION_TIME / 2, ScreenTransitioner.SceneChangeModes.Remove, ScreenTransitioner.TransitionModes.BlackScreen);
        MapMaster.EnemyEncountered.DeclareDefeated();
        InBattle = false;
    }

    public static void ApplyBattleStartChanges()
    {
        DeactivateStoredGameObjects();
        GameplayMaster.PlayerContainer.SetActive(true);
        GameplayMaster.Party.gameObject.SetActive(true);
        foreach (Transform t in GameplayMaster.Party.transform) t.gameObject.SetActive(true);
        SceneManager.MoveGameObjectToScene(GameplayMaster.PlayerContainer, SceneManager.GetSceneByName(BATTLE_SCENE));
    }

    public static void ApplyBattleEndChanges()
    {
        ActivateStoredGameObjects();
        GameplayMaster.OverworldAvatar.gameObject.SetActive(true);
        GameplayMaster.Party.gameObject.SetActive(false);
        SceneManager.MoveGameObjectToScene(GameplayMaster.PlayerContainer, SceneManager.GetSceneByName(MapMaster.SceneName));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Opening/closeing UI menus --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static void OpenMenu(string sceneName, ref bool menuCheck)
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        menuCheck = true;
        Time.timeScale = 0;
    }

    private static void CloseMenu(string sceneName, ref bool menuCheck)
    {
        MenuMaster.SetupSelectionBufferInGameplay(0.3f);
        SceneManager.UnloadSceneAsync(sceneName);
        menuCheck = false;
        if (!InMenu) Time.timeScale = 1;
    }

    public static void OpenMapMenu() => OpenMenu(MAP_MENU_SCENE, ref InMapMenu);

    public static void CloseMapMenu() => CloseMenu(MAP_MENU_SCENE, ref InMapMenu);

    public static void OpenPauseMenu() => OpenMenu(PAUSE_MENU_SCENE, ref InPauseMenu);

    public static void ClosePauseMenu() => CloseMenu(PAUSE_MENU_SCENE, ref InPauseMenu);

    public static void OpenFileSelect(FileSelect.FileMode fileMode)
    {
        FileSelect.FileSelectMode = fileMode;
        OpenMenu(FILE_SELECT_MENU_SCENE, ref InFileSelectMenu);
    }

    public static void CloseFileSelect() => CloseMenu(FILE_SELECT_MENU_SCENE, ref InFileSelectMenu);

    public static void OpenShop(bool onlyBuying, Shopkeeper shop)
    {
        GameplayMaster.Shop = shop;
        InShopMenu = onlyBuying ? 1 : 2;
        OpenMenu(SHOP_SCENE, ref InShop);
    }

    public static void CloseShop() => CloseMenu(SHOP_SCENE, ref InShop);

    public static void OpenStorage() => OpenMenu(STORAGE_SCENE, ref InStorageMenu);

    public static void CloseStorage() => CloseMenu(STORAGE_SCENE, ref InStorageMenu);

    public static void OpenCraftingMenu(Crafter crafter, InventorySystem.ListType craftables)
    {
        GameplayMaster.CraftingMode = craftables;
        OpenMapMenu();
    }

    public static void CloseCraftingMenu()
    {
        GameplayMaster.CraftingMode = InventorySystem.ListType.None;
        CloseMapMenu();
    }

    public static void OpenChangeClassMenu() => OpenMenu(CHANGE_CLASS_SCENE, ref InShop);

    public static void CloseChangeClassMenu() => CloseMenu(CHANGE_CLASS_SCENE, ref InShop);

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Cutscenes/Other --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void OpenCutscene()
    {
        InCutscene = true;
    }

    public static void CloseCutscene()
    {
        MenuMaster.SetupSelectionBufferInGameplay(0.5f);
        InCutscene = false;
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
