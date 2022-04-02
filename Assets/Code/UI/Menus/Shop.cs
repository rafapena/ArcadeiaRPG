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
    private enum Selections { None, Browsing, ConfirmingAmont, ConfirmedTransaction, EquippingWeapon }

    private Selections Selection;

    // Buying - general
    public MenuFrame BuyingFrame;
    public InventoryToolSelectionList BuyingList;
    public GameObject BuyingListBlock;
    public ObtainingWeapons WeaponsUI;
    public TextMeshProUGUI CarryingAmount;
    public NumberUpdater GoldAmountBuying;
    public GameObject NotEnoughGoldText;
    private IToolForInventory SelectedToolToInventory;

    // Selling - general
    public ToolListCollectionFrame SellingFrame;
    public GameObject GoldAmountSellingFrame;
    public NumberUpdater GoldAmountSellingValue;
    public GameObject AddedGoldFrame;
    public GameObject AddedGoldIcon;
    public TextMeshProUGUI AddedGoldValue;
    public GameObject CannotSellMesssage;

    // Confirming transaction
    public ConfirmAmount ConfirmFrame;
    public TextMeshProUGUI ConfirmText;
    public GameObject ResultCheckFrame;
    public TextMeshProUGUI ResultCheckCarrying;
    public TextMeshProUGUI ResultCheckGold;
    public Gauge ResultCheckCarryTracker;
    public GameObject TransactionConfirmedFrame;

    // Info for globals
    [HideInInspector] public PlayerParty PartyInfo;
    [HideInInspector] public Shopkeeper Shopkeep;

    // Transactions
    private bool DoneTransaction;
    private float JustTransactedTimer;
    private float TRANSACTED_TIME_BUFFER = 0.5f;

    private bool isBuying => SceneMaster.BuyingInShop;

    private bool CannotPurchase => NotEnoughGoldText.activeSelf;

    private void Start()
    {
        PartyInfo = GameplayMaster.Party;
        Shopkeep = GameplayMaster.Shop;
        PartyInfo.Inventory.UpdateToCurrentWeight();
        WeaponsUI.Initialize(PartyInfo);
        GoldAmountBuying.Initialize(PartyInfo.Inventory.Gold);
        GoldAmountSellingValue.Initialize(PartyInfo.Inventory.Gold);
        if (isBuying) BuyingFrame.Activate();
        else
        {
            SellingFrame.TargetFrame.Activate();
            SellingFrame.SetToolListOnTab(0, PartyInfo.Inventory.Items.FindAll(x => !x.IsKey));
            SellingFrame.SetToolListOnTab(1, PartyInfo.Inventory.Weapons);
            SellingFrame.SetToolListOnTab(2, PartyInfo.Inventory.Accessories);
            SellingFrame.InitializeSelection();
            SellingFrame.TrackCarryWeight(PartyInfo.Inventory);
        }
        Initialize();
        NotEnoughGoldText.SetActive(false);
        if (isBuying) EventSystem.current.SetSelectedGameObject(BuyingList.transform.GetChild(0).gameObject);
    }

    private void Update()
    {
        bool select = InputMaster.ProceedInMenu;
        bool back = InputMaster.GoingBack;

        switch (Selection)
        {
            case Selections.Browsing:
                if (!MenuMaster.ReadyToSelectInMenu) break;
                else if (back && !isBuying && SellingFrame.ActivatedSorter) SellingFrame.DeactivateSorter();
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
                else if (isBuying && SelectedToolToInventory is Weapon && !WeaponsUI.NooneNeeded)
                {
                    Selection = Selections.EquippingWeapon;
                    WeaponsUI.OpenEquipCharacterSelection();
                }
                else if (Time.unscaledTime >= JustTransactedTimer && (select || back)) ReturnToSelection();
                break;

            case Selections.EquippingWeapon:
                if (WeaponsUI.IsDone) ReturnToSelection();
                break;
        }
    }

    private void CloseShop()
    {
        Selection = Selections.None;
        Shopkeep.CloseShop(DoneTransaction);
        SceneMaster.CloseShop(PartyInfo);
    }

    public void ReturnToSelection()
    {
        if (isBuying) EventSystem.current.SetSelectedGameObject(BuyingList.SelectedButton.gameObject);
        else
        {
            SellingFrame.ToolList.SelectedButton.ClearHighlights();
            EventSystem.current.SetSelectedGameObject(SellingFrame.ToolList.SelectedButton.gameObject ?? SellingFrame.ToolList.transform.GetChild(0).gameObject);
        }
        Initialize();
    }

    public void Initialize()
    {
        Selection = Selections.Browsing;
        if (isBuying)
        {
            BuyingList.Refresh(Shopkeep.GetAllToolsInStock(), PriceHighEnough);
            BuyingList.Selecting = true;
        }
        else SellingFrame.SelectingToolList();

        BuyingListBlock.gameObject.SetActive(false);
        WeaponsUI.Deactivate();
        ConfirmFrame.gameObject.SetActive(false);
        ResultCheckFrame.SetActive(false);
        TransactionConfirmedFrame.SetActive(false);
    }

    private bool PriceHighEnough(IToolForInventory tool)
    {
        return PartyInfo.Inventory.Gold >= tool.Price;
    }
    
    private void UpdateCarryingAmount()
    {
        CarryingAmount.text = (SelectedToolToInventory?.Quantity ?? 0).ToString();
    }

    public void HoverOverToolBuying()
    {
        BuyingList.HoverOverTool();         // Sets up the data below
        if (BuyingList.DisplayedToolInfo)
        {
            SetupSelectToolToInventory();
            UpdateCarryingAmount();
            if (BuyingList.SelectedObject is Weapon wp) WeaponsUI.Refresh(wp);
            else WeaponsUI.InfoFrame.Deactivate();
            NotEnoughGoldText.SetActive(PartyInfo.Inventory.Gold - BuyingList.SelectedObject.Price < 0);
        }
        else
        {
            CarryingAmount.text = "";
            WeaponsUI.InfoFrame.Deactivate();
            NotEnoughGoldText.SetActive(false);
        }
    }

    public void HoverOverToolSelling()
    {
        SellingFrame.ToolList.HoverOverTool();          // Sets up the data below
        if (SellingFrame.ToolList.DisplayedToolInfo)
        {
            AddedGoldFrame.SetActive(true);
            bool canSell = SellingFrame.ToolList.SelectedObject.CanRemove;
            AddedGoldIcon.SetActive(canSell);
            AddedGoldValue.text = canSell ? ("+ " + SellingFrame.ToolList.SelectedObject.SellPrice) : "";
            CannotSellMesssage.SetActive(!canSell);
        }
        else AddedGoldFrame.SetActive(false);
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
        if (!SellingFrame.ToolList.SelectedObject.CanRemove)
        {
            SellingFrame.UndoSelectTool();
            return;
        }
        Selection = Selections.ConfirmingAmont;
        AddedGoldFrame.SetActive(false);
        ResultCheckFrame.SetActive(true);
        SellingFrame.ListBlocker.SetActive(true);
        ConfirmFrame.Activate(1, SellingFrame.ToolList.SelectedObject.Quantity);
        ConfirmText.text = "How many will you sell?";
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    public void SelectToolFailed()
    {
        //
    }

    public void UndoSelectToolSuccess()
    {
        Selection = Selections.Browsing;
        AddedGoldFrame.SetActive(true);
        ResultCheckFrame.SetActive(false);
        SellingFrame.ListBlocker.SetActive(false);
        ConfirmFrame.Deactivate();

    }

    public void ActivateSorterSuccess()
    {
        AddedGoldFrame.SetActive(false);
    }

    /// -- Selling menu only - END --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectToolBuying()
    {
        if (!MenuMaster.ReadyToSelectInMenu || !BuyingList.RefreshToolInfo() || CannotPurchase) return;
        Selection = Selections.ConfirmingAmont;
        WeaponsUI.InfoFrame.Deactivate();
        BuyingList.Selecting = false;
        BuyingListBlock.SetActive(true);
        BuyingList.SelectedButton.KeepSelected();
        SetupSelectToolToInventory();
        ConfirmFrame.Activate(1, PartyInfo.Inventory.Gold / SelectedToolToInventory.Price);
        ConfirmText.text = "How many will you purchase?";
        ResultCheckFrame.SetActive(true);
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    private void SetupSelectToolToInventory()
    {
        if (BuyingList.SelectedObject is Item it) SelectedToolToInventory = PartyInfo.Inventory.Items.Find(x => x.Id == it.Id);
        else if (BuyingList.SelectedObject is Weapon wp) SelectedToolToInventory = PartyInfo.Inventory.Weapons.Find(x => x.Id == wp.Id);
        else if (BuyingList.SelectedObject is Accessory ac) SelectedToolToInventory = PartyInfo.Inventory.Accessories.Find(x => x.Id == ac.Id);
    }

    private void UndoConfirmAmountBuying()
    {
        Selection = Selections.Browsing;
        BuyingListBlock.SetActive(false);
        BuyingList.Selecting = true;
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
        if (isBuying)
        {
            ResultCheckCarrying.text = ((SelectedToolToInventory?.Quantity ?? 0) + amount).ToString();
            ResultCheckGold.text = (PartyInfo.Inventory.Gold - SelectedToolToInventory.Price * amount).ToString();
            ResultCheckCarryTracker.Set(PartyInfo.Inventory.CarryWeight + amount * SelectedToolToInventory.Weight, PartyInfo.Inventory.WeightCapacity);
        }
        else
        {
            IToolForInventory ActiveTool = SellingFrame.ToolList.SelectedObject;
            ResultCheckCarrying.text = (ActiveTool.Quantity - amount).ToString();
            ResultCheckGold.text = (PartyInfo.Inventory.Gold + ActiveTool.SellPrice * amount).ToString();
            ResultCheckCarryTracker.Set(PartyInfo.Inventory.CarryWeight - amount * ActiveTool.Weight, PartyInfo.Inventory.WeightCapacity);
        }
    }

    public void ConfirmTransaction()
    {
        Selection = Selections.ConfirmedTransaction;
        DoneTransaction = true;
        ConfirmFrame.Deactivate();
        ResultCheckFrame.SetActive(false);
        TransactionConfirmedFrame.SetActive(true);
        int total = isBuying ? ConfirmPurchase() : ConfirmSell();
        JustTransactedTimer = Time.unscaledTime + TRANSACTED_TIME_BUFFER;
    }

    private int ConfirmPurchase()
    {
        int total = ConfirmFrame.Amount * SelectedToolToInventory.Price;
        GoldAmountBuying.Add(ref PartyInfo.Inventory.Gold, -total);
        PartyInfo.Inventory.Add(SelectedToolToInventory, ConfirmFrame.Amount);
        WeaponsUI.AmountToEquip = ConfirmFrame.Amount;

        TransactionConfirmedFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Purchased " + SelectedToolToInventory.Info.Name;
        TransactionConfirmedFrame.transform.GetChild(1).GetComponent<Image>().sprite = SelectedToolToInventory.Info.GetComponent<SpriteRenderer>().sprite;
        UpdateCarryingAmount();
        return total;
    }

    private int ConfirmSell()
    {
        IToolForInventory tool = SellingFrame.ToolList.SelectedObject;
        int total = ConfirmFrame.Amount * tool.SellPrice;
        GoldAmountSellingValue.Add(ref PartyInfo.Inventory.Gold, total);
        PartyInfo.Inventory.Remove(tool, ConfirmFrame.Amount);
        TransactionConfirmedFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Sold " + tool.Info.Name;
        TransactionConfirmedFrame.transform.GetChild(1).GetComponent<Image>().sprite = tool.Info.GetComponent<SpriteRenderer>().sprite;
        
        SellingFrame.ListBlocker.SetActive(true);
        switch (SellingFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Items:
                SellingFrame.Refresh(PartyInfo.Inventory.Items.FindAll(x => !x.IsKey));
                break;
            case InventorySystem.ListType.Weapons:
                SellingFrame.Refresh(PartyInfo.Inventory.Weapons);
                break;
            case InventorySystem.ListType.Accessories:
                SellingFrame.Refresh(PartyInfo.Inventory.Accessories);
                break;
        }
        return total;
    }
}