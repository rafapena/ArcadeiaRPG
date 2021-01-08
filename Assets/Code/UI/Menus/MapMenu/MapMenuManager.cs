using System;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapMenuManager : MonoBehaviour
{
    // Menu Pages
    public MMMain Main;
    public MMInventory Inventory;
    public MMEquips Equips;
    public MMSpecial Special;
    public MMTeam Team;
    public MMStats Stats;
    public MMObjectives Objectives;
    public MMMap Map;

    [HideInInspector] public PlayerParty PartyInfo;

    private void Awake()
    {
        if (!Main.gameObject.activeSelf) Main.gameObject.SetActive(true);
        if (!Inventory.gameObject.activeSelf) Inventory.gameObject.SetActive(true);
        if (!Equips.gameObject.activeSelf) Equips.gameObject.SetActive(true);
        if (!Special.gameObject.activeSelf) Special.gameObject.SetActive(true);
        if (!Team.gameObject.activeSelf) Team.gameObject.SetActive(true);
        if (!Stats.gameObject.activeSelf) Stats.gameObject.SetActive(true);
        if (!Map.gameObject.activeSelf) Map.gameObject.SetActive(true);
        if (!Objectives.gameObject.activeSelf) Objectives.gameObject.SetActive(true);
        PartyInfo = MenuMaster.PartyInfo;
        Main.Open();
    }

    private void Update()
    {
        if (InputMaster.GoingBack())
        {
            if (Main.MainComponent.Activated) Main.GoBack();
            else if (Inventory.MainComponent.Activated) Inventory.GoBack();
            else if (Equips.MainComponent.Activated) Equips.GoBack();
            else if (Special.MainComponent.Activated) Special.GoBack();
            else if (Team.MainComponent.Activated) Team.GoBack();
            else if (Stats.MainComponent.Activated) Stats.GoBack();
            else if (Map.MainComponent.Activated) Map.GoBack();
            else if (Objectives.MainComponent.Activated) Objectives.GoBack();
        }
    }

    public void ExitAll()
    {
        CloseAll();
        SceneMaster.CloseMapMenu(PartyInfo);
    }

    private void CloseAll()
    {
        Main.Close();
        Inventory.Close();
        Equips.Close();
        Special.Close();
        Team.Close();
        Stats.Close();
        Map.Close();
        Objectives.Close();
    }

    public void GoToMain()
    {
        CloseAll();
        Main.Open();
    }

    public void GoToInventory()
    {
        CloseAll();
        Inventory.Open();
    }

    public void GoToEquips()
    {
        CloseAll();
        Equips.Open();
    }

    public void GoToSpecial()
    {
        CloseAll();
        Special.Open();
    }

    public void GoToTeam()
    {
        CloseAll();
        Team.Open();
    }

    public void GoToStats()
    {
        CloseAll();
        Stats.Open();
    }

    public void GoToMap()
    {
        CloseAll();
        Map.Open();
    }

    public void GoToObjectives()
    {
        CloseAll();
        Objectives.Open();
    }
}
