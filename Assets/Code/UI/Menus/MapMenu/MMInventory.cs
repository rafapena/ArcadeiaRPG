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
using System.Linq;

public class MMInventory : MM_Super, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    public enum Selections
    {
        None, InventoryLists,
        Usage, Discard,
        UseOnCharacter, UsageDone,
        EquipOnCharacter, CharacterEquipsList, ConfirmEquip, EquippedDone
    }

    // Selection lists
    public ToolListCollectionFrame InventoryFrame;
    public InventoryToolSelectionList EquippedToolList;
    public PlayerSelectionList PartyList;

    // Child GameObjects
    public GameObject SelectingUsage;
    public GameObject ConfirmDiscard;
    public MenuFrame SelectingTeammate;
    public GameObject EquippedTools;
    public GameObject ConfirmSwap;

    // General selection tracking
    private Selections Selection;
    public TextMeshProUGUI EquippedToolsLabel;
    public TextMeshProUGUI DiscardLabel;
    public TextMeshProUGUI SelectTeammateLabel;

    // Keep track of selected content
    private bool SelectingEquipment => InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons || InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Accessories;
    private ListSelectable SelectedUsageListBtn;
    public GameObject[] DiscardButtons;

    // List of potential targets
    private List<Battler> SelectableTeammatesUse;
    private List<Battler> SelectableTeammatesEquip;
    private bool SelectAllTeammates;

    // Delay after an item/weapon has been used/equipped
    private float DoneTimer;

    protected override void Update()
    {
        if (!MainComponent.Activated) return;
        base.Update();
        switch (Selection)
        {
            case Selections.InventoryLists:
                InventoryFrame.SelectTabInputs();
                break;
            case Selections.UsageDone:
            case Selections.EquippedDone:
                if (Time.realtimeSinceStartup <= DoneTimer) return;
                EventSystem.current.SetSelectedGameObject(InventoryFrame.ToolList.SelectedButton.gameObject);
                ReturnToInitialSetup();
                Selection = Selections.InventoryLists;
                break;
        }
    }

    public override void Open()
    {
        base.Open();
        InventoryFrame.SetToolListOnTab(0, GetInventoryItems(false));
        InventoryFrame.SetToolListOnTab(1, MenuManager.PartyInfo.Inventory.Weapons);
        InventoryFrame.SetToolListOnTab(2, MenuManager.PartyInfo.Inventory.Accessories);
        InventoryFrame.SetToolListOnTab(3, GetInventoryItems(true));
        InventoryFrame.TrackCarryWeight(MenuManager.PartyInfo.Inventory);
        InventoryFrame.InitializeSelection();
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
            case Selections.InventoryLists:
                if (InventoryFrame.ActivatedSorter)
                {
                    InventoryFrame.DeactivateSorter();
                    break;
                }
                Selection = Selections.None;
                MenuManager.GoToMain();
                break;
            case Selections.Usage:
                InventoryFrame.UndoSelectTool();
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

    protected override void ReturnToInitialSetup()
    {
        InventoryFrame.SelectingToolList();
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        SelectingTeammate.Deactivate();
        EquippedTools.SetActive(false);
        ConfirmSwap.SetActive(false);
        PartyList.ClearSelections();
        EquippedToolList.ClearSelections();
        if (SelectedUsageListBtn)
            SelectedUsageListBtn.ClearHighlights();
        SelectableTeammatesUse = null;
        SelectableTeammatesEquip = null;
    }

    private List<Item> GetInventoryItems(bool isKey)
    {
        return MenuManager.PartyInfo.Inventory.Items.FindAll(x => x.IsKey == isKey);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Inventory Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectTabSuccess()
    {
        Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
    }

    public void SelectTabFailed()
    {
        //
    }

    public void SelectToolForInventory()
    {
        if (!(InventoryFrame.ToolList.SelectedObject as Item)?.IsKey ?? true) InventoryFrame.SelectTool();
    }

    public void SelectToolSuccess()
    {
        Selection = Selections.Usage;
        SelectingUsage.SetActive(true);
        ConfirmDiscard.SetActive(false);
        for (int i = 0; i < DiscardButtons.Length; i++)
        {
            if (InventoryFrame.ToolList.SelectedObject.CanRemove) MenuMaster.EnableSelection(ref DiscardButtons[i]);
            else MenuMaster.DisableSelection(ref DiscardButtons[i]);
        }
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
        SetupUsageButtons();
    }

    public void SelectToolFailed()
    {
        ConfirmDiscard.SetActive(false);
        SelectingUsage.SetActive(false);
    }

    public void UndoSelectToolSuccess()
    {
        Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
        foreach (Transform t0 in SelectingUsage.transform)
            foreach (Transform t in t0)
                t.GetComponent<ListSelectable>().ClearHighlights();
    }

    public void ActivateSorterSuccess()
    {
        Selection = Selections.InventoryLists;
        SelectingUsage.SetActive(false);
        ConfirmDiscard.SetActive(false);
        if (SelectedUsageListBtn) SelectedUsageListBtn.ClearHighlights();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- How will ActiveTool be used + Setup background data for character list --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupUsageButtons()
    {
        SelectableTeammatesUse = new List<Battler>();
        SelectableTeammatesEquip = new List<Battler>();
        SelectAllTeammates = false;
        Transform CurrentUsageList;
        switch (InventoryFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Items:
                CurrentUsageList = SelectingUsage.transform.GetChild(0);
                CurrentUsageList.GetChild(0).GetComponent<Button>().interactable = GetSelectableTeammatesForUsingItems();
                break;
            case InventorySystem.ListType.Weapons:
                CurrentUsageList = SelectingUsage.transform.GetChild(1);
                CurrentUsageList.GetChild(0).GetComponent<Button>().interactable = GetSelectableTeammatesForEquippingWeapons();
                break;
            case InventorySystem.ListType.Accessories:
                CurrentUsageList = SelectingUsage.transform.GetChild(1);
                CurrentUsageList.GetChild(0).GetComponent<Button>().interactable = GetSelectableTeammatesForEquippingAccessories();
                break;
            default:
                return;
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
        Item item = InventoryFrame.ToolList.SelectedObject as Item;
        switch (item.Scope)
        {
            case ActiveTool.ScopeType.OneTeammate:
            case ActiveTool.ScopeType.Self:
                foreach (Battler p in party)
                    if (!p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = false;
                break;

            case ActiveTool.ScopeType.OneKnockedOutTeammate:
                foreach (Battler p in party)
                    if (p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = false;
                break;

            case ActiveTool.ScopeType.AllTeammates:
            case ActiveTool.ScopeType.EveryoneButSelf:
            case ActiveTool.ScopeType.Everyone:
                foreach (Battler p in party)
                    if (!p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = true;
                break;

            case ActiveTool.ScopeType.AllKnockedOutTeammates:
                foreach (Battler p in party)
                    if (p.Unconscious) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = true;
                break;
        }
        return SelectableTeammatesUse.Count > 0;
    }

    private bool GetSelectableTeammatesForEquippingWeapons()
    {
        Weapon selectedWeapon = InventoryFrame.ToolList.SelectedObject as Weapon;
        foreach (BattlePlayer p in MenuManager.PartyInfo.AllPlayers)
        {
            bool matchClassExclusive = selectedWeapon.ClassExclusives.Count == 0 || selectedWeapon.ClassExclusives.Contains(p.Class);
            bool isUsableWeaponType = p.Class.UsableWeapon1Type == selectedWeapon.WeaponType || p.Class.UsableWeapon2Type == selectedWeapon.WeaponType;
            if (matchClassExclusive && isUsableWeaponType) SelectableTeammatesEquip.Add(p);
        }
        return SelectableTeammatesEquip.Count > 0;
    }

    private bool GetSelectableTeammatesForEquippingAccessories()
    {
        Accessory ac = InventoryFrame.ToolList.SelectedObject as Accessory;
        foreach (BattlePlayer p in MenuManager.PartyInfo.AllPlayers)
        {
            bool matchClassExclusive = ac.ClassExclusives.Count == 0 || ac.ClassExclusives.Contains(p.Class);
            if (matchClassExclusive) SelectableTeammatesEquip.Add(p);
        }
        return SelectableTeammatesEquip.Count > 0;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Discarding Inventory ActiveTool --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupDiscard()
    {
        Selection = Selections.Discard;
        SelectingUsage.SetActive(false);
        InventoryFrame.ListBlocker.SetActive(true);
        ConfirmDiscard.SetActive(true);
        DiscardLabel.text = "Discard\n" + InventoryFrame.ToolList.SelectedObject.Info.Name + "?";
        EventSystem.current.SetSelectedGameObject(ConfirmDiscard.transform.GetChild(1).gameObject);
    }

    public void UndoDiscard()
    {
        Selection = Selections.Usage;
        InventoryFrame.ListBlocker.SetActive(false);
        ConfirmDiscard.SetActive(false);
        InventoryFrame.UndoSelectTool();
    }

    public void DiscardTool()
    {
        switch (InventoryFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Items:
                MenuManager.PartyInfo.Inventory.Remove<Item>(InventoryFrame.ToolList.SelectedIndex);
                InventoryFrame.Refresh(GetInventoryItems(false));
                break;
            case InventorySystem.ListType.Weapons:
                MenuManager.PartyInfo.Inventory.Remove<Weapon>(InventoryFrame.ToolList.SelectedIndex);
                InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Weapons);
                break;
            case InventorySystem.ListType.Accessories:
                MenuManager.PartyInfo.Inventory.Remove<Accessory>(InventoryFrame.ToolList.SelectedIndex);
                InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Accessories);
                break;
        }
        UndoDiscard();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Select Teammate --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupUseOnCharacter()
    {
        Selection = Selections.UseOnCharacter;
        string name = InventoryFrame.ToolList.SelectedObject.Info.Name.ToUpper();
        SelectTeammateLabel.text = SelectAllTeammates ? ("USE " + name + " ON EVERYONE?") : ("USE " + name + " ON...");
        PartyList.Refresh(SelectableTeammatesUse);
        SetupTeammatesList();
    }

    public void SetupEquipOnCharacter()
    {
        Selection = Selections.EquipOnCharacter;
        SelectTeammateLabel.text = "EQUIP " + InventoryFrame.ToolList.SelectedObject.Info.Name.ToUpper() + " ON...";
        EquippedToolsLabel.text = "EQUIPMENT";
        PartyList.Refresh(SelectableTeammatesEquip);
        SetupTeammatesList();
    }

    private void SetupTeammatesList()
    {
        MenuMaster.KeepHighlightedSelected(ref SelectedUsageListBtn);
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
        EquippedToolList.Refresh(SelectableTeammatesEquip[index].Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS);
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
                MenuMaster.KeepHighlightedSelected(ref PartyList.SelectedButton);
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
        Item it = InventoryFrame.ToolList.SelectedObject as Item;
        if (it.Consumable)
        {
            MenuManager.PartyInfo.Inventory.Remove(it);
            InventoryFrame.Refresh(GetInventoryItems(false));
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
        PartyList.SelectedObject.ReceiveToolEffects(PartyList.SelectedObject, InventoryFrame.ToolList.SelectedObject as Item);
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
        EquippedToolsLabel.text = "SELECT A SLOT";
        EventSystem.current.SetSelectedGameObject(EquippedToolList.transform.GetChild(0).gameObject);
    }

    private void UndoSelectEquippedTool()
    {
        Selection = Selections.EquipOnCharacter;
        EquippedToolList.Selecting = false;
        EquippedToolsLabel.text = "EQUIPMENT";
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
        else if (EquippedToolList.RefreshToolInfo())
        {
            IToolForInventory sInv = InventoryFrame.ToolList.SelectedObject;
            IToolForInventory sEqp = EquippedToolList.SelectedObject;
            if (sInv.Info.Id == sEqp.Info.Id && sInv.Info.Name == sEqp.Info.Name) return;     // Selected same item
            MenuMaster.KeepHighlightedSelected(ref EquippedToolList.SelectedButton);
            SetupConfirmSwapButtons();
        }
        else if (Selection == Selections.CharacterEquipsList)   // Equip from blank slot
        {
            if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Weapons) Equip(false, PartyList.SelectedObject.Weapons.Count);
            else if (InventoryFrame.CurrentInventoryList == InventorySystem.ListType.Accessories) Equip(false, PartyList.SelectedObject.Accessories.Count);
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
        string txt = "Swap out\n<b>" + EquippedToolList.SelectedObject.Info.Name + "</b>\nwith\n" + "<b>" + InventoryFrame.ToolList.SelectedObject.Info.Name + "</b>?";
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

    // FIX SWITCHING
    public void Equip(bool switchOut, int slot)
    {
        if (Selection == Selections.EquippedDone) return;
        Selection = Selections.EquippedDone;
        DoneTimer = Time.realtimeSinceStartup + 1f;

        IToolEquippable tool = InventoryFrame.ToolList.SelectedObject as IToolEquippable;
        if (switchOut)
        {
            //PartyList.SelectedObject.ReplaceEquipWith(tool, slot);
            MenuManager.PartyInfo.Inventory.Add(EquippedToolList.SelectedObject);
        }
        else PartyList.SelectedObject.Equip(tool);

        switch (InventoryFrame.CurrentInventoryList)
        {
            case InventorySystem.ListType.Weapons:
                MenuManager.PartyInfo.Inventory.Remove<Weapon>(InventoryFrame.ToolList.SelectedIndex);
                InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Weapons);
                break;
            case InventorySystem.ListType.Accessories:
                MenuManager.PartyInfo.Inventory.Remove<Accessory>(InventoryFrame.ToolList.SelectedIndex);
                InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Accessories);
                break;
        }
        EquippedToolList.Refresh(PartyList.SelectedObject.Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS);
        ConfirmSwap.SetActive(false);
    }
}