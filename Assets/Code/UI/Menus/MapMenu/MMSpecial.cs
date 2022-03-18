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
    private ListSelectable SelectedSkillButton;
    private int SelectedSkillIndex;
    private Skill SelectedSkill;

    // Child GameObjects
    public GameObject SkillInfo;
    public GameObject SkillsListFrame;
    public TextMeshProUGUI NoSkillsLabel;
    public TextMeshProUGUI NameLabel;
    public TextMeshProUGUI SoloLabel;
    public TextMeshProUGUI TeamLabel;
    public GridLayoutGroup SkillsList;
    public GameObject CannotUseFrame;
    public TextMeshProUGUI CannotUseMessage;
    public GameObject PartyTargetFrame;
    public TextMeshProUGUI SelectTargetTeammateLabel;

    // General Selection Tracking
    private Selections Selection;
    private bool ListsSetup;

    // Colors
    private Color NormalSkillColor = Color.white;
    private Color DisabledSkillColor;

    // List of potential targets
    private List<Battler> SelectableTeammates;
    private bool SelectAllTeammates;

    // Delay after a skill has been used
    private float DoneTimer;

    protected override void Update()
    {
        if (!MainComponent.Activated) return;
        base.Update();
        if (Selection == Selections.UsageDone && Time.realtimeSinceStartup > DoneTimer)
        {
            PartyUserList.Setup(MenuManager.PartyInfo.AllPlayers);
            if (SelectedSkill.EnoughSPFrom(PartyUserList.SelectedObject)) Selection = Selections.SelectTarget;
            else UndoSelectTarget();
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
        SkillInfo.SetActive(false);
        SkillsListFrame.SetActive(false);
        CannotUseFrame.SetActive(false);
        PartyTargetFrame.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Team List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupUserList()
    {
        Selection = Selections.SelectPlayer;
        PartyUserList.Setup(MenuManager.PartyInfo.AllPlayers);
        ListsSetup = true;
    }

    public void HoverOverUser()
    {
        if (!ListsSetup || Selection != Selections.SelectPlayer) return;
        PartyUserList.SetSelected();
        SkillsListFrame.SetActive(true);
        SetupSkillList(PartyUserList.SelectedObject as BattlePlayer);
    }

    public void DeselectUser()
    {
        if (Selection != Selections.SelectPlayer) return;
        SkillInfo.SetActive(false);
        SkillsListFrame.SetActive(false);
    }

    public void SelectUser()
    {
        Selection = Selections.SelectSkill;
        PartyUserList.SetSelected();
        PartyUserList.UnhighlightAll();
        PartyUserList.SelectedButton.KeepSelected();
        SetupSkillList(PartyUserList.SelectedObject as BattlePlayer);
        bool hasAnySkill = PartyUserList.SelectedObject.Skills.Count > 0;
        SkillInfo.SetActive(hasAnySkill);
        CannotUseFrame.SetActive(false);
        if (hasAnySkill) EventSystem.current.SetSelectedGameObject(SkillsList.transform.GetChild(0).gameObject);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Skill List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupSkillList(BattlePlayer p)
    {
        bool hasSkills = p.Skills.Count > 0;
        bool hasAnySkills = hasSkills;
        if (!hasAnySkills) NoSkillsLabel.text = p.Name.ToUpper() + " DOES NOT HAVE ANY SKILLS YET";

        NameLabel.text = p.Name.ToUpper();
        NoSkillsLabel.gameObject.SetActive(!hasAnySkills);
        SoloLabel.gameObject.SetActive(hasSkills);
        SkillsList.gameObject.SetActive(hasSkills);
        if (hasSkills) SetupSkillList(SkillsList, p.Skills);
    }

    private void SetupSkillList<T>(GridLayoutGroup skillsListGUI, List<T> skillsListData) where T : Skill
    {
        int i = 0;
        for (; i < skillsListData.Count; i++)
        {
            skillsListGUI.transform.GetChild(i).gameObject.SetActive(true);
            skillsListGUI.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite = skillsListData[i].GetComponent<SpriteRenderer>().sprite;
            skillsListGUI.transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = skillsListData[i].Name;
            skillsListGUI.transform.GetChild(i).GetChild(2).GetComponent<TextMeshProUGUI>().text = skillsListData[i].SPConsume.ToString();
        }
        for (; i < skillsListGUI.transform.childCount; i++) skillsListGUI.transform.GetChild(i).gameObject.SetActive(false);
    }

    public void SetupSkill()
    {
        SetupSkill(PartyUserList.SelectedObject.Skills);
    }

    private void SetupSkill<T>(List<T> skillsList) where T : Skill
    {
        if (Selection != Selections.SelectSkill) return;
        ListSelectable btn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();    
        if (SelectedSkillButton) SelectedSkillButton.ClearHighlights();
        SelectedSkillButton = btn;
        SelectedSkillIndex = btn.Index;
        SelectedSkill = skillsList[SelectedSkillIndex];
        DisplayCannotUseMessage();
        SkillInfo.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedSkill.Name.ToUpper();
        SkillInfo.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = SelectedSkill.Description;
        InventoryToolSelectionList.SetElementImage(SkillInfo, 2, SelectedSkill);
        SkillInfo.transform.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedSkill.Power.ToString();
        SkillInfo.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedSkill.ConsecutiveActs.ToString();
        SkillInfo.transform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = "+" + SelectedSkill.CritcalRate + "%";
    }

    private void DisplayCannotUseMessage()
    {
        SelectableTeammates = new List<Battler>();
        SelectAllTeammates = false;
        string msg = "";
        if (!SelectedSkill.CanUseOutsideOfBattle) msg = "CANNOT USE OUTSIDE OF BATTLE";
        else if (!SelectedSkill.EnoughSPFrom(PartyUserList.SelectedObject)) msg = "NOT ENOUGH SP TO USE";
        else if (!SelectedSkill.UsedByClassUser(PartyUserList.SelectedObject)) msg = "MUST BE A " + SelectedSkill.ClassExclusives[0] + " TO USE THIS SKILL";
        else if (!SelectedSkill.UsedByWeaponUser(PartyUserList.SelectedObject)) msg = "MUST HAVE A " + SelectedSkill.WeaponExclusives[0] + " TO USE THIS SKILL";
        else if (!GetSelectableTargetsForUsingSkills()) msg = "NO AVAILABLE TEAMMATES TO USE THIS SKILL";
        CannotUseFrame.SetActive(!msg.Equals(""));
        CannotUseMessage.text = msg;
    }

    private void UndoSelectSkill()
    {
        Selection = Selections.SelectPlayer;
        SkillInfo.SetActive(false);
        CannotUseFrame.SetActive(false);
        PartyUserList.SelectedButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(PartyUserList.SelectedButton.gameObject);
    }

    public void SelectSkill()
    {
        if (CannotUseFrame.activeSelf) return;
        SetupSelectTarget();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Select Teammate --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private bool GetSelectableTargetsForUsingSkills()
    {
        List<Battler> party = MenuManager.PartyInfo.GetWholeParty();
        switch (SelectedSkill.Scope)
        {
            case Tool.ScopeType.OneTeammate:
            case Tool.ScopeType.Self:
                foreach (Battler p in party)
                    if (!p.Unconscious) SelectableTeammates.Add(p);
                SelectAllTeammates = false;
                break;

            case Tool.ScopeType.OneKnockedOutTeammate:
                foreach (Battler p in party)
                    if (p.Unconscious) SelectableTeammates.Add(p);
                SelectAllTeammates = false;
                break;

            case Tool.ScopeType.AllTeammates:
            case Tool.ScopeType.EveryoneButSelf:
            case Tool.ScopeType.Everyone:
                foreach (Battler p in party)
                    if (!p.Unconscious) SelectableTeammates.Add(p);
                SelectAllTeammates = true;
                break;

            case Tool.ScopeType.AllKnockedOutTeammates:
                foreach (Battler p in party)
                    if (p.Unconscious) SelectableTeammates.Add(p);
                SelectAllTeammates = true;
                break;
        }
        return SelectableTeammates.Count > 0;
    }

    private void SetupSelectTarget()
    {
        Selection = Selections.SelectTarget;
        SelectedSkillButton.KeepSelected();
        CannotUseFrame.SetActive(false);
        PartyTargetFrame.SetActive(true);
        SelectTargetTeammateLabel.text = SelectAllTeammates ? ("USE " + SelectedSkill.Name.ToUpper() + " ON EVERYONE?") : ("USE " + SelectedSkill.Name.ToUpper() + " ON...");
        PartyTargetsList.Setup(SelectableTeammates);
        EventSystem.current.SetSelectedGameObject(PartyTargetsList.transform.GetChild(0).gameObject);
        if (SelectAllTeammates && Selection == Selections.SelectTarget) PartyTargetsList.HighlightAll();
        else PartyTargetsList.UnhighlightAll();
    }

    private void UndoSelectTarget()
    {
        Selection = Selections.SelectSkill;
        PartyTargetFrame.SetActive(false);
        SelectedSkillButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(SelectedSkillButton.gameObject);
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
        PartyTargetsList.SelectedObject.ReceiveToolEffects(PartyUserList.SelectedObject, SelectedSkill);
        UpdateGauges(PartyTargetsList, index, PartyTargetsList.SelectedObject);
    }

    private void ApplyEffectsToUser()
    {
        PartyUserList.SelectedObject.ChangeSP(-SelectedSkill.SPConsume);
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