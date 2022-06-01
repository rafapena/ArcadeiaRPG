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
    public GameObject[] UseButtons;
    public GameObject[] EquipButtons;
    public GameObject[] DiscardButtons;

    // List of potential targets
    private List<Battler> SelectableTeammatesUse;
    private List<Battler> SelectableTeammatesEquip;
    private bool SelectAllTeammates;
    private BattlePlayer SelectedPlayerForEquipping;

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
        RefreshInventoryTabs();
        InventoryFrame.TrackCarryWeight(MenuManager.PartyInfo.Inventory);
        InventoryFrame.InitializeSelection();
    }

    private void RefreshInventoryTabs()
    {
        InventoryFrame.SetToolListOnTab(0, GetInventoryItems(false));
        InventoryFrame.SetToolListOnTab(1, MenuManager.PartyInfo.Inventory.Weapons);
        InventoryFrame.SetToolListOnTab(2, MenuManager.PartyInfo.Inventory.Accessories);
        InventoryFrame.SetToolListOnTab(3, GetInventoryItems(true));
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
        EnableDisableUsageButtons(InventoryFrame.ToolList.SelectedObject.CanRemove, ref DiscardButtons);
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

    private void EnableDisableUsageButtons(bool condition, ref GameObject[] identicalUsageButtonsList)
    {
        for (int i = 0; i < identicalUsageButtonsList.Length; i++)
        {
            if (condition) MenuMaster.EnableSelection(ref identicalUsageButtonsList[i]);
            else MenuMaster.DisableSelection(ref identicalUsageButtonsList[i]);
        }
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
                EnableDisableUsageButtons(GetSelectableTeammatesForUsingItems(), ref UseButtons);
                break;
            case InventorySystem.ListType.Weapons:
                CurrentUsageList = SelectingUsage.transform.GetChild(1);
                EnableDisableUsageButtons(GetSelectableTeammatesForEquippingWeapons(), ref EquipButtons);
                break;
            case InventorySystem.ListType.Accessories:
                CurrentUsageList = SelectingUsage.transform.GetChild(1);
                EnableDisableUsageButtons(GetSelectableTeammatesForEquippingAccessories(), ref EquipButtons);
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

        // If everything in the usage list has been disabled
        SelectingUsage.gameObject.SetActive(false);
        InventoryFrame.UndoSelectTool();
    }

    private bool GetSelectableTeammatesForUsingItems()
    {
        List<Battler> party = MenuManager.PartyInfo.WholeParty.ToList();
        Item item = InventoryFrame.ToolList.SelectedObject as Item;
        switch (item.Scope)
        {
            case ActiveTool.ScopeType.OneAlly:
            case ActiveTool.ScopeType.Self:
                foreach (Battler p in party)
                    if (!p.KOd) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = false;
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                foreach (Battler p in party)
                    if (p.KOd) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = false;
                break;

            case ActiveTool.ScopeType.AllAllies:
            case ActiveTool.ScopeType.EveryoneButSelf:
            case ActiveTool.ScopeType.Everyone:
                foreach (Battler p in party)
                    if (!p.KOd) SelectableTeammatesUse.Add(p);
                SelectAllTeammates = true;
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                foreach (Battler p in party)
                    if (p.KOd) SelectableTeammatesUse.Add(p);
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
        EventSystem.current.SetSelectedGameObject(ConfirmDiscard.transform.GetChild(2).gameObject);
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
                MenuManager.PartyInfo.Inventory.Remove(InventoryFrame.ToolList.SelectedObject);
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
        EquippedToolList.Refresh((SelectableTeammatesEquip[index] as BattlePlayer).Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS);
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
                SelectedPlayerForEquipping = PartyList.SelectedObject as BattlePlayer;
                MenuMaster.KeepHighlightedSelected(ref PartyList.SelectedButton);
                SetupCharacterEquipsList();
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Use item --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    private void UseItemOnSingle(int index)
    {
        PreUseItem();
        UseItem(index);
        PostUseItem();
    }
    
    private void UseItemOnMultiple()
    {
        PreUseItem();
        for (int i = 0; i < SelectableTeammatesUse.Count; i++) UseItem(i);
        PostUseItem();
    }

    private void PreUseItem()
    {
        Selection = Selections.UsageDone;
        DoneTimer = Time.realtimeSinceStartup + 1.5f;
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void UseItem(int index)
    {
        PartyList.SelectedObject = SelectableTeammatesUse[index];
        PartyList.SelectedObject.ReceiveToolEffects(PartyList.SelectedObject, InventoryFrame.ToolList.SelectedObject as Item, null);
        PartyList.UpdateEntry(PartyList.SelectedObject, index);
    }

    private void PostUseItem()
    {
        MenuManager.PartyInfo.Inventory.ApplyPostItemUseEffects(InventoryFrame.ToolList.SelectedObject as Item);
        InventoryFrame.Refresh(GetInventoryItems(false));
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
        else if (Selection == Selections.CharacterEquipsList) Equip(false);   // Equip from blank slot
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
        Equip(true);
    }

    public void Equip(bool switchOut)
    {
        if (Selection == Selections.EquippedDone) return;
        Selection = Selections.EquippedDone;
        DoneTimer = Time.realtimeSinceStartup + 1f;

        IToolEquippable tool = InventoryFrame.ToolList.SelectedObject as IToolEquippable;
        if (switchOut)
        {
            SelectedPlayerForEquipping.Unequip(EquippedToolList.SelectedObject as IToolEquippable);
            MenuManager.PartyInfo.Inventory.Add(EquippedToolList.SelectedObject);
        }
        SelectedPlayerForEquipping.Equip(tool);
        MenuManager.PartyInfo.Inventory.Remove(tool);

        RefreshInventoryTabs();
        if (tool is Weapon) InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Weapons);
        else InventoryFrame.Refresh(MenuManager.PartyInfo.Inventory.Accessories);
        EquippedToolList.Refresh(SelectedPlayerForEquipping.Equipment, BattleMaster.MAX_NUMBER_OF_EQUIPS);
        ConfirmSwap.SetActive(false);
    }
}