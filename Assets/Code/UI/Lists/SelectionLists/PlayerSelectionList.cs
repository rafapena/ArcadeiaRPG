﻿ using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Generates the structure of the player list GUI, with the following assumptions
/// - Content gameobject is inside a ScrollRect's viewport, which carries the entries
/// - Needs at least one entry has a ListSelectable object attached to it, with an explicit navigation set up
/// </summary>
public class PlayerSelectionList : SelectionList_Super<Battler>
{
    public void Refresh<T>(List<T> dataList) where T : Battler
    {
        ReferenceData.Clear();
        int i = 0;
        foreach (T dataEntry in dataList)
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
                entry = Instantiate(transform.GetChild(0).gameObject, transform);
                entry.GetComponent<Button>().navigation = new Navigation() { mode = Navigation.Mode.Explicit };
                SetVerticalPointer(transform.GetChild(i - 1), entry.transform);
                DuplicateHorizontalPointers(transform.GetChild(i - 1), entry.transform);
            }
            AddToList(entry.transform, dataEntry);
            ListSelectable e = entry.GetComponent<ListSelectable>();
            if (i % 2 == 1) e.SetMainAlpha(ENTRY_ALTERNATE_ALPHA);
            e.Index = i;
            e.ClearHighlights();
            i++;
        }

        // If there are excess blank rows, make them invisible
        for (; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    private void AddToList<T>(Transform entry, T dataEntry) where T : Battler
    {
        ReferenceData.Add(dataEntry);

        GameObject go0 = entry.GetChild(0).gameObject;
        if (go0.GetComponent<Image>() == null) go0.transform.GetChild(0).GetComponent<Image>().sprite = dataEntry.FaceImage;
        else go0.GetComponent<Image>().sprite = dataEntry.FaceImage;
        
        entry.GetChild(1).GetComponent<TextMeshProUGUI>().text = dataEntry.Name.ToUpper();
        entry.GetChild(2).GetComponent<TextMeshProUGUI>().text = dataEntry.Class?.Name.ToUpper() ?? "";

        if (entry.GetChild(3).gameObject.GetComponent<Gauge>()) entry.GetChild(3).GetComponent<Gauge>().Set(dataEntry.HP, dataEntry.MaxHP);
        if (entry.GetChild(4).gameObject.GetComponent<Gauge>()) entry.GetChild(4).GetComponent<Gauge>().Set(dataEntry.SP, 100);
        
        AddStates(entry, dataEntry);
        if (entry.childCount > 6) entry.GetChild(6).gameObject.SetActive(false);
    }

    private void AddStates<T>(Transform entry, T teammate) where T : Battler
    {
        Transform statesGO = entry.GetChild(5).transform;
        if (!statesGO.gameObject.activeSelf) return;
        int limit = teammate.States.Count < statesGO.childCount ? teammate.States.Count : statesGO.childCount;
        int i = 0;
        for (; i < limit; i++)
        {
            statesGO.GetChild(i).gameObject.SetActive(true);
            statesGO.GetChild(i).GetComponent<Image>().sprite = teammate.States[i].GetComponent<Image>().sprite;
        }
        for (; i < statesGO.childCount; i++) statesGO.GetChild(i).gameObject.SetActive(false);
    }

    public int GetId(int index)
    {
        return ReferenceData[index].Id;
    }
}