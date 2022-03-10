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

public class Storage : MonoBehaviour
{
    public const int MAX_AMOUNT_PER_ITEM = 1000;

    public enum Selections
    {
        None, SelectingInventory, SelectingStorage, ConfirmAmountInventory, ConfirmAmountStorage
    }

    // Child GameObjects;
    public MenuFrame MainFrame;
    public GameObject Legend;
    public TextMeshProUGUI SelectionName;
    public InventoryToolSelectionList ToolList;
    public GameObject ToolListTabs;
    public InventoryToolListSorter Sorter;
    
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
    private Selections Selection;
    private InventorySystem.ListType InventoryList;
    private ListSelectable SelectedInventoryTab;
    private InventorySystem SourceSystem;
    private InventorySystem TargetSystem;
    private ToolForInventory TargetTool;    // Map to selected tool from the opposite inventory system

    private bool SelectingTool => Selection == Selections.SelectingInventory || Selection == Selections.SelectingStorage;

    private bool ConfirmingAmount => Selection == Selections.ConfirmAmountInventory || Selection == Selections.ConfirmAmountStorage;

    // Data
    [HideInInspector] public PlayerParty PartyInfo;

    private void Start()
    {
        PartyInfo = GameplayMaster.Party;
        MainFrame.Activate();
        Legend.SetActive(true);
        Initialize();
        SelectInventory();
        SelectItemList();
        TargetTool = null;
    }

    void Update()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        switch (Selection)
        {
            case Selections.SelectingInventory:
            case Selections.SelectingStorage:
                if (InputMaster.GoingBack()) SceneMaster.CloseStorage(PartyInfo);
                else if (Input.GetKeyDown(KeyCode.Alpha1)) SelectItemList();
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeaponList();
                // else if (Input.GetKeyDown(KeyCode.C)) SetupStorageSorting();
                else if (Input.GetKeyDown(KeyCode.V)) Swap();
                break;
            case Selections.ConfirmAmountInventory:
            case Selections.ConfirmAmountStorage:
                if (InputMaster.GoingBack()) UndoConfirmAmount();
                break;
        }
    }

    protected void Initialize()
    {
        ToolList.Selecting = true;
        ConfirmFrame.Deactivate();
        FrameGoTo.Activate();
        ResultCheckFrame.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory system lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Swap()
    {
        if (Selection == Selections.SelectingInventory) SelectStorage();
        else if (Selection == Selections.SelectingStorage) SelectInventory();
    }

    private void SelectInventory()
    {
        SourceSystem = PartyInfo.Inventory;
        TargetSystem = PartyInfo.Storage;
        SelectInventorySystem(Selections.SelectingInventory, "INVENTORY");
        ToolList.CarryTracker.gameObject.SetActive(true);
    }

    private void SelectStorage()
    {
        SourceSystem = PartyInfo.Storage;
        TargetSystem = PartyInfo.Inventory;
        SelectInventorySystem(Selections.SelectingStorage, "STORAGE");
        ToolList.CarryTracker.gameObject.SetActive(false);
    }

    private void SelectInventorySystem(Selections selection, string title)
    {
        SelectionName.text = title;
        Selection = selection;
        ToolList.LinkToInventory(SourceSystem);
        ToolList.Selecting = true;
        ToolList.ClearSelections();
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                SelectItemList();
                break;
            case InventorySystem.ListType.Weapons:
                SelectWeaponList();
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists from tabs --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectItemList()
    {
        SelectToolTab(InventorySystem.ListType.Items, SourceSystem.Items, 0);
    }

    public void SelectWeaponList()
    {
        SelectToolTab(InventorySystem.ListType.Weapons, SourceSystem.Weapons, 1);
    }

    private void SelectToolTab<T>(InventorySystem.ListType inventoryList, List<T> toolList, int tabIndex) where T : ToolForInventory
    {
        if (!SelectingTool || Sorter.gameObject.activeSelf) return;
        InventoryList = inventoryList;
        EventSystem.current.SetSelectedGameObject(ToolListTabs.transform.GetChild(tabIndex).gameObject);
        
        if (Selection == Selections.ConfirmAmountInventory) SelectInventory();
        else if (Selection == Selections.ConfirmAmountStorage) SelectStorage();
        KeepOnlyHighlightedSelected(ref SelectedInventoryTab);

        ToolList.Selecting = true;
        ToolList.Refresh(toolList);
        if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(ToolList.transform.GetChild(0).gameObject);
    }

    public void ExtraHover()
    {
        TargetHoverFrame.SetActive(true);
        TargetHoverMessage.text = (Selection == Selections.SelectingInventory) ? "STORED" : "HELD";
        TargetHoverNumber.text = (GetTargetTool()?.Quantity ?? 0).ToString();
    }

    public void ExtraHoverCancel()
    {
        TargetHoverFrame.SetActive(false);
    }

    private void KeepOnlyHighlightedSelected(ref ListSelectable selectedListBtn)
    {
        if (selectedListBtn) selectedListBtn.ClearHighlights();
        selectedListBtn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        selectedListBtn.KeepSelected();
    }

    public void SelectTool()
    {
        ToolList.Selecting = false;

        if (!ToolList.SetupToolInfo())
        {
            if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
            if (ConfirmingAmount) UndoConfirmAmount();
            return;
        }
        else if (Selection == Selections.SelectingInventory)
        {
            Selection = Selections.ConfirmAmountInventory;
            ConfirmFrame.Activate(1, GetMaxAmountToStorage());
            ConfirmText.text = "STORE?";
        }
        else if (Selection == Selections.SelectingStorage)
        {
            Selection = Selections.ConfirmAmountStorage;
            ConfirmFrame.Activate(1, ToolList.SelectedObject.Quantity);
            ConfirmText.text = "TAKE OUT?";
        }
        TargetTool = GetTargetTool();

        TargetHoverFrame.SetActive(false);
        FrameGoTo.Deactivate();
        UpdateResultChecker(ConfirmFrame.Amount);
        KeepOnlyHighlightedSelected(ref ToolList.SelectedButton);
        EventSystem.current.SetSelectedGameObject(ConfirmFrame.OKButton.gameObject);
        Sorter.Undo();
    }

    private ToolForInventory GetTargetTool()
    {
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                return TargetSystem.Items.Find(x => x.Id == ToolList.SelectedObject.Id);
            case InventorySystem.ListType.Weapons:
                return TargetSystem.Weapons.Find(x => x.Id == ToolList.SelectedObject.Id);
            default:
                return null;
        }
    }

    private int GetMaxAmountToStorage()
    {
        int quantity = ToolList.SelectedObject.Quantity;
        int inStorage = TargetTool?.Quantity ?? 0;
        return (inStorage + quantity > MAX_AMOUNT_PER_ITEM) ? (MAX_AMOUNT_PER_ITEM - inStorage) : quantity;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*public void SetupSorting()
    {
        Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        ToolList.Selecting = false;
        ToolList.ClearSelections();
        Sorter.Setup(ToolList, MenuManager.PartyInfo.Inventory, InventoryList);
    }*/

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Confirm Amount --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UpdateResultChecker(int amount)
    {
        ResultCheckFrame.SetActive(true);
        int source = ToolList.SelectedObject.Quantity - amount;
        int target = (TargetTool?.Quantity ?? 0) + amount;

        if (Selection == Selections.ConfirmAmountInventory)
        {
            ResultCheckInventory.text = source.ToString();
            ResultCheckStorage.text = target.ToString();
            ResultCheckCarryWeight.Set(PartyInfo.Inventory.CarryWeight - amount, PartyInfo.Inventory.WeightCapacity);
        }
        else if (Selection == Selections.ConfirmAmountStorage)
        {
            ResultCheckInventory.text = target.ToString();
            ResultCheckStorage.text = source.ToString();
            ResultCheckCarryWeight.Set(PartyInfo.Inventory.CarryWeight + amount, PartyInfo.Inventory.WeightCapacity);
        }
    }

    public void UndoConfirmAmount()
    {
        EventSystem.current.SetSelectedGameObject(ToolList.SelectedButton.gameObject);
        Initialize();
        if (Selection == Selections.ConfirmAmountInventory) Selection = Selections.SelectingInventory;
        else if (Selection == Selections.ConfirmAmountStorage) Selection = Selections.SelectingStorage;
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
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                SourceSystem.RemoveItem(ToolList.SelectedObject as Item, ConfirmFrame.Amount);
                TargetSystem.AddItem((TargetTool ?? ToolList.SelectedObject) as Item, ConfirmFrame.Amount);
                ToolList.Refresh(SourceSystem.Items);
                break;
            case InventorySystem.ListType.Weapons:
                SourceSystem.RemoveWeapon(ToolList.SelectedObject as Weapon, ConfirmFrame.Amount);
                TargetSystem.AddWeapon((TargetTool ?? ToolList.SelectedObject) as Weapon, ConfirmFrame.Amount);
                ToolList.Refresh(SourceSystem.Weapons);
                break;
        }
        UndoConfirmAmount();
    }
}