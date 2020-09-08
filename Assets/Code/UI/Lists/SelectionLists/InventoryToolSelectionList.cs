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
/// Generates the structure of the inventiry GUI, with the following assumptions
/// - Content gameobject is inside a ScrollRect's viewport, which carries the entries
/// - Each entry has a ListSelectable object attached to it
/// - Each entry has three children: a blank box, item sprite, and number text
/// - The number of existing entries must be at least the current number of columns
/// - The navigation must be set on the first row's existing boxes
/// </summary>
public class InventoryToolSelectionList : SelectionList_Super<ToolForInventory>
{
    // Structure
    protected int NumberOfColumns;
    protected int NumberOfVisibleRows;
    protected Color CraftedBackgroundColor = new Color(0.7f, 0.9f, 1f, 0.7f);

    protected override void Awake()
    {
        NumberOfColumns = (int)(Container.GetComponent<RectTransform>().rect.width / GetComponent<GridLayoutGroup>().cellSize.x);
        NumberOfVisibleRows = (int)(Container.GetComponent<RectTransform>().rect.height / GetComponent<GridLayoutGroup>().cellSize.y);
        base.Awake();
    }

    public void Setup<T>(List<T> listData, int hardLimit = -1, bool customNavigation = false) where T : ToolForInventory
    {
        // Get number of scrollable rows
        int totalNumberOfRows = NumberOfVisibleRows;
        int numberOfBlankSquares = NumberOfVisibleRows * NumberOfColumns;
        while (numberOfBlankSquares < listData.Count)
        {
            numberOfBlankSquares += NumberOfColumns;
            totalNumberOfRows++;
        }

        // Add the data
        ReferenceData = new List<ToolForInventory>();
        int i = 0;
        for (int r = 0; r < totalNumberOfRows; r++)
        {
            for (int c = 0; c < NumberOfColumns; c++)
            {
                GameObject entry;
                if (i < transform.childCount)
                {
                    // Set box entry to visible as it already exists in the table
                    entry = transform.GetChild(i).gameObject;
                    entry.SetActive(true);
                }
                else
                {
                    // Allocate new space for new box entries the table will be adding
                    entry = Instantiate(transform.GetChild(c).gameObject, transform);
                    if (!customNavigation) SetNavigation(entry, i);
                }

                // Initialize button settings
                ListSelectable btn = entry.GetComponent<ListSelectable>();
                btn.Index = i;
                btn.ClearHighlights();
                if (btn.OnHoverInput != null) btn.OnHoverInput.AddListener(HoverOverTool);

                // Set the icon in the box entry
                Image blankImage = entry.transform.GetChild(0).GetComponent<Image>();
                bool inDataList = i < listData.Count;
                blankImage.color = (inDataList && listData[i].IsCraftable()) ? CraftedBackgroundColor : NormalBackgroundColor;
                if (inDataList) AddToList(entry.transform, listData[i]);
                else SetToBlank(entry);
                i++;
            }
        }

        // If there are excess blank squares, make them invisible
        if (hardLimit > 0) i = hardLimit;
        for (; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        // Highlight latest button if the entry is on in the table
        if (!transform.GetChild(SelectedIndex).gameObject.activeSelf)
            SetSelected(listData.Count - 1);
    }

    private void AddToList<T>(Transform entry, T dataEntry) where T : ToolForInventory
    {
        ReferenceData.Add(dataEntry);
        entry.transform.GetChild(1).gameObject.SetActive(true);
        entry.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = dataEntry.GetComponent<SpriteRenderer>().sprite;
        if (entry.transform.childCount > 2)
        {
            entry.transform.GetChild(2).gameObject.SetActive(true);
            entry.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>().text = dataEntry.Quantity.ToString();
        }
    }

    protected void SetNavigation(GameObject entry, int i)
    {
        entry.GetComponent<Button>().navigation = new Navigation() { mode = Navigation.Mode.Explicit };
        SetHorizontalPointer(transform.GetChild(i - 1), entry.transform);
        SetVerticalPointer(transform.GetChild(i - NumberOfColumns), entry.transform);
    }

    private void SetToBlank(GameObject entry)
    {
        entry.transform.GetChild(1).gameObject.SetActive(false);
        if (entry.transform.childCount > 2)
            entry.transform.GetChild(2).gameObject.SetActive(false);
    }

    public void FilterUnneededBlanks(InventorySystem inventory)
    {
        if (ReferenceData == null) return;
        int limit = ReferenceData.Count + (inventory.ToolCapacity - inventory.NumberOfTools);
        for (int i = limit; i < transform.childCount; i++)
        {
            GameObject g = transform.GetChild(i).gameObject;
            if (g.activeSelf) g.SetActive(false);
            else return;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Browsing Through List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool SetupToolInfo()
    {
        SetupToolInfoAUX();
        return InfoIsSetup;
    }

    private void HoverOverTool()
    {
        if (Selecting) SetupToolInfoAUX();
    }

    private void SetupToolInfoAUX()
    {
        ListSelectable btn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        int tmpIdx = btn.Index;
        
        bool isBlankSquare = (tmpIdx >= ReferenceData.Count);
        InfoFrame.SetActive(!isBlankSquare);
        InfoIsSetup = !isBlankSquare;
        if (isBlankSquare) return;

        if (SelectedButton) SelectedButton.ClearHighlights();
        SelectedButton = btn;
        SelectedIndex = tmpIdx;
        SelectedObject = ReferenceData[SelectedIndex];

        InfoFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedObject.Name.ToUpper();
        InfoFrame.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = SelectedObject.Description;

        bool isWeapon = (SelectedObject.GetType().Name == "Weapon");
        InfoFrame.transform.GetChild(2).gameObject.SetActive(isWeapon);
        InfoFrame.transform.GetChild(3).gameObject.SetActive(isWeapon);
        InfoFrame.transform.GetChild(4).gameObject.SetActive(isWeapon);
        InfoFrame.transform.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedObject.Power.ToString();
        InfoFrame.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedObject.ConsecutiveActs.ToString();
        InfoFrame.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = "+" + SelectedObject.CritcalRate + "%";
    }
}