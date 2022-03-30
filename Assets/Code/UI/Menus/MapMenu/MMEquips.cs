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
    public InventoryToolSelectionList EquippedTools;
    public StatsList StatsList;

    // Child GameObjects
    public ToolListCollectionFrame InventoryFrame;
    public GameObject EquippedToolsFrame;

    // General selection tracking
    private bool SelectingEquipment => InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons || InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Accessories;
    private Selections Selection;
    public TextMeshProUGUI EquippedToolsOwner;
    public TextMeshProUGUI EquippedToolsLabel;

    protected override void Update()
    {
        if (!MainComponent.Activated) return;
        base.Update();
        if (Selection == Selections.SelectTool || Selection == Selections.SelectToolSwap)
        {
            InventoryFrame.SelectTabInputs();
            if (Input.GetKeyDown(KeyCode.V) && Selection != Selections.SelectToolSwap) AutoEquip();
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
        EquippedTools.ClearSelections();
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
        PartyList.Refresh(MenuManager.PartyInfo.AllPlayers);
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
        EquippedTools.Refresh(PartyList.SelectedObject.Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS, true);
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
        RefreshInventoryEquipTabs();
        InventoryFrame.InitializeSelection();
        EquippedTools.Selecting = true;
        InventoryFrame.SelectTab(0);
        EventSystem.current.SetSelectedGameObject(EquippedTools.transform.GetChild(0).gameObject);
    }

    private void RefreshInventoryEquipTabs(IToolEquippable tool = null, bool resetPosition = false)
    {
        InventoryFrame.SetToolListOnTab(0, GetWeapons());
        InventoryFrame.SetToolListOnTab(1, GetAccessories());
        if (tool is Weapon)
        {
            if (resetPosition) InventoryFrame.SelectTab(0);
            else InventoryFrame.Refresh(GetWeapons());
        }
        else if (tool is Accessory)
        {
            if (resetPosition) InventoryFrame.SelectTab(1);
            else InventoryFrame.Refresh(GetAccessories());
        }
    }

    private List<Weapon> GetWeapons() => MenuManager.PartyInfo.Inventory.Weapons.FindAll(w => w.CanEquipWith(PartyList.SelectedObject.Class));

    private List<Accessory> GetAccessories() => MenuManager.PartyInfo.Inventory.Accessories.FindAll(a => a.CanEquipWith(PartyList.SelectedObject.Class));

    public void UndoToolSelection()
    {
        Selection = Selections.PartyList;
        InventoryFrame.TargetFrame.Deactivate();
        EquippedTools.Selecting = false;
        PartyList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
    }

    public void DeselectingEquippedTool()
    {
        EquippedTools.InfoFrame.SetActive(false);
    }

    public void DeselectingInventoryTool()
    {
        if (Selection == Selections.SelectTool) InventoryFrame.ToolList.InfoFrame.SetActive(false);
    }

    public void SelectEquippedTool()
    {
        if (!EquippedTools.RefreshToolInfo()) return;
        InventoryFrame.DeactivateSorter();
        if (Selection == Selections.SelectTool) UnequipTool();
        else if (Selection == Selections.SelectToolSwap) SwapTool();
    }

    public void SelectTabSuccess()
    {
        if (Selection == Selections.SelectTool) SetEquippedToolVisibility();
        else if (Selection == Selections.SelectToolSwap) UndoToolSwap();
    }

    public void SelectTabFailed()
    {
        //
    }

    // Seleting from the inventory
    public void SelectToolSuccess()
    {
        if (Selection == Selections.SelectToolSwap && SelectingEquipment) SetupToolSwap();
        else if (SelectingEquipment)
        {
            InventoryFrame.DeactivateSorter();
            if (PartyList.SelectedObject.MaxEquipment) SetupToolSwap();
            else EquipTool();
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

    private void SetEquippedToolVisibility()
    {
        EquippedToolsLabel.text = "SELECT AN ITEM TO SWAP OUT";
        EquippedTools.gameObject.SetActive(true);
        InventoryFrame.ToolList.UpdateNavRight(EquippedTools.transform.GetChild(0));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equip/Unequip/Swap Items --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UnequipTool()
    {
        IToolEquippable tool = EquippedTools.SelectedObject as IToolEquippable;
        PartyList.SelectedObject.Unequip(tool);
        MenuManager.PartyInfo.Inventory.Add(tool);
        EquippedTools.Refresh(PartyList.SelectedObject.Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS, true);
        RefreshInventoryEquipTabs(tool, true);
    }

    private void EquipTool()
    {
        IToolEquippable tool = InventoryFrame.ToolList.SelectedObject as IToolEquippable;
        PartyList.SelectedObject.Equip(tool);
        MenuManager.PartyInfo.Inventory.Remove(tool);
        EquippedTools.Refresh(PartyList.SelectedObject.Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS, true);
        RefreshInventoryEquipTabs(tool);
    }

    private void SetupToolSwap()
    {
        Selection = Selections.SelectToolSwap;
        SetEquippedToolVisibility();
        InventoryFrame.ToolList.Selecting = false;
        InventoryFrame.ToolList.UnhighlightAll();
        InventoryFrame.ToolList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(EquippedTools.transform.GetChild(0).gameObject);
    }

    private void UndoToolSwap()
    {
        Selection = Selections.SelectTool;
        SetEquippedToolVisibility();
        InventoryFrame.ToolList.Selecting = true;
        InventoryFrame.ToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(InventoryFrame.ToolList.SelectedButton.gameObject);
    }

    private void SwapTool()
    {
        Selection = Selections.SelectTool;
        IToolEquippable inventoryTool = InventoryFrame.ToolList.SelectedObject as IToolEquippable;
        IToolEquippable equippedTool = EquippedTools.SelectedObject as IToolEquippable;
        PartyList.SelectedObject.Unequip(equippedTool);
        PartyList.SelectedObject.Equip(inventoryTool);
        MenuManager.PartyInfo.Inventory.Add(equippedTool);
        MenuManager.PartyInfo.Inventory.Remove(inventoryTool);
        EquippedTools.Refresh(PartyList.SelectedObject.Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS, true);
        RefreshInventoryEquipTabs(inventoryTool, true);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Auto Equips --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void AutoEquip()
    {
        if (Selection != Selections.SelectTool) return;
        List<IToolEquippable> equips = PartyList.SelectedObject.Equipment;
        foreach (IToolEquippable tool in equips)
        {
            PartyList.SelectedObject.Unequip(tool);
            MenuManager.PartyInfo.Inventory.Add(tool);
        }
    }
}