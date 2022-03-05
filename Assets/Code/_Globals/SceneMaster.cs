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

    public static bool InMenu => InMapMenu || InPauseMenu || InFileSelectMenu;

    public static bool InCutscene { get; private set; } = false;

    public static bool InBattle { get; private set; } = false;
     
    public static List<GameObject> StoredMapScene;

    public const string TITLE_SCREEN_SCENE = "Title";
    public const string MAP_MENU_SCENE = "MapMenu";
    public const string PAUSE_MENU_SCENE = "PauseMenu";
    public const string FILE_SELECT_MENU_SCENE = "FileSelect";
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

    public static void StartBattle(PlayerParty playerParty, EnemyParty enemyParty)
    {
        InBattle = true;
        BattleMaster.Setup(null, playerParty, enemyParty);
        StoreGameObjects();
        ChangeScene(BATTLE_SCENE, BATTLE_TRANSITION_TIME, ScreenTransitioner.SceneChangeModes.Add, ScreenTransitioner.TransitionModes.Batte);
    }

    public static void EndBattle(PlayerParty playerParty)
    {
        MenuMaster.SetupSelectionBufferInGameplay(0.5f);
        ChangeScene(BATTLE_SCENE, BATTLE_TRANSITION_TIME / 2, ScreenTransitioner.SceneChangeModes.Remove, ScreenTransitioner.TransitionModes.BlackScreen);
        MapMaster.EnemyEncountered.DeclareDefeated();
        InBattle = false;
    }

    public static void OpenMapMenu(PlayerParty playerParty)
    {
        GameplayMaster.Party = playerParty;
        SceneManager.LoadScene(MAP_MENU_SCENE, LoadSceneMode.Additive);
        InMapMenu = true;
        Time.timeScale = 0;
    }

    public static void CloseMapMenu(PlayerParty playerParty)
    {
        MenuMaster.SetupSelectionBufferInGameplay(0.5f);
        GameplayMaster.Party = playerParty;
        SceneManager.UnloadSceneAsync(MAP_MENU_SCENE);
        InMapMenu = false;
        if (!InMenu) Time.timeScale = 1;
    }

    public static void OpenPauseMenu(PlayerParty playerParty)
    {
        GameplayMaster.Party = playerParty;
        SceneManager.LoadScene(PAUSE_MENU_SCENE, LoadSceneMode.Additive);
        InPauseMenu = true;
        Time.timeScale = 0;
    }

    public static void ClosePauseMenu(PlayerParty playerParty)
    {
        MenuMaster.SetupSelectionBufferInGameplay(0.5f);
        GameplayMaster.Party = playerParty;
        SceneManager.UnloadSceneAsync(PAUSE_MENU_SCENE);
        InPauseMenu = false;
        if (!InMenu) Time.timeScale = 1;
    }

    public static void OpenFileSelect(FileSelect.FileMode fileMode, PlayerParty playerParty = null)
    {
        if (!GameplayMaster.Party) GameplayMaster.Party = playerParty;
        FileSelect.FileSelectMode = fileMode;
        SceneManager.LoadScene(FILE_SELECT_MENU_SCENE, LoadSceneMode.Additive);
        InFileSelectMenu = true;
        Time.timeScale = 0;
    }

    public static void CloseFileSelect()
    {
        MenuMaster.SetupSelectionBufferInGameplay();
        SceneManager.UnloadSceneAsync(FILE_SELECT_MENU_SCENE);
        InFileSelectMenu = false;
        if (!InMenu) Time.timeScale = 1;
    }

    public static void OpenCutscene()
    {
        InCutscene = true;
    }

    public static void CloseCutscene()
    {
        MenuMaster.SetupSelectionBufferInGameplay();
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
