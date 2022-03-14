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

public class MMBlueprints : MM_Super, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    public enum Selections { None, SelectingBlueprints, ConfirmCraft, Crafting, CraftDone }

    private Selections Selection;
    public ToolListCollectionFrame CollectionFrame;
    public GameObject ConfirmCraft;
    public GameObject MaterialsCheck;
    public MenuFrame RequirementsFrame;
    public Transform RequirementsList;

    public GameObject CraftDoneBlock;
    public GameObject CraftDoneStar;
    public MenuFrame CraftDoneToolInfo;
    public MenuFrame CraftDoneTool;

    private bool BrowseOnly;
    private float DoneTimer;
    private const float CRAFTING_TIME = 3f;
    private bool Resetting;

    private Color MaterialsCheckTextColor;


    protected override void Start()
    {
        base.Start();
        MaterialsCheckTextColor = MaterialsCheck.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color;
    }

    protected override void Update()
    {
        base.Update();
        switch (Selection)
        {
            case Selections.SelectingBlueprints:
                CollectionFrame.SelectTabInputs();
                break;
            case Selections.Crafting:
                if (Time.unscaledTime <= DoneTimer) return;
                CraftDoneSetup();
                break;
            case Selections.CraftDone:
                if (!Resetting && (Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0) || InputMaster.GoingBack())) CraftDoneUndoSetup();
                else if (Resetting && Time.unscaledTime > DoneTimer) ResetFromCraftDone();
                break;
        }
    }

    public override void Open()
    {
        base.Open();
        CollectionFrame.RegisterToolList(0, MenuManager.PartyInfo.CraftableItems);
        CollectionFrame.RegisterToolList(1, MenuManager.PartyInfo.CraftableWeapons);
        CollectionFrame.InitializeSelection();
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
            case Selections.SelectingBlueprints:
                Selection = Selections.None;
                MenuManager.GoToMain();
                break;
            case Selections.ConfirmCraft:
                CollectionFrame.UndoSelectTool();
                break;
            case Selections.CraftDone:
                break;
        }
    }

    protected override void ReturnToInitialSetup()
    {
        Selection = Selections.SelectingBlueprints;
        CollectionFrame.SelectingToolList();
        ConfirmCraft.SetActive(false);
        MaterialsCheck.SetActive(false);
        RequirementsFrame.Deactivate();
        CraftDoneBlock.SetActive(false);
        CraftDoneStar.SetActive(false);
        CraftDoneToolInfo.Deactivate();
        CraftDoneTool.Deactivate();
    }

    public void SelectTabSuccess()
    {
        Selection = Selections.SelectingBlueprints;
    }

    public void SelectTabFailed()
    {
        //
    }

    public void SelectToolForCrafting()
    {
        if (!BrowseOnly && !MaterialsCheck.gameObject.activeSelf) CollectionFrame.SelectTool();
    }

    public void SelectToolSuccess()
    {
        Selection = Selections.ConfirmCraft;
        CollectionFrame.ListBlocker.SetActive(true);
        ConfirmCraft.SetActive(true);
        EventSystem.current.SetSelectedGameObject(ConfirmCraft.transform.GetChild(1).gameObject);
    }

    public void SelectToolFailed()
    {
        CollectionFrame.ListBlocker.SetActive(false);
    }

    public void UndoSelectToolSuccess()
    {
        Selection = Selections.SelectingBlueprints;
        CollectionFrame.ListBlocker.SetActive(false);
        ConfirmCraft.SetActive(false);
    }

    public void ActivateSorterSuccess()
    {
        //
    }

    public void HoverOverTool()
    {
        CollectionFrame.ToolList.HoverOverTool();
        if (CollectionFrame.ToolList.DisplayedToolInfo)
        {
            RequirementsFrame.Activate();
            RefreshRequirements(CollectionFrame.ToolList.SelectedObject.RequiredTools);
        }
        else RequirementsFrame.Deactivate();
    }

    public void RefreshRequirements(List<CraftToolQuantity> craftsList)
    {
        MaterialsCheck.SetActive(false);
        for (int i = 0; i < RequirementsList.childCount; i++)
        {
            Transform t = RequirementsList.GetChild(i);

            bool limitReached = (i >= craftsList.Count);
            t.gameObject.SetActive(!limitReached);
            if (limitReached) continue;
            t.GetChild(0).GetComponent<Image>().sprite = craftsList[i].Tool.GetComponent<SpriteRenderer>().sprite;
            t.GetChild(1).GetComponent<TextMeshProUGUI>().text = craftsList[i].Tool.Name;

            int currentAmount = GetQuantityFromInventory(craftsList, i);
            int requiredAmount = craftsList[i].Quantity;
            t.GetChild(2).GetComponent<TextMeshProUGUI>().text = currentAmount + "/" + requiredAmount;
            t.GetChild(2).GetComponent<TextMeshProUGUI>().color = (currentAmount < requiredAmount) ? MaterialsCheckTextColor : Color.white;
            if (currentAmount < requiredAmount) MaterialsCheck.SetActive(true);
        }
    }

    private int GetQuantityFromInventory(List<CraftToolQuantity> craftsList, int i)
    {
        if (craftsList[i].Tool is Item it) return MenuManager.PartyInfo.Inventory.Items.Find(x => x.Id == it.Id)?.Quantity ?? 0;
        else if (craftsList[i].Tool is Weapon wp) return MenuManager.PartyInfo.Inventory.Weapons.Find(x => x.Id == wp.Id)?.Quantity ?? 0;
        else return 0;
    }

    public void CraftItem()
    {
        Selection = Selections.Crafting;
        DoneTimer = Time.unscaledTime + CRAFTING_TIME;
        ConfirmCraft.SetActive(false);
        CraftDoneBlock.SetActive(true);
        UpdateInventory();
    }

    private void UpdateInventory()
    {
        var tool = CollectionFrame.ToolList.SelectedObject;
        foreach (CraftToolQuantity c in tool.RequiredTools)
        {
            if (c.Tool is Item it0) MenuManager.PartyInfo.Inventory.RemoveItem(it0, c.Quantity);
            else if (c.Tool is Weapon wp0) MenuManager.PartyInfo.Inventory.RemoveWeapon(wp0, c.Quantity);
        }
        if (tool is Item it) MenuManager.PartyInfo.Inventory.AddItem(it);
        else if (tool is Weapon wp) MenuManager.PartyInfo.Inventory.AddWeapon(wp);
    }

    public void CraftDoneSetup()
    {
        Selection = Selections.CraftDone;
        CraftDoneStar.SetActive(true);
        CraftDoneToolInfo.Activate();
        CraftDoneTool.Activate();
        SetupCraftDoneInfo();
    }

    private void SetupCraftDoneInfo()
    {
        InventorySystem.ListType type = (CollectionFrame.ToolList.SelectedObject is Weapon) ? InventorySystem.ListType.Weapons : InventorySystem.ListType.Items;
        InventoryToolSelectionList.CloneTo(CraftDoneToolInfo.gameObject, CollectionFrame.ToolList.InfoFrame.gameObject, type);
        CraftDoneTool.GetComponent<Image>().sprite = CollectionFrame.ToolList.SelectedObject.GetComponent<SpriteRenderer>().sprite;
    }

    private void CraftDoneUndoSetup()
    {
        CraftDoneToolInfo.Deactivate();
        CraftDoneTool.Deactivate();
        DoneTimer = Time.unscaledTime + 0.5f;
        CraftDoneStar.SetActive(false);
        Resetting = true;
    }

    private void ResetFromCraftDone()
    {
        CraftDoneBlock.SetActive(false);
        CollectionFrame.UndoSelectTool();
        MenuMaster.SetupSelectionBufferInMenu();
        Resetting = false;
    }
}