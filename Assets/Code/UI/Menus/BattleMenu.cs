using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private bool MenuLoaded;
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
        public StaticTargetField Widespread;
    }
    public TargetFieldsGroup TargetFields;

    // Child GameObjects
    public MenuFrame PartyFrame;
    public PlayerSelectionList PartyList;
    public MenuFrame OptionRunFrame;
    public MenuFrame OptionBackFrame;
    public MenuFrame OptionWeaponFrame;
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
    private float SKILL_NAME_DISPLAY_TIME = 3f;
    private bool ActivatedActionFrame;

    private void Start()
    {
        Selection = Selections.Disabled;
        SelectItemsList.ToolList.SetEnableCondition(GetScopeUsability);
        ClearScope(true);
    }

    private void LateUpdate()
    {
        if (DeactivateActionFrame)
        {
            PlayerActionFrame.Deactivate();
            EnemyActionFrame.Deactivate();
            ActivatedActionFrame = false;
        }

        if (Selection == Selections.Disabled || CurrentBattle.Waiting) return;
        else if (!MenuLoaded)
        {
            ActingPlayer.EnableArrowKeyMovement();
            SetBlinkingBattlers(true);
            SetupForSelectAction();
            PartyFrame.Activate();
            MenuLoaded = true;
            return;
        }

        switch (Selection)
        {
            case Selections.Actions:
                UpdateTarget?.Invoke();
                SelectingAction();
                break;

            case Selections.Skills:
                if (Input.GetKeyDown(KeyCode.X)) SetupForSelectAction();
                else if (Input.GetKeyDown(KeyCode.Q)) UpdateWeapon();
                break;

            case Selections.Items:
                if (Input.GetKeyDown(KeyCode.X)) SetupForSelectAction();
                SelectItemsList.SelectTabInputs();
                break;

            case Selections.Targeting:
                UpdateTarget?.Invoke();
                if (PassedSelectedToolBuffer) SelectingTarget();
                break;

            default:
                break;
        }
    }

    public void Setup(BattlePlayer p)
    {
        Selection = Selections.Awaiting;
        PartyList.Refresh(CurrentBattle.PlayerParty.Players);
        SelectItemsList.SetToolListOnTab(0, CurrentBattle.PlayerParty.Inventory.Items.FindAll(x => !x.IsKey));
        ActingPlayer = p;
        DeclareCurrent(p);
    }

    public void Hide()
    {
        Selection = Selections.Disabled;
        //PartyFrame.Deactivate();
        OptionRunFrame.Deactivate();
        OptionBackFrame.Deactivate();
        OptionWeaponFrame.Deactivate();
        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
    }

    private void DeclareCurrent(BattlePlayer p)
    {
        int i = 0;
        foreach (Transform t in PartyList.transform)
        {
            if (p.Id == PartyList.GetId(i++)) t.GetComponent<ListSelectable>().KeepHighlighted();
            else t.GetComponent<ListSelectable>().ClearHighlights();
        }
    }

    public void DeclareNext(Battler nextActingBattler)
    {
        if (nextActingBattler is BattlePlayer nextP)
        {
            int i = 0;
            foreach (Transform t in PartyList.transform) t.GetChild(6).gameObject.SetActive(nextP.Id == PartyList.GetId(i++));
        }
        else if (nextActingBattler is BattlerAI nextA)
        {
            foreach (BattleAlly ally in CurrentBattle.PlayerParty.Allies) ally.SetNextLabel(nextA == ally);
        }
        else if (nextActingBattler is BattleEnemy nextE)
        {
            foreach (BattleEnemy enemy in CurrentBattle.EnemyParty.Enemies) enemy.SetNextLabel(nextE == enemy);
        }
    }

    public void ClearAllNextLabels()
    {
        foreach (Transform t in PartyList.transform) t.GetChild(6).gameObject.SetActive(false);
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
        SelectActionFrame.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        OptionWeaponFrame.transform.GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        OptionWeaponFrame.transform.GetChild(2).gameObject.SetActive(ActingPlayer.Weapons.Count > 1);
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
        OptionRunFrame.Activate();
        OptionBackFrame.Deactivate();
        OptionWeaponFrame.Activate();
        SetActionFrame(true);

        GrayOutIconSelection(SelectActionFrame.transform.GetChild(1).gameObject, !ActingPlayer.HasAnySkills);
        GrayOutIconSelection(SelectActionFrame.transform.GetChild(2).gameObject, CurrentBattle.PlayerParty.Inventory.Items.Count == 0);
        GrayOutIconSelection(OptionRunFrame.gameObject, CurrentBattle.EnemyParty.RunDisabled);
        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        ActingPlayer.EnableArrowKeyMovement();
        ActingPlayer.SelectedTool = ActingPlayer.BasicAttackSkill;
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
        else if (Input.GetKeyDown(KeyCode.Q)) UpdateWeapon();
    }

    private bool IsDisabled(GameObject go)
    {
        return go.GetComponent<Image>().color.a == DISABLED_ICON_TRANSPARENCY;
    }

    private void GrayOutIconSelection(GameObject go, bool condition)
    {
        float a = condition ? DISABLED_ICON_TRANSPARENCY : 1;
        if (condition && go.GetComponent<Image>().color.a == DISABLED_ICON_TRANSPARENCY) return;
        else if (!condition && go.GetComponent<Image>().color.a != DISABLED_ICON_TRANSPARENCY) return;
        go.GetComponent<Image>().color = new Color(1, 1, 1, a);
        go.transform.GetChild(0).GetComponent<Image>().color = new Color(1, 1, 1, a);
        if (Selection == Selections.Actions)
        {
            go.transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, a);
            go.transform.GetChild(2).gameObject.SetActive(!condition);
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
        OptionRunFrame.Deactivate();
        OptionBackFrame.Activate();
        OptionWeaponFrame.Activate();
        SetActionFrame(true);

        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Activate();
        SelectItemsFrame.Deactivate();
        SelectSkillsList.Refresh(ActingPlayer, CurrentBattle.FightingPlayerParty.ToList());
        EventSystem.current.SetSelectedGameObject(SelectSkillsList.transform.GetChild(0).gameObject);

        ClearScope(true);
        ActingPlayer.DisableArrowKeyMovement();
        ActingPlayer.SelectedTool = null;
    }

    public void SelectSkill()
    {
        if (!SelectSkillsList.CanSelectSkill) return;
        ActingPlayer.SelectedTool = SelectSkillsList.SelectedObject;
        SetupForPositioning();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ITEM --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectItem()
    {
        Selection = Selections.Items;
        OptionRunFrame.Deactivate();
        OptionBackFrame.Activate();
        OptionWeaponFrame.Deactivate();
        SetActionFrame(true);

        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Activate();
        SelectItemsList.ToolList.Selecting = true;
        SelectItemsList.Refresh(CurrentBattle.PlayerParty.Inventory.Items.FindAll(x => !x.IsKey));
        EventSystem.current.SetSelectedGameObject(SelectItemsList.ToolList.transform.GetChild(0).gameObject);

        ClearScope(true);
        ActingPlayer.DisableArrowKeyMovement();
        ActingPlayer.SelectedTool = null;
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
        ActingPlayer.SelectedTool = SelectItemsList.ToolList.SelectedObject as Item;
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
        OptionRunFrame.Deactivate();
        OptionBackFrame.Activate();
        OptionWeaponFrame.Deactivate();
        SetActionFrame(false);

        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        SelectedActiveTool.transform.GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedTool.GetComponent<SpriteRenderer>().sprite;
        SelectedActiveTool.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = ActingPlayer.SelectedTool.Name.ToUpper();
        JustSelectedToolTimer = Time.unscaledTime + JUST_SELECTED_TOOL_TIME_BUFFER;

        ActingPlayer.EnableArrowKeyMovement();
        ActingPlayer.TryConvertSkillToWeaponSettings();
        SetScopeTargetSearch();
    }

    private void SelectingTarget()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!ActingPlayer.UsingBasicAttack && ActingPlayer.SelectedTool is Skill) SetupForSelectSkill();
            else if (ActingPlayer.SelectedTool is Item) SetupForSelectItem();
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
        switch (ActingPlayer.SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
                TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestEnemy();
                else UpdateTarget = AimAtNearestEnemy;
                break;

            case ActiveTool.ScopeType.OneArea:
                SetSplashTarget();
                break;

            case ActiveTool.ScopeType.StraightThrough:
                TargetFields.StraightThrough.Activate(ActingPlayer);
                break;

            case ActiveTool.ScopeType.Widespread:
                TargetFields.Widespread.Activate(ActingPlayer);
                break;

            case ActiveTool.ScopeType.AllEnemies:
                TargetAll(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd), false);
                break;

            case ActiveTool.ScopeType.Self:
                TargetFields.Single.Activate(ActingPlayer);
                DisableTargetingForBattlers(CurrentBattle.FightingPlayerParty);
                UpdateTarget = () => TargetFields.Single.AimAt(ActingPlayer, false);
                break;

            case ActiveTool.ScopeType.OneAlly:
                TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestPlayer();
                else UpdateTarget = AimAtNearestPlayer;
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestKOdPlayer();
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

    private void AimAtNearestPlayer() => TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => !x.KOd)), ActingPlayer.SelectedTool.Ranged);

    private void AimAtNearestKOdPlayer() => TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => x.KOd)), ActingPlayer.SelectedTool.Ranged);

    private void AimAtNearestEnemy() => TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), ActingPlayer.SelectedTool.Ranged);

    private void SetSplashTarget(float scale = 1.0f, bool isSetup = false)
    {
        TargetFieldsGroup tfg = TargetFields;
        TargetField tf = Instantiate(ActingPlayer.SelectedTool.Ranged ? (TargetField)tfg.SplashRange : (TargetField)tfg.SplashMeelee, tfg.FieldsList);
        tf.DisposeOnDeactivate = true;
        tf.Activate(ActingPlayer, isSetup);
        tf.transform.localScale *= scale;
        if (tf is DynamicTargetField dtf) dtf.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), true);
    }

    private void TargetAll<T>(IEnumerable<T> battlers, bool includeActingPlayer) where T : Battler
    {
        TargetFields.Single.Activate(ActingPlayer);
        if (includeActingPlayer) UpdateTarget = () => TargetFields.Single.AimAt(ActingPlayer, false);
        foreach (T b in battlers)
        {
            if (b is BattlePlayer p && p.Id == ActingPlayer.Id) continue;
            DynamicTargetField dtf = Instantiate(TargetFields.Single, TargetFields.FieldsList);
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
        if (ActingPlayer.SelectedTool.Scope == ActiveTool.ScopeType.TrapSetup && CurrentBattle.AllBattlers.Any(x => x.IsSelected)) return;
        Selection = Selections.Disabled;
        ActingPlayer.DisableArrowKeyMovement();
        Hide();
        CurrentBattle.PrepareForAction();
        ClearScope(false);
        SetBlinkingBattlers(false);
        if (ActingPlayer.AimingForEnemies())
        {
            // After blinking is removed, select specifically enemies for flexible targeting
            foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies) e.Select(true);
        }
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
