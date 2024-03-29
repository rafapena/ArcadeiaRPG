﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Linq;

public class MMSpecial : MM_Super
{
    public enum Selections { None, SelectPlayer, SelectSkill, SelectTarget, UsageDone }

    // Selections lists and Skill List attributes
    public PlayerSelectionList PartyUserList;
    public PlayerSelectionList PartyTargetsList;

    // Child GameObjects
    public GameObject SkillsListFrame;
    public SkillSelectionList SkillsList;
    public GameObject PartyTargetFrame;
    public TextMeshProUGUI SelectTargetTeammateLabel;

    // General Selection Tracking
    private Selections Selection;
    private bool ListsSetup;

    // List of potential targets
    private List<Battler> SelectableTeammates;
    private bool SelectAllTeammates;
    private BattlePlayer HoveredPlayer;
    private BattlePlayer SelectedPlayer;

    // Delay after a skill has been used
    private float DoneTimer;


    protected override void Start()
    {
        base.Start();
        SelectableTeammates = new List<Battler>();
    }

    protected override void Update()
    {
        if (!MainComponent.Activated) return;
        base.Update();
        if (Selection == Selections.UsageDone && Time.realtimeSinceStartup > DoneTimer)
        {
            PartyUserList.Refresh(MenuManager.PartyInfo.AllPlayers);
            if (SkillsList.SelectedObject.EnoughSPFrom(PartyUserList.SelectedObject)) Selection = Selections.SelectTarget;
            else
            {
                SkillsList.Refresh(SelectedPlayer, MenuManager.PartyInfo.WholeParty.ToList());
                UndoSelectTarget();
            }
        }
    }

    public override void Open()
    {
        base.Open();
        SetupUserList();
        HoverOverUser();
    }

    public override void Close()
    {
        base.Close();
    }

    public override void GoBack()
    {
        switch (Selection)
        {
            case Selections.SelectPlayer:
                Selection = Selections.None;
                ReturnToInitialSetup();
                MenuManager.GoToMain();
                break;
            case Selections.SelectSkill:
                UndoSelectSkill();
                break;
            case Selections.SelectTarget:
                UndoSelectTarget();
                break;
        }
    }

    protected override void ReturnToInitialSetup()
    {
        PartyTargetFrame.SetActive(false);
        SkillsListFrame.SetActive(true);
        SkillsList.Deactivate();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Team List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupUserList()
    {
        Selection = Selections.SelectPlayer;
        PartyUserList.Refresh(MenuManager.PartyInfo.AllPlayers);
        ListsSetup = true;
    }

    public void HoverOverUser()
    {
        if (!ListsSetup || Selection != Selections.SelectPlayer && Selection != Selections.SelectSkill) return;
        PartyUserList.SetSelected();
        HoveredPlayer = PartyUserList.SelectedObject as BattlePlayer;
        SkillsList.Activate();
        SkillsList.Refresh(HoveredPlayer, MenuManager.PartyInfo.WholeParty.ToList());
    }

    public void DeselectUser()
    {
        if (Selection != Selections.SelectPlayer) return;
        SkillsList.Deactivate();
    }

    public void SelectUser()
    {
        if (!HoveredPlayer.HasAnySkills) return;
        Selection = Selections.SelectSkill;
        PartyUserList.SetSelected();
        PartyUserList.UnhighlightAll();
        PartyUserList.SelectedButton.KeepSelected();
        SelectedPlayer = PartyUserList.SelectedObject as BattlePlayer;
        EventSystem.current.SetSelectedGameObject(SkillsList.transform.GetChild(0).gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Skill List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    private void UndoSelectSkill()
    {
        Selection = Selections.SelectPlayer;
        SkillsList.Deactivate();
        PartyUserList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyUserList.SelectedButton.gameObject);
    }

    public void SelectSkill()
    {
        if (!SkillsList.CanSelectSkill) return;
        SkillsList.UnhighlightAll();
        SkillsList.SelectedButton.KeepSelected();
        GetSelectableTargetsForUsingSkills();
        SetupSelectTarget();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Select Teammate --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private bool GetSelectableTargetsForUsingSkills()
    {
        List<Battler> party = MenuManager.PartyInfo.WholeParty.ToList();
        SelectableTeammates.Clear();
        switch (SkillsList.SelectedObject.Scope)
        {
            case ActiveTool.ScopeType.OneAlly:
            case ActiveTool.ScopeType.Self:
                foreach (Battler p in party)
                    if (!p.KOd) SelectableTeammates.Add(p);
                SelectAllTeammates = false;
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                foreach (Battler p in party)
                    if (p.KOd) SelectableTeammates.Add(p);
                SelectAllTeammates = false;
                break;

            case ActiveTool.ScopeType.AllAllies:
            case ActiveTool.ScopeType.EveryoneButSelf:
            case ActiveTool.ScopeType.Everyone:
                foreach (Battler p in party)
                    if (!p.KOd) SelectableTeammates.Add(p);
                SelectAllTeammates = true;
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                foreach (Battler p in party)
                    if (p.KOd) SelectableTeammates.Add(p);
                SelectAllTeammates = true;
                break;
        }
        return SelectableTeammates.Count > 0;
    }

    private void SetupSelectTarget()
    {
        Selection = Selections.SelectTarget;
        SkillsList.SelectedButton.KeepSelected();
        PartyTargetFrame.SetActive(true);

        string sk = SkillsList.SelectedObject.Name.ToUpper();
        SelectTargetTeammateLabel.text = SelectAllTeammates ? ("USE " + sk + " ON EVERYONE?") : ("USE " + sk + " ON...");
        
        PartyTargetsList.Refresh(SelectableTeammates);
        EventSystem.current.SetSelectedGameObject(PartyTargetsList.transform.GetChild(0).gameObject);
        if (SelectAllTeammates && Selection == Selections.SelectTarget) PartyTargetsList.HighlightAll();
        else PartyTargetsList.UnhighlightAll();
    }

    private void UndoSelectTarget()
    {
        Selection = Selections.SelectSkill;
        PartyTargetFrame.SetActive(false);
        SkillsList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(SkillsList.SelectedButton.gameObject);
    }

    public void SelectTarget()
    {
        if (Selection != Selections.SelectTarget) return;
        else if (SelectAllTeammates) UseItemOnMultiple();
        else UseItemOnSingle(EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Use skill --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetToUseSkillSelection()
    {
        Selection = Selections.UsageDone;
        DoneTimer = Time.realtimeSinceStartup + 1f;
    }

    private void UseItemOnSingle(int index)
    {
        SetToUseSkillSelection();
        UseSkill(index);
        ApplyEffectsToUser();
    }

    private void UseItemOnMultiple()
    {
        SetToUseSkillSelection();
        for (int i = 0; i < SelectableTeammates.Count; i++) UseSkill(i);
        ApplyEffectsToUser();
    }

    private void UseSkill(int index)
    {
        PartyTargetsList.SelectedObject = SelectableTeammates[index];
        PartyTargetsList.SelectedObject.ReceiveToolEffects(PartyUserList.SelectedObject, SkillsList.SelectedObject, null);
        PartyTargetsList.UpdateEntry(PartyTargetsList.SelectedObject, index);
    }

    private void ApplyEffectsToUser()
    {
        PartyUserList.SelectedObject.AddSP(-SkillsList.SelectedObject.SPConsume);
        PartyUserList.UpdateEntry(PartyUserList.SelectedObject, PartyUserList.SelectedIndex);
        for (int i = 0; i < SelectableTeammates.Count; i++)
        {
            if (SelectableTeammates[i].Id != PartyUserList.SelectedObject.Id) continue;
            PartyTargetsList.UpdateEntry(PartyUserList.SelectedObject, i);
            break;
        }
    }
}