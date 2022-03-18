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

    // Buying - general
    public MenuFrame BuyingFrame;
    public InventoryToolSelectionList BuyingList;
    public GameObject BuyingListBlock;
    public TextMeshProUGUI CarryingAmount;
    public TextMeshProUGUI GoldAmountBuying;
    public GameObject NotEnoughGoldText;
    private ToolForInventory SelectedToolToInventory;

    // Selling - general
    public ToolListCollectionFrame SellingFrame;
    public GameObject GoldAmountSellingFrame;
    public TextMeshProUGUI GoldAmountSellingValue;
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

    // Buying equipment
    public GameObject CharacterEquipCheckFrame;
    public Transform CharacterEquipCheckList;
    public GameObject EquipNowConfirmation;
    public MenuFrame SelectEquipsFrame;
    public GameObject ConfirmSwap;

    // Info for globals
    [HideInInspector] public PlayerParty PartyInfo;
    [HideInInspector] public Shopkeeper Shopkeep;

    // Transactions
    private bool DoneTransaction;
    private float JustTransactedTimer;
    private float TRANSACTED_TIME_BUFFER = 0.5f;
    private int DisplayedGold;
    private int DisplayedGoldChangeSpeed;

    private bool isBuying => SceneMaster.BuyingInShop;

    private bool CannotPurchase => NotEnoughGoldText.activeSelf;

    private void Start()
    {
        PartyInfo = GameplayMaster.Party;
        Shopkeep = GameplayMaster.Shop;
        PartyInfo.Inventory.UpdateToCurrentWeight();
        DisplayedGold = PartyInfo.Inventory.Gold;
        if (isBuying) BuyingFrame.Activate();
        else
        {
            SellingFrame.TargetFrame.Activate();
            SellingFrame.RegisterToolList(0, PartyInfo.Inventory.Items);
            SellingFrame.RegisterToolList(1, PartyInfo.Inventory.Weapons);
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
        if (isBuying) RefreshGoldDisplayBuying();
        else RefreshGoldDisplaySelling();

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
                else if (isBuying && SelectedToolToInventory is Weapon)
                {
                    if (!EquipNowConfirmation.gameObject.activeSelf) SetupEquipConfirmation();
                    else break;
                }
                else if (Time.unscaledTime >= JustTransactedTimer && (select || back)) ReturnToSelection();
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
        Shopkeep.CloseShop(DoneTransaction);
        SceneMaster.CloseShop(PartyInfo);
    }

    public void ReturnToSelection()
    {
        if (isBuying) EventSystem.current.SetSelectedGameObject(BuyingList.SelectedButton.gameObject);
        else
        {
            SellingFrame.ToolList.SelectedButton.ClearHighlights();
            SellingFrame.Refresh();
            EventSystem.current.SetSelectedGameObject(SellingFrame.ToolList.SelectedButton.gameObject ?? SellingFrame.ToolList.transform.GetChild(0).gameObject);
        }
        Initialize();
    }

    public void Initialize()
    {
        Selection = Selections.Browsing;
        if (isBuying)
        {
            BuyingList.Refresh(Shopkeep.ToolsInStock);
            BuyingList.Selecting = true;
        }
        else SellingFrame.SelectingToolList();

        BuyingListBlock.gameObject.SetActive(false);
        if (!(BuyingList.SelectedObject is Weapon))
            CharacterEquipCheckFrame.SetActive(false);

        ConfirmFrame.gameObject.SetActive(false);
        ResultCheckFrame.SetActive(false);
        TransactionConfirmedFrame.SetActive(false);
        EquipNowConfirmation.SetActive(false);
        SelectEquipsFrame.Deactivate();
        ConfirmSwap.SetActive(false);
    }

    private void RefreshGoldDisplayBuying()
    {
        int targetGold = PartyInfo.Inventory.Gold;
        DisplayedGold = (DisplayedGold > targetGold) ? (DisplayedGold - DisplayedGoldChangeSpeed) : targetGold;
        GoldAmountBuying.text = DisplayedGold.ToString();
    }

    private void RefreshGoldDisplaySelling()
    {
        int targetGold = PartyInfo.Inventory.Gold;
        DisplayedGold = (DisplayedGold < targetGold) ? (DisplayedGold + DisplayedGoldChangeSpeed) : targetGold;
        GoldAmountSellingValue.text = DisplayedGold.ToString();
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
            if (BuyingList.SelectedObject is Weapon wp) RefreshPartyMemberEquips(wp);
            else CharacterEquipCheckFrame.SetActive(false);
            NotEnoughGoldText.SetActive(PartyInfo.Inventory.Gold - BuyingList.SelectedObject.DefaultPrice < 0);
        }
        else
        {
            CarryingAmount.text = "";
            CharacterEquipCheckFrame.SetActive(false);
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
            AddedGoldValue.text = canSell ? ("+ " + SellingFrame.ToolList.SelectedObject.DefaultSellPrice) : "";
            CannotSellMesssage.SetActive(!canSell);
        }
        else AddedGoldFrame.SetActive(false);
    }

    private void RefreshPartyMemberEquips(Weapon wp)
    {
        CharacterEquipCheckFrame.SetActive(true);
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
        BuyingList.Selecting = false;
        BuyingListBlock.SetActive(true);
        BuyingList.SelectedButton.KeepSelected();
        SetupSelectToolToInventory();
        ConfirmFrame.Activate(1, PartyInfo.Inventory.Gold / SelectedToolToInventory.DefaultPrice);
        ConfirmText.text = "How many will you purchase?";
        CharacterEquipCheckFrame.SetActive(false);
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
            ResultCheckGold.text = (PartyInfo.Inventory.Gold - SelectedToolToInventory.DefaultPrice * amount).ToString();
            ResultCheckCarryTracker.Set(PartyInfo.Inventory.CarryWeight + amount * SelectedToolToInventory.Weight, PartyInfo.Inventory.WeightCapacity);
        }
        else
        {
            ToolForInventory tool = SellingFrame.ToolList.SelectedObject;
            ResultCheckCarrying.text = (tool.Quantity - amount).ToString();
            ResultCheckGold.text = (PartyInfo.Inventory.Gold + tool.DefaultSellPrice * amount).ToString();
            ResultCheckCarryTracker.Set(PartyInfo.Inventory.CarryWeight - amount * tool.Weight, PartyInfo.Inventory.WeightCapacity);
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
        DisplayedGoldChangeSpeed = total / 60 + 1;
        JustTransactedTimer = Time.unscaledTime + TRANSACTED_TIME_BUFFER;
    }

    private int ConfirmPurchase()
    {
        int total = ConfirmFrame.Amount * SelectedToolToInventory.DefaultPrice;
        PartyInfo.Inventory.Gold -= total;
        if (SelectedToolToInventory is Item it) PartyInfo.Inventory.AddItem(it, ConfirmFrame.Amount);
        else if (SelectedToolToInventory is Weapon wp) PartyInfo.Inventory.AddWeapon(wp, ConfirmFrame.Amount);

        TransactionConfirmedFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Purchased " + SelectedToolToInventory.Name;
        TransactionConfirmedFrame.transform.GetChild(1).GetComponent<Image>().sprite = SelectedToolToInventory.GetComponent<SpriteRenderer>().sprite;
        UpdateCarryingAmount();
        return total;
    }

    private int ConfirmSell()
    {
        ToolForInventory tool = SellingFrame.ToolList.SelectedObject;

        int total = ConfirmFrame.Amount * tool.DefaultSellPrice;
        PartyInfo.Inventory.Gold += total;
        if (tool is Item it) PartyInfo.Inventory.RemoveItem(it, ConfirmFrame.Amount);
        else if (tool is Weapon wp) PartyInfo.Inventory.RemoveWeapon(wp, ConfirmFrame.Amount);

        TransactionConfirmedFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Sold " + tool.Name;
        TransactionConfirmedFrame.transform.GetChild(1).GetComponent<Image>().sprite = tool.GetComponent<SpriteRenderer>().sprite;
        SellingFrame.Refresh();
        SellingFrame.ListBlocker.SetActive(true);
        return total;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Buying menu only - Equipment management --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupEquipConfirmation()
    {
        EquipNowConfirmation.SetActive(true);
        EventSystem.current.SetSelectedGameObject(EquipNowConfirmation.transform.GetChild(1).gameObject);
    }

    public void SetupSelectEquipsList()
    {
        SelectEquipsFrame.Activate();
    }
}