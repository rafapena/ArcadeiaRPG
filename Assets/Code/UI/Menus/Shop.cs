using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

public class Shop : MonoBehaviour, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    private enum Selections { None, Browsing, ConfirmingAmont, ConfirmedTransaction, SelectingEquips, ConfirmSwap }

    private Selections Selection;

    public ToolListCollectionFrame SellingFrame;
    public MenuFrame BuyingFrame;
    public InventoryToolSelectionList BuyingList;
    public GameObject BuyingListBlock;
    public TextMeshProUGUI CarryingAmount;
    public TextMeshProUGUI GoldAmount;
    public GameObject NotEnoughGoldText;

    public ConfirmAmount ConfirmFrame;
    public TextMeshProUGUI ConfirmText;
    public GameObject ResultCheckFrame;
    public TextMeshProUGUI ResultCheckCarrying;
    public TextMeshProUGUI ResultCheckGold;
    public Gauge ResultCheckCarryTracker;
    public GameObject TransactionConfirmedFrame;
    public GameObject EquipNowConfirmation;
    public MenuFrame SelectEquipsFrame;
    public GameObject ConfirmSwap;

    public GameObject CharacterEquipCheckFrame;
    public Transform CharacterEquipCheckList;

    [HideInInspector] public PlayerParty PartyInfo;
    [HideInInspector] public Shopkeeper Shopkeep;

    private ToolForInventory SelectedToolToInventory;
    private bool DoneTransaction;

    private float JustBoughtTimer;
    private float BOUGHT_TIME_BUFFER = 0.5f;

    private bool isBuying => SceneMaster.BuyingInShop;

    private bool CannotPurchase => NotEnoughGoldText.activeSelf;

    private void Start()
    {
        PartyInfo = GameplayMaster.Party;
        Shopkeep = GameplayMaster.Shop;
        PartyInfo.Inventory.UpdateToCurrentWeight();
        if (isBuying) BuyingFrame.Activate();
        else
        {
            SellingFrame.TargetFrame.Activate();
            SellingFrame.RegisterToolList(0, PartyInfo.Inventory.Items);
            SellingFrame.RegisterToolList(1, PartyInfo.Inventory.Weapons);
        }
        Initialize();
        NotEnoughGoldText.SetActive(false);
        EventSystem.current.SetSelectedGameObject(BuyingList.transform.GetChild(0).gameObject);
    }

    private void Update()
    {
        bool select = InputMaster.ProceedInMenu;
        bool back = InputMaster.GoingBack;
        switch (Selection)
        {
            case Selections.Browsing:
                if (!MenuMaster.ReadyToSelectInMenu) break;
                else if (back) CloseShop();
                else if (!isBuying) SellingFrame.SelectTabInputs();
                break;

            case Selections.ConfirmingAmont:
                if (back)
                {
                    if (isBuying) UndoConfirmAmountBuying();
                    else SellingFrame.UndoSelectTool();
                }
                break;

            case Selections.ConfirmedTransaction:
                if (!MenuMaster.ReadyToSelectInMenu) break;
                else if (isBuying && SelectedToolToInventory is Weapon)
                {
                    if (!EquipNowConfirmation.gameObject.activeSelf) SetupEquipConfirmation();
                    else break;
                }
                else if (Time.unscaledTime >= JustBoughtTimer && (select || back))
                {
                    if (isBuying) EventSystem.current.SetSelectedGameObject(BuyingList.SelectedButton.gameObject);
                    Initialize();
                }
                break;

            case Selections.SelectingEquips:
                break;

            case Selections.ConfirmSwap:
                break;
        }
    }

    private void CloseShop()
    {
        Selection = Selections.None;
        SceneMaster.CloseShop(DoneTransaction, PartyInfo, Shopkeep);
    }

    public void Initialize()
    {
        if (isBuying)
        {
            BuyingList.Refresh(Shopkeep.ToolsInStock);
            BuyingList.Selecting = true;
        }
        else
        {
            SellingFrame.SelectingToolList();
            SellingFrame.InitializeSelection();
        }
        Selection = Selections.Browsing;
        BuyingListBlock.gameObject.SetActive(false);
        CharacterEquipCheckFrame.SetActive(false);
        GoldAmount.text = PartyInfo.Inventory.Gold.ToString();
        ConfirmFrame.gameObject.SetActive(false);
        ResultCheckFrame.SetActive(false);
        TransactionConfirmedFrame.SetActive(false);
        EquipNowConfirmation.SetActive(false);
        SelectEquipsFrame.Deactivate();
        ConfirmSwap.SetActive(false);
    }

    public void HoverOverTool()
    {
        if (isBuying)
        {
            BuyingList.HoverOverTool();         // Sets up the data below
            if (BuyingList.DisplayedToolInfo)
            {
                SetupSelectToolToInventory();
                CarryingAmount.text = (SelectedToolToInventory?.Quantity ?? 0).ToString();
                NotEnoughGoldText.SetActive(PartyInfo.Inventory.Gold - BuyingList.SelectedObject.DefaultPrice < 0);
            }
            else
            {
                CarryingAmount.text = "";
                NotEnoughGoldText.SetActive(false);
            }
        }
        else SellingFrame.ToolList.HoverOverTool();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- START - Selling menu only --

    public void SelectTabSuccess()
    {
        //
    }

    public void SelectTabFailed()
    {
        //
    }

    public void SelectToolSuccess()
    {
        //
    }

    public void SelectToolFailed()
    {
        //
    }

    public void UndoSelectToolSuccess()
    {
        //
    }

    public void ActivateSorterSuccess()
    {
        //
    }

    /// -- Selling menu only - END --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectToolBuying()
    {
        if (!MenuMaster.ReadyToSelectInMenu || !BuyingList.RefreshToolInfo() || CannotPurchase) return;
        Selection = Selections.ConfirmingAmont;
        BuyingList.Selecting = false;
        BuyingListBlock.SetActive(true);
        BuyingList.SelectedButton.KeepSelected();
        SetupSelectToolToInventory();
        ConfirmFrame.Activate(1, PartyInfo.Inventory.Gold / SelectedToolToInventory.DefaultPrice);
        ConfirmText.text = "HOW MANY WILL YOU PURCHASE?";
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    private void SetupSelectToolToInventory()
    {
        if (BuyingList.SelectedObject is Item it) SelectedToolToInventory = PartyInfo.Inventory.Items.Find(x => x.Id == it.Id);
        else if (BuyingList.SelectedObject is Weapon wp) SelectedToolToInventory = PartyInfo.Inventory.Weapons.Find(x => x.Id == wp.Id);
    }

    private void UndoConfirmAmountBuying()
    {
        Selection = Selections.Browsing;
        BuyingListBlock.SetActive(false);
        ResultCheckFrame.SetActive(false);
        ConfirmFrame.Deactivate();
        BuyingList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(BuyingList.SelectedButton.gameObject);
    }

    public void ConfirmAmountMoveUp()
    {
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    public void ConfirmAmountMoveDown()
    {
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    private void UpdateResultChecker(int amount)
    {
        ResultCheckFrame.SetActive(true);
        int mod = isBuying ? 1 : -1;
        ResultCheckCarrying.text = ((SelectedToolToInventory?.Quantity ?? 0) + amount * mod).ToString();
        ResultCheckGold.text = (PartyInfo.Inventory.Gold - SelectedToolToInventory.DefaultPrice * amount * mod).ToString();
        ResultCheckCarryTracker.Set(PartyInfo.Inventory.CarryWeight + amount * mod * SelectedToolToInventory.Weight, PartyInfo.Inventory.WeightCapacity);
    }

    public void ConfirmTransaction()
    {
        Selection = Selections.ConfirmedTransaction;
        DoneTransaction = true;
        ConfirmFrame.Deactivate();
        ResultCheckFrame.SetActive(false);
        if (isBuying) ConfirmPurchase();
        else ConfirmSell();
        JustBoughtTimer = Time.unscaledTime + BOUGHT_TIME_BUFFER;
        TransactionConfirmedFrame.SetActive(true);
        TransactionConfirmedFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "PURCHASED " + SelectedToolToInventory.Name;
        TransactionConfirmedFrame.transform.GetChild(1).GetComponent<Image>().sprite = SelectedToolToInventory.GetComponent<SpriteRenderer>().sprite;
    }

    private void ConfirmPurchase()
    {
        PartyInfo.Inventory.Gold -= ConfirmFrame.Amount * SelectedToolToInventory.DefaultPrice;
        if (BuyingList.SelectedObject is Item it) PartyInfo.Inventory.AddItem(it, ConfirmFrame.Amount);
        else if (BuyingList.SelectedObject is Weapon wp) PartyInfo.Inventory.AddWeapon(wp, ConfirmFrame.Amount);
    }

    private void ConfirmSell()
    {
        PartyInfo.Inventory.Gold += ConfirmFrame.Amount * SelectedToolToInventory.DefaultPrice;
        if (BuyingList.SelectedObject is Item it) PartyInfo.Inventory.RemoveItem(it, ConfirmFrame.Amount);
        else if (BuyingList.SelectedObject is Weapon wp) PartyInfo.Inventory.RemoveWeapon(wp, ConfirmFrame.Amount);
    }

    private void SetupEquipConfirmation()
    {
        EquipNowConfirmation.SetActive(true);
        EventSystem.current.SetSelectedGameObject(EquipNowConfirmation.transform.GetChild(1).gameObject);
    }
}