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

public class MMInventory : MM_Super
{
    public enum Selections
    {
        None, InventoryLists,
        Usage, Discard,
        UseOnCharacter, UsageDone,
        EquipOnCharacter, CharacterEquipsList, ConfirmEquip, EquippedDone
    }

    // Selection lists
    public InventoryToolSelectionList ToolList;
    public InventoryToolSelectionList EquippedToolList;
    public PlayerSelectionList PartyList;

    // Child GameObjects;
    public GameObject ToolListTabs;
    public InventoryToolListSorter Sorter;
    public GameObject SelectingUsage;
    public GameObject ConfirmDiscard;
    public MenuFrame SelectingTeammate;
    public GameObject EquippedTools;
    public GameObject ConfirmSwap;

    // General selection tracking
    private Selections Selection;
    private InventorySystem.ListType InventoryList;
    public TextMeshProUGUI DiscardLabel;
    public TextMeshProUGUI SelectTeammateLabel;

    // Keep track of selected content
    private ListSelectable SelectedInventoryTab;
    private ListSelectable SelectedUsageListBtn;

    // List of potential targets
    private List<Battler> SelectableTeammatesUse;
    private List<Battler> SelectableTeammatesEquip;
    private bool SelectAllTeammates;

    // Delay after an item/weapon has been used/equipped
    private float DoneTimer;

    protected override void Update()
    {
        base.Update();
        switch (Selection)
        {
            case Selections.InventoryLists:
                if (Input.GetKeyDown(KeyCode.Alpha1)) SelectItemList();
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectWeaponList();
                else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectKeyItemList();
                // else if (Input.GetKeyDown(KeyCode.C)) SetupSorting();
                break;
            case Selections.UsageDone:
            case Selections.EquippedDone:
                if (Time.realtimeSinceStartup <= DoneTimer) return;
                EventSystem.current.SetSelectedGameObject(ToolList.SelectedButton.gameObject);
                ReturnToInitialStep();
                Selection = Selections.InventoryLists;
                break;
        }
    }

    public override void Open()
    {
        base.Open();
        ToolList.LinkToInventory(MenuManager.PartyInfo.Inventory);
        SelectItemList();
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
            case Selections.InventoryLists:
                if (Sorter.gameObject.activeSelf)
                {
                    ToolList.Selecting = true;
                    Sorter.Undo();
                    break;
                }
                Selection = Selections.None;
                MenuManager.GoToMain();
                break;
            case Selections.Usage:
                UndoSelectTool();
                break;
            case Selections.Discard:
                UndoDiscard();
                break;
            case Selections.UseOnCharacter:
            case Selections.EquipOnCharacter:
                UndoSelectTeammate();
                break;
            case Selections.CharacterEquipsList:
                UndoSelectEquippedTool();
                break;
            case Selections.ConfirmEquip:
                UndoConfirmSwap();
                break;
        }
    }

    protected override void ReturnToInitialStep()
    {
        ToolList.Selecting = true;
        Sorter.Undo();
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        SelectingTeammate.Deactivate();
        EquippedTools.SetActive(false);
        ConfirmSwap.SetActive(false);
        ToolList.ClearSelections();
        PartyList.ClearSelections();
        EquippedToolList.ClearSelections();
        if (SelectedUsageListBtn)
            SelectedUsageListBtn.ClearHighlights();
        SelectableTeammatesUse = null;
        SelectableTeammatesEquip = null;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectItemList()
    {
        SelectToolTab(InventorySystem.ListType.Items, MenuManager.PartyInfo.Inventory.Items, 0);
    }

    public void SelectWeaponList()
    {
        SelectToolTab(InventorySystem.ListType.Weapons, MenuManager.PartyInfo.Inventory.Weapons, 1);
    }

    public void SelectKeyItemList()
    {
        SelectToolTab(InventorySystem.ListType.KeyItems, MenuManager.PartyInfo.Inventory.KeyItems, 2);
    }

    private void SelectToolTab<T>(InventorySystem.ListType inventoryList, List<T> toolList, int tabIndex) where T : ToolForInventory
    {
        if (Sorter.gameObject.activeSelf) return;
        Selection = Selections.InventoryLists;
        InventoryList = inventoryList;
        EventSystem.current.SetSelectedGameObject(ToolListTabs.transform.GetChild(tabIndex).gameObject);
        ReturnToInitialStep();
        KeepOnlyHighlightedSelected(ref SelectedInventoryTab);
        ToolList.Selecting = true;
        ToolList.Setup(toolList);
        if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(ToolList.transform.GetChild(0).gameObject);
    }

    public void SelectTool()
    {
        ConfirmDiscard.SetActive(false);
        if (!ToolList.SetupToolInfo())
        {
            if (ToolList.SelectedButton) ToolList.SelectedButton.ClearHighlights();
            SelectingUsage.SetActive(false);
            return;
        }
        Selection = Selections.Usage;
        ToolList.Selecting = false;
        SelectingUsage.SetActive(true);
        KeepOnlyHighlightedSelected(ref ToolList.SelectedButton);
        Sorter.Undo();
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
        SetupUsageButtons();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupSorting()
    {
        Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        ToolList.Selecting = false;
        ToolList.ClearSelections();
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
        Sorter.Setup(ToolList, MenuManager.PartyInfo.Inventory, InventoryList);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- How will Tool be used + Setup background data for character list --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupUsageButtons()
    {
        SelectableTeammatesUse = new List<Battler>();
        SelectableTeammatesEquip = new List<Battler>();
        SelectAllTeammates = false;
        Transform CurrentUsageList;
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                CurrentUsageList = SelectingUsage.transform.GetChild(0);
                CurrentUsageList.GetChild(0).GetComponent<Button>().interactable = GetSelectableTeammatesForUsingItems();
                CurrentUsageList.GetChild(1).GetComponent<Button>().interactable = GetSelectableTeammatesForEquippingItems();
                break;
            case InventorySystem.ListType.Weapons:
                CurrentUsageList = SelectingUsage.transform.GetChild(1);
                CurrentUsageList.GetChild(0).GetComponent<Button>().interactable = GetSelectableTeammatesForEquippingWeapons();
                break;
            default:
                return;     // Key Items and anything else
        }
        SelectingUsage.transform.GetChild(0).gameObject.SetActive(false);
        SelectingUsage.transform.GetChild(1).gameObject.SetActive(false);
        CurrentUsageList.gameObject.SetActive(true);
        for (int i = 0; i < CurrentUsageList.childCount; i++)
        {
            if (!CurrentUsageList.GetChild(i).GetComponent<Selectable>().interactable) continue;
            EventSystem.current.SetSelectedGameObject(CurrentUsageList.GetChild(i).gameObject);
            return;
        }
        EventSystem.current.SetSelectedGameObject(null);    // If everything in the usage list has been disabled
    }

    private bool GetSelectableTeammatesForUsingItems()
    {
        List<Battler> party = MenuManager.PartyInfo.GetWholeParty();
        switch (ToolList.SelectedObject.Scope)
        {
            case Tool.ScopeType.OneTeammate:
            case Tool.ScopeType.Self:
                foreach (Battler p in party)
                    if (!p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = false;
                break;

            case Tool.ScopeType.OneKnockedOutTeammate:
                foreach (Battler p in party)
                    if (p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = false;
                break;

            case Tool.ScopeType.AllTeammates:
            case Tool.ScopeType.EveryoneButSelf:
            case Tool.ScopeType.Everyone:
                foreach (Battler p in party)
                    if (!p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = true;
                break;

            case Tool.ScopeType.AllKnockedOutTeammates:
                foreach (Battler p in party)
                    if (p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = true;
                break;
        }
        return SelectableTeammatesUse.Count > 0;
    }

    private bool GetSelectableTeammatesForEquippingItems()
    {
        foreach (BattlePlayer p in MenuManager.PartyInfo.AllPlayers)
        {
            bool matchClassExclusive = ToolList.SelectedObject.ClassExclusives.Count == 0 || ToolList.SelectedObject.ClassExclusives.Contains(p.Class);
            if (matchClassExclusive) SelectableTeammatesEquip.Add(p);
        }
        return SelectableTeammatesEquip.Count > 0;
    }

    private bool GetSelectableTeammatesForEquippingWeapons()
    {
        Weapon selectedWeapon = ToolList.SelectedObject as Weapon;
        foreach (BattlePlayer p in MenuManager.PartyInfo.AllPlayers)
        {
            bool matchClassExclusive = ToolList.SelectedObject.ClassExclusives.Count == 0 || ToolList.SelectedObject.ClassExclusives.Contains(p.Class);
            bool isUsableWeaponType = p.Class.UsableWeapon1Type == selectedWeapon.WeaponType || p.Class.UsableWeapon2Type == selectedWeapon.WeaponType;
            if (matchClassExclusive && isUsableWeaponType) SelectableTeammatesEquip.Add(p);
        }
        return SelectableTeammatesEquip.Count > 0;
    }

    public void UndoSelectTool()
    {
        Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        ToolList.SelectedButton.ClearHighlights();
        ToolList.Selecting = true;
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
        foreach (Transform t0 in SelectingUsage.transform)
            foreach (Transform t in t0)
                t.GetComponent<ListSelectable>().ClearHighlights();
        EventSystem.current.SetSelectedGameObject(ToolList.SelectedButton.gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Discarding Inventory Tool --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupDiscard()
    {
        Selection = Selections.Discard;
        KeepOnlyHighlightedSelected(ref SelectedUsageListBtn);
        ConfirmDiscard.SetActive(true);
        DiscardLabel.text = "Discard\n" + ToolList.SelectedObject.Name + "?";
        EventSystem.current.SetSelectedGameObject(ConfirmDiscard.transform.GetChild(1).gameObject);
    }

    public void UndoDiscard()
    {
        Selection = Selections.Usage;
        ConfirmDiscard.SetActive(false);
        SelectedUsageListBtn.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(SelectedUsageListBtn.gameObject);
    }

    public void DiscardTool()
    {
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                MenuManager.PartyInfo.Inventory.RemoveItem(ToolList.SelectedIndex);
                ToolList.Setup(MenuManager.PartyInfo.Inventory.Items);
                break;
            case InventorySystem.ListType.Weapons:
                MenuManager.PartyInfo.Inventory.RemoveWeapon(ToolList.SelectedIndex);
                ToolList.Setup(MenuManager.PartyInfo.Inventory.Weapons);
                break;
        }
        UndoDiscard();
        UndoSelectTool();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Select Teammate --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupUseOnCharacter()
    {
        Selection = Selections.UseOnCharacter;
        SelectTeammateLabel.text = SelectAllTeammates ? ("USE " + ToolList.SelectedObject.Name.ToUpper() + " ON EVERYONE?") : ("USE " + ToolList.SelectedObject.Name.ToUpper() + " ON...");
        PartyList.Setup(SelectableTeammatesUse);
        SetupTeammatesList();
    }

    public void SetupEquipOnCharacter()
    {
        Selection = Selections.EquipOnCharacter;
        SelectTeammateLabel.text = "EQUIP " + ToolList.SelectedObject.Name.ToUpper() + " ON...";
        EquippedTools.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (InventoryList == InventorySystem.ListType.Items) ? "EQUIPPED ITEMS" : "EQUIPPED WEAPONS";
        PartyList.Setup(SelectableTeammatesEquip);
        SetupTeammatesList();
    }

    private void SetupTeammatesList()
    {
        KeepOnlyHighlightedSelected(ref SelectedUsageListBtn);
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        SelectingTeammate.Activate();
        EventSystem.current.SetSelectedGameObject(PartyList.transform.GetChild(0).gameObject);
        EquippedToolList.Selecting = false;
        if (SelectAllTeammates && Selection == Selections.UseOnCharacter) PartyList.HighlightAll();
        else PartyList.UnhighlightAll();
    }

    public void HoveringFromTeammates()
    {
        if (Selection != Selections.EquipOnCharacter) return;
        SetupEquippedTools();
    }

    private void SetupEquippedTools()
    {
        int index = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index;
        EquippedTools.SetActive(true);
        if (InventoryList == InventorySystem.ListType.Weapons) EquippedToolList.Setup(SelectableTeammatesEquip[index].Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS);
        else if (InventoryList == InventorySystem.ListType.Items) EquippedToolList.Setup(SelectableTeammatesEquip[index].Items, BattleMaster.MAX_NUMBER_OF_ITEMS);
    }

    public void DeselectingTeammates()
    {
        if (Selection != Selections.EquipOnCharacter) return;
        EquippedTools.SetActive(false);
    }

    public void UndoSelectTeammate()
    {
        Selection = Selections.Usage;
        SelectingUsage.SetActive(true);
        SelectingTeammate.Deactivate();
        EquippedTools.SetActive(false);
        SelectedUsageListBtn.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(SelectedUsageListBtn.gameObject);
    }

    public void SelectTeammate()
    {
        if (SelectAllTeammates && Selection == Selections.UseOnCharacter)
        {
            UseItemOnMultiple();
        }
        else
        {
            int index = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index;
            if (Selection == Selections.UseOnCharacter)
            {
                UseItemOnSingle(index);
            }
            else if (Selection == Selections.EquipOnCharacter || Selection == Selections.CharacterEquipsList)
            {
                if (Selection == Selections.CharacterEquipsList) SetupEquippedTools();   // Can reclick another teammate, while selecting equipment from current teammate
                PartyList.SelectedObject = SelectableTeammatesEquip[index];
                KeepOnlyHighlightedSelected(ref PartyList.SelectedButton);
                SetupCharacterEquipsList();
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Use item --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void UseItem()
    {
        Selection = Selections.UsageDone;
        DoneTimer = Time.realtimeSinceStartup + 1.5f;
        EventSystem.current.SetSelectedGameObject(null);
        Item it = ToolList.SelectedObject as Item;
        if (it.Consumable)
        {
            MenuManager.PartyInfo.Inventory.RemoveItem(it);
            ToolList.Setup(MenuManager.PartyInfo.Inventory.Items);
        }
    }
    
    private void UseItemOnSingle(int index)
    {
        UseItem();
        UseItem(index);
    }
    
    private void UseItemOnMultiple()
    {
        UseItem();
        for (int i = 0; i < SelectableTeammatesUse.Count; i++) UseItem(i);
    }

    private void UseItem(int index)
    {
        PartyList.SelectedObject = SelectableTeammatesUse[index];
        PartyList.SelectedObject.ReceiveToolEffects(PartyList.SelectedObject, ToolList.SelectedObject);
        Gauge hpg = PartyList.transform.GetChild(index).GetChild(3).GetComponent<Gauge>();
        Gauge spg = PartyList.transform.GetChild(index).GetChild(4).GetComponent<Gauge>();
        hpg.SetAndAnimate(PartyList.SelectedObject.HP, PartyList.SelectedObject.Stats.MaxHP);
        spg.SetAndAnimate(PartyList.SelectedObject.SP, BattleMaster.SP_CAP);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- On Character Equipment List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupCharacterEquipsList()
    {
        Selection = Selections.CharacterEquipsList;
        EquippedToolList.Selecting = true;
        EquippedTools.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "SELECT A SLOT";
        EventSystem.current.SetSelectedGameObject(EquippedToolList.transform.GetChild(0).gameObject);
    }

    private void UndoSelectEquippedTool()
    {
        Selection = Selections.EquipOnCharacter;
        EquippedToolList.Selecting = false;
        EquippedTools.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (InventoryList == InventorySystem.ListType.Items) ? "EQUIPPED ITEMS" : "EQUIPPED WEAPONS";
        EquippedToolList.InfoFrame.SetActive(false);
        PartyList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
    }

    public void SelectEquippedTool()
    {
        if (!EquippedToolList.Selecting)
        {
            return;
        }
        else if (EquippedToolList.SetupToolInfo())
        {
            if (EquippedToolList.SelectedObject.Id == ToolList.SelectedObject.Id &&
                EquippedToolList.SelectedObject.Name == ToolList.SelectedObject.Name)
                return;     // Selected same item
            KeepOnlyHighlightedSelected(ref EquippedToolList.SelectedButton);
            SetupConfirmSwapButtons();
        }
        else if (Selection == Selections.CharacterEquipsList)
        {
            // Equip from blank slot
            if (InventoryList == InventorySystem.ListType.Items) Equip(false, PartyList.SelectedObject.Items.Count);
            else if (InventoryList == InventorySystem.ListType.Weapons) Equip(false, PartyList.SelectedObject.Weapons.Count);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Confirm swap or equip empty spot --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    private void SetupConfirmSwapButtons()
    {
        Selection = Selections.ConfirmEquip;
        EquippedToolList.Selecting = false;
        ConfirmSwap.SetActive(true);
        string txt = "Swap out\n<b>" + EquippedToolList.SelectedObject.Name + "</b>\nwith\n" + "<b>" + ToolList.SelectedObject.Name + "</b>?";
        ConfirmSwap.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = txt;
        EventSystem.current.SetSelectedGameObject(ConfirmSwap.transform.GetChild(1).gameObject);
    }

    public void UndoConfirmSwap()
    {
        if (Selection == Selections.EquippedDone) return;
        EquippedToolList.Selecting = true;
        Selection = Selections.CharacterEquipsList;
        ConfirmSwap.SetActive(false);
        if (EquippedToolList.SelectedButton)
        {
            EquippedToolList.SelectedButton.ClearHighlights();
            EventSystem.current.SetSelectedGameObject(EquippedToolList.SelectedButton.gameObject);
        }
    }

    public void SwitchEquipment()
    {
        Equip(true, EquippedToolList.SelectedIndex);
    }

    // USE REPLACE INSTEAD
    public void Equip(bool switchOut, int slot)
    {
        if (Selection == Selections.EquippedDone) return;
        Selection = Selections.EquippedDone;
        DoneTimer = Time.realtimeSinceStartup + 1f;
        if (InventoryList == InventorySystem.ListType.Items)
        {
            if (switchOut)
            {
                PartyList.SelectedObject.ReplaceItemWith(ToolList.SelectedObject as Item, slot);
                MenuManager.PartyInfo.Inventory.AddItem(EquippedToolList.SelectedObject as Item);
            }
            else PartyList.SelectedObject.EquipItem(ToolList.SelectedObject as Item);
            MenuManager.PartyInfo.Inventory.RemoveItem(ToolList.SelectedIndex);
            ToolList.Setup(MenuManager.PartyInfo.Inventory.Items);
            EquippedToolList.Setup(PartyList.SelectedObject.Items, BattleMaster.MAX_NUMBER_OF_ITEMS);
        }
        else if (InventoryList == InventorySystem.ListType.Weapons)
        {
            if (switchOut)
            {
                PartyList.SelectedObject.ReplaceWeaponWith(ToolList.SelectedObject as Weapon, slot);
                MenuManager.PartyInfo.Inventory.AddWeapon(EquippedToolList.SelectedObject as Weapon);
            }
            else PartyList.SelectedObject.EquipWeapon(ToolList.SelectedObject as Weapon);
            MenuManager.PartyInfo.Inventory.RemoveWeapon(ToolList.SelectedIndex);
            ToolList.Setup(MenuManager.PartyInfo.Inventory.Weapons);
            EquippedToolList.Setup(PartyList.SelectedObject.Weapons, BattleMaster.MAX_NUMBER_OF_WEAPONS);
        }
        ConfirmSwap.SetActive(false);
    }
}