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

public class Storage : MonoBehaviour, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    public const int MAX_AMOUNT_PER_ITEM = 1000;

    // Child GameObjects;
    public MenuFrame MainFrame;
    public GameObject Legend;
    public TextMeshProUGUI SelectionName;
    public ToolListCollectionFrame CollectionFrame;
    
    public MenuFrame FrameGoTo;
    public TextMeshProUGUI MessageGoTo;

    public GameObject TargetHoverFrame;
    public TextMeshProUGUI TargetHoverMessage;
    public TextMeshProUGUI TargetHoverNumber;

    public ConfirmAmount ConfirmFrame;
    public TextMeshProUGUI ConfirmText;

    public GameObject ResultCheckFrame;
    public TextMeshProUGUI ResultCheckInventory;
    public TextMeshProUGUI ResultCheckStorage;
    public Gauge ResultCheckCarryWeight;

    // General selection tracking
    private bool InventoryMode;             // Browsnig/selecting in inventory or storage
    private InventorySystem SourceSystem;
    private InventorySystem TargetSystem;
    private IToolForInventory TargetTool;    // Map to selected ActiveTool from the opposite inventory system

    // Data
    [HideInInspector] public PlayerParty PartyInfo;

    private void Start()
    {
        PartyInfo = GameplayMaster.Party;
        MainFrame.Activate();
        Legend.SetActive(true);
        Initialize();
        SelectInventory();
        CollectionFrame.TrackCarryWeight(PartyInfo.Inventory);
        CollectionFrame.InitializeSelection();
        TargetTool = null;
    }

    void Update()
    {
        if (CollectionFrame.SelectTabInputs()) return;
        else if (InputMaster.GoingBack)
        {
            if (CollectionFrame.ActivatedSorter) CollectionFrame.DeactivateSorter();
            else if (ConfirmFrame.gameObject.activeSelf) UndoConfirmAmount();
            else SceneMaster.CloseStorage();
        }
        else if (Input.GetKeyDown(KeyCode.V)) Swap();
    }

    protected void Initialize()
    {
        CollectionFrame.SelectingToolList();
        TargetHoverFrame.SetActive(true);
        ConfirmFrame.Deactivate();
        FrameGoTo.Activate();
        ResultCheckFrame.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory system lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Swap()
    {
        if (CollectionFrame.ListBlocker.activeSelf) return;
        else if (InventoryMode) SelectStorage();
        else SelectInventory();
    }

    private void SelectInventory()
    {
        SourceSystem = PartyInfo.Inventory;
        TargetSystem = PartyInfo.Storage;
        SelectInventorySystem(true, "INVENTORY");
        CollectionFrame.CarryTracker.gameObject.SetActive(true);
    }

    private void SelectStorage()
    {
        SourceSystem = PartyInfo.Storage;
        TargetSystem = PartyInfo.Inventory;
        SelectInventorySystem(false, "STORAGE");
        CollectionFrame.CarryTracker.gameObject.SetActive(false);
    }

    private void SelectInventorySystem(bool inventoryMode, string title)
    {
        SelectionName.text = title;
        InventoryMode = inventoryMode;
        CollectionFrame.SetToolListOnTab(0, SourceSystem.Items.FindAll(x => !x.IsKey));
        CollectionFrame.SetToolListOnTab(1, SourceSystem.Weapons);
        CollectionFrame.SetToolListOnTab(2, SourceSystem.Accessories);
        switch (CollectionFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Items:
                CollectionFrame.SelectTab(0);
                break;
            case InventorySystem.ListType.Weapons:
                CollectionFrame.SelectTab(1);
                break;
            case InventorySystem.ListType.Accessories:
                CollectionFrame.SelectTab(2);
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists from tabs --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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
        ConfirmFrame.Activate(1, InventoryMode ? GetMaxAmountToStorage() : CollectionFrame.ToolList.SelectedObject.Quantity);
        ConfirmText.text = InventoryMode ? "STORE?" : "TAKE OUT?";
        CollectionFrame.ListBlocker.SetActive(true);
        TargetTool = GetTargetTool();
        TargetHoverFrame.SetActive(false);
        FrameGoTo.Deactivate();
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    public void SelectToolFailed()
    {
        if (ConfirmFrame.gameObject.activeSelf) UndoConfirmAmount();
    }

    public void UndoSelectToolSuccess()
    {
        CollectionFrame.ListBlocker.SetActive(false);
    }

    public void ActivateSorterSuccess()
    {
        //
    }

    public void HoverOverToolInStorage()
    {
        CollectionFrame.ToolList.HoverOverTool();
        bool hoverFrame = CollectionFrame.ToolList.DisplayedToolInfo && CollectionFrame.ToolList.Selecting;
        TargetHoverFrame.SetActive(hoverFrame);
        if (hoverFrame)
        {
            TargetHoverMessage.text = InventoryMode ? "STORED" : "CARRYING";
            TargetHoverNumber.text = (GetTargetTool()?.Quantity ?? 0).ToString();
        }
    }

    private IToolForInventory GetTargetTool()
    {
        switch (CollectionFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Items:
                return TargetSystem.Items.Find(x => x.Id == CollectionFrame.ToolList.SelectedObject.Info.Id);
            case InventorySystem.ListType.Weapons:
                return TargetSystem.Weapons.Find(x => x.Id == CollectionFrame.ToolList.SelectedObject.Info.Id);
            case InventorySystem.ListType.Accessories:
                return TargetSystem.Accessories.Find(x => x.Id == CollectionFrame.ToolList.SelectedObject.Info.Id);
            default:
                return null;
        }
    }

    private int GetMaxAmountToStorage()
    {
        int quantity = CollectionFrame.ToolList.SelectedObject.Quantity;
        int inStorage = TargetTool?.Quantity ?? 0;
        return (inStorage + quantity > MAX_AMOUNT_PER_ITEM) ? (MAX_AMOUNT_PER_ITEM - inStorage) : quantity;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Confirm Amount --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UpdateResultChecker(int amount)
    {
        ResultCheckFrame.SetActive(true);
        IToolForInventory ActiveTool = CollectionFrame.ToolList.SelectedObject;
        int source = ActiveTool.Quantity - amount;
        int target = (TargetTool?.Quantity ?? 0) + amount;

        if (InventoryMode)
        {
            ResultCheckInventory.text = source.ToString();
            ResultCheckStorage.text = target.ToString();
            ResultCheckCarryWeight.Set(PartyInfo.Inventory.CarryWeight - amount * ActiveTool.Weight, PartyInfo.Inventory.WeightCapacity);
        }
        else
        {
            ResultCheckInventory.text = target.ToString();
            ResultCheckStorage.text = source.ToString();
            ResultCheckCarryWeight.Set(PartyInfo.Inventory.CarryWeight + amount * ActiveTool.Weight, PartyInfo.Inventory.WeightCapacity);
        }
    }

    public void UndoConfirmAmount()
    {
        Initialize();
        EventSystem.current.SetSelectedGameObject(CollectionFrame.ToolList.SelectedButton.gameObject);
    }

    public void ConfirmAmountMoveUp()
    {
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    public void ConfirmAmountMoveDown()
    {
        UpdateResultChecker(ConfirmFrame.Amount);
    }

    public void ConfirmTransaction()
    {
        SourceSystem.Remove(CollectionFrame.ToolList.SelectedObject, ConfirmFrame.Amount);
        TargetSystem.Add(TargetTool ?? CollectionFrame.ToolList.SelectedObject, ConfirmFrame.Amount);
        switch (CollectionFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Items:
                CollectionFrame.Refresh(SourceSystem.Items.FindAll(x => !x.IsKey));
                break;
            case InventorySystem.ListType.Weapons:
                CollectionFrame.Refresh(SourceSystem.Weapons);
                break;
            case InventorySystem.ListType.Accessories:
                CollectionFrame.Refresh(SourceSystem.Accessories);
                break;
        }
        UndoConfirmAmount();
    }
}