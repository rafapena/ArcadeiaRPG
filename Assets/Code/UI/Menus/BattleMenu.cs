using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    private enum Selections { Awaiting, Actions, Skills, Items, Targeting, Running, Disabled }

    // Keep track of the current battle and player/ActiveTool pointers 
    public Battle CurrentBattle;
    private int CurrentPlayer;
    private BattlePlayer ActingPlayer;

    // Selection management
    private bool PassedSelectedToolBuffer => Time.unscaledTime > JustSelectedToolTimer;
    private float JustSelectedToolTimer;
    private float JUST_SELECTED_TOOL_TIME_BUFFER = 0.5f;
    private readonly float DISABLED_ICON_TRANSPARENCY = 0.3f;
    private bool MenuLoaded;
    private Selections Selection;

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

    // Target selection
    private delegate void ScopeUpdateCheck();
    private ScopeUpdateCheck UpdateTarget;


    private void Start()
    {
        Selection = Selections.Disabled;
        SelectItemsList.ToolList.SetEnableCondition(GetScopeUsability);
    }

    private void LateUpdate()
    {
        if (Selection == Selections.Disabled || CurrentBattle.Waiting) return;
        else if (!MenuLoaded)
        {
            ActingPlayer.EnableMoving();
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
        PartyFrame.Deactivate();
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

    public void DeclareNext(BattlePlayer p)
    {
        int i = 0;
        foreach (Transform t in PartyList.transform)
        {
            t.GetChild(6).gameObject.SetActive(p.Id == PartyList.GetId(i++));
        }
    }

    private void UpdateWeapon()
    {
        int selectedI = ActingPlayer.Weapons.FindIndex(x => x.Id == ActingPlayer.SelectedWeapon.Id);
        ActingPlayer.SelectedWeapon = ActingPlayer.Weapons[(selectedI + 1) % ActingPlayer.Weapons.Count];
        if (ActingPlayer.TryConvertSkillToWeaponSettings()) SetScopeTargetSearch();
        if (SelectSkillsFrame.Activated)
        {
            SelectSkillsList.Refresh(ActingPlayer, FightingPlayerParty.ToList());
            SelectSkillsList.HoverOverTool();   // Refresh current entry immediately
        }
        SetWeaponOnMenuAndCharacter();
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

        GrayOutIconSelection(SelectActionFrame.transform.GetChild(1).gameObject, ActingPlayer.Skills.Count == 0);
        GrayOutIconSelection(SelectActionFrame.transform.GetChild(2).gameObject, CurrentBattle.PlayerParty.Inventory.Items.Count == 0);
        GrayOutIconSelection(OptionRunFrame.gameObject, CurrentBattle.EnemyParty.RunDisabled);
        SetWeaponOnMenuAndCharacter();
        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        ActingPlayer.ClearDecisions();
        ActingPlayer.EnableMoving();
        ActingPlayer.SelectedTool = ActingPlayer.BasicAttackSkill;
        ActingPlayer.TryConvertSkillToWeaponSettings();
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
        SelectSkillsList.Refresh(ActingPlayer, FightingPlayerParty.ToList());
        EventSystem.current.SetSelectedGameObject(SelectSkillsList.transform.GetChild(0).gameObject);

        ClearScope();
        ActingPlayer.DisableMoving();
        ActingPlayer.SelectedTool = null;
    }

    public IEnumerable<Battler> FightingPlayerParty => CurrentBattle.PlayerParty.Players.Cast<Battler>().Concat(CurrentBattle.PlayerParty.Allies);

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

        ClearScope();
        ActingPlayer.DisableMoving();
        ActingPlayer.SelectedTool = null;
    }

    private bool GetScopeUsability(IToolForInventory tool)
    {
        return (tool is Item it) ? it.AvailableTeammateTargets(FightingPlayerParty.ToList()) : true;
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

        ActingPlayer.EnableMoving();
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

    public void ClearScope()
    {
        UpdateTarget = null;
        foreach (Transform t in CurrentBattle.TargetFields.FieldsList) t.GetComponent<TargetField>().Deactivate();
    }

    private void SetScopeTargetSearch()
    {
        ClearScope();
        switch (ActingPlayer.SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestEnemy();
                else UpdateTarget = AimAtNearestEnemy;
                break;

            case ActiveTool.ScopeType.OneArea:
                SetSplashTarget();
                break;

            case ActiveTool.ScopeType.StraightThrough:
                CurrentBattle.TargetFields.StraightThrough.Activate(ActingPlayer);
                break;

            case ActiveTool.ScopeType.Widespread:
                CurrentBattle.TargetFields.Widespread.Activate(ActingPlayer);
                break;

            case ActiveTool.ScopeType.AllEnemies:
                TargetAll(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd), false);
                break;

            case ActiveTool.ScopeType.Self:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                UpdateTarget = () => CurrentBattle.TargetFields.Single.AimAt(ActingPlayer, false);
                break;

            case ActiveTool.ScopeType.OneAlly:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestPlayer();
                else UpdateTarget = AimAtNearestPlayer;
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestKOdPlayer();
                else UpdateTarget = AimAtNearestKOdPlayer;
                break;

            case ActiveTool.ScopeType.AllAllies:
                TargetAll(FightingPlayerParty.Where(x => !x.KOd), true);
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                TargetAll(FightingPlayerParty.Where(x => x.KOd), false);
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

            default:
                break;
        }
    }

    private void AimAtNearestPlayer() => CurrentBattle.TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => !x.KOd)), ActingPlayer.SelectedTool.Ranged);

    private void AimAtNearestKOdPlayer() => CurrentBattle.TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => x.KOd)), ActingPlayer.SelectedTool.Ranged);

    private void AimAtNearestEnemy() => CurrentBattle.TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), ActingPlayer.SelectedTool.Ranged);

    private void SetSplashTarget(float scale = 1.0f)
    {
        Battle.TargetFieldsGroup tfg = CurrentBattle.TargetFields;
        TargetField tf = Instantiate(ActingPlayer.SelectedTool.Ranged ? (TargetField)tfg.SplashRange : (TargetField)tfg.SplashMeelee, tfg.FieldsList);
        tf.DisposeOnDeactivate = true;
        tf.Activate(ActingPlayer);
        tf.transform.localScale *= scale;
        if (tf is DynamicTargetField dtf) dtf.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), true);
    }

    private void TargetAll<T>(IEnumerable<T> battlers, bool includeActingPlayer) where T : Battler
    {
        CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
        if (includeActingPlayer) UpdateTarget = () => CurrentBattle.TargetFields.Single.AimAt(ActingPlayer, false);
        foreach (T b in battlers)
        {
            if (b is BattlePlayer p && p.Id == ActingPlayer.Id) continue;
            DynamicTargetField dtf = Instantiate(CurrentBattle.TargetFields.Single, CurrentBattle.TargetFields.FieldsList);
            dtf.DisposeOnDeactivate = true;
            dtf.AimAt(b, false);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Finsihed setup: User selecting target in run-time --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void FinalizeSelections()
    {
        //
    }

    private void SelectedPlayerOrAllyTarget<T>(List<T> partyList, int index) where T : Battler
    {
        if (index >= partyList.Count) return;
        T pa = partyList[index];
        if (ActingPlayer.SelectedTool.Scope == ActiveTool.ScopeType.OneAlly && !pa.KOd ||
            ActingPlayer.SelectedTool.Scope == ActiveTool.ScopeType.OneKnockedOutAlly && pa.KOd)
        {
            ActingPlayer.SelectedTargets.Add(pa);
            EndDecisions();
        }
    }

    private void SelectedEnemyTarget(Battler.VerticalPositions vp, Battler.HorizontalPositions hp)
    {
        bool hitOne = false;
        switch (ActingPlayer.SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
            case ActiveTool.ScopeType.OneArea:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                {
                    if (!e.Selectable() || e.RowPosition != vp || e.ColumnPosition != hp) continue;
                    ActingPlayer.SelectedTargets.Add(e);
                    EndDecisions();
                }
                break;
            case ActiveTool.ScopeType.StraightThrough:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                {
                    if (!e.Selectable() || e.RowPosition != vp) continue;
                    ActingPlayer.SelectedTargets.Add(e);
                    hitOne = true;
                }
                if (hitOne) EndDecisions();
                break;
            case ActiveTool.ScopeType.Widespread:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                {
                    if (!e.Selectable() || e.ColumnPosition != hp) continue;
                    ActingPlayer.SelectedTargets.Add(e);
                    hitOne = true;
                }
                if (hitOne) EndDecisions();
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Turn execution --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void EndDecisions()
    {
        /*SetSPConsumption();
        if (CurrentPlayer + 1 < CurrentBattle.PlayerParty.Players.Count)
            ShiftPlayer(1);
        else
        {
            Hide();
            CurrentPlayer = -1;
            CurrentBattle.ExecuteTurn(CurrentBattle.PlayerParty.GetBattlingParty(), CurrentBattle.EnemyParty.ConvertToGeneric());
        }*/
    }
    
    private void SetSPConsumption()
    {
        ActingPlayer.SPToConsumeThisTurn = (ActingPlayer.SelectedTool as Skill).SPConsume;
    }

    public void EndTurn()
    {
        for (int i = 0; i < CurrentBattle.PlayerParty.Players.Count; i++)
        {
            GameObject selected = PartyFrame.transform.GetChild(i).GetChild(5).gameObject;
            selected.GetComponent<Image>().sprite = null;
            selected.SetActive(false);
        }
        CurrentPlayer = 0;
        while (!CurrentBattle.PlayerParty.Players[CurrentPlayer].CanDoAction())
            CurrentPlayer++;
        Selection = Selections.Actions;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Common action helpers --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    private void SetWeaponOnMenuAndCharacter()
    {
        SelectActionFrame.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        OptionWeaponFrame.transform.GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        OptionWeaponFrame.transform.GetChild(2).gameObject.SetActive(ActingPlayer.Weapons.Count > 1);
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

    private bool IsDisabled(GameObject go)
    {
        return go.GetComponent<Image>().color.a == DISABLED_ICON_TRANSPARENCY;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: RUN --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SelectRun()
    {
        Hide();
        Selection = Selections.Running;
        //CurrentBattle.RunAway();
    }

    private void RunFailed()
    {
        Selection = Selections.Disabled;
        for (int i = 0; i < CurrentBattle.PlayerParty.Players.Count; i++) CurrentBattle.PlayerParty.Players[i].ClearDecisions();
    }
}
