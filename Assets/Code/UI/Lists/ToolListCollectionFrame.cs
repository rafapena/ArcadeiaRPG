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
using UnityEngine.Events;
using System;

public class ToolListCollectionFrame : MonoBehaviour
{
    // Main frame GameObject
    public MenuFrame TargetFrame;

    // Tabs
    public GameObject Tabs;
    private ListSelectable SelectedInventoryTab;
    private int CurrentTabIndex;

    // Selection list
    public InventoryToolSelectionList ToolList;
    private IEnumerable<ToolForInventory>[] ListOptions;
    [HideInInspector] public InventorySystem.ListType CurrentInventoryList;

    // Carry tracking
    public Gauge CarryTracker;
    [HideInInspector] public InventorySystem ReferenceInventory;

    // Sorting
    public ListSelectable SortButton;
    public GameObject SortFrame;

    // General functions
    public UnityEvent SelectTabSuccess;
    public UnityEvent SelectTabFailed;
    public UnityEvent SelectToolSuccess;
    public UnityEvent SelectToolFailed;
    public UnityEvent UndoSelectToolSuccess;

    private void Start()
    {
        int numberOfTabs = 0;
        foreach (Transform t in Tabs.transform)
            if (t.gameObject.GetComponent<Button>().interactable)
                numberOfTabs++;
        ListOptions = new IEnumerable<ToolForInventory>[numberOfTabs];
    }

    public void RegisterToolList<T>(int tabIndex, List<T> list) where T : ToolForInventory
    {
        if (tabIndex >= ListOptions.Length || tabIndex < 0 || list == null) return;
        ListOptions[tabIndex] = list;
    }

    public void TrackCarryWeight(InventorySystem inventory)
    {
        if (!ReferenceInventory) ReferenceInventory = inventory;
        RefreshCarryWeight();
    }

    public bool SelectTabInputs()
    {
        bool selected = true;
        if (!MenuMaster.ReadyToSelectInMenu) selected = false;
        else if (Input.GetKeyDown(KeyCode.Q)) LeftTab();
        else if (Input.GetKeyDown(KeyCode.E)) RightTab();
        else if (Input.GetKeyDown(KeyCode.C)) SetupSorting();
        else selected = false;
        return selected;
    }

    public void SelectingToolList()
    {
        ToolList.Selecting = true;
        UndoSort();
    }

    public void Refresh<T>(List<T> listData) where T : ToolForInventory
    {
        ToolList.Refresh(listData);
        RefreshCarryWeight();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void InitializeSelection()
    {
        if (!TargetFrame.Activated) TargetFrame.Activate();
        SelectTab(0);
    }

    public void LeftTab()
    {
        if (CurrentTabIndex > 0) SelectTab(CurrentTabIndex - 1);
    }

    public void RightTab()
    {
        if (CurrentTabIndex < ListOptions.Length - 2) SelectTab(CurrentTabIndex + 1);
    }

    public void SelectTab(int tabIndex)
    {
        if (SortFrame.gameObject.activeSelf)
        {
            SelectTabFailed?.Invoke();
            return;
        }
        SelectingToolList();
        CurrentTabIndex = tabIndex;
        EventSystem.current.SetSelectedGameObject(Tabs.transform.GetChild(tabIndex).gameObject);
        MenuMaster.KeepHighlightedSelected(ref SelectedInventoryTab);
        
        ToolList.Selecting = true;
        switch (ListOptions[tabIndex].GetType().GetGenericArguments()[0].ToString())
        {
            case "Item":
                CurrentInventoryList = InventorySystem.ListType.Items;
                ToolList.Refresh(ListOptions[tabIndex] as List<Item>);
                break;
            case "Weapon":
                CurrentInventoryList = InventorySystem.ListType.Weapons;
                ToolList.Refresh(ListOptions[tabIndex] as List<Weapon>);
                break;
        }
        if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
        RefreshCarryWeight();
        SelectTabSuccess?.Invoke();
        EventSystem.current.SetSelectedGameObject(ToolList.transform.GetChild(0).gameObject);
    }

    public void SelectTool()
    {
        if (!ToolList.RefreshToolInfo())
        {
            if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
            SelectToolFailed?.Invoke();
            return;
        }
        ToolList.Selecting = false;
        MenuMaster.KeepHighlightedSelected(ref ToolList.SelectedButton);
        UndoSort();
        SelectToolSuccess?.Invoke();
    }

    public void UndoSelectTool()
    {
        ToolList.Selecting = true;
        ToolList.SelectedButton.ClearHighlights();
        UndoSelectToolSuccess?.Invoke();
        EventSystem.current.SetSelectedGameObject(ToolList.SelectedButton.gameObject);
    }

    private void RefreshCarryWeight()
    {
        if (!ReferenceInventory) return;
        ReferenceInventory.UpdateToCurrentWeight();
        CarryTracker.Set(ReferenceInventory.CarryWeight, ReferenceInventory.WeightCapacity);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupSorting()
    {
        /*Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        ToolList.Selecting = false;
        ToolList.ClearSelections();
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
        Sorter.Setup(ToolList, MenuManager.PartyInfo.Inventory, InventoryList);*/
    }

    public bool UndoSort(bool selectToolList = true)
    {
        if (SortFrame.gameObject.activeSelf)
        {
            if (selectToolList) ToolList.Selecting = true;
            SortFrame.SetActive(false);
            return true;
        }
        return false;
    }

    /*
    public ListSelectable SortButton;
    private InventoryToolSelectionList TargetInventoryGUI;
    private InventorySystem TargetInventory;
    private InventorySystem.ListType InventoryList;
    private List<ToolForInventory> ReferenceData;

    [HideInInspector] public delegate void ExtraFunction();
    private ExtraFunction ExtraFunc;

    private void Awake()
    {
        ClearSelections();
    }

    public void ClearSelections()
    {
        gameObject.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Setup(InventoryToolSelectionList tig, InventorySystem tinv, InventorySystem.ListType inventoryList)
    {
        TargetInventoryGUI = tig;
        TargetInventory = tinv;
        InventoryList = inventoryList;
        gameObject.SetActive(true);
        SortButton.KeepSelected();
        GameObject firstButton = transform.GetChild(1).GetChild(0).gameObject;
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void Undo()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        SortButton.ClearHighlights();
        GameObject firstButton = TargetInventoryGUI.transform.GetChild(0).gameObject;
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void SortByDefaultAscending()
    {
        GetList();
        TargetInventoryGUI.Refresh(ReferenceData.OrderBy(t => t.Id).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByDefaultDescending()
    {
        GetList();
        TargetInventoryGUI.Refresh(ReferenceData.OrderByDescending(t => t.Id).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByQuantityAscending()
    {
        GetList();
        TargetInventoryGUI.Refresh(ReferenceData.OrderBy(t => t.Quantity).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByQuantityDescending()
    {
        GetList();
        TargetInventoryGUI.Refresh(ReferenceData.OrderByDescending(t => t.Quantity).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByWeightAscending()
    {
        GetList();
        TargetInventoryGUI.Refresh(ReferenceData.OrderBy(t => t.Weight).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByWeightDescending()
    {
        GetList();
        TargetInventoryGUI.Refresh(ReferenceData.OrderByDescending(t => t.Weight).ToList());
        ExtraFunc?.Invoke();
    }

    private void GetList()
    {
        ReferenceData = new List<ToolForInventory>();
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                foreach (Item i in TargetInventory.Items) ReferenceData.Add(i);
                break;
            case InventorySystem.ListType.Weapons:
                foreach (Weapon w in TargetInventory.Weapons) ReferenceData.Add(w);
                break;
            case InventorySystem.ListType.KeyItems:
                foreach (Item i in TargetInventory.KeyItems) ReferenceData.Add(i);
                break;
        }
    }
    */
}