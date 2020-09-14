using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System;
using System.Linq;

/// <summary>
///
/// </summary>
public class InventoryToolListSorter : MonoBehaviour
{
    public ListSelectable SortButton;
    private InventoryToolSelectionList TargetInventoryGUI;
    private InventorySystem TargetInventory;
    private InventorySystem.ListType InventoryList;
    private List<ToolForInventory> ReferenceData;

    [HideInInspector] public delegate void ExtraFunction();
    private ExtraFunction ExtraFunc;

    private void Awake()
    {
        ClearSelections();
    }

    public void ClearSelections()
    {
        gameObject.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Setup(InventoryToolSelectionList tig, InventorySystem tinv, InventorySystem.ListType inventoryList)
    {
        TargetInventoryGUI = tig;
        TargetInventory = tinv;
        InventoryList = inventoryList;
        gameObject.SetActive(true);
        SortButton.KeepSelected();
        GameObject firstButton = transform.GetChild(1).GetChild(0).gameObject;
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void Undo()
    {
        if (!gameObject.activeSelf) return;
        gameObject.SetActive(false);
        SortButton.ClearHighlights();
        GameObject firstButton = TargetInventoryGUI.transform.GetChild(0).gameObject;
        EventSystem.current.SetSelectedGameObject(firstButton);
    }

    public void SortByDefaultAscending()
    {
        GetList();
        TargetInventoryGUI.Setup(ReferenceData.OrderBy(t => t.Id).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByDefaultDescending()
    {
        GetList();
        TargetInventoryGUI.Setup(ReferenceData.OrderByDescending(t => t.Id).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByQuantityAscending()
    {
        GetList();
        TargetInventoryGUI.Setup(ReferenceData.OrderBy(t => t.Quantity).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByQuantityDescending()
    {
        GetList();
        TargetInventoryGUI.Setup(ReferenceData.OrderByDescending(t => t.Quantity).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByWeightAscending()
    {
        GetList();
        TargetInventoryGUI.Setup(ReferenceData.OrderBy(t => t.Weight).ToList());
        ExtraFunc?.Invoke();
    }

    public void SortByWeightDescending()
    {
        GetList();
        TargetInventoryGUI.Setup(ReferenceData.OrderByDescending(t => t.Weight).ToList());
        ExtraFunc?.Invoke();
    }

    private void GetList()
    {
        ReferenceData = new List<ToolForInventory>();
        switch (InventoryList)
        {
            case InventorySystem.ListType.Items:
                foreach (Item i in TargetInventory.Items) ReferenceData.Add(i);
                break;
            case InventorySystem.ListType.Weapons:
                foreach (Weapon w in TargetInventory.Weapons) ReferenceData.Add(w);
                break;
            case InventorySystem.ListType.KeyItems:
                foreach (Item i in TargetInventory.KeyItems) ReferenceData.Add(i);
                break;
        }
    }
}