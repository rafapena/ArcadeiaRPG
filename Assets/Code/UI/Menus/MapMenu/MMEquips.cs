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

public class MMEquips : MM_Super
{
    public enum Selections { None, PartyList, SelectTool, SelectToolSwap }

    // Selection lists
    public PlayerSelectionList PartyList;
    public InventoryToolSelectionList EquippedWeapons;
    public InventoryToolSelectionList EquippedItems;
    public StatsList StatsList;
    public InventoryToolSelectionList InventoryToolList;

    // Child GameObjects
    public GameObject EquippedToolsFrame;
    public MenuFrame InventoryFrame;
    public GameObject InventoryTabs;
    public InventoryToolListSorter Sorter;

    // General selection tracking
    private Selections Selection;
    public TextMeshProUGUI EquippedToolsOwner;
    public TextMeshProUGUI EquippedWeaponsLabel;
    public TextMeshProUGUI EquippedItemsLabel;

    // Keep track of selected content
    private InventorySystem.ListType SelectedToolList;
    private ListSelectable SelectedInventoryList;

    protected override void Update()
    {
        base.Update();
        if (Selection == Selections.SelectTool || Selection == Selections.SelectToolSwap)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectItemTab();
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeaponTab();
            // else if (Input.GetKeyDown(KeyCode.C) && Selection != Selections.SelectToolSwap) SetupSorting();
            // else if (Input.GetKeyDown(KeyCode.V) && Selection != Selections.SelectToolSwap) OptimizeEquips();
        }
    }

    public override void Open()
    {
        base.Open();
        SetupSelectTeammate();
        PartyList.ResetSelected();
        HoveringFromTeammates();
        InventoryToolList.LinkToInventory(MenuManager.PartyInfo.Inventory);
    }

    public override void Close()
    {
        base.Close();
        ReturnToInitialSetup();
    }

    public override void GoBack()
    {
        switch (Selection)
        {
            case Selections.PartyList:
                Selection = Selections.None;
                ReturnToInitialSetup();
                MenuManager.GoToMain();
                break;
            case Selections.SelectTool:
                if (Sorter.gameObject.activeSelf) UndoSort();
                else UndoToolSelection();
                break;
            case Selections.SelectToolSwap:
                if (Sorter.gameObject.activeSelf) UndoSort();
                else UndoToolSwap();
                break;
        }
    }

    private void UndoSort()
    {
        InventoryToolList.Selecting = true;
        Sorter.Undo();
    }

    protected override void ReturnToInitialSetup()
    {
        PartyList.ClearSelections();
        EquippedWeapons.ClearSelections();
        EquippedItems.ClearSelections();
        StatsList.gameObject.SetActive(false);
        InventoryToolList.ClearSelections();
        EquippedToolsFrame.SetActive(false);
        InventoryFrame.Deactivate();
        Sorter.Undo();
        if (SelectedInventoryList) SelectedInventoryList.ClearHighlights();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Select Teammate --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupSelectTeammate()
    {
        Selection = Selections.PartyList;
        PartyList.Setup(MenuManager.PartyInfo.AllPlayers);
        EventSystem.current.SetSelectedGameObject(PartyList.transform.GetChild(0).gameObject);
    }

    public void HoveringFromTeammates()
    {
        if (Selection != Selections.PartyList) return;
        HoverFromTeammatesAUX();
    }

    private void HoverFromTeammatesAUX()
    {
        PartyList.SetSelected();
        EquippedToolsOwner.text = PartyList.SelectedObject.Name.ToUpper();
        EquippedToolsFrame.SetActive(true);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        StatsList.Setup(PartyList.SelectedObject as BattlePlayer);
        StatsList.gameObject.SetActive(true);
    }

    public void DeselectingTeammates()
    {
        if (Selection != Selections.PartyList) return;
        EquippedToolsFrame.SetActive(false);
        StatsList.gameObject.SetActive(false);
    }

    public void SelectTeammate()
    {
        HoverFromTeammatesAUX();
        PartyList.UnhighlightAll();
        PartyList.SelectedButton.KeepSelected();
        SetupToolSelection();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Tool Selection --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupToolSelection()
    {
        Selection = Selections.SelectTool;
        InventoryFrame.Activate();
        EquippedItems.Selecting = true;
        EquippedWeapons.Selecting = true;
        SelectItemTab();
        EventSystem.current.SetSelectedGameObject(EquippedWeapons.transform.GetChild(0).gameObject);
    }

    public void UndoToolSelection()
    {
        Selection = Selections.PartyList;
        InventoryFrame.Deactivate();
        EquippedItems.Selecting = false;
        EquippedWeapons.Selecting = false;
        PartyList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
    }

    public void DeselectingEquippedTool()
    {
        EquippedItems.InfoFrame.SetActive(false);
        EquippedWeapons.InfoFrame.SetActive(false);
    }

    public void DeselectingInventoryTool()
    {
        if (Selection == Selections.SelectTool) InventoryToolList.InfoFrame.SetActive(false);
    }

    public void SelectEquippedItem()
    {
        if (!EquippedItems.RefreshToolInfo()) return;
        Sorter.Undo();
        if (Selection == Selections.SelectTool) UnequipItem();
        else if (Selection == Selections.SelectToolSwap) SwapItem();
    }

    public void SelectEquippedWeapon()
    {
        if (!EquippedWeapons.RefreshToolInfo()) return;
        Sorter.Undo();
        if (Selection == Selections.SelectTool) UnequipWeapon();
        else if (Selection == Selections.SelectToolSwap) SwapWeapon();
    }

    public void SelectItemTab()
    {
        SelectTab(InventorySystem.ListType.Items, 0, MenuManager.PartyInfo.Inventory.Items);
    }

    public void SelectWeaponTab()
    {
        SelectTab(InventorySystem.ListType.Weapons, 1, GetWeapons());
    }

    private List<Weapon> GetWeapons()
    {
        return MenuManager.PartyInfo.Inventory.Weapons.Where(
            w => (w.WeaponType == PartyList.SelectedObject.Class.UsableWeapon1Type || w.WeaponType == PartyList.SelectedObject.Class.UsableWeapon2Type)
            ).ToList();
    }

    private void SelectTab<T>(InventorySystem.ListType listType, int tabIndex, List<T> inventoryToolList) where T : ToolForInventory
    {
        SelectedToolList = listType;
        if (Selection == Selections.SelectTool) SetEquippedToolVisibility(true, true);
        else if (Selection == Selections.SelectToolSwap) UndoToolSwap();
        if (tabIndex >= 0) EventSystem.current.SetSelectedGameObject(InventoryTabs.transform.GetChild(tabIndex).gameObject);
        
        InventoryFrame.Activate();
        KeepOnlyHighlightedSelected(ref SelectedInventoryList);
        InventoryToolList.Selecting = true;
        InventoryToolList.Refresh(inventoryToolList);
        Sorter.Undo();
        
        if (InventoryToolList.SelectedButton) InventoryToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(InventoryToolList.transform.GetChild(0).gameObject);
    }

    public void SelectInventoryTool()
    {
        if (!InventoryToolList.RefreshToolInfo())
        {
            if (InventoryToolList.SelectedButton)
                InventoryToolList.SelectedButton.ClearHighlights();
        }
        else if (Selection == Selections.SelectToolSwap)
        {
            if (SelectedToolList == InventorySystem.ListType.Items) SetupToolSwap(EquippedItems, true, false);
            else if (SelectedToolList == InventorySystem.ListType.Weapons) SetupToolSwap(EquippedWeapons, false, true);
        }
        else if (SelectedToolList == InventorySystem.ListType.Items)
        {
            UndoSort();
            if (PartyList.SelectedObject.Items.Count == BattleMaster.MAX_NUMBER_OF_ITEMS) SetupToolSwap(EquippedItems, true, false);
            else EquipItem();
        }
        else if (SelectedToolList == InventorySystem.ListType.Weapons)
        {
            UndoSort();
            if (PartyList.SelectedObject.Weapons.Count == BattleMaster.MAX_NUMBER_OF_WEAPONS) SetupToolSwap(EquippedWeapons, false, true);
            else EquipWeapon();
        }
    }

    private void SetEquippedToolVisibility(bool enableItemList, bool enableWeaponList)
    {
        EquippedItemsLabel.text = enableItemList ? "EQUIPPED ITEMS" : "\n\n\n\nSELECT A WEAPON TO SWAP OUT";
        EquippedItems.gameObject.SetActive(enableItemList);
        EquippedWeaponsLabel.text = enableWeaponList ? "EQUIPPED WEAPONS" : "\n\n\n\nSELECT AN ITEM TO SWAP OUT";
        EquippedWeapons.gameObject.SetActive(enableWeaponList);
        InventoryToolSelectionList first = enableWeaponList ? EquippedWeapons : EquippedItems;
        InventoryToolList.UpdateNavRight(first.transform.GetChild(0));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equip/Unequip Items --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UnequipItem()
    {
        PartyList.SelectedObject.UnequipItem(EquippedItems.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddItem(EquippedItems.SelectedObject as Item);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        SelectItemTab();
    }

    private void UnequipWeapon()
    {
        PartyList.SelectedObject.UnequipWeapon(EquippedWeapons.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddWeapon(EquippedWeapons.SelectedObject as Weapon);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        SelectWeaponTab();
    }

    private void EquipItem()
    {
        Item it = InventoryToolList.SelectedObject as Item;
        PartyList.SelectedObject.EquipItem(it);
        MenuManager.PartyInfo.Inventory.RemoveItem(it);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS);
        InventoryToolList.Refresh(MenuManager.PartyInfo.Inventory.Items);
    }

    private void EquipWeapon()
    {
        Weapon wp = InventoryToolList.SelectedObject as Weapon;
        PartyList.SelectedObject.EquipWeapon(wp);
        MenuManager.PartyInfo.Inventory.RemoveWeapon(wp);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS);
        InventoryToolList.Refresh(GetWeapons());
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Tool Swap --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupToolSwap(InventoryToolSelectionList toolListGUI, bool enableItemList, bool enableWeaponList)
    {
        Selection = Selections.SelectToolSwap;
        SetEquippedToolVisibility(enableItemList, enableWeaponList);
        InventoryToolList.Selecting = false;
        InventoryToolList.UnhighlightAll();
        InventoryToolList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(toolListGUI.transform.GetChild(0).gameObject);
    }

    private void UndoToolSwap()
    {
        Selection = Selections.SelectTool;
        SetEquippedToolVisibility(true, true);
        InventoryToolList.Selecting = true;
        InventoryToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(InventoryToolList.SelectedButton.gameObject);
    }

    private void SwapItem()
    {
        Selection = Selections.SelectTool;
        Item it = InventoryToolList.SelectedObject as Item;
        PartyList.SelectedObject.ReplaceItemWith(it, EquippedItems.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddItem(EquippedItems.SelectedObject as Item);
        MenuManager.PartyInfo.Inventory.RemoveItem(it);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        SelectItemTab();
    }

    private void SwapWeapon()
    {
        Selection = Selections.SelectTool;
        Weapon wp = InventoryToolList.SelectedObject as Weapon;
        PartyList.SelectedObject.ReplaceWeaponWith(wp, EquippedWeapons.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddWeapon(EquippedWeapons.SelectedObject as Weapon);
        MenuManager.PartyInfo.Inventory.RemoveWeapon(wp);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        SelectWeaponTab();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupSorting()
    {
        if (Selection != Selections.SelectTool) return;
        InventoryToolList.Selecting = false;
        InventoryToolList.ClearSelections();
        Sorter.Setup(InventoryToolList, MenuManager.PartyInfo.Inventory, SelectedToolList);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Optimizing --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void OptimizeEquips()
    {
        if (Selection != Selections.SelectTool) return;
        //
    }
}