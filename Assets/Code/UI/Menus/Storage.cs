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

    // Delay after an item/weapon has been used/equipped
    private float DoneTimer;

    // Data
    [HideInInspector] public PlayerParty PartyInfo;
    [HideInInspector] public InventorySystem StoredItems;

    private void Start()
    {
        PartyInfo = GameplayMaster.Party;
        MainFrame.Activate();
        Legend.SetActive(true);
        Initialize();
        SelectInventory();
        SelectItemList();
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
        ToolList.ClearSelections();
        ConfirmFrame.Deactivate();
        FrameGoTo.Activate();
        ResultCheckFrame.SetActive(false);
    }

    public void Swap()
    {
        if (Selection == Selections.SelectingInventory) SelectStorage();
        else if (Selection == Selections.SelectingStorage) SelectInventory();
    }

    private void SelectInventory()
    {
        Selection = Selections.SelectingInventory;
        ToolList.LinkToInventory(PartyInfo.Inventory);
    }

    private void SelectStorage()
    {
        Selection = Selections.SelectingStorage;
        ToolList.LinkToInventory(StoredItems);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectItemList()
    {
        if (Selection == Selections.SelectingInventory) SelectToolTab(InventorySystem.ListType.Items, PartyInfo.Inventory.Items, 0);
        else if (Selection == Selections.SelectingStorage) SelectToolTab(InventorySystem.ListType.Items, StoredItems.Items, 0);
    }

    public void SelectWeaponList()
    {
        if (Selection == Selections.SelectingInventory) SelectToolTab(InventorySystem.ListType.Weapons, PartyInfo.Inventory.Weapons, 1);
        else if (Selection == Selections.SelectingStorage) SelectToolTab(InventorySystem.ListType.Weapons, StoredItems.Weapons, 1);
    }

    private void SelectToolTab<T>(InventorySystem.ListType inventoryList, List<T> toolList, int tabIndex) where T : ToolForInventory
    {
        if (Sorter.gameObject.activeSelf) return;
        InventoryList = inventoryList;
        EventSystem.current.SetSelectedGameObject(ToolListTabs.transform.GetChild(tabIndex).gameObject);
        
        if (Selection == Selections.ConfirmAmountInventory) SelectInventory();
        else if (Selection == Selections.ConfirmAmountStorage) SelectStorage();
        KeepOnlyHighlightedSelected(ref SelectedInventoryTab);

        ToolList.Selecting = true;
        ToolList.Setup(toolList);
        if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(ToolList.transform.GetChild(0).gameObject);
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
            UndoConfirmAmount();
            return;
        }
        /*else
        {
            FrameCheckOther.SetActive(true);
            AmountCheckOther.text = "";
        }*/
        if (Selection == Selections.SelectingInventory)
        {
            Selection = Selections.ConfirmAmountInventory;
            ConfirmFrame.Activate(0, ToolList.SelectedObject.Quantity);
            ConfirmText.text = "STORE?";
        }
        else if (Selection == Selections.SelectingStorage)
        {
            Selection = Selections.ConfirmAmountStorage;
            ConfirmFrame.Activate(0, ToolList.SelectedObject.Quantity);
            ConfirmText.text = "TAKE OUT?";
        }
        FrameGoTo.Deactivate();
        ResultCheckFrame.SetActive(true);
        KeepOnlyHighlightedSelected(ref ToolList.SelectedButton);
        EventSystem.current.SetSelectedGameObject(ConfirmFrame.OKButton.gameObject);
        Sorter.Undo();
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

    public void UndoConfirmAmount()
    {
        EventSystem.current.SetSelectedGameObject(ToolList.SelectedButton.gameObject);
        Initialize();
        if (Selection == Selections.ConfirmAmountInventory) Selection = Selections.SelectingInventory;
        else if (Selection == Selections.ConfirmAmountStorage) Selection = Selections.SelectingStorage;
    }

    public void ConfirmAmountMoveUp()
    {

    }

    public void ConfirmAmountMoveDown()
    {

    }

    public void ConfirmTransaction()
    {
        UndoConfirmAmount();
    }
}