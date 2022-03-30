using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour
{
    private enum Selections { Actions, Skills, Items, Teammates, Targets, Animating } // Selection process navigation

    private enum SelectedActions { None, Attack, Skill, Item, Run }     // Selections on the Action section

    private enum DisabledTool { None, NoScope, LowSP, WarmupOrCoolDown, NoWeapon, NotClass }

    // Keep track of the current battle and player/ActiveTool pointers 
    public Battle CurrentBattle;
    private int CurrentPlayer;
    private BattlePlayer CP;
    private Transform CPCI;
    private ActiveTool SelectedTool;

    // Selection management
    private Selections Selection;
    private SelectedActions SelectedAction;
    private string KeyPressed;

    // Loading
    private bool LoadReady;
    private float WaitTimeBeforeTurnStarts = 1f;

    // Child GameObjects
    public MenuFrame CharacterInfo;
    public MenuFrame CommonActionsFrame;
    public MenuFrame SelectionFrame;
    public GameObject SelectActionList;
    public GameObject SelectToolList;
    public MenuFrame ConfirmToolFrame;
    public GameObject SelectTargetInTeam;

    // General UI
    private GameObject CSelected;       // Changing icon in the CharacterInfo character
    private Color CInfoFrameMainColor;
    private Color CInfoFrameCurrentColor;
    private readonly float DISABLED_TRANSPARENCY = 0.3f;

    // ActiveTool Selection phase
    private int ConfirmToolMenuIndex = -1;
    private string[] DisableToolReasons;
    private string[] TOOL_LETTER_COMMANDS = new string[] { "A", "S", "D", "Z", "X", "C" };

    // Constants
    private readonly int MAX_NUMBER_OF_SOLO_SKILLS = 3;

    private void Start()
    {
        SelectTargetInTeam.SetActive(false);
        CInfoFrameMainColor = CharacterInfo.transform.GetChild(0).GetComponent<Image>().color;
        CInfoFrameCurrentColor = new Color(0.5f, 0.6f, 0.9f);
        WaitTimeBeforeTurnStarts = Time.time + WaitTimeBeforeTurnStarts;
    }

    private void LateUpdate()
    {
        if (Time.time <= WaitTimeBeforeTurnStarts) return;
        if (!LoadReady)
        {
            SetupPlayerComponents();
            CurrentBattle.TurnStartSetup();
            SetupForSelectAction();
            LoadReady = true;
            return;
        }
        KeyPressed = Input.inputString.ToUpper();
        switch (Selection)
        {
            case Selections.Actions: SelectAction(); break;
            case Selections.Skills: SelectSkill(); break;
            case Selections.Items: SelectItem(); break;
            case Selections.Teammates: SelectTeammates(); break;
            case Selections.Targets: SelectTarget(); break;
            default: break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup icons for the current player --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupPlayerComponents()
    {
        if (Selection == Selections.Animating) return;     // Indicates next player instead of next turn
        int i = 0;
        for (; i < CurrentBattle.PlayerParty.Players.Count; i++)
        {
            BattlePlayer p = CurrentBattle.PlayerParty.Players[i];
            Transform oneCI = CharacterInfo.transform.GetChild(i);
            oneCI.GetChild(0).GetComponent<TextMeshProUGUI>().text = p.Name.ToUpper();
            oneCI.GetChild(1).GetComponent<Image>().sprite = p.MainImage;
            oneCI.GetChild(2).GetComponent<Gauge>().Set(p.HP, p.Stats.MaxHP);
            oneCI.GetChild(3).GetComponent<Gauge>().Set(p.SP, 100);
            SetupPlayerStateComponents(p, oneCI);
            oneCI.GetChild(5).gameObject.SetActive(oneCI.GetChild(5).GetComponent<Image>().sprite != null);
        }
        for (; i < CharacterInfo.transform.childCount; i++)
            CharacterInfo.transform.GetChild(i).gameObject.SetActive(false);
    }

    private void SetupPlayerStateComponents(BattlePlayer p, Transform oneCI)
    {
        int i = 0;
        Transform ocis = oneCI.GetChild(4);
        int statesLimit = p.States.Count < ocis.childCount ? p.States.Count : ocis.childCount;
        for (; i < statesLimit; i++)
        {
            ocis.GetChild(i).GetComponent<Image>().sprite = CP.States[i].GetComponent<SpriteRenderer>().sprite;
            ocis.GetChild(i).gameObject.SetActive(true);
        }
        for (; i < ocis.childCount; i++) ocis.GetChild(i).gameObject.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ACTION --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectAction()
    {
        SetupCurrentSelection();
        ResetChoices();
        Selection = 0;
        SelectedAction = SelectedActions.None;
        GrayOutIconSelection(SelectActionList.transform.GetChild(1).gameObject, CP.Skills.Count == 0);
        CharacterInfo.Activate();
        CommonActionsFrame.Activate();
        CommonActionsFrame.transform.GetChild(0).gameObject.SetActive(CurrentPlayer == 0);
        CommonActionsFrame.transform.GetChild(1).gameObject.SetActive(CurrentPlayer > 0);
        SetWeaponOnMenuAndCharacter();
        GrayOutIconSelection(CommonActionsFrame.transform.GetChild(0).gameObject, CurrentBattle.EnemyParty.RunDisabled);
        SelectionFrame.Activate();
        SelectActionList.SetActive(true);
        SelectToolList.SetActive(false);
        ConfirmToolFrame.Deactivate();
        SelectTargetInTeam.SetActive(false);
        CSelected.SetActive(false);
    }

    private void SetupCurrentSelection()
    {
        CP = CurrentBattle.PlayerParty.Players[CurrentPlayer];
        CPCI = CharacterInfo.transform.GetChild(CurrentPlayer);
        for (int i = 0; i < CharacterInfo.transform.childCount; i++)
            CharacterInfo.transform.GetChild(i).GetComponent<Image>().color = CInfoFrameMainColor;
        CPCI.GetComponent<Image>().color = CInfoFrameCurrentColor;
        CSelected = CPCI.GetChild(5).gameObject;
    }

    private void ResetChoices()
    {
        ConfirmToolMenuIndex = -1;
        SelectedTool = null;
        CP.ClearTurnChoices();
    }

    private void SelectAction()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        if (Input.GetKeyDown(KeyCode.Backspace) && CurrentPlayer > 0)
        {
            CSelected.GetComponent<Image>().sprite = null;
            ShiftPlayer(-1);
            return;
        }
        switch (KeyPressed)
        {
            case "A":
                CP.SelectedSkill = CP.AttackSkill;
                SelectedTool = CP.SelectedWeapon;
                SelectedAction = SelectedActions.Attack;
                CSelected.SetActive(true);
                CSelected.GetComponent<Image>().sprite = SelectActionList.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite;
                SetupForSelectTarget();
                break;
            case "S":
                if (IsDisabled(SelectActionList.transform.GetChild(1).gameObject)) break;
                SelectedAction = SelectedActions.Skill;
                CSelected.SetActive(true);
                CSelected.GetComponent<Image>().sprite = SelectActionList.transform.GetChild(1).GetChild(0).GetComponent<Image>().sprite;
                SetupForSelectSkill();
                break;
            case "D":
                if (IsDisabled(SelectActionList.transform.GetChild(2).gameObject)) break;
                SelectedAction = SelectedActions.Item;
                CSelected.SetActive(true);
                CSelected.GetComponent<Image>().sprite = SelectActionList.transform.GetChild(2).GetChild(0).GetComponent<Image>().sprite;
                SetupForSelectItem();
                break;
            case "R":
                SelectedAction = SelectedActions.Run;
                SelectRun();
                break;
            case "Q":
                CP.SelectedWeapon = GetNextWeapon();
                SetWeaponOnMenuAndCharacter();
                break;
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: SKILL --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectSkill()
    {
        Selection = Selections.Skills;
        SelectedTool = null;
        CP.ClearTurnChoices();
        CommonActionsFrame.transform.GetChild(0).gameObject.SetActive(false);
        CommonActionsFrame.transform.GetChild(1).gameObject.SetActive(true);
        CommonActionsFrame.transform.GetChild(2).gameObject.SetActive(true);
        SelectionFrame.Activate();
        SelectActionList.SetActive(false);
        SelectToolList.SetActive(true);
        ConfirmToolFrame.Deactivate();
        SelectTargetInTeam.SetActive(false);
        DisableToolReasons = new string[MAX_NUMBER_OF_SOLO_SKILLS];
        for (int i = 0; i < DisableToolReasons.Length; i++)
            DisableToolReasons[i] = "";
        SetupForSkillSelectIcons(CP.Skills, 0, MAX_NUMBER_OF_SOLO_SKILLS);
    }

    // Skill and TeamSkill have different lists, but share the same set of icons on the UI
    private void SetupForSkillSelectIcons<T>(List<T> skillList, int start, int maxListCount) where T : Skill
    {
        int i = 0;
        int sLimit = skillList.Count < maxListCount ? skillList.Count : maxListCount;
        for (; i < sLimit; i++)
        {
            int iconI = start + i;
            Transform t = SelectToolList.transform.GetChild(iconI);
            T skill = skillList[i];
            t.GetChild(0).GetComponent<Image>().sprite = skill.GetComponent<SpriteRenderer>().sprite;
            t.GetChild(1).GetComponent<TextMeshProUGUI>().text = skill.SPConsume > 0 ? skill.SPConsume.ToString() : "";
            t.GetChild(2).GetComponent<TextMeshProUGUI>().text = skill.DisabledFromWarmupOrCooldown() ? skill.DisabledCount.ToString() : "";
            t.gameObject.SetActive(true);

            if (NoAvailableTargets(skill))
                DisableToolReasons[iconI] = "No available teammates to select";
            else if (skill.DisabledFromWarmupOrCooldown())
                DisableToolReasons[iconI] = "Must wait for " + skill.DisabledCount + " more turns before using";
            else if (!skill.UsedByWeaponUser(CP))
                DisableToolReasons[iconI] = "A " + System.Enum.GetName(typeof(BattleMaster.WeaponTypes), skill.WeaponExclusives[0]) + " is required to use this";
            else if (!skill.UsedByClassUser(CP))
                DisableToolReasons[iconI] = "Must be at class " + skill.ClassExclusives[0].Name + " to use this";
            GrayOutIconSelection(t.gameObject, DisableToolReasons[iconI].Length > 0);
        }
        for (; i < maxListCount; i++)
        {
            int iconI = start + i;
            SelectToolList.transform.GetChild(iconI).gameObject.SetActive(false);
        }
    }

    private void SelectSkill()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            SetupForSelectAction();
            return;
        }
        switch (KeyPressed)
        {
            case "A": ConfirmSkillSelection(CP.Skills, 0, 0); break;
            case "S": ConfirmSkillSelection(CP.Skills, 1, 1); break;
            case "D": ConfirmSkillSelection(CP.Skills, 2, 2); break;
            case "Q":
                CP.SelectedWeapon = GetNextWeapon();
                SetWeaponOnMenuAndCharacter();
                for (int i = 0; i < DisableToolReasons.Length; i++)
                    DisableToolReasons[i] = "";
                SetupForSkillSelectIcons(CP.Skills, 0, MAX_NUMBER_OF_SOLO_SKILLS);
                SetConfirmUsability(7, ConfirmToolMenuIndex);
                break;
        }
    }

    private void ConfirmSkillSelection(List<Skill> Skills, int skillListIndex, int toolConfirmIndex)
    {
        if (skillListIndex >= Skills.Count) return;
        CP.SelectedSkill = ConfirmGenericToolSelection(Skills, skillListIndex, toolConfirmIndex);
        if (!CP.SelectedSkill) return;
        ConfirmToolFrame.transform.GetChild(6).gameObject.SetActive(true);
        ConfirmToolFrame.transform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = "1";
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: ITEM --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectItem()
    {
        Selection = Selections.Items;
        SelectedTool = null;
        CP.ClearTurnChoices();
        CommonActionsFrame.transform.GetChild(0).gameObject.SetActive(false);
        CommonActionsFrame.transform.GetChild(1).gameObject.SetActive(true);
        CommonActionsFrame.transform.GetChild(2).gameObject.SetActive(false);
        SelectionFrame.Activate();
        SelectActionList.SetActive(false);
        SelectToolList.SetActive(true);
        ConfirmToolFrame.Deactivate();
        SelectTargetInTeam.SetActive(false);
        for (int i = 0; i < DisableToolReasons.Length; i++)
            DisableToolReasons[i] = "";
        SetupForItemSelectIcons();
    }

    private void SetupForItemSelectIcons()
    {
        /*int i = 0;
        int iLimit = (CP.Items.Count < BattleMaster.MAX_NUMBER_OF_ITEMS) ? CP.Items.Count : BattleMaster.MAX_NUMBER_OF_ITEMS;
        for (; i < iLimit; i++)
        {
            SelectToolList.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite = CP.Items[i].GetComponent<SpriteRenderer>().sprite;
            SelectToolList.transform.GetChild(i).GetChild(1).gameObject.SetActive(false);
            SelectToolList.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
            SelectToolList.transform.GetChild(i).gameObject.SetActive(true);
            bool nva = NoAvailableTargets(CP.Items[i]);
            bool nc =  !CP.Items[i].UsedByClassUser(CP);
            GrayOutIconSelection(SelectToolList.transform.GetChild(i).gameObject, nva || nc);
            if (nva) DisableToolReasons[i] = "No available teammates to select";
            else if (nc) DisableToolReasons[i] = "Must be at class " + CP.Items[i].ClassExclusives[0].Name + " to use this";
        }
        for (; i < BattleMaster.MAX_NUMBER_OF_ITEMS; i++)
            SelectToolList.transform.GetChild(i).gameObject.SetActive(false);*/
    }

    private void SelectItem()
    {
        /*if (!MenuMaster.ReadyToSelectInMenu) return;
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            SetupForSelectAction();
            return;
        }
        switch (KeyPressed)
        {
            case "A": ConfirmItemSelection(CP.Items, 0, 0); break;
            case "S": ConfirmItemSelection(CP.Items, 1, 1); break;
            case "D": ConfirmItemSelection(CP.Items, 2, 2); break;
            case "Z": ConfirmItemSelection(CP.Items, 3, 3); break;
            case "X": ConfirmItemSelection(CP.Items, 4, 4); break;
            case "C": ConfirmItemSelection(CP.Items, 5, 5); break;
        }*/
    }

    private void ConfirmItemSelection(List<Item> items, int itemListIndex, int toolConfirmIndex)
    {
        if (itemListIndex < items.Count)
            CP.SelectedItem = ConfirmGenericToolSelection(items, itemListIndex, toolConfirmIndex);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: SKILL/ITEM Helpers --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private bool NoAvailableTargets(ActiveTool ActiveTool)
    {
        if (ActiveTool.Scope != ActiveTool.ScopeType.OneKnockedOutAllies) return false;
        foreach (BattlePlayer p in CurrentBattle.PlayerParty.Players)
            if (p.HP == 0) return false;
        foreach (BattleAlly a in CurrentBattle.PlayerParty.Allies)
            if (a.HP == 0) return false;
        return true;
    }

    private T ConfirmGenericToolSelection<T>(List<T> tools, int toolListIndex, int toolMenuConfirmIndex) where T : ActiveTool
    {
        if (toolListIndex >= tools.Count) return null;
        T ActiveTool = tools[toolListIndex];

        bool isDisabled = DisableToolReasons[toolMenuConfirmIndex].Length > 0;
        if (ConfirmToolMenuIndex == toolMenuConfirmIndex) return isDisabled ? null : GenericToolSelectionFinal(ActiveTool);   // Final confirmation on ActiveTool, before going to target selection
        ConfirmToolMenuIndex = toolMenuConfirmIndex;

        bool isSkill = (Selection == Selections.Skills);
        ConfirmToolFrame.transform.GetChild(4).gameObject.SetActive(isSkill);
        ConfirmToolFrame.transform.GetChild(5).gameObject.SetActive(isSkill);
        ConfirmToolFrame.transform.GetChild(6).gameObject.SetActive(isSkill);

        ConfirmToolFrame.transform.GetChild(0).GetComponent<Image>().sprite = ActiveTool.GetComponent<SpriteRenderer>().sprite;
        ConfirmToolFrame.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = ActiveTool.Name.ToUpper();
        ConfirmToolFrame.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = ActiveTool.Description;
        InventoryToolSelectionList.SetElementImage(ConfirmToolFrame.gameObject, 3, ActiveTool);
        ConfirmToolFrame.transform.GetChild(4).GetChild(0).GetComponent<TextMeshProUGUI>().text = ActiveTool.Power.ToString();
        ConfirmToolFrame.transform.GetChild(5).GetChild(0).GetComponent<TextMeshProUGUI>().text = ActiveTool.ConsecutiveActs.ToString();
        ConfirmToolFrame.transform.GetChild(6).GetChild(0).GetComponent<TextMeshProUGUI>().text = ActiveTool.CriticalRate + "%"; //GetCritStr(ActiveTool.CritcalRate);
        ConfirmToolFrame.transform.GetChild(7).gameObject.SetActive(false); // Always false for Items, uses a different function to activate for Skills
        SetConfirmUsability(8, toolMenuConfirmIndex);
        ConfirmToolFrame.Activate();
        return ActiveTool;
    }

    private void SetElementImage<T>(int index, T ActiveTool) where T : ActiveTool
    {
        try
        {
            ConfirmToolFrame.transform.GetChild(index).GetComponent<Image>().sprite = UIMaster.ElementImages[ActiveTool.Element];
            ConfirmToolFrame.transform.GetChild(index).gameObject.SetActive(true);
        }
        catch (KeyNotFoundException) { ConfirmToolFrame.transform.GetChild(index).gameObject.SetActive(false); }
    }

    private string GetCritStr(int critRate)
    {
        string str = "";
        if (critRate > 0) str += "+" + critRate;
        else if (critRate < 0) str += "-" + -critRate;
        else return "-";
        return str + "%";
    }

    private void SetConfirmUsability(int index, int toolMenuConfirmIndex)
    {
        if (toolMenuConfirmIndex < 0) return;
        if (DisableToolReasons[toolMenuConfirmIndex].Length > 0)
        {
            ConfirmToolFrame.transform.GetChild(index).GetComponent<TextMeshProUGUI>().color = Color.yellow;
            ConfirmToolFrame.transform.GetChild(index).GetComponent<TextMeshProUGUI>().text = DisableToolReasons[toolMenuConfirmIndex];
            ConfirmToolFrame.transform.GetChild(index).GetChild(0).gameObject.SetActive(false);
        }
        else
        {
            ConfirmToolFrame.transform.GetChild(index).GetComponent<TextMeshProUGUI>().color = Color.white;
            ConfirmToolFrame.transform.GetChild(index).GetComponent<TextMeshProUGUI>().text = "\nCONFIRM";
            ConfirmToolFrame.transform.GetChild(index).GetChild(0).gameObject.SetActive(true);
            ConfirmToolFrame.transform.GetChild(index).GetChild(0).GetComponent<Image>().sprite = UIMaster.LetterCommands[TOOL_LETTER_COMMANDS[toolMenuConfirmIndex]];
        }
    }

    private T GenericToolSelectionFinal<T>(T ActiveTool) where T : ActiveTool
    {
        if (CP.SelectedItem)
        {
            CSelected.GetComponent<Image>().sprite = CP.SelectedItem.GetComponent<SpriteRenderer>().sprite;
            SelectedTool = CP.SelectedItem;
            CP.SelectedSkill = null;
            SetupForSelectTarget();
        }
        else if (CP.SelectedSkill)
        {
            CSelected.GetComponent<Image>().sprite = CP.SelectedSkill.GetComponent<SpriteRenderer>().sprite;
            CP.SelectedItem = null;
            SelectedTool = CP.SelectedSkill;
            SetupForSelectTarget();
        }
        return ActiveTool;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: TEAMMATES --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectTeammates()
    {
        Selection = Selections.Teammates;
        CommonActionsFrame.transform.GetChild(0).gameObject.SetActive(false);
        CommonActionsFrame.transform.GetChild(1).gameObject.SetActive(true);
        CommonActionsFrame.transform.GetChild(2).gameObject.SetActive(false);
        SelectionFrame.Deactivate();
        ConfirmToolFrame.Deactivate();
        SelectTargetInTeam.SetActive(true);
        int i = 0;
        string[] plc = new string[] { "A", "S", "D", "F" };
        for (; i < CurrentBattle.PlayerParty.Players.Count; i++)
            SetupKeyInputForPlayer(SelectTargetInTeam.transform.GetChild(i).gameObject, CurrentBattle.PlayerParty.Players[i], plc[i]);
        for (; i < CurrentBattle.PlayerParty.MAX_NUMBER_OF_PLAYABLE_BATTLERS; i++)
            SelectTargetInTeam.transform.GetChild(i).gameObject.SetActive(false);
    }

    private void SetupKeyInputForPlayer(GameObject go, BattlePlayer p, string letterCommand)
    {
        go.SetActive(!p.Equals(CP));
        if (!go.activeSelf) return;

        int crsp = GetCutRelationSP(p);
        int netSPConsume = CP.SelectedSkill.SPConsume - crsp;
        if (netSPConsume < 0) netSPConsume = 0;
        Color disabler = new Color(1, 1, 1, netSPConsume > CP.SP ? DISABLED_TRANSPARENCY : 1);

        go.transform.GetChild(0).GetComponent<Image>().sprite = UIMaster.LetterCommands[letterCommand];
        go.transform.GetChild(0).GetComponent<Image>().color = disabler;
        go.transform.GetChild(1).gameObject.SetActive(crsp > 0);
        go.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = CP.SelectedSkill.SPConsume + "SP";
        go.transform.GetChild(2).gameObject.SetActive(crsp > 0);
        go.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "-" + crsp + "SP";
        go.transform.GetChild(3).gameObject.SetActive(true);
        go.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = netSPConsume + " SP";
        go.transform.GetChild(3).GetComponent<TextMeshProUGUI>().color = disabler;
    }

    private int GetCutRelationSP<B>(B player) where B : Battler
    {
        try
        {
            PlayerRelation pcInfo = CurrentBattle.PlayerParty.GetCompanionshipInfo(CP, player as BattlePlayer);
            return pcInfo.Points / 2;
        }
        catch { return 0; }
    }

    private void SelectTeammates()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            HideKeyboardChoiceButtons();
            ConfirmToolMenuIndex = -1;
            SetupForSelectSkill();
            return;
        }
        switch (KeyPressed)
        {
            case "A": ConfirmTeammateSelection(0); break;
            case "S": ConfirmTeammateSelection(1); break;
            case "D": ConfirmTeammateSelection(2); break;
            case "F": ConfirmTeammateSelection(3); break;
        }
    }

    private void ConfirmTeammateSelection(int playerIndex)
    {
        GameObject go = SelectTargetInTeam.transform.GetChild(playerIndex).gameObject;
        if (!go.activeSelf || go.transform.GetChild(3).GetComponent<TextMeshProUGUI>().color.a == DISABLED_TRANSPARENCY) return;
        go.SetActive(false);
        HideKeyboardChoiceButtons();
        SetupForSelectTarget();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: TARGET --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupForSelectTarget()
    {
        Selection = Selections.Targets;
        CommonActionsFrame.transform.GetChild(0).gameObject.SetActive(false);
        CommonActionsFrame.transform.GetChild(1).gameObject.SetActive(true);
        CommonActionsFrame.transform.GetChild(2).gameObject.SetActive(false);
        SelectionFrame.Deactivate();
        ConfirmToolFrame.Deactivate();
        TargetSetupTeamTargets();
        SetupFromScope();
    }

    private void TargetSetupTeamTargets()
    {
        bool selectOneInOwnTeam = false;
        bool selectLotsInOwnTeam = false;
        switch (SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneAlly:
            case ActiveTool.ScopeType.OneKnockedOutAllies:
                SelectTargetInTeam.gameObject.SetActive(true);
                selectOneInOwnTeam = true;
                break;
            case ActiveTool.ScopeType.AllAllies:               // Button only visible if there are allies, otherwise EndDecision() immediately removes the buttons
            case ActiveTool.ScopeType.AllKnockedOutAllies:     // Button only visible if there are allies, otherwise EndDecision() immediately removes the buttons
            case ActiveTool.ScopeType.EveryoneButSelf:
            case ActiveTool.ScopeType.Everyone:
                SelectTargetInTeam.gameObject.SetActive(true);
                selectLotsInOwnTeam = true;
                break;
        }
        if (selectOneInOwnTeam || selectLotsInOwnTeam)
        {
            string[] playerLetterCommands = new string[] { "A", "S", "D", "F" };
            for (int i = 0; i < CurrentBattle.PlayerParty.MAX_NUMBER_OF_PLAYABLE_BATTLERS; i++)
            {
                SelectTargetInTeam.transform.GetChild(i).GetChild(0).GetComponent<Image>().sprite = UIMaster.LetterCommands[selectOneInOwnTeam ? playerLetterCommands[i] : "A"];
                SelectTargetInTeam.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
                SelectTargetInTeam.transform.GetChild(i).GetChild(1).gameObject.SetActive(false);
                SelectTargetInTeam.transform.GetChild(i).GetChild(2).gameObject.SetActive(false);
                SelectTargetInTeam.transform.GetChild(i).GetChild(3).gameObject.SetActive(false);
                SelectTargetInTeam.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        SelectTargetInTeam.transform.GetChild(4).gameObject.SetActive(false);
    }

    private void SetupFromScope()
    {
        CP.SelectedTargets = new List<Battler>();
        int i = 0;
        int i0 = 0;
        string[] allyKeys = new string[] { "Z", "X", "C", "V", "B" };
        switch (SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
            case ActiveTool.ScopeType.OneArea:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                    AddTargetButtonForSpecificEnemy(e, true, e.GenerateDefaultChoiceUI);
                break;

            case ActiveTool.ScopeType.StraightThrough:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                    AddTargetButtonForSpecificEnemy(e, true, e.GenerateRowChoiceUI);
                break;

            case ActiveTool.ScopeType.Widespread:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                    AddTargetButtonForSpecificEnemy(e, true, e.GenerateColumnChoiceUI);
                break;

            case ActiveTool.ScopeType.AllEnemies:
                TargetAllAI(CurrentBattle.EnemyParty.Enemies, true);
                break;

            case ActiveTool.ScopeType.Self:
                CP.SelectedTargets.Add(CP);
                EndDecisions();
                break;

            case ActiveTool.ScopeType.OneAlly:
                if (SelectedTool.RandomTarget)
                {
                    EndDecisions();
                    return;
                }
                foreach (BattlePlayer p in CurrentBattle.PlayerParty.Players) 
                    SelectTargetInTeam.transform.GetChild(i++).gameObject.SetActive(!p.Unconscious);
                foreach (BattleAlly a in CurrentBattle.PlayerParty.Allies)
                    AddTargetButtonForSpecificAlly(a, true, a.GenerateChoiceUI, allyKeys[i0++]);
                break;

            case ActiveTool.ScopeType.OneKnockedOutAllies:
                if (SelectedTool.RandomTarget)
                {
                    EndDecisions();
                    return;
                }
                foreach (BattlePlayer p in CurrentBattle.PlayerParty.Players)
                    SelectTargetInTeam.transform.GetChild(i++).gameObject.SetActive(p.Unconscious);
                foreach (BattleAlly a in CurrentBattle.PlayerParty.Allies)
                    AddTargetButtonForSpecificAlly(a, false, a.GenerateChoiceUI, allyKeys[i0++]);
                break;

            case ActiveTool.ScopeType.AllAllies:
                TargetAllPlayers(CurrentBattle.PlayerParty.Players, true, true);
                if (!TargetAllAI(CurrentBattle.PlayerParty.Allies, true)) EndDecisions();
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                TargetAllPlayers(CurrentBattle.PlayerParty.Players, false, false);
                if (!TargetAllAI(CurrentBattle.PlayerParty.Allies, false)) EndDecisions();
                break;

            case ActiveTool.ScopeType.EveryoneButSelf:
                TargetAllPlayers(CurrentBattle.PlayerParty.Players, true, false);
                TargetAllAI(CurrentBattle.PlayerParty.Allies, true);
                TargetAllAI(CurrentBattle.EnemyParty.Enemies, true);
                break;

            case ActiveTool.ScopeType.Everyone:
                TargetAllPlayers(CurrentBattle.PlayerParty.Players, true, true);
                TargetAllAI(CurrentBattle.PlayerParty.Allies, true);
                TargetAllAI(CurrentBattle.EnemyParty.Enemies, true);
                break;

            default:
                EndDecisions();
                break;
        }
    }

    private delegate void GenerateChoiceUIMode();
    private void AddTargetButtonForSpecificEnemy(BattleEnemy ai, bool mustBeConscious, GenerateChoiceUIMode func)
    {
        if (!ConsciousnessCheck(ai.Unconscious, mustBeConscious)) return;
        else if (SelectedTool.RandomTarget) ai.GenerateChoiceUI("A");
        else func();
    }

    private delegate void GenerateChoiceUIModeArg(string customLetter);
    private void AddTargetButtonForSpecificAlly(BattleAlly ai, bool mustBeConscious, GenerateChoiceUIModeArg func, string argStr)
    {
        if (!ConsciousnessCheck(ai.Unconscious, mustBeConscious)) return;
        else if (SelectedTool.RandomTarget) ai.GenerateChoiceUI("A");
        else func(argStr);
    }

    private void TargetAllPlayers(List<BattlePlayer> pList, bool mustBeConscious, bool includeUser)
    {
        for (int i = 0; i < pList.Count; i++)
        {
            if (!ConsciousnessCheck(pList[i].Unconscious, mustBeConscious)|| pList[i].Equals(CP) && !includeUser) continue;
            CP.SelectedTargets.Add(pList[i]);
            SelectTargetInTeam.transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    private bool TargetAllAI<T>(List<T> aiList, bool mustBeConscious) where T : Battler
    {
        int availableTargets = 0;
        foreach (T ai in aiList)
        {
            if (!ConsciousnessCheck(ai.Unconscious, mustBeConscious)) continue;
            ai.GenerateChoiceUI("A");
            CP.SelectedTargets.Add(ai);
            availableTargets++;
        }
        return availableTargets > 0;
    }

    private bool ConsciousnessCheck(bool IsUnconscious, bool mustBeConscious)
    {
        if (mustBeConscious) return !IsUnconscious;
        else return IsUnconscious;
    }

    //////////////////////////////////////////////////////////////////////////////////////
    /// -- Finsihed setup: User selecting target in run-time --
    //////////////////////////////////////////////////////////////////////////////////////

    private void SelectTarget()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            HideKeyboardChoiceButtons();
            ConfirmToolMenuIndex = -1;
            if (SelectedAction == SelectedActions.Skill) SetupForSelectSkill();
            else if (SelectedAction == SelectedActions.Item) SetupForSelectItem();
            else SetupForSelectAction();
            return;
        }
        if (SelectedTool.RandomTarget)
        {
            if (KeyPressed.Equals("A")) EndDecisions();
            return;
        }
        switch (SelectedTool.Scope)
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
        }
    }

    private void SelectedPlayerOrAllyTarget<T>(List<T> partyList, int index) where T : Battler
    {
        if (index >= partyList.Count) return;
        T pa = partyList[index];
        if (SelectedTool.Scope == ActiveTool.ScopeType.OneAlly && !pa.Unconscious ||
            SelectedTool.Scope == ActiveTool.ScopeType.OneKnockedOutAllies && pa.Unconscious)
        {
            CP.SelectedTargets.Add(pa);
            EndDecisions();
        }
    }

    private void SelectedEnemyTarget(Battler.VerticalPositions vp, Battler.HorizontalPositions hp)
    {
        bool hitOne = false;
        switch (SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
            case ActiveTool.ScopeType.OneArea:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                {
                    if (!e.Selectable() || e.RowPosition != vp || e.ColumnPosition != hp) continue;
                    CP.SelectedTargets.Add(e);
                    EndDecisions();
                }
                break;
            case ActiveTool.ScopeType.StraightThrough:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                {
                    if (!e.Selectable() || e.RowPosition != vp) continue;
                    CP.SelectedTargets.Add(e);
                    hitOne = true;
                }
                if (hitOne) EndDecisions();
                break;
            case ActiveTool.ScopeType.Widespread:
                foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies)
                {
                    if (!e.Selectable() || e.ColumnPosition != hp) continue;
                    CP.SelectedTargets.Add(e);
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
        HideKeyboardChoiceButtons();
        SetSPConsumption();
        if (CurrentPlayer + 1 < CurrentBattle.PlayerParty.Players.Count)
            ShiftPlayer(1);
        else
        {
            HideAll();
            CurrentPlayer = -1;
            CurrentBattle.ExecuteTurn(CurrentBattle.PlayerParty.GetBattlingParty(), CurrentBattle.EnemyParty.ConvertToGeneric());
        }
    }
    
    private void SetSPConsumption()
    {
        CP.SPToConsumeThisTurn = CP.SelectedSkill.SPConsume;
    }

    public void EndTurn()
    {
        for (int i = 0; i < CurrentBattle.PlayerParty.Players.Count; i++)
        {
            GameObject selected = CharacterInfo.transform.GetChild(i).GetChild(5).gameObject;
            selected.GetComponent<Image>().sprite = null;
            selected.SetActive(false);
        }
        CurrentPlayer = 0;
        while (!CurrentBattle.PlayerParty.Players[CurrentPlayer].CanMove())
            CurrentPlayer++;
        Selection = Selections.Actions;
        LoadReady = false;
        WaitTimeBeforeTurnStarts = Time.time + 1f;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Common action helpers --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void ShiftPlayer(int nextPlayer)
    {
        CurrentPlayer += nextPlayer;
        while (!CurrentBattle.PlayerParty.Players[CurrentPlayer].CanMove())
            CurrentPlayer += nextPlayer;
        Selection = Selections.Actions;
        LoadReady = false;
        CommonActionsFrame.Deactivate();
        SelectionFrame.Deactivate();
        WaitTimeBeforeTurnStarts = Time.time + 0.3f;
    }

    private void HideKeyboardChoiceButtons()
    {
        SelectTargetInTeam.gameObject.SetActive(false);
        for (int i = 0; i < CurrentBattle.PlayerParty.Players.Count; i++) SelectTargetInTeam.transform.GetChild(i).gameObject.SetActive(false);
        foreach (BattleAlly a in CurrentBattle.PlayerParty.Allies) a.RemoveChoiceUI();
        foreach (BattleEnemy e in CurrentBattle.EnemyParty.Enemies) e.RemoveChoiceUI();
    }

    private void HideAll()
    {
        Selection = Selections.Animating;
        SelectedAction = SelectedActions.None;
        CharacterInfo.Deactivate();
        CommonActionsFrame.Deactivate();
        SelectionFrame.Deactivate();
        ConfirmToolFrame.Deactivate();
        SelectTargetInTeam.SetActive(false);
    }

    private Weapon GetNextWeapon()
    {
        int selectedI = 0;
        for (int i = 1; i < CP.Weapons.Count; i++)
            if (CP.Weapons[i].Equals(CP.SelectedWeapon))
                selectedI = i;
        return CP.Weapons[(selectedI + 1) % CP.Weapons.Count];
    }

    private void SetWeaponOnMenuAndCharacter()
    {
        SelectActionList.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = CP.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        CommonActionsFrame.transform.GetChild(2).GetChild(0).GetComponent<Image>().sprite = CP.SelectedWeapon.GetComponent<SpriteRenderer>().sprite;
        CommonActionsFrame.transform.GetChild(2).GetChild(2).gameObject.SetActive(CP.Weapons.Count > 1);
        CommonActionsFrame.transform.GetChild(2).gameObject.SetActive(true);
        // Set weapon on character as well
    }

    private void GrayOutIconSelection(GameObject go, bool condition)
    {
        float a = condition ? DISABLED_TRANSPARENCY : 1;
        if (condition && go.GetComponent<Image>().color.a == DISABLED_TRANSPARENCY) return;
        else if (!condition && go.GetComponent<Image>().color.a != DISABLED_TRANSPARENCY) return;
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
        return go.GetComponent<Image>().color.a == DISABLED_TRANSPARENCY;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Battle phase process: RUN --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SelectRun()
    {
        HideAll();
        SelectedAction = SelectedActions.Run;
        //CurrentBattle.RunAway();
    }

    private void RunFailed()
    {
        Selection = Selections.Animating;
        for (int i = 0; i < CurrentBattle.PlayerParty.Players.Count; i++)
        {
            CurrentBattle.PlayerParty.Players[i].SelectedSkill = null;
            CurrentBattle.PlayerParty.Players[i].SelectedItem = null;
        }
    }
}
