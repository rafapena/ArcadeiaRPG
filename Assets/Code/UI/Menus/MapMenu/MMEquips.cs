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

public class MMEquips : MM_Super, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    public enum Selections { None, PartyList, SelectTool, SelectToolSwap }

    // Selection lists
    public PlayerSelectionList PartyList;
    public InventoryToolSelectionList EquippedWeapons;
    public InventoryToolSelectionList EquippedItems;
    public StatsList StatsList;

    // Child GameObjects
    public ToolListCollectionFrame InventoryFrame;
    public GameObject EquippedToolsFrame;

    // General selection tracking
    private Selections Selection;
    public TextMeshProUGUI EquippedToolsOwner;
    public TextMeshProUGUI EquippedWeaponsLabel;
    public TextMeshProUGUI EquippedItemsLabel;

    // Keep track of selected content
    //private InventorySystem.ListType SelectedToolList;
    //private ListSelectable SelectedInventoryList;

    protected override void Update()
    {
        base.Update();
        if (Selection == Selections.SelectTool || Selection == Selections.SelectToolSwap)
        {
            InventoryFrame.SelectTabInputs();
            if (Input.GetKeyDown(KeyCode.V) && Selection != Selections.SelectToolSwap) OptimizeEquips();
        }
    }

    public override void Open()
    {
        base.Open();
        SetupSelectTeammate();
        PartyList.ResetSelected();
        HoveringFromTeammates();
        InventoryFrame.TrackCarryWeight(MenuManager.PartyInfo.Inventory);
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
                if (InventoryFrame.ActivatedSorter) InventoryFrame.DeactivateSorter();
                else UndoToolSelection();
                break;
            case Selections.SelectToolSwap:
                if (InventoryFrame.ActivatedSorter) InventoryFrame.DeactivateSorter();
                else UndoToolSwap();
                break;
        }
    }

    protected override void ReturnToInitialSetup()
    {
        PartyList.ClearSelections();
        EquippedWeapons.ClearSelections();
        EquippedItems.ClearSelections();
        StatsList.gameObject.SetActive(false);
        InventoryFrame.SelectingToolList();
        InventoryFrame.TargetFrame.Deactivate();
        InventoryFrame.ToolList.ClearSelections();
        EquippedToolsFrame.SetActive(false);
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
        InventoryFrame.RegisterToolList(0, MenuManager.PartyInfo.Inventory.Items);
        InventoryFrame.RegisterToolList(1, GetWeapons());
        InventoryFrame.InitializeSelection();
        EquippedItems.Selecting = true;
        EquippedWeapons.Selecting = true;
        InventoryFrame.SelectTab(1);
        EventSystem.current.SetSelectedGameObject(EquippedWeapons.transform.GetChild(0).gameObject);
    }

    private List<Weapon> GetWeapons()
    {
        return MenuManager.PartyInfo.Inventory.Weapons.Where(
            w => (w.WeaponType == PartyList.SelectedObject.Class.UsableWeapon1Type || w.WeaponType == PartyList.SelectedObject.Class.UsableWeapon2Type)
            ).ToList();
    }

    public void UndoToolSelection()
    {
        Selection = Selections.PartyList;
        InventoryFrame.TargetFrame.Deactivate();
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
        if (Selection == Selections.SelectTool) InventoryFrame.ToolList.InfoFrame.SetActive(false);
    }

    public void SelectEquippedItem()
    {
        if (!EquippedItems.RefreshToolInfo()) return;
        InventoryFrame.DeactivateSorter();
        if (Selection == Selections.SelectTool) UnequipItem();
        else if (Selection == Selections.SelectToolSwap) SwapItem();
    }

    public void SelectEquippedWeapon()
    {
        if (!EquippedWeapons.RefreshToolInfo()) return;
        InventoryFrame.DeactivateSorter();
        if (Selection == Selections.SelectTool) UnequipWeapon();
        else if (Selection == Selections.SelectToolSwap) SwapWeapon();
    }

    public void SelectTabSuccess()
    {
        if (Selection == Selections.SelectTool) SetEquippedToolVisibility(true, true);
        else if (Selection == Selections.SelectToolSwap) UndoToolSwap();
    }

    public void SelectTabFailed()
    {
        //
    }

    // Seleting from the inventory
    public void SelectToolSuccess()
    {
        if (Selection == Selections.SelectToolSwap)
        {
            if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Items) SetupToolSwap(EquippedItems, true, false);
            else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons) SetupToolSwap(EquippedWeapons, false, true);
        }
        else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Items)
        {
            InventoryFrame.DeactivateSorter();
            if (PartyList.SelectedObject.Items.Count == BattleMaster.MAX_NUMBER_OF_ITEMS) SetupToolSwap(EquippedItems, true, false);
            else EquipItem();
        }
        else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons)
        {
            InventoryFrame.DeactivateSorter();
            if (PartyList.SelectedObject.Weapons.Count == BattleMaster.MAX_NUMBER_OF_WEAPONS) SetupToolSwap(EquippedWeapons, false, true);
            else EquipWeapon();
        }
    }

    // Seleting from the inventory
    public void SelectToolFailed()
    {
        if (InventoryFrame.ToolList.SelectedButton) InventoryFrame.ToolList.SelectedButton.ClearHighlights();
    }

    // Seleting from the inventory
    public void UndoSelectToolSuccess()
    {
        //
    }

    public void ActivateSorterSuccess()
    {
        //
    }

    private void SetEquippedToolVisibility(bool enableItemList, bool enableWeaponList)
    {
        EquippedItemsLabel.text = enableItemList ? "EQUIPPED ITEMS" : "\n\n\n\nSELECT A WEAPON TO SWAP OUT";
        EquippedItems.gameObject.SetActive(enableItemList);
        EquippedWeaponsLabel.text = enableWeaponList ? "EQUIPPED WEAPONS" : "\n\n\n\nSELECT AN ITEM TO SWAP OUT";
        EquippedWeapons.gameObject.SetActive(enableWeaponList);
        InventoryToolSelectionList first = enableWeaponList ? EquippedWeapons : EquippedItems;
        InventoryFrame.ToolList.UpdateNavRight(first.transform.GetChild(0));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equip/Unequip Items --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UnequipItem()
    {
        PartyList.SelectedObject.UnequipItem(EquippedItems.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddItem(EquippedItems.SelectedObject as Item);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        InventoryFrame.SelectTab(0);
    }

    private void UnequipWeapon()
    {
        PartyList.SelectedObject.UnequipWeapon(EquippedWeapons.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddWeapon(EquippedWeapons.SelectedObject as Weapon);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        InventoryFrame.SelectTab(1);
    }

    private void EquipItem()
    {
        Item it = InventoryFrame.ToolList.SelectedObject as Item;
        PartyList.SelectedObject.EquipItem(it);
        MenuManager.PartyInfo.Inventory.RemoveItem(it);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS);
        InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Items);
    }

    private void EquipWeapon()
    {
        Weapon wp = InventoryFrame.ToolList.SelectedObject as Weapon;
        PartyList.SelectedObject.EquipWeapon(wp);
        MenuManager.PartyInfo.Inventory.RemoveWeapon(wp);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS);
        InventoryFrame.Refresh(GetWeapons());
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Tool Swap --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupToolSwap(InventoryToolSelectionList toolListGUI, bool enableItemList, bool enableWeaponList)
    {
        Selection = Selections.SelectToolSwap;
        SetEquippedToolVisibility(enableItemList, enableWeaponList);
        InventoryFrame.ToolList.Selecting = false;
        InventoryFrame.ToolList.UnhighlightAll();
        InventoryFrame.ToolList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(toolListGUI.transform.GetChild(0).gameObject);
    }

    private void UndoToolSwap()
    {
        Selection = Selections.SelectTool;
        SetEquippedToolVisibility(true, true);
        InventoryFrame.ToolList.Selecting = true;
        InventoryFrame.ToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(InventoryFrame.ToolList.SelectedButton.gameObject);
    }

    private void SwapItem()
    {
        Selection = Selections.SelectTool;
        Item it = InventoryFrame.ToolList.SelectedObject as Item;
        PartyList.SelectedObject.ReplaceItemWith(it, EquippedItems.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddItem(EquippedItems.SelectedObject as Item);
        MenuManager.PartyInfo.Inventory.RemoveItem(it);
        EquippedItems.Refresh(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        InventoryFrame.SelectTab(0);
    }

    private void SwapWeapon()
    {
        Selection = Selections.SelectTool;
        Weapon wp = InventoryFrame.ToolList.SelectedObject as Weapon;
        PartyList.SelectedObject.ReplaceWeaponWith(wp, EquippedWeapons.SelectedIndex);
        MenuManager.PartyInfo.Inventory.AddWeapon(EquippedWeapons.SelectedObject as Weapon);
        MenuManager.PartyInfo.Inventory.RemoveWeapon(wp);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        InventoryFrame.SelectTab(1);
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