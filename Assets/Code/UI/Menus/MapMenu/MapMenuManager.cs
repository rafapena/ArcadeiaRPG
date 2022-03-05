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
        PartyInfo = GameplayMaster.Party;
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
        if (CloseAll()) SceneMaster.CloseMapMenu(PartyInfo);
    }

    private bool CloseAll()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return false;
        Main.Close();
        Inventory.Close();
        Equips.Close();
        Special.Close();
        Team.Close();
        Stats.Close();
        Map.Close();
        Objectives.Close();
        return true;
    }

    public void GoToMain()
    {
        if (CloseAll()) Main.Open();
    }

    public void GoToInventory()
    {
        if (CloseAll()) Inventory.Open();
    }

    public void GoToEquips()
    {
        if (CloseAll()) Equips.Open();
    }

    public void GoToSpecial()
    {
        if (CloseAll()) Special.Open();
    }

    public void GoToTeam()
    {
        if (CloseAll()) Team.Open();
    }

    public void GoToStats()
    {
        if (CloseAll()) Stats.Open();
    }

    public void GoToMap()
    {
        if (CloseAll()) Map.Open();
    }

    public void GoToObjectives()
    {
        if (CloseAll()) Objectives.Open();
    }
}
