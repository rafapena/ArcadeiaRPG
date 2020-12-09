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

public class MMObjectives : MM_Super
{
    public enum Selections { None, SelectObjective, SelectSubObjective }

    // Selection lists
    public ObjectiveSelectionList ObjectivesList;
    public ObjectiveSelectionList SubObjectivesList;
    public InventoryToolSelectionList RewardsToolList;

    // Child GameObjects
    public MenuFrame InfoFrame;
    public GameObject ObjectivesListTabs;
    public TextMeshProUGUI[] StaticLabels;
    public TextMeshProUGUI NameLabel;
    public TextMeshProUGUI GoldLabel;
    public TextMeshProUGUI EXPLabel;
    public TextMeshProUGUI DescriptionLabel;

    // General selection content
    private Selections Selection;
    private ListSelectable SelectedObjectivesTabBtn;
    private List<SubObjective> SubObjectivesData;

    protected override void Update()
    {
        base.Update();
        if (Selection == Selections.SelectObjective)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectMainList();
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSecondaryList();
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectOtherList();
        }
    }

    public override void Open()
    {
        base.Open();
        SelectMainList();
        InfoFrame.Activate();
    }

    public override void Close()
    {
        base.Close();
    }

    public override void GoBack()
    {
        if (Selection == Selections.SelectObjective)
        {
            ReturnToInitialStep();
            Selection = Selections.None;
            MenuManager.GoToMain();
        }
        else if (Selection == Selections.SelectSubObjective)
        {
            UndoSelectSubObjective();
        }
    }

    protected override void ReturnToInitialStep()
    {
        InfoFrame.Deactivate();
        RewardsToolList.gameObject.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Objectives Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectMainList()
    {
        EventSystem.current.SetSelectedGameObject(ObjectivesListTabs.transform.GetChild(0).gameObject);
        SelectObjectiveTab(GetObjectivesList(MenuManager.PartyInfo.LoggedObjectives, Objective.Types.Main));
    }

    public void SelectSecondaryList()
    {
        EventSystem.current.SetSelectedGameObject(ObjectivesListTabs.transform.GetChild(1).gameObject);
        SelectObjectiveTab(GetObjectivesList(MenuManager.PartyInfo.LoggedObjectives, Objective.Types.Secondary));
    }

    public void SelectOtherList()
    {
        EventSystem.current.SetSelectedGameObject(ObjectivesListTabs.transform.GetChild(2).gameObject);
        SelectObjectiveTab(GetObjectivesList(MenuManager.PartyInfo.LoggedObjectives, Objective.Types.Other));
    }
    
    private List<Objective> GetObjectivesList(List<Objective> oldList, Objective.Types objType)
    {
        List<Objective> objectives = new List<Objective>();
        foreach (Objective o in oldList)
            if (o.Type == objType) objectives.Add(o);
        return objectives;
    }

    private void SelectObjectiveTab<T>(List<T> objectiveList) where T : Objective
    {
        Selection = Selections.SelectObjective;
        SetFrameVisibility(objectiveList.Count != 0);
        KeepOnlyHighlightedSelected(ref SelectedObjectivesTabBtn);
        ObjectivesList.Setup(objectiveList);
        if (ObjectivesList.SelectedButton) ObjectivesList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(ObjectivesList.transform.GetChild(0).gameObject);
    }

    public void HoverOverObjective()
    {
        if (Selection == Selections.SelectObjective) SetupObjective();
    }

    private void SetupObjective()
    {
        ObjectivesList.SetSelected();
        RewardsToolList.gameObject.SetActive(true);
        GenerateSubObjectivesList(ObjectivesList.SelectedObject.NextObjectives);
        SubObjectivesList.Setup(SubObjectivesData);
        NameLabel.text = ObjectivesList.SelectedObject.Name.ToUpper();
        GoldLabel.text = ObjectivesList.SelectedObject.InventoryRewards.Gold.ToString();
        EXPLabel.text = ObjectivesList.SelectedObject.EXPReward.ToString();
        List<ToolForInventory> list = ObjectivesList.SelectedObject.InventoryRewards.GetItemsAndWeapons();
        RewardsToolList.Setup(list, list.Count);
        SetupDescription();
    }

    private void GenerateSubObjectivesList(List<SubObjective> subsList)
    {
        SubObjectivesData = new List<SubObjective>();
        foreach (SubObjective so in subsList)
            AddToSubObjectives(so);
    }

    private void AddToSubObjectives(SubObjective entry)
    {
        SubObjectivesData.Add(entry);
        if (entry.NextObjectives == null) return;
        foreach (SubObjective so in entry.NextObjectives)
            AddToSubObjectives(so);
    }

    private void SetupDescription()
    {
        DescriptionLabel.text = ObjectivesList.SelectedObject.Description;
        foreach (SubObjective so in SubObjectivesData)
            if (so.Cleared) DescriptionLabel.text += ("\n\n" + so.Name);
    }

    public void SelectObjective()
    {
        if (ObjectivesList.SelectedObject.Cleared) return;
        Selection = Selections.SelectSubObjective;
        SetupObjective();
        ObjectivesList.UnhighlightAll();
        ObjectivesList.SelectedButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(SubObjectivesList.transform.GetChild(0).gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sub Objectives List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetFrameVisibility(bool visibility)
    {
        ObjectivesList.gameObject.SetActive(visibility);
        SubObjectivesList.gameObject.SetActive(visibility);
        RewardsToolList.gameObject.SetActive(visibility);
        foreach (TextMeshProUGUI text in StaticLabels)
            text.gameObject.SetActive(visibility);
        NameLabel.gameObject.SetActive(visibility);
        GoldLabel.gameObject.SetActive(visibility);
        EXPLabel.gameObject.SetActive(visibility);
        DescriptionLabel.gameObject.SetActive(visibility);
    }

    private void UndoSelectSubObjective()
    {
        Selection = Selections.SelectObjective;
        ObjectivesList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(ObjectivesList.SelectedButton.gameObject);
    }

    public void SelectSubObjective()
    {
        Selection = Selections.SelectSubObjective;
        ObjectivesList.UnhighlightAll();
        ObjectivesList.SelectedButton.KeepSelected();
        SubObjectivesList.SetSelected();

        if (!SubObjectivesList.SelectedObject.Cleared)
        {
            foreach (Objective obj in MenuManager.PartyInfo.LoggedObjectives)
            {
                obj.UnMark();
            }
            for (int i = 0; i < ObjectivesList.transform.childCount; i++)
            {
                ObjectivesList.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
            }
            for (int i = 0; i < SubObjectivesList.transform.childCount; i++)
            {
                SubObjectivesList.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
            }
            ObjectivesList.SelectedObject.Marked = true;
            SubObjectivesList.SelectedObject.Marked = true;
            ObjectivesList.transform.GetChild(ObjectivesList.SelectedIndex).GetChild(2).gameObject.SetActive(true);
            SubObjectivesList.transform.GetChild(SubObjectivesList.SelectedIndex).GetChild(2).gameObject.SetActive(true);
        }
    }
}