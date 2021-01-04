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
public class FileSelectionList : SelectionList_Super<SaveData>
{
    public static GameObject HighlightedButtonAfterUndo;
    private bool SelectedFinal;
    public int TotalNumberOfFiles;
    public string EmptyText;

    public void Setup()
    {
        ReferenceData = new List<SaveData>();
        GameObject go = transform.GetChild(0).gameObject;
        EventSystem.current.SetSelectedGameObject(go);
        for (int i = transform.childCount; i < TotalNumberOfFiles; i++)
        {
            GameObject entry = Instantiate(go, transform);
            entry.GetComponent<Button>().navigation = new Navigation() { mode = Navigation.Mode.Explicit };
            SetVerticalPointer(transform.GetChild(i - 1), entry.transform);
            ListSelectable e = entry.GetComponent<ListSelectable>();
            e.Index = i;
            e.ClearHighlights();
        }
        for (int i = 0; i < TotalNumberOfFiles; i++)
        {
            SaveData data = new SaveData(i);
            if (data.FileExists) SetFileContent(data, i);
            else SetEmptyFile(i);
            ReferenceData.Add(data);
        }
    }

    public void SetFileContent(SaveData data, int i)
    {
        transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = "File " + (i + 1);
        transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = data.PlayerName;
    }

    public void SetEmptyFile(int i)
    {
        transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = EmptyText;
        transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
    }

    private void OnDestroy()
    {
        if (HighlightedButtonAfterUndo) EventSystem.current.SetSelectedGameObject(HighlightedButtonAfterUndo);
        HighlightedButtonAfterUndo = null;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Browsing Through List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ResetSelected()
    {
        if (ReferenceData.Count > 0) base.ResetSelected();
    }

    public override void SetSelected()
    {
        if (ReferenceData.Count > 0) base.SetSelected();
    }

    public override void SetSelected(int index)
    {
        if (ReferenceData.Count > 0) base.SetSelected(index);
    }
}