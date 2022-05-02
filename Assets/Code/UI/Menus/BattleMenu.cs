using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    private enum Selections { Awaiting, Actions, Skills, Items, Targeting, Running, Disabled }

    // Battle tracking
    public Battle CurrentBattle;
    private BattlePlayer ActingPlayer;

    // Selection management
    private bool PassedSelectedToolBuffer => Time.unscaledTime > JustSelectedToolTimer;
    private float JustSelectedToolTimer;
    private const float JUST_SELECTED_TOOL_TIME_BUFFER = 0.5f;
    private const float DISABLED_ICON_TRANSPARENCY = 0.3f;
    private Action UpdateTarget;
    private Selections Selection;

    // Target fields
    [System.Serializable]
    public struct TargetFieldsGroup
    {
        public Transform FieldsList;
        public DynamicTargetField Single;
        public DynamicTargetField SplashRange;
        public StaticTargetField SplashMeelee;
        public StaticTargetField StraightThrough;
    }
    public TargetFieldsGroup TargetFields;

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
    private bool DeactivateActionFrame => ActivatedActionFrame && Time.time > SkillNameDisplayTimer;
    public MenuFrame PlayerActionFrame;
    public TextMeshProUGUI PlayerActionName;
    public MenuFrame EnemyActionFrame;
    public TextMeshProUGUI EnemyActionName;
    private float SkillNameDisplayTimer;
    private float SKILL_NAME_DISPLAY_TIME = 2f;
    private bool ActivatedActionFrame;

    private void Start()
    {
        Selection = Selections.Disabled;
        SelectItemsList.ToolList.SetEnableCondition(GetScopeUsability);
        ClearScope(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && (Selection == Selections.Actions || Selection == Selections.Skills)) UpdateWeapon();
    }

    private void LateUpdate()
    {
        if (DeactivateActionFrame)
        {
            PlayerActionFrame.Deactivate();
            EnemyActionFrame.Deactivate();
            ActivatedActionFrame = false;
        }

        switch (Selection)
        {
            case Selections.Awaiting:
                if (CurrentBattle.Waiting) break;
                PostAwaitSetup();
                SetupForSelectAction();
                break;

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

    public void Setup(BattlePlayer p)
    {
        Selection = Selections.Awaiting;
        PartyList.Refresh(CurrentBattle.PlayerParty.Players);
        SelectItemsList.SetToolListOnTab(0, CurrentBattle.PlayerParty.Inventory.Items.FindAll(x => !x.IsKey));
        ActingPlayer = p;
        p.IsDecidingAction = true;
    }

    private void PostAwaitSetup()
    {
        ActingPlayer.EnableArrowKeyMovement();
        SetBlinkingBattlers(true);
        PartyFrame.Activate();
        DeclareCurrent(ActingPlayer);
        DeclareNext(CurrentBattle.NextActingBattler);
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

    public void DeclareCurrent(Battler actingBattler)
    {
        int i = 0;
        if (actingBattler is BattlePlayer p)
        {
            foreach (Transform t in PartyList.transform)
            {
                if (p.Id == PartyList.GetId(i++)) t.GetComponent<ListSelectable>().KeepHighlighted();
                else t.GetComponent<ListSelectable>().ClearHighlights();
            }
        }
        else if (actingBattler is BattlerAI ai)
        {
            //
        }
    }

    public void DeclareNext(Battler nextActingBattler)
    {
        /*if (nextActingBattler is BattlePlayer nextP)
        {
            int i = 0;
            foreach (Transform t in PartyList.transform) t.GetChild(6).gameObject.SetActive(nextP.Id == PartyList.GetId(i++));
        }
        else if (nextActingBattler is BattleAlly nextA)
        {
            foreach (BattleAlly ally in CurrentBattle.PlayerParty.Allies) ally.SetNextLabel(nextA == ally);
        }
        else if (nextActingBattler is BattleEnemy nextE)
        {
            foreach (BattleEnemy enemy in CurrentBattle.EnemyParty.Enemies) enemy.SetNextLabel(nextE == enemy);
        }*/
    }

    public void ClearAllTurnIndicatorLabels()
    {
        foreach (Transform t in PartyList.transform)
        {
            t.GetComponent<ListSelectable>().ClearHighlights();
            t.GetChild(6).gameObject.SetActive(false);
        }
        foreach (BattleAlly ally in CurrentBattle.PlayerParty.Allies) ally.SetNextLabel(false);
        foreach (BattleEnemy enemy in CurrentBattle.EnemyParty.Enemies) enemy.SetNextLabel(false);
    }

    private void UpdateWeapon()
    {
        int selectedI = ActingPlayer.Weapons.FindIndex(x => x.Id == ActingPlayer.SelectedWeapon.Id);
        ActingPlayer.SelectedWeapon = ActingPlayer.Weapons[(selectedI + 1) % ActingPlayer.Weapons.Count];
        if (ActingPlayer.TryConvertSkillToWeaponSettings()) SetScopeTargetSearch();
        if (SelectSkillsFrame.Activated)
        {
            SelectSkillsList.Refresh(ActingPlayer, CurrentBattle.FightingPlayerParty.ToList());
            SelectSkillsList.HoverOverTool();   // Refresh current entry immediately
        }
        SetWeaponOnMenuAndCharacter();
    }

    private void SetWeaponOnMenuAndCharacter()
    {
        OptionWeapon.transform.GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        OptionWeapon.transform.GetChild(2).gameObject.SetActive(ActingPlayer.Weapons.Count > 1);
        ActingPlayer.Properties.SpriteInfo.RightArmHold(ActingPlayer.SelectedWeapon.Name);
    }

    private void SetBlinkingBattlers(bool blinking)
    {
        foreach (Battler b in CurrentBattle.AllBattlers) b.SetBlinkingBattler(blinking);
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
        else if (Input.GetKeyDown(KeyCode.R)) SelectRun();
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
        SelectSkillsList.Refresh(ActingPlayer, CurrentBattle.FightingPlayerParty.ToList());
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
        return (tool is Item it) ? it.AvailableTeammateTargets(CurrentBattle.FightingPlayerParty.ToList()) : true;
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
        foreach (Transform t in TargetFields.FieldsList) t.GetComponent<TargetField>().Deactivate();
        if (resetSelectedTargets) CurrentBattle.ResetSelectedTargets();
    }

    private void SetScopeTargetSearch()
    {
        ClearScope(true);
        switch (ActingPlayer.SelectedAction.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
                TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedAction.Ranged) AimAtNearestEnemy();
                else UpdateTarget = AimAtNearestEnemy;
                break;

            case ActiveTool.ScopeType.OneArea:
                SetSplashTarget();
                break;

            case ActiveTool.ScopeType.StraightThrough:
                TargetFields.StraightThrough.Activate(ActingPlayer);
                break;

            case ActiveTool.ScopeType.AllEnemies:
                TargetAll(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd), false);
                break;

            case ActiveTool.ScopeType.Self:
                TargetFields.Single.Activate(ActingPlayer);
                TargetFields.Single.AimingPlayerCanReverse = false;
                DisableTargetingForBattlers(CurrentBattle.FightingPlayerParty);
                UpdateTarget = () => TargetFields.Single.AimAt(ActingPlayer, false);
                break;

            case ActiveTool.ScopeType.OneAlly:
                TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedAction.Ranged) AimAtNearestPlayer();
                else UpdateTarget = AimAtNearestPlayer;
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedAction.Ranged) AimAtNearestKOdPlayer();
                else UpdateTarget = AimAtNearestKOdPlayer;
                break;

            case ActiveTool.ScopeType.AllAllies:
                TargetAll(CurrentBattle.FightingPlayerParty.Where(x => !x.KOd), true);
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                TargetAll(CurrentBattle.FightingPlayerParty.Where(x => x.KOd), false);
                break;

            case ActiveTool.ScopeType.TrapSetup:
                SetSplashTarget(0.6f);
                break;

            case ActiveTool.ScopeType.Planting:
                SetSplashTarget(1.5f);
                break;

            case ActiveTool.ScopeType.EveryoneButSelf:
                TargetAll(CurrentBattle.AllBattlers.Where(x => !x.KOd && x.Id != ActingPlayer.Id), false);
                break;

            case ActiveTool.ScopeType.Everyone:
                TargetAll(CurrentBattle.AllBattlers.Where(x => !x.KOd), true);
                break;
        }
    }

    private void AimAtNearestPlayer() => TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => !x.KOd)), ActingPlayer.SelectedAction.Ranged);

    private void AimAtNearestKOdPlayer() => TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => x.KOd)), ActingPlayer.SelectedAction.Ranged);

    private void AimAtNearestEnemy()
    {
        Battler target = ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd));
        ActingPlayer.SelectedSingleMeeleeTarget = ActingPlayer.SelectedAction.Ranged ? null : target;
        TargetFields.Single.AimAt(target, ActingPlayer.SelectedAction.Ranged);
    }

    private void SetSplashTarget(float scale = 1.0f, bool isSetup = false)
    {
        TargetFieldsGroup tfg = TargetFields;
        TargetField tf = Instantiate(ActingPlayer.SelectedAction.Ranged ? (TargetField)tfg.SplashRange : (TargetField)tfg.SplashMeelee, tfg.FieldsList);
        tf.DisposeOnDeactivate = true;
        tf.Activate(ActingPlayer, isSetup);
        tf.transform.localScale *= scale;
        if (tf is DynamicTargetField dtf) dtf.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), true);
    }

    private void TargetAll<T>(IEnumerable<T> battlers, bool addTargetFieldToActingPlayer) where T : Battler
    {
        TargetFields.Single.Activate(ActingPlayer);
        if (addTargetFieldToActingPlayer) UpdateTarget = () => TargetFields.Single.AimAt(ActingPlayer, false);
        foreach (T b in battlers)
        {
            if (b is BattlePlayer p && p.Id == ActingPlayer.Id) continue;
            DynamicTargetField dtf = Instantiate(TargetFields.Single, TargetFields.FieldsList);
            dtf.AimingPlayerCanReverse = false;
            dtf.DisposeOnDeactivate = true;
            dtf.AimAt(b, false);
            b.Select(true);
            b.LockSelectTrigger = true;
        }
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
        if (!CheckTargetField()) return;
        Selection = Selections.Disabled;
        ActingPlayer.DisableArrowKeyMovement();
        ActingPlayer.Movement = Vector3.zero;
        ActingPlayer.IsDecidingAction = false;
        Hide();
        CurrentBattle.PrepareForAction();
        ClearScope(false);
        SetBlinkingBattlers(false);
    }

    private bool CheckTargetField()
    {
        if (ActingPlayer.AimingForTeammates()) return CurrentBattle.FightingPlayerParty.Any(x => x.IsSelected);
        if (ActingPlayer.AimingForEnemies()) return CurrentBattle.EnemyParty.Enemies.Any(x => x.IsSelected);
        if (ActingPlayer.SelectedAction.Scope == ActiveTool.ScopeType.TrapSetup) return CurrentBattle.AllBattlers.All(x => !x.IsSelected);
        return true;
    }

    public void DisplayUsedAction(Battler battler, ActiveTool action)
    {
        if (action is Skill sk && sk.Basic) return;

        else if (battler is BattleEnemy)
        {
            EnemyActionFrame.Activate();
            EnemyActionName.text = action.Name;
        }
        else  // Player or ally
        {
            PlayerActionFrame.Activate();
            PlayerActionName.text = action.Name;
        }
        SkillNameDisplayTimer = Time.time + SKILL_NAME_DISPLAY_TIME;
        ActivatedActionFrame = true;
    }

    public void EndTurn()
    {
        // Handle states
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Updating party list in real-time --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ChangeHP() { }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: RUN --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SelectRun()
    {
        Selection = Selections.Running;
        Hide();
        ClearScope(true);
        CurrentBattle.RunAway();
    }

    public void RunFailed()
    {
        //
    }
}
