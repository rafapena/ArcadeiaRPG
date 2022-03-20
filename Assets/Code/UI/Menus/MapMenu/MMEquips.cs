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
    public InventoryToolSelectionList EquippedAccessories;
    public StatsList StatsList;

    // Child GameObjects
    public ToolListCollectionFrame InventoryFrame;
    public GameObject EquippedToolsFrame;

    // General selection tracking
    private Selections Selection;
    public TextMeshProUGUI EquippedToolsOwner;
    public TextMeshProUGUI EquippedWeaponsLabel;
    public TextMeshProUGUI EquippedAccessoriesLabel;

    // Keep track of selected content
    //private InventorySystem.ListType SelectedToolList;
    //private ListSelectable SelectedInventoryList;

    protected override void Update()
    {
        if (!MainComponent.Activated) return;
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
        EquippedAccessories.ClearSelections();
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
        EquippedAccessories.Refresh(PartyList.SelectedObject.Accessories, BattleMaster.MAX_NUMBER_OF_ACCESSORIES, true);
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
    /// -- ActiveTool Selection --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupToolSelection()
    {
        Selection = Selections.SelectTool;
        RegisterLists();
        InventoryFrame.InitializeSelection();
        EquippedAccessories.Selecting = true;
        EquippedWeapons.Selecting = true;
        InventoryFrame.SelectTab(0);
        EventSystem.current.SetSelectedGameObject(EquippedWeapons.transform.GetChild(0).gameObject);
    }

    private void RegisterLists()
    {
        BattlerClass bc = PartyList.SelectedObject.Class;
        InventoryFrame.RegisterToolList(0, MenuManager.PartyInfo.Inventory.Weapons.FindAll(w => w.WeaponType == bc.UsableWeapon1Type || w.WeaponType == bc.UsableWeapon2Type));
        InventoryFrame.RegisterToolList(1, MenuManager.PartyInfo.Inventory.Accessories);
    }

    public void UndoToolSelection()
    {
        Selection = Selections.PartyList;
        InventoryFrame.TargetFrame.Deactivate();
        EquippedAccessories.Selecting = false;
        EquippedWeapons.Selecting = false;
        PartyList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
    }

    public void DeselectingEquippedTool()
    {
        EquippedAccessories.InfoFrame.SetActive(false);
        EquippedWeapons.InfoFrame.SetActive(false);
    }

    public void DeselectingInventoryTool()
    {
        if (Selection == Selections.SelectTool) InventoryFrame.ToolList.InfoFrame.SetActive(false);
    }

    public void SelectEquippedWeapon()
    {
        if (!EquippedWeapons.RefreshToolInfo()) return;
        InventoryFrame.DeactivateSorter();
        if (Selection == Selections.SelectTool) UnequipWeapon();
        else if (Selection == Selections.SelectToolSwap) SwapWeapon();
    }

    public void SelectEquippedAccessory()
    {
        if (!EquippedAccessories.RefreshToolInfo()) return;
        InventoryFrame.DeactivateSorter();
        if (Selection == Selections.SelectTool) UnequipAccessory();
        else if (Selection == Selections.SelectToolSwap) SwapAccessory();
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
            if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons) SetupToolSwap(EquippedWeapons, true, false);
            else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Accessories) SetupToolSwap(EquippedAccessories, false, true);
        }
        else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons)
        {
            InventoryFrame.DeactivateSorter();
            if (PartyList.SelectedObject.Weapons.Count == BattleMaster.MAX_NUMBER_OF_WEAPONS) SetupToolSwap(EquippedWeapons, true, false);
            else EquipWeapon();
        }
        else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Accessories)
        {
            InventoryFrame.DeactivateSorter();
            if (PartyList.SelectedObject.Accessories.Count == BattleMaster.MAX_NUMBER_OF_ACCESSORIES) SetupToolSwap(EquippedAccessories, false, true);
            else EquipAccessory();
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

    private void SetEquippedToolVisibility(bool enableWeaponList, bool enableAccessoriesList)
    {
        EquippedAccessoriesLabel.text = enableAccessoriesList ? "EQUIPPED ACCESSORIES" : "\n\n\n\nSELECT A WEAPON TO SWAP OUT";
        EquippedAccessories.gameObject.SetActive(enableAccessoriesList);
        EquippedWeaponsLabel.text = enableWeaponList ? "EQUIPPED WEAPONS" : "\n\n\n\nSELECT AN ACCESSORY TO SWAP OUT";
        EquippedWeapons.gameObject.SetActive(enableWeaponList);
        InventoryToolSelectionList first = enableWeaponList ? EquippedWeapons : EquippedAccessories;
        InventoryFrame.ToolList.UpdateNavRight(first.transform.GetChild(0));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equip/Unequip Items --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UnequipWeapon()
    {
        IToolForInventory wp = PartyList.SelectedObject.Unequip<Weapon>(EquippedWeapons.SelectedIndex);
        MenuManager.PartyInfo.Inventory.Add(wp);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        RegisterLists();
        InventoryFrame.SelectTab(0);
    }

    private void EquipWeapon()
    {
        Weapon wp = InventoryFrame.ToolList.SelectedObject as Weapon;
        PartyList.SelectedObject.Equip(wp);
        MenuManager.PartyInfo.Inventory.Remove(wp);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        RegisterLists();
        InventoryFrame.Refresh();
    }

    private void UnequipAccessory()
    {
        IToolForInventory ac = PartyList.SelectedObject.Unequip<Accessory>(EquippedAccessories.SelectedIndex);
        MenuManager.PartyInfo.Inventory.Add(ac);
        EquippedAccessories.Refresh(PartyList.SelectedObject.Accessories, BattleMaster.MAX_NUMBER_OF_ACCESSORIES, true);
        InventoryFrame.SelectTab(1);
    }

    private void EquipAccessory()
    {
        Accessory ac = InventoryFrame.ToolList.SelectedObject as Accessory;
        PartyList.SelectedObject.Equip(ac);
        MenuManager.PartyInfo.Inventory.Remove(ac);
        EquippedAccessories.Refresh(PartyList.SelectedObject.Accessories, BattleMaster.MAX_NUMBER_OF_ACCESSORIES, true);
        InventoryFrame.Refresh();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- ActiveTool Swap --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupToolSwap(InventoryToolSelectionList toolListGUI, bool enableWeaponList, bool enableAccessoryList)
    {
        Selection = Selections.SelectToolSwap;
        SetEquippedToolVisibility(enableWeaponList, enableAccessoryList);
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

    private void SwapWeapon()
    {
        Selection = Selections.SelectTool;
        PartyList.SelectedObject.ReplaceEquipWith(InventoryFrame.ToolList.SelectedObject, EquippedWeapons.SelectedIndex);
        MenuManager.PartyInfo.Inventory.Add(EquippedWeapons.SelectedObject);
        MenuManager.PartyInfo.Inventory.Remove(InventoryFrame.ToolList.SelectedObject);
        EquippedWeapons.Refresh(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        InventoryFrame.SelectTab(0);
    }

    private void SwapAccessory()
    {
        Selection = Selections.SelectTool;
        PartyList.SelectedObject.ReplaceEquipWith(InventoryFrame.ToolList.SelectedObject, EquippedAccessories.SelectedIndex);
        MenuManager.PartyInfo.Inventory.Add(EquippedAccessories.SelectedObject);
        MenuManager.PartyInfo.Inventory.Remove(InventoryFrame.ToolList.SelectedObject);
        EquippedAccessories.Refresh(PartyList.SelectedObject.Accessories, BattleMaster.MAX_NUMBER_OF_ACCESSORIES, true);
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