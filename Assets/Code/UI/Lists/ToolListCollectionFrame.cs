using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine.Events;
using System;

public class ToolListCollectionFrame : MonoBehaviour
{
    // Main frame GameObject
    public MenuFrame TargetFrame;
    public GameObject ListBlocker;

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
    [HideInInspector] public bool ActivatedSorter;       // Using bool instead of activeSelf due to initializing phase
    [HideInInspector] public int SavedSortSetting;
    public ListSelectable SortButton;
    public GameObject SortFrame;
    public Transform SortTypesList;
    delegate void SortingFunc();
    private SortingFunc[] SortFunctions;

    // General functions
    public UnityEvent SelectTabSuccess;
    public UnityEvent SelectTabFailed;
    public UnityEvent SelectToolSuccess;
    public UnityEvent SelectToolFailed;
    public UnityEvent UndoSelectToolSuccess;
    public UnityEvent ActivateSorterSuccess;

    private void Start()
    {
        int numberOfTabs = 0;
        foreach (Transform t in Tabs.transform)
            if (t.gameObject.activeSelf)
                numberOfTabs++;

        ListOptions = new IEnumerable<ToolForInventory>[numberOfTabs];
        SortFunctions = new SortingFunc[]{
            SortByDefaultAscending, SortByDefaultDescending,
            SortByQuantityAscending, SortByQuantityDescending,
            SortByWeightAscending, SortByWeightDescending
        };
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
        else if (Input.GetKeyDown(KeyCode.C)) ActivateSorter();
        else selected = false;
        return selected;
    }

    public void SelectingToolList()
    {
        ToolList.Selecting = true;
        DeactivateSorter();
    }

    public void Refresh<T>(List<T> listData) where T : ToolForInventory
    {
        ToolList.Refresh(listData);
        SortFunctions[SavedSortSetting].Invoke();
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
        if (CurrentTabIndex < ListOptions.Length - 1) SelectTab(CurrentTabIndex + 1);
    }

    public void SelectTab(int tabIndex)
    {
        if (ListBlocker.activeSelf)
        {
            SelectTabFailed?.Invoke();
            return;
        }
        SelectingToolList();
        CurrentTabIndex = tabIndex;
        EventSystem.current.SetSelectedGameObject(Tabs.transform.GetChild(tabIndex).gameObject);
        MenuMaster.KeepHighlightedSelected(ref SelectedInventoryTab);

        ToolList.Selecting = true;
        ToolList.Refresh(ListOptions[tabIndex].ToList());
        SetInventoryList(tabIndex);
        SortFunctions[SavedSortSetting].Invoke();
        if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();

        RefreshCarryWeight();
        SelectTabSuccess?.Invoke();
        EventSystem.current.SetSelectedGameObject(ToolList.transform.GetChild(0).gameObject);
    }

    private void SetInventoryList(int tab)
    {
        switch (ListOptions[tab].GetType().GetGenericArguments()[0].ToString())
        {
            case "Item":
                CurrentInventoryList = InventorySystem.ListType.Items;
                break;
            case "Weapon":
                CurrentInventoryList = InventorySystem.ListType.Weapons;
                break;
        }
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
        DeactivateSorter();
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

    public void ActivateSorter()
    {
        if (ActivatedSorter || ListBlocker.activeSelf) return;
        ListBlocker.SetActive(true);
        SortFrame.SetActive(true);
        ToolList.Selecting = false;
        ToolList.ClearSelections();
        ActivateSorterSuccess?.Invoke();
        SortButton.KeepSelected();
        GameObject selectedButton = SortTypesList.GetChild(SavedSortSetting).gameObject;
        EventSystem.current.SetSelectedGameObject(selectedButton);
        ActivatedSorter = true;
    }

    public void DeactivateSorter()
    {
        ListBlocker.SetActive(false);
        SortFrame.SetActive(false);
        if (!ActivatedSorter) return;
        ToolList.Selecting = true;
        SortButton.ClearHighlights();
        GameObject firstEntry = ToolList.transform.GetChild(0).gameObject;
        EventSystem.current.SetSelectedGameObject(firstEntry);
        ActivatedSorter = false;
    }

    public void SortByDefaultAscending()
    {
        SortList(0, ListOptions[CurrentTabIndex].OrderBy(t => t.Id).ToList());
    }

    public void SortByDefaultDescending()
    {
        SortList(1, ListOptions[CurrentTabIndex].OrderByDescending(t => t.Id).ToList());
    }

    public void SortByQuantityAscending()
    {
        SortList(2, ListOptions[CurrentTabIndex].OrderBy(t => t.Quantity).ToList());
    }

    public void SortByQuantityDescending()
    {
        SortList(3, ListOptions[CurrentTabIndex].OrderByDescending(t => t.Quantity).ToList());
    }

    public void SortByWeightAscending()
    {
        SortList(4, ListOptions[CurrentTabIndex].OrderBy(t => t.Weight).ToList());
    }

    public void SortByWeightDescending()
    {
        SortList(5, ListOptions[CurrentTabIndex].OrderByDescending(t => t.Weight).ToList());
    }

    private void SortList<T>(int type, List<T> list) where T : ToolForInventory
    {
        SavedSortSetting = type;
        ToolList.Refresh(list);
        DeactivateSorter();
    }
}