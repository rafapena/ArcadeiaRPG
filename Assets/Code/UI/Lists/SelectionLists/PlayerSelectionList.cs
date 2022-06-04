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

            dataEntry.CurrentListIndex = i;
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

        var hpg = entry.GetChild(3).gameObject.GetComponent<Gauge>();
        var spg = entry.GetChild(4).gameObject.GetComponent<Gauge>();
        if (hpg) hpg.Set(dataEntry.HP, dataEntry.MaxHP);
        if (spg) spg.Set(dataEntry.SP, 100);
        
        UpdateStates(entry, dataEntry);
        if (entry.childCount > 6) entry.GetChild(6).gameObject.SetActive(dataEntry.KOd);
    }

    private void UpdateStates<T>(Transform entry, T battler) where T : Battler
    {
        Transform statesGO = entry.GetChild(5).transform;
        if (!statesGO.gameObject.activeSelf) return;
        int limit = battler.States.Count < statesGO.childCount ? battler.States.Count : statesGO.childCount;
        int i = 0;
        for (; i < limit; i++)
        {
            statesGO.GetChild(i).gameObject.SetActive(true);
            statesGO.GetChild(i).GetComponent<Image>().sprite = battler.States[i].GetComponent<SpriteRenderer>().sprite;
        }
        for (; i < statesGO.childCount; i++) statesGO.GetChild(i).gameObject.SetActive(false);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Real-time updates --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    public void UpdateEntry<T>(T battler, int index) where T : Battler
    {
        Transform entry = transform.GetChild(index);
        Gauge hpg = entry.GetChild(3).GetComponent<Gauge>();
        Gauge spg = entry.GetChild(4).GetComponent<Gauge>();
        hpg.SetAndAnimate(battler.HP, battler.MaxHP);
        spg.SetAndAnimate(battler.SP, BattleMaster.SP_CAP);
        UpdateStates(entry, battler);
        entry.GetChild(6).gameObject.SetActive(battler.KOd);
    }
}