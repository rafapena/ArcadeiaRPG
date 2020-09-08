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

    public void ChangeScene(string sceneName, string BlackScreenTime)
    {
        SceneManager.LoadScene("ScreenTransition", LoadSceneMode.Additive);
    }

    public static void StartBattle(PlayerParty playerParty, EnemyParty enemyParty)
    {
        BattleMaster.Setup(null, playerParty, enemyParty);
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject g in allObjects)
            g.SetActive(false);
        SceneManager.LoadScene("Battle", LoadSceneMode.Additive);
    }

    public static void EndBattle(PlayerParty playerParty)
    {
        SceneManager.UnloadSceneAsync("Battle");
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject g in allObjects)
            g.SetActive(true);
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
}
