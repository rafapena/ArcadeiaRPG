using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleWin : MonoBehaviour
{
    // Selection process navigation
    private enum Selections
    {
        None,
        FirstWinScreen,
        LevelUp,
        CompanionShipUp,
        ItemInventoryFull_SelectNew, ItemInventoryFull_SelectOld, AreYouSureDiscardItems, ItemDiscarded
    }

    // Battle data, including parties, levelling/exp and inventory info
    public Battle FinishedBattle;

    // Selection management
    private Selections Selection;
    private string KeyPressed;

    // Loading + Animating
    private float DisableTime;
    private bool TriggerAnimation;

    // Child GameObjects
    public MenuFrame VictoryBanner;
    public MenuFrame LevelEXP;
    public MenuFrame ItemsObtained;
    public MenuFrame GoldObtained;
    public MenuFrame LevelUp;
    public MenuFrame CompanionShipUp;
    public MenuFrame ProceedButton;

    // General UI
    private Color CInfoFrameMainColor;
    private Color CInfoFrameLevelUpColor;

    // Level and EXP
    private int LevelUps;
    private Gauge EXPGauge;     // Quickly access the child Game Object
    private int EXPEarned;
    private int EXPIncreaseSpeed;
    private int NewEXPTotal;

    // Gold and Items
    private int GoldEarned;
    private int GoldIncreaseSpeed;
    private int NewGoldTotal;
    private List<ToolForInventory> ItemsEarned;

    // Companionships
    private List<PlayerCompanionship> CompaionshipUpPlayers;
    private List<int> CompanionshipUpBoostAmounts;

    private void Start()
    {
        Selection = Selections.None;
        CInfoFrameMainColor = LevelEXP.GetComponent<Image>().color;
        CInfoFrameLevelUpColor = new Color(1, 0.9f, 0.3f, CInfoFrameMainColor.a);
        ItemsEarned = new List<ToolForInventory>();
        CompaionshipUpPlayers = new List<PlayerCompanionship>();
        CompanionshipUpBoostAmounts = new List<int>();
    }

    private void LateUpdate()
    {
        if (DisableTime > Time.time) return;
        KeyPressed = Input.inputString.ToUpper();
        switch (Selection)
        {
            case Selections.FirstWinScreen:
                AnimateWinScreenContents();
                if (FinishedBattle.PlayerParty.Inventory.Gold == NewGoldTotal && FinishedBattle.PlayerParty.EXP == NewEXPTotal)
                {
                    if (LevelUps > 0) SetupForLevelUp();
                    else if (CompaionshipUpPlayers.Count > 0) SetupForCompanionshipUp();
                    else ProceedButton.Activate();
                }
                if (KeyPressed == "Z" && ProceedButton.Activated) ExitBattle();
                break;
            case Selections.LevelUp:
                break;
            case Selections.CompanionShipUp:
                break;
            case Selections.ItemInventoryFull_SelectNew:
                break;
            case Selections.ItemInventoryFull_SelectOld:
                break;
            case Selections.AreYouSureDiscardItems:
                break;
            case Selections.ItemDiscarded:
                break;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- First Win Screen --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Setup()
    {
        FinishedBattle.PlayerParty.SetupExpCurve();
        Selection = Selections.FirstWinScreen;
        VictoryBanner.Activate();
        LevelEXP.Activate();
        ItemsObtained.Activate();
        GoldObtained.Activate();
        SetupEnemyInfo();
        SetupLevelEXPInfo();
        SetupItems();
        SetupGold();
        UpdateCompanionships();
        DisableTime = Time.time + 2f;
    }

    private void SetupEnemyInfo()
    {
        foreach (BattleEnemy e in FinishedBattle.EnemyParty.Enemies)
        {
            EXPEarned += e.Exp;
            GoldEarned += e.Gold;
            foreach (ItemDropRate idr in e.DroppedItems)
            {
                if (Random.Range(0f, 100f) >= idr.Rate) continue;
                bool itemAlreadyInList = false;
                for (int i = 0; i < ItemsEarned.Count; i++)
                {
                    if (idr.ItemDropped.Id == ItemsEarned[i].Id)
                    {
                        ItemsEarned[i].Quantity++;
                        itemAlreadyInList = true;
                        break;
                    }
                }
                if (itemAlreadyInList) continue;
                ToolForInventory item = Instantiate(idr.ItemDropped);
                item.Quantity = 1;
                ItemsEarned.Add(item);
            }
        }
    }

    private void SetupLevelEXPInfo()
    {
        EXPGauge = LevelEXP.transform.GetChild(1).GetChild(2).GetComponent<Gauge>();
        int gCurr = FinishedBattle.PlayerParty.EXP - FinishedBattle.PlayerParty.LastEXPToNext;
        int gMax = FinishedBattle.PlayerParty.EXPToNext - FinishedBattle.PlayerParty.LastEXPToNext;
        EXPGauge.Set(gCurr, gMax);
        //EXPIncreaseSpeed = (int)(gMax * EXPGauge.ChangePerSecond);
        NewEXPTotal = FinishedBattle.PlayerParty.EXP + EXPEarned;
        UpdateForLevelUpInfo();
        UpdateEXPInfo();
    }

    private void UpdateForLevelUpInfo()
    {
        TextMeshProUGUI lvlTxt = LevelEXP.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        lvlTxt.text = FinishedBattle.PlayerParty.Level.ToString();
        if (LevelUps > 0) lvlTxt.color = new Color(0.7f, 1, 0.5f);
        LevelEXP.transform.GetChild(1).GetChild(4).GetComponent<TextMeshProUGUI>().text = "/ " + (FinishedBattle.PlayerParty.EXPToNext - FinishedBattle.PlayerParty.LastEXPToNext);
    }

    private void UpdateEXPInfo()
    {
        int totalExp = FinishedBattle.PlayerParty.EXP;
        LevelEXP.transform.GetChild(1).GetChild(1).GetComponent<TextMeshProUGUI>().text = EXPEarned > 0 ? ("+" + EXPEarned) : "";
        LevelEXP.transform.GetChild(1).GetChild(3).GetComponent<TextMeshProUGUI>().text = (totalExp - FinishedBattle.PlayerParty.LastEXPToNext).ToString();
        LevelEXP.transform.GetChild(1).GetChild(6).GetComponent<TextMeshProUGUI>().text = totalExp.ToString();
    }

    private void SetupItems()
    {
        int i = 0;
        if (ItemsEarned.Count == 0)
        {
            ItemsObtained.transform.GetChild(i++).gameObject.SetActive(true);
            for (; i < ItemsObtained.transform.childCount; i++)
                ItemsObtained.transform.GetChild(i).gameObject.SetActive(false);
            return;
        }
        ItemsObtained.transform.GetChild(0).gameObject.SetActive(false);
        int childSize = ItemsObtained.transform.childCount - 1;
        int limit = ItemsEarned.Count < childSize ? ItemsEarned.Count : childSize;
        for (; i < limit; i++)
        {
            Transform it = ItemsObtained.transform.GetChild(i + 1);
            it.gameObject.SetActive(true);
            it.GetChild(0).GetComponent<TextMeshProUGUI>().text = ItemsEarned[i].Name;
            it.GetChild(1).GetComponent<Image>().sprite = ItemsEarned[i].GetComponent<SpriteRenderer>().sprite;
            it.GetChild(2).GetComponent<TextMeshProUGUI>().text = "+" + ItemsEarned[i].Quantity;
        }
        for (; i < childSize; i++)
            ItemsObtained.transform.GetChild(i + 1).gameObject.SetActive(false);
    }

    private void SetupGold()
    {
        GoldObtained.gameObject.SetActive(GoldEarned > 0);
        GoldIncreaseSpeed = GoldEarned / 60;
        NewGoldTotal = FinishedBattle.PlayerParty.Inventory.Gold + GoldEarned;
        UpdateGoldInfo();
    }

    private void UpdateGoldInfo()
    {
        GoldObtained.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = GoldEarned > 0 ? ("+" + GoldEarned) : "";
        GoldObtained.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = FinishedBattle.PlayerParty.Inventory.Gold.ToString();
    }

    private void UpdateCompanionships()
    {
        /*foreach (PlayerCompanionship pc in FinishedBattle.PlayerParty.PartyPlayerCompanionShips)
        {
            //
        }*/
    }

    private void AnimateWinScreenContents()
    {
        int lastExp = FinishedBattle.PlayerParty.LastEXPToNext;
        if (!TriggerAnimation)
        {
            TriggerAnimation = true;
            EXPGauge.SetAndAnimate(NewEXPTotal - lastExp, FinishedBattle.PlayerParty.EXPToNext - lastExp);
        }
        else if (FinishedBattle.PlayerParty.EXP - lastExp >= FinishedBattle.PlayerParty.EXPToNext - lastExp)
        {
            NotifyLevelUp();
        }
        AnimateTotalCount(ref EXPEarned, ref FinishedBattle.PlayerParty.EXP, EXPIncreaseSpeed, NewEXPTotal);
        AnimateTotalCount(ref GoldEarned, ref FinishedBattle.PlayerParty.Inventory.Gold, GoldIncreaseSpeed, NewGoldTotal);
        UpdateEXPInfo();
        UpdateGoldInfo();
    }

    private void NotifyLevelUp()
    {
        LevelUps++;
        FinishedBattle.PlayerParty.LevelUp();
        int lastExp = FinishedBattle.PlayerParty.LastEXPToNext;
        EXPGauge.Empty();
        EXPGauge.SetAndAnimate(NewEXPTotal - lastExp, FinishedBattle.PlayerParty.EXPToNext - lastExp);
        Instantiate(UIMaster.Popups["LevelUp"], transform.position, Quaternion.identity);
        LevelEXP.GetComponent<Image>().color = CInfoFrameLevelUpColor;
        GoldObtained.GetComponent<Image>().color = CInfoFrameLevelUpColor;
        ItemsObtained.GetComponent<Image>().color = CInfoFrameLevelUpColor;
        UpdateForLevelUpInfo();
    }

    private void AnimateTotalCount(ref int earnedDisplay, ref int currentTotal, int earnSpeed, int savedNewTotal)
    {
        if (earnedDisplay > 0)
        {
            earnedDisplay -= earnSpeed;
            currentTotal += earnSpeed;
            return;
        }
        earnedDisplay = 0;
        currentTotal = savedNewTotal;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Level Up --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupForLevelUp()
    {
        Selection = Selections.LevelUp;
        LevelUp.Activate();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- CompanionShip Up --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupForCompanionshipUp()
    {
        Selection = Selections.CompanionShipUp;
        CompanionShipUp.Activate();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Item Inventory Replace --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupForInventoryReplace()
    {

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Exit screen --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ExitBattle()
    {
        SceneMaster.EndBattle(FinishedBattle.PlayerParty);
    }
}
