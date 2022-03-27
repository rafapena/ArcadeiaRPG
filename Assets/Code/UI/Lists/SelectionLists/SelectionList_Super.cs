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
public abstract class SelectionList_Super<T> : MonoBehaviour
{
    // Structure
    public Transform Container;

    // Selected data
    [HideInInspector] public ListSelectable SelectedButton;
    [HideInInspector] public int SelectedIndex;
    [HideInInspector] public T SelectedObject;
    protected float ENTRY_ALTERNATE_ALPHA = 0.05f;

    // Data
    protected List<T> ReferenceData;

    // Selected data
    [HideInInspector] public bool Selecting;

    // Slot info
    public GameObject InfoFrame;
    protected Color NormalBackgroundColor;

    protected virtual void Awake()
    {
        try
        {
            Color c = transform.GetChild(0).GetChild(0).GetComponent<Image>().color;
            NormalBackgroundColor = new Color(c.r, c.g, c.b, c.a);
        }
        catch (Exception) { }
        if (InfoFrame) InfoFrame.SetActive(false);
        ReferenceData = new List<T>();
    }

    public virtual void ClearSelections()
    {
        if (SelectedButton) SelectedButton.ClearHighlights();
        SelectedButton = null;
        SelectedIndex = 0;
        SelectedObject = default;
        if (InfoFrame) InfoFrame.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Navigation Layer --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected void SetHorizontalPointer(Transform leftEntry, Transform rightEntry)
    {
        Button lBtn = leftEntry.GetComponent<Button>();
        Button rBtn = rightEntry.GetComponent<Button>();
        Navigation lNav = lBtn.navigation;
        Navigation rNav = rBtn.navigation;
        lNav.selectOnRight = rBtn;
        rNav.selectOnLeft = lBtn;
        lBtn.navigation = lNav;
        rBtn.navigation = rNav;
    }

    protected void SetVerticalPointer(Transform aboveEntry, Transform belowEntry)
    {
        Button tBtn = aboveEntry.GetComponent<Button>();
        Button bBtn = belowEntry.GetComponent<Button>();
        Navigation tNav = tBtn.navigation;
        Navigation bNav = bBtn.navigation;
        tNav.selectOnDown = bBtn;
        bNav.selectOnUp = tBtn;
        tBtn.navigation = tNav;
        bBtn.navigation = bNav;
    }

    protected void DuplicateHorizontalPointers(Transform aboveEntry, Transform belowEntry)
    {
        Navigation tNav = aboveEntry.GetComponent<Button>().navigation;
        Navigation bNav = belowEntry.GetComponent<Button>().navigation;
        bNav.selectOnLeft = tNav.selectOnLeft;
        bNav.selectOnRight = tNav.selectOnRight;
        belowEntry.GetComponent<Button>().navigation = bNav;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Browsing Through List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void HighlightAll()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).GetComponent<ListSelectable>().KeepHighlighted();
    }

    public void UnhighlightAll()
    {
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).GetComponent<ListSelectable>().ClearHighlights();
    }

    public virtual void ResetSelected()
    {
        SelectedIndex = 0;
        SelectedButton = transform.GetChild(0).GetComponent<ListSelectable>();
        SelectedObject = ReferenceData[0];
    }

    public virtual void SetSelected()
    {
        SelectedButton = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        SelectedIndex = SelectedButton.Index;
        SelectedObject = ReferenceData[SelectedIndex];
    }

    public virtual void SetSelected(int index)
    {
        SelectedIndex = index;
        SelectedButton = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        SelectedObject = ReferenceData[SelectedIndex];
    }
}