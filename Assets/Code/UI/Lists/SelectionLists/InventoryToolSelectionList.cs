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
public class InventoryToolSelectionList : SelectionList_Super<IToolForInventory>
{
    public enum AdditionalDisplayAttributes { None, Quanitity, Price }

    // Data tracker
    [HideInInspector] public InventorySystem ReferenceInventory;

    // Hovering/Navigation
    public bool DisplayedToolInfo { get; private set; }
    private Selectable NavToLeft;
    private Selectable NavToRight;

    // Structure
    protected int NumberOfBlankSquares;
    protected int NumberOfColumns;
    protected int NumberOfVisibleRows;

    // List enable/disable conditions
    private const float DISABLED_TOOL_OPACITY = 0.2f;
    public delegate bool EnableCondition(IToolForInventory data);

    // Other settings
    private const string COST_PREFIX = "<sprite=\"MenuIcons\" index=10> ";
    public AdditionalDisplayAttributes AdditionalAttribute = AdditionalDisplayAttributes.Quanitity;

    protected override void Awake()
    {
        NumberOfColumns = (int)(Container.GetComponent<RectTransform>().rect.width / GetComponent<GridLayoutGroup>().cellSize.x);
        NumberOfVisibleRows = (int)(Container.GetComponent<RectTransform>().rect.height / GetComponent<GridLayoutGroup>().cellSize.y);
        base.Awake();
    }

    public void Refresh<T>(List<T> listData, int hardLimit = -1, bool customNavigation = false) where T : IToolForInventory
    {
        Refresh(listData, null, hardLimit, customNavigation);
    }

    public void Refresh<T>(List<T> listData, EnableCondition enableCondition, int hardLimit = -1, bool customNavigation = false) where T : IToolForInventory
    {
        // Get number of scrollable rows
        int totalNumberOfRows = NumberOfVisibleRows;
        NumberOfBlankSquares = NumberOfVisibleRows * NumberOfColumns;
        while (NumberOfBlankSquares < listData.Count)
        {
            NumberOfBlankSquares += NumberOfColumns;
            NumberOfBlankSquares += NumberOfColumns;
            totalNumberOfRows++;
        }

        // Set navigation buttons
        NavToLeft = transform.GetChild(0).GetComponent<Button>().navigation.selectOnLeft;
        NavToRight = transform.GetChild(NumberOfColumns - 1).GetComponent<Button>().navigation.selectOnRight;

        // Add the data
        ReferenceData.Clear();
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
                    if (!customNavigation) SetNavigation(entry, i, c);
                }

                // Initialize button settings
                ListSelectable btn = entry.GetComponent<ListSelectable>();
                btn.Index = i;
                btn.ClearHighlights();
                if (btn.OnHoverInput != null) btn.OnHoverInput = transform.GetChild(0).GetComponent<ListSelectable>().OnHoverInput;

                // Set the icon in the box entry
                Image blankImage = entry.transform.GetChild(0).GetComponent<Image>();
                bool inDataList = i < listData.Count;
                if (inDataList)
                {
                    AddToList(entry.transform, listData[i]);
                    EnableOrDisableEntry(entry, enableCondition, listData[i]);
                }
                else SetToBlank(entry);
                i++;
            }

            // Reset right navigation to point to the top
            if (NavToRight) SetHorizontalPointer(transform.GetChild(NumberOfColumns - 1), NavToRight.transform);
        }

        // If there are excess blank squares, make them invisible
        if (hardLimit > 0) i = hardLimit;
        for (; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        // Highlight latest button if the entry is on in the table
        if (!transform.GetChild(SelectedIndex).gameObject.activeSelf)
            SetSelected(listData.Count - 1);
    }

    private void AddToList<T>(Transform entry, T dataEntry) where T : IToolForInventory
    {
        ReferenceData.Add(dataEntry);
        entry.transform.GetChild(1).gameObject.SetActive(true);
        entry.transform.GetChild(1).gameObject.GetComponent<Image>().sprite = dataEntry.Info.GetComponent<SpriteRenderer>().sprite;
        if (entry.transform.childCount > 2)
        {
            GameObject go = entry.transform.GetChild(2).gameObject;
            go.SetActive(true);
            switch (AdditionalAttribute)
            {
                case AdditionalDisplayAttributes.None:
                    go.SetActive(false);
                    break;
                case AdditionalDisplayAttributes.Quanitity:
                    int quantity = dataEntry.Quantity;
                    go.GetComponent<TextMeshProUGUI>().text = quantity == 1 ? "" : quantity.ToString();
                    break;
                case AdditionalDisplayAttributes.Price:
                    go.GetComponent<TextMeshProUGUI>().text = COST_PREFIX + dataEntry.Price;
                    break;
            }
        }
    }

    private void SetNavigation(GameObject entry, int i, int columnIndex)
    {
        entry.GetComponent<Button>().navigation = new Navigation() { mode = Navigation.Mode.Explicit };
        Transform aboveEntry = transform.GetChild(i - NumberOfColumns);
        if (columnIndex == 0)
        {
            if (NavToLeft) SetHorizontalPointer(NavToLeft.transform, entry.transform);
        }
        else
        {
            if (NavToRight && columnIndex == NumberOfColumns - 1) SetHorizontalPointer(entry.transform, NavToRight.transform);
            SetHorizontalPointer(transform.GetChild(i - 1), entry.transform);
        }
        SetVerticalPointer(aboveEntry, entry.transform);
    }

    private void EnableOrDisableEntry(GameObject entry, EnableCondition enableCondition, IToolForInventory data)
    {
        Color tempColor = entry.transform.GetChild(1).GetComponent<Image>().color;
        tempColor.a = (enableCondition?.Invoke(data) ?? true) ? 1.0f : DISABLED_TOOL_OPACITY;
        entry.transform.GetChild(1).GetComponent<Image>().color = tempColor;
    }

    private void SetToBlank(GameObject entry)
    {
        entry.transform.GetChild(1).gameObject.SetActive(false);
        if (entry.transform.childCount > 2)
            entry.transform.GetChild(2).gameObject.SetActive(false);
    }

    public void RefreshEnabling(EnableCondition enableCondition)
    {
        int i = 0;
        foreach (Transform t in transform)
        {
            if (i >= ReferenceData.Count) return;
            EnableOrDisableEntry(t.gameObject, enableCondition, ReferenceData[i++]);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Browsing Through List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool RefreshToolInfo()
    {
        return RefreshToolInfoAUX();
    }

    public void HoverOverTool()
    {
        if (Selecting) RefreshToolInfoAUX();
    }

    private bool RefreshToolInfoAUX()
    {
        ListSelectable btn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        int tmpIdx = btn.Index;
        
        bool isBlankSquare = (tmpIdx >= ReferenceData.Count);
        InfoFrame.SetActive(!isBlankSquare);
        DisplayedToolInfo = !isBlankSquare;
        if (isBlankSquare) return false;

        if (SelectedButton) SelectedButton.ClearHighlights();
        SelectedButton = btn;
        SelectedIndex = tmpIdx;
        SelectedObject = ReferenceData[SelectedIndex];

        InfoFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedObject.Info.Name.ToUpper();
        InfoFrame.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = SelectedObject.Info.Description;
        SetElementImage(InfoFrame, 2, SelectedObject);

        bool isWeapon = SelectedObject is Weapon;
        InfoFrame.transform.GetChild(3).gameObject.SetActive(isWeapon);
        InfoFrame.transform.GetChild(4).gameObject.SetActive(isWeapon);
        InfoFrame.transform.GetChild(5).gameObject.SetActive(isWeapon);
        if (SelectedObject is Weapon wp)
        {
            InfoFrame.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = wp.Power.ToString();
            InfoFrame.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = wp.Range + "%";
            InfoFrame.transform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = "+" + wp.CriticalRate + "%";
        }

        InfoFrame.transform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedObject.Weight.ToString();
        return true;
    }

    public static void SetElementImage<T>(GameObject infoFrame, int index, T tool)
    {
        if (tool is ActiveTool at)
        {
            bool hasElement = UIMaster.ElementImages.ContainsKey(at.Element);
            if (hasElement) infoFrame.transform.GetChild(index).GetComponent<Image>().sprite = UIMaster.ElementImages[at.Element];
            infoFrame.transform.GetChild(index).gameObject.SetActive(hasElement);
        }
        else infoFrame.transform.GetChild(index).gameObject.SetActive(false);
    }

    public void UpdateNavRight(Transform newNavToRight)
    {
        NavToRight = newNavToRight.GetComponent<Button>();
        for (int i = NumberOfColumns - 1; i < NumberOfBlankSquares; i += NumberOfColumns)
            SetHorizontalPointer(transform.GetChild(i).transform, NavToRight.transform);
        SetHorizontalPointer(transform.GetChild(NumberOfColumns - 1), NavToRight.transform);
    }

    public static void CloneTo(GameObject dest, GameObject src, InventorySystem.ListType listType)
    {
        dest.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = src.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
        dest.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = src.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text;
        dest.transform.GetChild(2).GetComponent<Image>().sprite = src.transform.GetChild(2).GetComponent<Image>().sprite;
        bool isWeapon = (listType == InventorySystem.ListType.Weapons);
        dest.transform.GetChild(3).gameObject.SetActive(isWeapon);
        dest.transform.GetChild(4).gameObject.SetActive(isWeapon);
        dest.transform.GetChild(5).gameObject.SetActive(isWeapon);
        dest.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = src.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text;
        dest.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = src.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text;
        dest.transform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = src.transform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text;
        dest.transform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = src.transform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text;
    }
}