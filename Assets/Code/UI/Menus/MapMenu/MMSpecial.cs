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
                SkillsList.Refresh(SelectedPlayer, MenuManager.PartyInfo.GetWholeParty());
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
        if (!ListsSetup || Selection != Selections.SelectPlayer) return;
        PartyUserList.SetSelected();
        SelectedPlayer = PartyUserList.SelectedObject as BattlePlayer;
        SkillsList.Activate();
        SkillsList.Refresh(SelectedPlayer, MenuManager.PartyInfo.GetWholeParty());
    }

    public void DeselectUser()
    {
        if (Selection != Selections.SelectPlayer) return;
        SkillsList.Deactivate();
    }

    public void SelectUser()
    {
        if (!SelectedPlayer.HasAnySkills) return;
        Selection = Selections.SelectSkill;
        PartyUserList.SetSelected();
        PartyUserList.UnhighlightAll();
        PartyUserList.SelectedButton.KeepSelected();
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
        List<Battler> party = MenuManager.PartyInfo.GetWholeParty();
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
        PartyTargetsList.SelectedObject.ReceiveToolEffects(PartyUserList.SelectedObject, SkillsList.SelectedObject);
        UpdateGauges(PartyTargetsList, index, PartyTargetsList.SelectedObject);
    }

    private void ApplyEffectsToUser()
    {
        PartyUserList.SelectedObject.ChangeSP(-SkillsList.SelectedObject.SPConsume);
        UpdateGauges(PartyUserList, PartyUserList.SelectedIndex, PartyUserList.SelectedObject);
        for (int i = 0; i < SelectableTeammates.Count; i++)
        {
            if (SelectableTeammates[i].Id != PartyUserList.SelectedObject.Id) continue;
            UpdateGauges(PartyTargetsList, i, PartyUserList.SelectedObject);
            break;
        }
    }

    private void UpdateGauges(PlayerSelectionList partyList, int index, Battler selectedTeammate)
    {
        Gauge hpg = partyList.transform.GetChild(index).GetChild(3).GetComponent<Gauge>();
        Gauge spg = partyList.transform.GetChild(index).GetChild(4).GetComponent<Gauge>();
        hpg.SetAndAnimate(selectedTeammate.HP, selectedTeammate.Stats.MaxHP);
        spg.SetAndAnimate(selectedTeammate.SP, BattleMaster.SP_CAP);
    }
}