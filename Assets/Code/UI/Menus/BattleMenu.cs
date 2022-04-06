using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour, Assets.Code.UI.Lists.IToolCollectionFrameOperations
{
    private enum Selections { Awaiting, Actions, Skills, Items, Positioning, Aiming, Running, Disabled }

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
    public MenuFrame CommonActionsFrame;
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
    }

    private void LateUpdate()
    {
        if (Selection == Selections.Disabled || CurrentBattle.Waiting) return;
        else if (!MenuLoaded)
        {
            ActingPlayer.EnableMoving();
            SetupForSelectAction();
            ActivateCommonFrames();
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
            case Selections.Items:
                if (Input.GetKeyDown(KeyCode.X)) SetupForSelectAction();
                break;

            case Selections.Positioning:
            case Selections.Aiming:
                UpdateTarget?.Invoke();
                if (PassedSelectedToolBuffer) SelectingPositioningOrAiming();
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
        CommonActionsFrame.Deactivate();
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

    private void ActivateCommonFrames()
    {
        PartyFrame.Activate();
        CommonActionsFrame.Activate();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ACTION --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectAction()
    {
        Selection = Selections.Actions;
        SetActionFrames(true, false);
        GrayOutIconSelection(SelectActionFrame.transform.GetChild(1).gameObject, ActingPlayer.Skills.Count == 0);
        GrayOutIconSelection(CommonActionsFrame.transform.GetChild(0).gameObject, CurrentBattle.EnemyParty.RunDisabled);
        SetWeaponOnMenuAndCharacter();
        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        ActingPlayer.ClearDecisions();
        ActingPlayer.EnableMoving();
        ActingPlayer.SelectedTool = ActingPlayer.BasicAttackSkill;
        ActingPlayer.TryConvertToWeaponSettings();
        SetScopeTargetSearch(false);
    }

    private void SelectingAction()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            if (ActingPlayer.SelectedTool.Ranged) SetupForAiming();
            //else;
        }
        else if (Input.GetKeyDown(KeyCode.C) && !IsDisabled(SelectActionFrame.transform.GetChild(1).gameObject)) SetupForSelectSkill();
        else if (Input.GetKeyDown(KeyCode.V) && !IsDisabled(SelectActionFrame.transform.GetChild(2).gameObject)) SetupForSelectItem();
        else if (Input.GetKeyDown(KeyCode.R)) SelectRun();
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            ActingPlayer.SelectedWeapon = GetNextWeapon();
            SetWeaponOnMenuAndCharacter();
        }
    }

    private void SetActionFrames(bool actionSelection, bool backButton)
    {
        CommonActionsFrame.transform.GetChild(0).gameObject.SetActive(!backButton);
        CommonActionsFrame.transform.GetChild(1).gameObject.SetActive(backButton);
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
        SetActionFrames(true, true);
        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Activate();
        SelectItemsFrame.Deactivate();
        SelectSkillsList.Refresh(ActingPlayer, FightingParty);
        EventSystem.current.SetSelectedGameObject(SelectSkillsList.transform.GetChild(0).gameObject);

        ActingPlayer.DisableMoving();
        ActingPlayer.SelectedTool = null;
    }

    private List<Battler> FightingParty => CurrentBattle.PlayerParty.Players.Cast<Battler>().Concat(CurrentBattle.PlayerParty.Allies).ToList();

    public void SelectSkill()
    {
        ActingPlayer.SelectedTool = SelectSkillsList.SelectedObject;
        SetupForPositioning();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ITEM --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectItem()
    {
        Selection = Selections.Items;
        SetActionFrames(true, true);
        SelectActionFrame.Deactivate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Activate();
        SelectItemsList.ToolList.Selecting = true;
        SelectItemsList.Refresh(CurrentBattle.PlayerParty.Inventory.Items.FindAll(x => !x.IsKey));
        EventSystem.current.SetSelectedGameObject(SelectItemsList.ToolList.transform.GetChild(0).gameObject);

        ActingPlayer.DisableMoving();
        ActingPlayer.SelectedTool = null;
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
        Selection = Selections.Positioning;
        SetActionFrames(false, true);
        SelectActionFrame.Activate();
        SelectSkillsFrame.Deactivate();
        SelectItemsFrame.Deactivate();
        EventSystem.current.SetSelectedGameObject(null);

        SelectedActiveTool.transform.GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedTool.GetComponent<SpriteRenderer>().sprite;
        SelectedActiveTool.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = ActingPlayer.SelectedTool.Name.ToUpper();
        JustSelectedToolTimer = Time.unscaledTime + JUST_SELECTED_TOOL_TIME_BUFFER;

        ActingPlayer.EnableMoving();
        ActingPlayer.TryConvertToWeaponSettings();
        SetScopeTargetSearch(true);
    }

    public void CleanupScope()
    {
        foreach (Transform t in CurrentBattle.TargetFields.FieldsList)
            t.GetComponent<TargetField>().Deactivate();
    }

    private void SetScopeTargetSearch(bool setToAim)
    {
        UpdateTarget = null;
        if (setToAim && ActingPlayer.SelectedTool.Ranged) SetupForAiming();

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
                TargetAll(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd));
                break;

            case ActiveTool.ScopeType.Self:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                UpdateTarget = () => CurrentBattle.TargetFields.Single.AimAt(ActingPlayer, false);
                break;

            case ActiveTool.ScopeType.OneAlly:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                if (ActingPlayer.SelectedTool.Ranged) AimAtNearestKOdPlayer();
                else UpdateTarget = AimAtNearestKOdPlayer;
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
                CurrentBattle.TargetFields.Single.AimAt(ActingPlayer, true);
                break;

            case ActiveTool.ScopeType.AllAllies:
                TargetAll(CurrentBattle.PlayerParty.Players.Where(x => !x.KOd));
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                TargetAll(CurrentBattle.PlayerParty.Players.Where(x => x.KOd));
                break;

            case ActiveTool.ScopeType.TrapSetup:
                SetSplashTarget(0.6f);
                break;

            case ActiveTool.ScopeType.Planting:
                SetSplashTarget(1.5f);
                break;

            case ActiveTool.ScopeType.EveryoneButSelf:
                TargetAll(CurrentBattle.AllBattlers.Where(x => !x.KOd && x.Id != ActingPlayer.Id));
                break;

            case ActiveTool.ScopeType.Everyone:
                TargetAll(CurrentBattle.AllBattlers.Where(x => !x.KOd));
                break;

            default:
                break;
        }
    }

    private void AimAtNearestKOdPlayer()
    {
        CurrentBattle.TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.PlayerParty.Players.Where(x => x.KOd)), ActingPlayer.SelectedTool.Ranged);
    }

    private void AimAtNearestEnemy()
    {
        CurrentBattle.TargetFields.Single.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), ActingPlayer.SelectedTool.Ranged);
    }

    private void SetSplashTarget(float scale = 1.0f)
    {
        Battle.TargetFieldsGroup tfg = CurrentBattle.TargetFields;
        TargetField tf = Instantiate(ActingPlayer.SelectedTool.Ranged ? (TargetField)tfg.SplashRange : (TargetField)tfg.SplashMeelee, tfg.FieldsList);
        tf.DisposeOnDeactivate = true;
        tf.Activate(ActingPlayer);
        tf.transform.localScale *= scale;
        if (tf is DynamicTargetField dtf) dtf.AimAt(ActingPlayer.GetNearestTarget(CurrentBattle.EnemyParty.Enemies.Where(x => !x.KOd)), true);
    }

    private void TargetAll<T>(IEnumerable<T> battlers) where T : Battler
    {
        CurrentBattle.TargetFields.Single.Activate(ActingPlayer);
        foreach (T battler in battlers)
        {
            DynamicTargetField dtf = Instantiate(CurrentBattle.TargetFields.Single, CurrentBattle.TargetFields.FieldsList);
            dtf.DisposeOnDeactivate = true;
            dtf.AimAt(battler, false);
        }
        CurrentBattle.TargetFields.Single.Deactivate();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: AIMING (Ranged moves only) --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForAiming()
    {
        Selection = Selections.Aiming;
        SetActionFrames(false, true);
        JustSelectedToolTimer = Time.unscaledTime + JUST_SELECTED_TOOL_TIME_BUFFER;
        ActingPlayer.DisableMoving();
    }

    private void SelectingPositioningOrAiming()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            CleanupScope();
            if (ActingPlayer.SelectedTool is Skill) SetupForSelectSkill();
            else if (ActingPlayer.SelectedTool is Item) SetupForSelectItem();
            else SetupForSelectAction();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            if (Selection == Selections.Positioning) SetupForAiming();
            //else ;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Finsihed setup: User selecting target in run-time --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SelectTarget()
    {
        if (InputMaster.GoingBack)
        {
            if (ActingPlayer.SelectedTool is Skill && ActingPlayer.SelectedTool.Id != ActingPlayer.BasicAttackSkill.Id) SetupForSelectSkill();
            else if (ActingPlayer.SelectedTool is Item) SetupForSelectItem();
            else SetupForSelectAction();
            return;
        }
        /*if (SelectedTool.RandomTarget)
        {
            if (KeyPressed.Equals("A")) EndDecisions();
            return;
        }
        switch (ActingPlayer.SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.AllAllies:
            case ActiveTool.ScopeType.AllKnockedOutAllies:
            case ActiveTool.ScopeType.AllEnemies:
            case ActiveTool.ScopeType.EveryoneButSelf:
            case ActiveTool.ScopeType.Everyone:
                if (KeyPressed.Equals("A")) EndDecisions();
                break;
            case ActiveTool.ScopeType.OneAlly:
            case ActiveTool.ScopeType.OneKnockedOutAllies:
                switch (KeyPressed)
                {
                    case "A": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Players, 0); break;
                    case "S": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Players, 1); break;
                    case "D": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Players, 2); break;
                    case "F": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Players, 3); break;
                    case "Z": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Allies, 0); break;
                    case "X": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Allies, 1); break;
                    case "C": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Allies, 2); break;
                    case "V": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Allies, 3); break;
                    case "B": SelectedPlayerOrAllyTarget(CurrentBattle.PlayerParty.Allies, 4); break;
                }
                break;
            default:
                switch (KeyPressed)
                {
                    case "Q": SelectedEnemyTarget(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Left); break;
                    case "W": SelectedEnemyTarget(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Center); break;
                    case "E": SelectedEnemyTarget(Battler.VerticalPositions.Top, Battler.HorizontalPositions.Right); break;
                    case "A": SelectedEnemyTarget(Battler.VerticalPositions.Center, Battler.HorizontalPositions.Left); break;
                    case "S": SelectedEnemyTarget(Battler.VerticalPositions.Center, Battler.HorizontalPositions.Center); break;
                    case "D": SelectedEnemyTarget(Battler.VerticalPositions.Center, Battler.HorizontalPositions.Right); break;
                    case "Z": SelectedEnemyTarget(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Left); break;
                    case "X": SelectedEnemyTarget(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Center); break;
                    case "C": SelectedEnemyTarget(Battler.VerticalPositions.Bottom, Battler.HorizontalPositions.Right); break;
                }
                break;
        }*/
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
        SetSPConsumption();
        if (CurrentPlayer + 1 < CurrentBattle.PlayerParty.Players.Count)
            ShiftPlayer(1);
        else
        {
            Hide();
            CurrentPlayer = -1;
            CurrentBattle.ExecuteTurn(CurrentBattle.PlayerParty.GetBattlingParty(), CurrentBattle.EnemyParty.ConvertToGeneric());
        }
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

    private void ShiftPlayer(int nextPlayer)
    {
        CurrentPlayer += nextPlayer;
        while (!CurrentBattle.PlayerParty.Players[CurrentPlayer].CanDoAction())
            CurrentPlayer += nextPlayer;
        Selection = Selections.Actions;
        CommonActionsFrame.Deactivate();
    }

    private Weapon GetNextWeapon()
    {
        int selectedI = ActingPlayer.Weapons.FindIndex(x => x.Id == ActingPlayer.SelectedWeapon.Id);
        return ActingPlayer.Weapons[(selectedI + 1) % ActingPlayer.Weapons.Count];
    }

    private void SetWeaponOnMenuAndCharacter()
    {
        SelectActionFrame.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        CommonActionsFrame.transform.GetChild(2).GetChild(0).GetComponent<Image>().sprite = ActingPlayer.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        CommonActionsFrame.transform.GetChild(2).GetChild(2).gameObject.SetActive(ActingPlayer.Weapons.Count > 1);
        CommonActionsFrame.transform.GetChild(2).gameObject.SetActive(true);
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
