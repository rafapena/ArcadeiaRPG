﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    private enum Selections { Actions, Skills, Items, Targeting, Disabled, Escaping }

    // Battle tracking
    public Battle CurrentBattle;
    private BattlePlayer ActingPlayer;

    // Selection management
    private bool PassedSelectedToolBuffer => Time.unscaledTime > JustSelectedToolTimer;
    private float JustSelectedToolTimer;
    private const float JUST_SELECTED_TOOL_TIME_BUFFER = 0.5f;
    private const float DISABLED_ICON_TRANSPARENCY = 0.3f;
    private Action UpdateTarget;
    private bool AimRelativeToPlayer = true;
    private Selections Selection;

    // Target fields
    [System.Serializable]
    public struct TargetFieldsGroup
    {
        public Transform FieldsList;
        public Transform SingleArrow;
        public DynamicTargetField Default;
        public Transform EnemyTargetDefault;
        public StaticTargetField StraightThrough;
        public Transform PositionRestrictor;
    }
    public TargetFieldsGroup TargetFields;
    public Transform EscapeCatchHitboxes;
    private Vector3 DefaultTargetFieldScale;
    private bool RestrictorIsBlinking = false;
    private const string NON_TRIGGER_TARGET_FIELD = "NonTriggerTargetField";

    // Child GameObjects
    public MenuFrame PartyFrame;
    public PlayerSelectionList PartyList;
    public MenuFrame EnemiesFrame;
    public PlayerSelectionList EnemiesList;
    public GameObject OptionRun;
    public GameObject OptionBack;
    public GameObject OptionWeapon;
    public MenuFrame SelectActionFrame;
    public MenuFrame SelectSkillsFrame;
    public SkillSelectionList SelectSkillsList;
    public MenuFrame SelectItemsFrame;
    public ToolListCollectionFrame SelectItemsList;
    public GameObject SelectedActiveTool;

    // Skill management
    public MenuFrame PlayerActionFrame;
    public TextMeshProUGUI PlayerActionName;
    public MenuFrame EnemyActionFrame;
    public TextMeshProUGUI EnemyActionName;
    private const float DISPLAY_SKILL_USE_TIME = 3f;

    // Other
    private float EnemyListHeight;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup + Standard Operations --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void Start()
    {
        Selection = Selections.Disabled;
        SelectItemsList.ToolList.SetEnableCondition(GetScopeUsability);
        ClearScope(true);
        EnemyListHeight = EnemiesFrame.gameObject.GetComponent<RectTransform>().sizeDelta.y;
        DefaultTargetFieldScale = TargetFields.Default.transform.localScale;
        TargetFields.PositionRestrictor.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && (Selection == Selections.Actions || Selection == Selections.Skills)) UpdateWeapon();
        if (Selection != Selections.Disabled) TargetFields.PositionRestrictor.position = ActingPlayer.Position;
    }

    private void LateUpdate()
    {
        switch (Selection)
        {
            case Selections.Actions:
                UpdateTarget?.Invoke();
                SelectingAction();
                break;
            case Selections.Skills:
                if (Input.GetKeyDown(KeyCode.X)) SetupForSelectAction();
                break;
            case Selections.Items:
                if (Input.GetKeyDown(KeyCode.X)) SetupForSelectAction();
                SelectItemsList.SelectTabInputs();
                break;
            case Selections.Targeting:
                UpdateTarget?.Invoke();
                if (PassedSelectedToolBuffer) SelectingTarget();
                break;
        }
    }

    public void Setup(Battler b)
    {
        SelectItemsList.SetToolListOnTab(0, CurrentBattle.PlayerParty.Inventory.Items.FindAll(x => !x.IsKey));
        if (b is BattlePlayer p)
        {
            ActingPlayer = p;
            p.IsDecidingAction = true;
            p.SpriteInfo.ActionHitbox.gameObject.SetActive(false);
        }
        ActingPlayer.EnableArrowKeyMovement();
        SetupForSelectAction();
    }

    public void RefreshPartyFrames()
    {
        PartyList.Refresh(CurrentBattle.PlayerParty.Players);
        UpdateEnemyPartyList();
        PartyFrame.Activate();
        EnemiesFrame.Activate();
    }

    public void RemovePartyFrames()
    {
        PartyFrame.Deactivate();
        EnemiesFrame.Deactivate();
    }

    public void Hide()
    {
        Selection = Selections.Disabled;
        OptionRun.gameObject.SetActive(false);
        OptionBack.gameObject.SetActive(false);
        OptionWeapon.gameObject.SetActive(false);
        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
    }

    private void UpdateWeapon()
    {
        int selectedI = ActingPlayer.Weapons.FindIndex(x => x.Id == ActingPlayer.SelectedWeapon.Id);
        ActingPlayer.SelectedWeapon = ActingPlayer.Weapons[(selectedI + 1) % ActingPlayer.Weapons.Count];
        if (ActingPlayer.TryConvertSkillToWeaponSettings()) SetScopeTargetSearch();
        if (SelectSkillsFrame.Activated)
        {
            SelectSkillsList.Refresh(ActingPlayer, CurrentBattle.PlayerParty.BattlingParty.ToList());
            SelectSkillsList.HoverOverTool();   // Refresh current entry immediately
        }
        SetWeaponOnMenuAndCharacter();
    }

    private void SetWeaponOnMenuAndCharacter()
    {
        if (!ActingPlayer.SelectedWeapon) return;
        OptionWeapon.transform.GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        OptionWeapon.transform.GetChild(2).gameObject.SetActive(ActingPlayer.Weapons.Count > 1);
        ActingPlayer.SpriteInfo.RightArmHold(ActingPlayer.SelectedWeapon.Name);
    }

    private void UpdateEnemyPartyList()
    {
        EnemiesList.Refresh(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd).ToList());
        Vector2 size = EnemiesFrame.gameObject.GetComponent<RectTransform>().sizeDelta;
        size.y = EnemyListHeight * CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd).Count();
        EnemiesFrame.gameObject.GetComponent<RectTransform>().sizeDelta = size;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ACTION --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectAction()
    {
        Selection = Selections.Actions;
        OptionRun.gameObject.SetActive(true);
        OptionBack.gameObject.SetActive(false);
        OptionWeapon.gameObject.SetActive(ActingPlayer.Weapons.Count > 1);
        SetActionFrame(true);

        TryGrayOutIconSelection(SelectActionFrame.transform.GetChild(1).gameObject, !ActingPlayer.HasAnySkills);
        TryGrayOutIconSelection(SelectActionFrame.transform.GetChild(2).gameObject, !CurrentBattle.PlayerParty.Inventory.Items.Where(x => !x.IsKey).Any());
        TryGrayOutIconSelection(OptionRun.gameObject, CurrentBattle.EnemyParty.RunDisabled);
        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        ActingPlayer.EnableArrowKeyMovement();
        ActingPlayer.SelectedAction = ActingPlayer.BasicAttackSkill;
        ActingPlayer.TryConvertSkillToWeaponSettings();
        SetWeaponOnMenuAndCharacter();
        SetScopeTargetSearch();
    }

    private void SelectingAction()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        else if (Input.GetKeyDown(KeyCode.Z)) FinalizeSelections();
        else if (Input.GetKeyDown(KeyCode.C) && !IsDisabled(SelectActionFrame.transform.GetChild(1).gameObject)) SetupForSelectSkill();
        else if (Input.GetKeyDown(KeyCode.V) && !IsDisabled(SelectActionFrame.transform.GetChild(2).gameObject)) SetupForSelectItem();
        else if (Input.GetKeyDown(KeyCode.R)) SelectEscape();
    }

    private bool IsDisabled(GameObject go)
    {
        return go.GetComponent<Image>().color.a == DISABLED_ICON_TRANSPARENCY;
    }

    private void TryGrayOutIconSelection(GameObject go, bool condition)
    {
        float a = condition ? DISABLED_ICON_TRANSPARENCY : 1;
        if (condition && go.GetComponent<Image>().color.a == DISABLED_ICON_TRANSPARENCY) return;
        else if (!condition && go.GetComponent<Image>().color.a != DISABLED_ICON_TRANSPARENCY) return;

        Color newColor = new Color(1, 1, 1, a);
        go.GetComponent<Image>().color = newColor;
        go.transform.GetChild(0).GetComponent<Image>().color = newColor;
        if (Selection == Selections.Actions)
        {
            go.transform.GetChild(1).gameObject.SetActive(!condition);
            go.transform.GetChild(2).GetComponent<TextMeshProUGUI>().color = newColor;
        }
    }

    private void SetActionFrame(bool actionSelection)
    {
        SelectActionFrame.transform.GetChild(0).gameObject.SetActive(actionSelection);
        SelectActionFrame.transform.GetChild(1).gameObject.SetActive(actionSelection);
        SelectActionFrame.transform.GetChild(2).gameObject.SetActive(actionSelection);
        SelectActionFrame.transform.GetChild(3).gameObject.SetActive(!actionSelection);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: SKILL --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectSkill()
    {
        Selection = Selections.Skills;
        OptionRun.gameObject.SetActive(false);
        OptionBack.gameObject.SetActive(true);
        OptionWeapon.gameObject.SetActive(true);
        SetActionFrame(true);

        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Activate();
        SelectItemsFrame.Deactivate();
        SelectSkillsList.Refresh(ActingPlayer, CurrentBattle.PlayerParty.BattlingParty.ToList());
        EventSystem.current.SetSelectedGameObject(SelectSkillsList.transform.GetChild(0).gameObject);

        ClearScope(true);
        ActingPlayer.DisableArrowKeyMovement();
        ActingPlayer.SelectedAction = null;
    }

    public void SelectSkill()
    {
        if (!SelectSkillsList.CanSelectSkill) return;
        ActingPlayer.SelectedAction = SelectSkillsList.SelectedObject;
        SetupForPositioning();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ITEM --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectItem()
    {
        Selection = Selections.Items;
        OptionRun.gameObject.SetActive(false);
        OptionBack.gameObject.SetActive(true);
        OptionWeapon.gameObject.SetActive(false);
        SetActionFrame(true);

        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Activate();
        SelectItemsList.ToolList.Selecting = true;
        SelectItemsList.Refresh(CurrentBattle.PlayerParty.Inventory.Items.FindAll(x => !x.IsKey));
        EventSystem.current.SetSelectedGameObject(SelectItemsList.ToolList.transform.GetChild(0).gameObject);

        ClearScope(true);
        ActingPlayer.DisableArrowKeyMovement();
        ActingPlayer.SelectedAction = null;
    }

    private bool GetScopeUsability(IToolForInventory tool)
    {
        return (tool is Item it) ? it.AvailableTeammateTargets(CurrentBattle.PlayerParty.BattlingParty) : true;
    }

    public void SelectTabSuccess() { }

    public void SelectTabFailed() { }

    public void SelectToolSuccess()
    {
        SelectItemsList.ToolList.Selecting = false;
        ActingPlayer.SelectedAction = SelectItemsList.ToolList.SelectedObject as Item;
        SetupForPositioning();
    }

    public void SelectToolFailed() { }

    public void UndoSelectToolSuccess() { }

    public void ActivateSorterSuccess() { }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: TARGET --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForPositioning()
    {
        Selection = Selections.Targeting;
        OptionRun.gameObject.SetActive(false);
        OptionBack.gameObject.SetActive(true);
        OptionWeapon.gameObject.SetActive(false);
        SetActionFrame(false);

        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        SelectedActiveTool.transform.GetChild(0).GetComponent<Image>().sprite = (ActingPlayer.SelectedAction is Skill sk) ? sk.Image : ActingPlayer.SelectedAction.GetComponent<SpriteRenderer>().sprite;
        SelectedActiveTool.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = ActingPlayer.SelectedAction.Name.ToUpper();
        JustSelectedToolTimer = Time.unscaledTime + JUST_SELECTED_TOOL_TIME_BUFFER;

        ActingPlayer.EnableArrowKeyMovement();
        ActingPlayer.TryConvertSkillToWeaponSettings();
        SetScopeTargetSearch();
    }

    private void SelectingTarget()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!ActingPlayer.UsingBasicAttack && ActingPlayer.SelectedAction is Skill) SetupForSelectSkill();
            else if (ActingPlayer.SelectedAction is Item) SetupForSelectItem();
            else SetupForSelectAction();
        }
        else if (Input.GetKeyDown(KeyCode.Z)) FinalizeSelections();
    }

    public void ClearScope(bool resetSelectedTargets)
    {
        UpdateTarget = null;
        CurrentBattle.IgnoreSplitWallForTarget(false);
        foreach (Transform t in TargetFields.FieldsList) t.GetComponent<TargetField>()?.Deactivate();
        if (resetSelectedTargets) CurrentBattle.ResetSelectedTargets();
    }

    private void SetScopeTargetSearch()
    {
        ClearScope(true);
        switch (ActingPlayer.SelectedAction.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:             TargetAim(1f, 0.7f, true, AimAtNearestEnemy); break;
            case ActiveTool.ScopeType.OneArea:              TargetAim(3f, false, AimAtNearestEnemy); break;
            case ActiveTool.ScopeType.WideFrontal:          TargetAll(GetWideFrontalTargets(), false); break;
            case ActiveTool.ScopeType.StraightThrough:      TargetFields.StraightThrough.Activate(ActingPlayer); break;
            case ActiveTool.ScopeType.AllEnemies:           TargetAll(CurrentBattle.EnemyParty.Enemies.Where(x => x.CanTarget), false); break;
            case ActiveTool.ScopeType.Self:                 AimAtSelf(); break;
            case ActiveTool.ScopeType.OneAlly:              TargetAim(1f, 0.7f, true, AimAtNearestPlayer); break;
            case ActiveTool.ScopeType.OneKnockedOutAlly:    TargetAim(1f, 0.7f, true, AimAtNearestKOdPlayer); break;
            case ActiveTool.ScopeType.AllAllies:            TargetAll(CurrentBattle.PlayerParty.BattlingParty.Where(x => x.CanTarget), true); break;
            case ActiveTool.ScopeType.AllKnockedOutAllies:  TargetAll(CurrentBattle.PlayerParty.BattlingParty.Where(x => x.KOd), false); break;
            case ActiveTool.ScopeType.TrapSetup:            TargetAim(0.7f, false, AimAtNearestEnemy, true); break;
            case ActiveTool.ScopeType.Planting:             TargetAim(1.5f, false, AimAtNearestEnemy, true); break;
            case ActiveTool.ScopeType.EveryoneButSelf:      TargetAll(CurrentBattle.AllBattlers.Where(x => x.CanTarget && x.Id != ActingPlayer.Id), false); break;
            case ActiveTool.ScopeType.Everyone:             TargetAll(CurrentBattle.AllBattlers.Where(x => x.CanTarget), true); break;
        }
    }

    private void AimAtNearestPlayer() => AimAtNearestBattler(CurrentBattle.PlayerParty.Players.Where(x => x.CanTarget));

    private void AimAtNearestKOdPlayer() => AimAtNearestBattler(CurrentBattle.PlayerParty.Players.Where(x => x.KOd));

    private void AimAtNearestEnemy() => AimAtNearestBattler(CurrentBattle.EnemyParty.Enemies.Where(x => x.CanTarget));

    private void AimAtNearestBattler<T>(IEnumerable<T> targets) where T : Battler
    {
        var battler = GetNearestTarget(AimRelativeToPlayer ? ActingPlayer.Position : TargetFields.Default.transform.position, targets);
        ActingPlayer.SingleSelectedTarget = battler;
        TargetFields.Default.AimAt(battler, ActingPlayer.SelectedAction.Ranged);
    }

    private void TargetAim(float scale, bool targetOnlyOne, Action aimFunc, bool isSetup = false) => TargetAim(scale, scale, targetOnlyOne, aimFunc, isSetup);

    private void TargetAim(float scaleMeelee, float scaleRanged, bool targetOnlyOne, Action aimFunc, bool isSetup = false)
    {
        TargetFields.Default.Activate(ActingPlayer, isSetup);
        TargetFields.Default.TargetOnlyOne = targetOnlyOne;
        if (ActingPlayer.SelectedAction.Ranged) aimFunc.Invoke();
        else UpdateTarget = aimFunc;
        CurrentBattle.IgnoreSplitWallForTarget(isSetup);
        TargetFields.Default.transform.localScale = DefaultTargetFieldScale * (ActingPlayer.SelectedAction.Ranged ? scaleRanged : scaleMeelee);
    }

    private void AimAtSelf()
    {
        TargetFields.Default.Activate(ActingPlayer);
        TargetFields.Default.TargetOnlyOne = true;
        DisableTargetingForBattlers(CurrentBattle.PlayerParty.BattlingParty);
        UpdateTarget = () => TargetFields.Default.AimAt(ActingPlayer, false);
    }

    private IEnumerable<Battler> GetWideFrontalTargets()
    {
        // TO IMPLEMENT
        return CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd);
    }

    private void TargetAll<T>(IEnumerable<T> battlers, bool addTargetFieldToActingPlayer) where T : Battler
    {
        TargetFields.Default.Activate(ActingPlayer);
        TargetFields.Default.TargetOnlyOne = false;
        TargetFields.Default.transform.localScale = DefaultTargetFieldScale;
        if (addTargetFieldToActingPlayer) UpdateTarget = () => TargetFields.Default.AimAt(ActingPlayer, false);
        foreach (T b in battlers)
        {
            if (b is BattlePlayer p && p.Id == ActingPlayer.Id) continue;
            DynamicTargetField dtf = Instantiate(TargetFields.Default, TargetFields.FieldsList);
            dtf.DisposeOnDeactivate = true;
            dtf.AimAt(b, false);
            b.Select(true);
            b.LockSelectTrigger = true;
        }
    }

    public T GetNearestTarget<T>(Vector3 source, IEnumerable<T> targetOptions) where T : Battler
    {
        T minB = targetOptions.First();
        float minDist = float.MaxValue;
        foreach (T b in targetOptions)
        {
            float dist = Vector3.Distance(source, b.Position);
            if (dist >= minDist) continue;
            minB = b;
            minDist = dist;
        }
        return minB;
    }

    public void DisableTargetingForBattlers<T>(IEnumerable<T> battlers) where T : Battler
    {
        foreach (Battler b in battlers)
        {
            b.Select(false);
            b.LockSelectTrigger = true;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Finsihed setup: User selecting target in run-time --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void FinalizeSelections()
    {
        if (!CheckTargetField())
        {
            AimRelativeToPlayer = false;
            SetScopeTargetSearch();
            AimRelativeToPlayer = true;
            return;
        }
        ActingPlayer.FinalizeDecision();
        Hide();
        ClearScope(false);
    }

    public bool SelectedAction() => !ActingPlayer.IsDecidingAction;

    private bool CheckTargetField()
    {
        if (TargetFields.PositionRestrictor.gameObject.activeSelf || RestrictorIsBlinking)
        {
            if (!RestrictorIsBlinking) StartCoroutine(BlinkRestrictor());
            return false;
        }
        if (ActingPlayer.AimingForTeammates()) return CurrentBattle.PlayerParty.BattlingParty.Any(x => x.IsSelected);
        if (ActingPlayer.AimingForEnemies()) return CurrentBattle.EnemyParty.Enemies.Any(x => x.IsSelected);
        if (ActingPlayer.SelectedAction.Scope == ActiveTool.ScopeType.TrapSetup) return CurrentBattle.AllBattlers.All(x => !x.IsSelected);
        return true;
    }

    private IEnumerator BlinkRestrictor()
    {
        RestrictorIsBlinking = true;
        float counter = 1;
        while (counter > 0)
        {
            TargetFields.PositionRestrictor.gameObject.GetComponent<SpriteRenderer>().color = new Color(counter, counter, counter);
            counter -= 0.1f;
            yield return new WaitForSeconds(0.01f);
        }
        while (counter < 1)
        {
            TargetFields.PositionRestrictor.gameObject.GetComponent<SpriteRenderer>().color = new Color(counter, counter, counter);
            counter += 0.1f;
            yield return new WaitForSeconds(0.01f);
        }
        RestrictorIsBlinking = false;
    }

    public IEnumerator DisplayUsedAction(Battler battler, string action)
    {
        if (action.Equals(string.Empty))
        {
            yield break;
        }
        else if (battler is BattleEnemy)
        {
            EnemyActionFrame.Activate();
            EnemyActionName.text = action;
            yield return new WaitForSeconds(DISPLAY_SKILL_USE_TIME);
            EnemyActionFrame.Deactivate();
        }
        else  // Player or ally
        {
            PlayerActionFrame.Activate();
            PlayerActionName.text = action;
            yield return new WaitForSeconds(DISPLAY_SKILL_USE_TIME);
            PlayerActionFrame.Deactivate();
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Updating party in real-time --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UpdatePlayerEntry(Battler b) => PartyList.UpdateEntry(b, b.CurrentListIndex);

    public void UpdateEnemyEntry(Battler b) => EnemiesList.UpdateEntry(b, b.CurrentListIndex);

    public void SetHighlightSelectedPlayerEntries(Battler b, bool highlight) => SetHighlightSelectedBattlerEntries(PartyList, b, highlight);

    public void SetHighlightSelectedEnemyEntries(Battler b, bool highlight) => SetHighlightSelectedBattlerEntries(EnemiesList, b, highlight);

    private void SetHighlightSelectedBattlerEntries(PlayerSelectionList BattlerList, Battler b, bool highlight)
    {
        if (b.CurrentListIndex >= BattlerList.transform.childCount) return;
        var entry = BattlerList.transform.GetChild(b.CurrentListIndex).GetComponent<ListSelectable>();
        if (highlight) entry.KeepSelected();
        else entry.ClearHighlights();
    }

    public void DisplayAITargets()
    {
        foreach (var b in CurrentBattle.AllBattlers)
        {
            if (b.IsSelected) Instantiate(TargetFields.EnemyTargetDefault, b.Position, Quaternion.identity, TargetFields.FieldsList).gameObject.tag = NON_TRIGGER_TARGET_FIELD;
        }
    }

    public void RemoveAITargets()
    {
        foreach (Transform t in TargetFields.FieldsList)
        {
            if (t.gameObject.CompareTag(NON_TRIGGER_TARGET_FIELD)) Destroy(t.gameObject);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ESCAPE --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SelectEscape()
    {
        Hide();
        Selection = Selections.Escaping;
        ActingPlayer.FinalizeDecision();
        ClearScope(true);
    }

    public void AddEscapeHitboxes()
    {
        foreach (var b in CurrentBattle.PlayerParty.BattlingParty)
        {
            if (b.KOd) return;
            Transform t = Instantiate(EscapeCatchHitboxes.GetChild(0), EscapeCatchHitboxes);
            var e = t.GetComponent<EscapeCatchHitbox>();
            e.Followee = b;
        }
    }

    public void RemoveEscapeHitboxes()
    {
        Selection = Selections.Disabled;
        for (int i = EscapeCatchHitboxes.childCount - 1; i >= 1; i--)
        {
            Destroy(EscapeCatchHitboxes.GetChild(i).gameObject);
        }
    }

    public bool Escaping => Selection == Selections.Escaping;
}
