using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MMMain : MM_Super
{
    // Child GameObjects
    public GameObject ButtonList;
    public MenuFrame InfoSection;
    public PlayerSelectionList PartyList;

    // UI Elements
    public TextMeshProUGUI Time;
    public TextMeshProUGUI Money;
    public TextMeshProUGUI Level;
    public TextMeshProUGUI TotalEXP;
    public TextMeshProUGUI EXPLeft;
    public Gauge ToNextEXPBar;

    public override void Open()
    {
        base.Open();
        InfoSection.Activate();
        Money.text = MenuManager.PartyInfo.Inventory.Gold.ToString();
        Level.text = MenuManager.PartyInfo.Level.ToString();
        SetupEXPInfo();
        PartyList.Setup(MenuManager.PartyInfo.Players);
    }

    public override void Close()
    {
        base.Close();
        InfoSection.Deactivate();
    }

    public override void GoBack()
    {
        MenuManager.ExitAll();
    }

    protected override void ReturnToInitialStep()
    {
        //
    }

    private void SetupEXPInfo()
    {
        int exp = MenuManager.PartyInfo.EXP;
        int lastExpToNext = MenuManager.PartyInfo.LastEXPToNext;
        int expToNext = MenuManager.PartyInfo.EXPToNext;
        TotalEXP.text = exp.ToString();
        EXPLeft.text = (expToNext - exp).ToString();
        ToNextEXPBar.Set(exp - lastExpToNext, expToNext - lastExpToNext);
    }

    protected override void Update()
    {
        base.Update();
        Time.text = "12:00 PM";
    }
}
