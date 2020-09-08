using UnityEngine;
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
public class ObjectiveSelectionList : SelectionList_Super<Objective>
{
    public void Setup<T>(List<T> dataList, int hardLimit = -1) where T : Objective
    {
        ReferenceData = new List<Objective>();
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
            e.Index = i;
            e.ClearHighlights();
            i++;
        }

        // If there are excess entries, make them invisible
        if (hardLimit > 0) i = hardLimit;
        for (; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    public void AddToList<T>(Transform entry, T dataEntry) where T : Objective
    {
        ReferenceData.Add(dataEntry);
        bool isSub = (dataEntry.GetType().Name == "SubObjective");
        if (isSub)
        {
            SubObjective so = dataEntry as SubObjective;
            entry.gameObject.SetActive(!so.Hidden);
        }
        entry.GetChild(0).GetComponent<TextMeshProUGUI>().text = dataEntry.Name;
        entry.GetChild(2).gameObject.SetActive(dataEntry.Marked);
        entry.GetChild(3).gameObject.SetActive(dataEntry.Cleared);
    }
}