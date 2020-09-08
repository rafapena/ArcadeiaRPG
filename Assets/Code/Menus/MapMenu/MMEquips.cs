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
    public enum Selections
    {
        None, PartyList,
        SelectEquip, ConfirmUnequip,
        SelectItemInventory, SelectWeaponInventory, EquippedDone
    }

    // Selection lists
    public PlayerSelectionList PartyList;
    public InventoryToolSelectionList EquippedWeapons;
    public InventoryToolSelectionList EquippedItems;
    public StatsList StatsList;
    public InventoryToolSelectionList InventoryWeapons;
    public InventoryToolSelectionList InventoryItems;

    // Child GameObjects
    public GameObject EquippedTools;
    public GameObject WeaponListTabs;
    public GameObject WeaponListTabBtns;
    public InventoryToolListSorter Sorter;
    public MenuFrame WeaponInventoryFrame;
    public MenuFrame ItemInventoryFrame;
    public GameObject ConfirmUnequip;

    // General selection tracking
    private Selections Selection;
    private BattleMaster.WeaponTypes WeaponList;
    public TextMeshProUGUI EquippedToolsOwner;
    public TextMeshProUGUI UnequipLabel;

    // Keep track of selected content
    private InventorySystem.ListType SelectedEquippedList;
    private ListSelectable SelectedWeaponsTabBtn;
    private delegate void SelectList();
    private List<SelectList> SelectLists;
    private KeyCode[] TabButtons = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5 };
    private int NumberOfTabs;

    // Delay after an item/weapon has been used/equipped
    private float DoneTimer;

    protected override void Update()
    {
        base.Update();
        switch (Selection)
        {
            case Selections.SelectItemInventory:
                if (Input.GetKeyDown(KeyCode.C)) SetupSorting();
                break;
            case Selections.SelectWeaponInventory:
                for (int i = 0; i < NumberOfTabs; i++)
                {
                    if (!Input.GetKeyDown(TabButtons[i])) continue;
                    SelectLists[i].Invoke();
                    break;
                }
                if (Input.GetKeyDown(KeyCode.C)) SetupSorting();
                break;
            case Selections.EquippedDone:
                if (Time.realtimeSinceStartup <= DoneTimer) return;
                EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
                Selection = Selections.PartyList;
                ReturnToInitialStep();
                HoveringFromTeammates();
                break;
        }
    }

    public override void Open()
    {
        base.Open();
        SetupSelectTeammate();
        // Manual startup - assumes the default selected button is the first entry in the Current Party List
        PartyList.ResetSelected();
        HoveringFromTeammates();
    }

    public override void Close()
    {
        base.Close();
        ReturnToInitialStep();
    }

    public override void GoBack()
    {
        switch (Selection)
        {
            case Selections.PartyList:
                Selection = Selections.None;
                MenuManager.GoToMain();
                break;
            case Selections.SelectEquip:
                UndoSelectEquip();
                break;
            case Selections.ConfirmUnequip:
                UndoUnequip();
                break;
            case Selections.SelectItemInventory:
            case Selections.SelectWeaponInventory:
                if (Sorter.gameObject.activeSelf)
                {
                    InventoryItems.Selecting = (Selection == Selections.SelectItemInventory);
                    InventoryWeapons.Selecting = (Selection == Selections.SelectWeaponInventory);
                    Sorter.Undo();
                }
                else UndoSelectTool();
                break;
        }
    }

    protected override void ReturnToInitialStep()
    {
        PartyList.ClearSelections();
        EquippedWeapons.ClearSelections();
        EquippedItems.ClearSelections();
        StatsList.gameObject.SetActive(false);
        InventoryWeapons.ClearSelections();
        InventoryItems.ClearSelections();
        EquippedTools.SetActive(false);
        WeaponListTabs.SetActive(false);
        WeaponListTabBtns.SetActive(false);
        WeaponInventoryFrame.Deactivate();
        ItemInventoryFrame.Deactivate();
        Sorter.Undo();
        ConfirmUnequip.SetActive(false);
        if (SelectedWeaponsTabBtn) SelectedWeaponsTabBtn.ClearHighlights();
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
        EquippedToolsOwner.text = PartyList.SelectedObject.Name.ToUpper(); //PartyList.SelectedTeammate.Name.ToUpper();
        SetupEquippedTools();
    }

    private void SetupEquippedTools()
    {
        EquippedTools.SetActive(true);
        EquippedWeapons.Setup(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        EquippedItems.Setup(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        StatsList.Setup(PartyList.SelectedObject as BattlePlayer);
        StatsList.gameObject.SetActive(true);
    }

    public void DeselectingTeammates()
    {
        if (Selection != Selections.PartyList) return;
        EquippedTools.SetActive(false);
        StatsList.gameObject.SetActive(false);
    }

    public void SelectTeammate()
    {
        if (Selection == Selections.ConfirmUnequip) return;
        HoverFromTeammatesAUX();
        PartyList.UnhighlightAll();
        PartyList.SelectedButton.KeepSelected();
        WeaponListTabs.SetActive(false);
        WeaponListTabBtns.SetActive(false);
        SetupCharacterEquipsList();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equipped Items/Weapons List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupCharacterEquipsList()
    {
        Selection = Selections.SelectEquip;
        EquippedItems.Selecting = true;
        EquippedWeapons.Selecting = true;
        EventSystem.current.SetSelectedGameObject(EquippedWeapons.transform.GetChild(0).gameObject);
    }

    public void UndoSelectEquip()
    {
        Selection = Selections.PartyList;
        EquippedItems.Selecting = false;
        EquippedWeapons.Selecting = false;
        PartyList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
    }

    public void SelectEquippedItem()
    {
        if (Selection != Selections.SelectEquip) return;
        SelectedEquippedList = InventorySystem.ListType.Items;
        EquippedItems.Selecting = false;
        EquippedWeapons.Selecting = false;
        if (EquippedItems.SetupToolInfo()) SetupUnequip(EquippedItems);
        else SelectItemList();
    }

    public void SelectEquippedWeapon()
    {
        if (Selection != Selections.SelectEquip) return;
        SelectedEquippedList = InventorySystem.ListType.Weapons;
        EquippedItems.Selecting = false;
        EquippedWeapons.Selecting = false;
        if (EquippedWeapons.SetupToolInfo()) SetupUnequip(EquippedWeapons);
        else SelectFirstEquippableWeaponTypeList();
    }

    private void SelectFirstEquippableWeaponTypeList()
    {
        WeaponListTabs.SetActive(true);
        WeaponListTabBtns.SetActive(true);
        bool[] hasWeapons = new bool[]
        {
            HasWeapon(BattleMaster.WeaponTypes.Blade),
            HasWeapon(BattleMaster.WeaponTypes.Hammer),
            HasWeapon(BattleMaster.WeaponTypes.Charm),
            HasWeapon(BattleMaster.WeaponTypes.Gun),
            HasWeapon(BattleMaster.WeaponTypes.Tools)
        };
        SelectList[] selectFuncs = new SelectList[] { SelectBladeList, SelectHammerList, SelectCharmList, SelectGunList, SelectBombList };
        SelectLists = new List<SelectList>();

        NumberOfTabs = 0;
        int i = 0;
        for (; i < WeaponListTabs.transform.childCount; i++)
        {
            WeaponListTabs.transform.GetChild(i).gameObject.SetActive(hasWeapons[i]);
            if (!hasWeapons[i]) continue;
            SelectLists.Add(selectFuncs[i]);
            NumberOfTabs++;
        }
        i = 0;
        for (; i < NumberOfTabs; i++) WeaponListTabBtns.transform.GetChild(i).gameObject.SetActive(true);
        for (; i < WeaponListTabBtns.transform.childCount; i++) WeaponListTabBtns.transform.GetChild(i).gameObject.SetActive(false);
        SelectLists[0].Invoke();
    }

    private bool HasWeapon(BattleMaster.WeaponTypes type)
    {
        return (PartyList.SelectedObject.Class.UsableWeapon1Type == type || PartyList.SelectedObject.Class.UsableWeapon2Type == type);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Unequipping Item --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupUnequip(InventoryToolSelectionList toolListGUI)
    {
        Selection = Selections.ConfirmUnequip;
        ConfirmUnequip.SetActive(true);
        toolListGUI.Selecting = false;
        UnequipLabel.text = "UnEquip\n" + toolListGUI.SelectedObject.Name + "?";
        toolListGUI.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(ConfirmUnequip.transform.GetChild(1).gameObject);
    }

    public void UndoUnequip()
    {
        Selection = Selections.SelectEquip;
        ConfirmUnequip.SetActive(false);
        if (SelectedEquippedList == InventorySystem.ListType.Items) UndoUnequipList(EquippedItems);
        else if (SelectedEquippedList == InventorySystem.ListType.Weapons) UndoUnequipList(EquippedWeapons);
    }

    private void UndoUnequipList(InventoryToolSelectionList toolListGUI)
    {
        toolListGUI.Selecting = true;
        toolListGUI.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(toolListGUI.SelectedButton.gameObject);
    }

    public void UnequipTool()
    {
        if (SelectedEquippedList == InventorySystem.ListType.Items)
        {
            PartyList.SelectedObject.UnequipItem(EquippedItems.SelectedIndex);
            MenuManager.PartyInfo.Inventory.AddItem(EquippedItems.SelectedObject as Item);
            EquippedItems.Setup(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS, true);
        }
        else if (SelectedEquippedList == InventorySystem.ListType.Weapons)
        {
            PartyList.SelectedObject.UnequipWeapon(EquippedWeapons.SelectedIndex);
            MenuManager.PartyInfo.Inventory.AddWeapon(EquippedWeapons.SelectedObject as Weapon);
            EquippedWeapons.Setup(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS, true);
        }
        UndoUnequip();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectItemList()
    {
        WeaponList = BattleMaster.WeaponTypes.None;
        EventSystem.current.SetSelectedGameObject(InventoryItems.transform.GetChild(0).gameObject);
        SelectWeaponTab(Selections.SelectItemInventory, ItemInventoryFrame, InventoryItems, MenuManager.PartyInfo.Inventory.Items);
    }

    public void SelectBladeList()
    {
        WeaponList = BattleMaster.WeaponTypes.Blade;
        EventSystem.current.SetSelectedGameObject(WeaponListTabs.transform.GetChild(0).gameObject);
        List<Weapon> wp = MenuManager.PartyInfo.Inventory.Weapons.Where(w => w.WeaponType == BattleMaster.WeaponTypes.Blade).ToList();
        SelectWeaponTab(Selections.SelectWeaponInventory, WeaponInventoryFrame, InventoryWeapons, wp);
    }

    public void SelectHammerList()
    {
        WeaponList = BattleMaster.WeaponTypes.Hammer;
        EventSystem.current.SetSelectedGameObject(WeaponListTabs.transform.GetChild(1).gameObject);
        List<Weapon> wp = MenuManager.PartyInfo.Inventory.Weapons.Where(w => w.WeaponType == BattleMaster.WeaponTypes.Hammer).ToList();
        SelectWeaponTab(Selections.SelectWeaponInventory, WeaponInventoryFrame, InventoryWeapons, wp);
    }

    public void SelectCharmList()
    {
        WeaponList = BattleMaster.WeaponTypes.Charm;
        EventSystem.current.SetSelectedGameObject(WeaponListTabs.transform.GetChild(2).gameObject);
        List<Weapon> wp = MenuManager.PartyInfo.Inventory.Weapons.Where(w => w.WeaponType == BattleMaster.WeaponTypes.Charm).ToList();
        SelectWeaponTab(Selections.SelectWeaponInventory, WeaponInventoryFrame, InventoryWeapons, wp);
    }

    public void SelectGunList()
    {
        WeaponList = BattleMaster.WeaponTypes.Gun;
        EventSystem.current.SetSelectedGameObject(WeaponListTabs.transform.GetChild(3).gameObject);
        List<Weapon> wp = MenuManager.PartyInfo.Inventory.Weapons.Where(w => w.WeaponType == BattleMaster.WeaponTypes.Gun).ToList();
        SelectWeaponTab(Selections.SelectWeaponInventory, WeaponInventoryFrame, InventoryWeapons, wp);
    }

    public void SelectBombList()
    {
        WeaponList = BattleMaster.WeaponTypes.Tools;
        EventSystem.current.SetSelectedGameObject(WeaponListTabs.transform.GetChild(4).gameObject);
        List<Weapon> wp = MenuManager.PartyInfo.Inventory.Weapons.Where(w => w.WeaponType == BattleMaster.WeaponTypes.Tools).ToList();
        SelectWeaponTab(Selections.SelectWeaponInventory, WeaponInventoryFrame, InventoryWeapons, wp);
    }

    private void SelectWeaponTab<T>(Selections selection, MenuFrame frame, InventoryToolSelectionList toolListGUI, List<T> toolList) where T : ToolForInventory
    {
        Selection = selection;
        frame.Activate();
        KeepOnlyHighlightedSelected(ref SelectedWeaponsTabBtn);
        toolListGUI.Selecting = true;
        toolListGUI.Setup(toolList);
        Sorter.Undo();
        if (toolListGUI.SelectedButton) toolListGUI.SelectedButton.ClearHighlights();
        RemoveBlankSquaresBasedOnInventoryCap();
        EventSystem.current.SetSelectedGameObject(toolListGUI.transform.GetChild(0).gameObject);
    }

    public void SelectInventoryWeapon()
    {
        EquipTool(InventoryWeapons);
    }

    public void SelectInventoryItem()
    {
        EquipTool(InventoryItems);
    }

    private void EquipTool(InventoryToolSelectionList toolListGUI)
    {
        if (Selection == Selections.EquippedDone) return;
        if (!toolListGUI.SetupToolInfo())
        {
            if (toolListGUI.SelectedButton)
                toolListGUI.SelectedButton.ClearHighlights();
            return;
        }
        if (SelectedEquippedList == InventorySystem.ListType.Items)
        {
            PartyList.SelectedObject.EquipItem(InventoryItems.SelectedObject as Item);
            MenuManager.PartyInfo.Inventory.RemoveItem(InventoryItems.SelectedIndex);
            InventoryItems.Setup(MenuManager.PartyInfo.Inventory.Items);
            EquippedItems.Setup(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS);
            if (PartyList.SelectedObject.Items.Count == BattleMaster.MAX_NUMBER_OF_ITEMS) SetEquipDone();
        }
        else if (SelectedEquippedList == InventorySystem.ListType.Weapons)
        {
            PartyList.SelectedObject.EquipWeapon(InventoryWeapons.SelectedObject as Weapon);
            MenuManager.PartyInfo.Inventory.RemoveWeapon(InventoryWeapons.SelectedIndex);
            InventoryWeapons.Setup(MenuManager.PartyInfo.Inventory.Weapons);
            EquippedWeapons.Setup(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS);
            if (PartyList.SelectedObject.Weapons.Count == BattleMaster.MAX_NUMBER_OF_WEAPONS) SetEquipDone();
        }
        RemoveBlankSquaresBasedOnInventoryCap();
        Sorter.Undo();
    }

    private void SetEquipDone()
    {
        Selection = Selections.EquippedDone;
        DoneTimer = Time.realtimeSinceStartup + 1f;
    }

    private void UndoSelectTool()
    {
        Selection = Selections.SelectEquip;
        ItemInventoryFrame.Deactivate();
        WeaponInventoryFrame.Deactivate();
        if (SelectedEquippedList == InventorySystem.ListType.Items && EquippedItems.SelectedButton)
        {
            EquippedItems.SelectedButton.ClearHighlights();
            EventSystem.current.SetSelectedGameObject(EquippedItems.SelectedButton.gameObject);
        }
        else if (SelectedEquippedList == InventorySystem.ListType.Weapons && EquippedWeapons.SelectedButton)
        {
            EquippedWeapons.SelectedButton.ClearHighlights();
            EventSystem.current.SetSelectedGameObject(EquippedWeapons.SelectedButton.gameObject);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void RemoveBlankSquaresBasedOnInventoryCap()
    {
        MenuManager.PartyInfo.Inventory.UpdateNumberOfTools();
        InventoryItems.FilterUnneededBlanks(MenuManager.PartyInfo.Inventory);
        InventoryWeapons.FilterUnneededBlanks(MenuManager.PartyInfo.Inventory);
    }

    public void SetupSorting()
    {
        if (Selection == Selections.SelectItemInventory) SetupSorting(InventoryItems);
        else if (Selection == Selections.SelectWeaponInventory) SetupSorting(InventoryWeapons);
    }

    private void SetupSorting(InventoryToolSelectionList toolListGUI)
    {
        toolListGUI.Selecting = false;
        toolListGUI.ClearSelections();
        switch (WeaponList)
        {
            case BattleMaster.WeaponTypes.None:
                Sorter.Setup(toolListGUI, MenuManager.PartyInfo.Inventory, InventorySystem.ListType.Items, RemoveBlankSquaresBasedOnInventoryCap);
                break;
            case BattleMaster.WeaponTypes.Blade:
                Sorter.Setup(toolListGUI, MenuManager.PartyInfo.Inventory, InventorySystem.ListType.Weapons, RemoveBlankSquaresBasedOnInventoryCap);
                break;
            case BattleMaster.WeaponTypes.Hammer:
                Sorter.Setup(toolListGUI, MenuManager.PartyInfo.Inventory, InventorySystem.ListType.Weapons, RemoveBlankSquaresBasedOnInventoryCap);
                break;
            case BattleMaster.WeaponTypes.Charm:
                Sorter.Setup(toolListGUI, MenuManager.PartyInfo.Inventory, InventorySystem.ListType.Weapons, RemoveBlankSquaresBasedOnInventoryCap);
                break;
            case BattleMaster.WeaponTypes.Gun:
                Sorter.Setup(toolListGUI, MenuManager.PartyInfo.Inventory, InventorySystem.ListType.Weapons, RemoveBlankSquaresBasedOnInventoryCap);
                break;
            case BattleMaster.WeaponTypes.Tools:
                Sorter.Setup(toolListGUI, MenuManager.PartyInfo.Inventory, InventorySystem.ListType.Weapons, RemoveBlankSquaresBasedOnInventoryCap);
                break;
        }
    }
}