using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.iOS;
using UnityEngine.UI;

public abstract class Battler : BaseObject
{
    [HideInInspector] protected Transform Properties;
    public enum VerticalPositions { Top, Center, Bottom }
    public enum HorizontalPositions { Left, Center, Right }

    // Battler position
    public VerticalPositions RowPosition;
    public HorizontalPositions ColumnPosition;

    public Rigidbody2D Figure;

    // General battler data
    public Sprite MainImage;
    public int Level = 1;
    public BattlerClass Class;
    [HideInInspector] public int HP;
    [HideInInspector] public int SP;
    public Stats Stats;
    [HideInInspector] public Stats StatBoosts;
    [HideInInspector] public List<Skill> Skills;
    public List<Weapon> Weapons;

    // Overall battle info
    [HideInInspector] public Tool SelectedToolMove { get; private set; }
    [HideInInspector] public Skill SelectedSkill;
    [HideInInspector] public Weapon SelectedWeapon;
    [HideInInspector] public Item SelectedItem;
    [HideInInspector] public List<Battler> SelectedTeamSkillPartners;
    [HideInInspector] public List<Battler> SelectedTargets;
    [HideInInspector] public List<State> States;

    // Action execution info
    [HideInInspector] public bool ExecutedAction;
    [HideInInspector] public bool HitCritical;
    [HideInInspector] public bool HitWeakness;
    [HideInInspector] public bool HitResistant;

    // PassiveEffect dependent info
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;
    public int ComboDifficulty = 100;
    public bool Flying;
    [HideInInspector] public Stats StatModifiers;
    [HideInInspector] public bool Unconscious;
    [HideInInspector] public bool Petrified;
    [HideInInspector] public int CannotMove;
    [HideInInspector] public int SPConsumeRate;
    [HideInInspector] public int Counter;
    [HideInInspector] public int Reflect;
    [HideInInspector] public List<BattleMaster.ToolTypes> DisabledToolTypes;
    [HideInInspector] public List<int> RemoveByHit;
    [HideInInspector] public List<int> ContactSpread;


    // Size of array == All elements and states, respectively. Takes values from ChangedElementRates and ChangedStateRates
    private string[][] DefaultChoiceButtonLetters;
    [HideInInspector] public string TargetLetterCommand;
    [HideInInspector] public int[] ElementRates;
    [HideInInspector] public int[] StateRates;

    [HideInInspector] protected Vector3 MainLocation;
    [HideInInspector] public bool Waiting;
    [HideInInspector] public int SPToConsumeThisTurn;
    [HideInInspector] public bool IsCharging;   // Skill has this as well, but is needed to the BattleMenu

    protected override void Awake()
    {
        base.Awake();
        Properties = transform.GetChild(0).transform;
        Figure = gameObject.GetComponent<Rigidbody2D>();
        MainLocation = transform.position;
        SetupElementRates();
    }

    protected virtual void Start()
    {
        DefaultChoiceButtonLetters = new string[][]
        {
            new string[]{ "Q", "W", "E" },
            new string[]{ "A", "S", "D" },
            new string[]{ "Z", "X", "C" }
        };
    }

    public abstract void StatConversion();

    private void SetupElementRates()
    {
        ElementRates = new int[System.Enum.GetNames(typeof(BattleMaster.Elements)).Length];
        for (int i = 0; i < ElementRates.Length; i++) ElementRates[i] = BattleMaster.DEFAULT_RATE;
        foreach (ElementRate er in ChangedElementRates) ElementRates[(int)er.Element] = er.Rate;
    }

    protected virtual void Update()
    {
        //
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- UI --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void GenerateChoiceUI(string button)
    {
        GenerateChoiceUIAUX(button);
    }

    public void GenerateDefaultChoiceUI()
    {
        GenerateChoiceUIAUX(DefaultChoiceButtonLetters[(int)RowPosition][(int)ColumnPosition]);
    }

    public void GenerateColumnChoiceUI()
    {
        GenerateChoiceUIAUX(DefaultChoiceButtonLetters[0][(int)ColumnPosition]);
    }

    public void GenerateRowChoiceUI()
    {
        GenerateChoiceUIAUX(DefaultChoiceButtonLetters[(int)RowPosition][0]);
    }

    public bool Selectable()
    {
        return Properties.gameObject.activeSelf;
    }

    public void RemoveChoiceUI()
    {
        TargetLetterCommand = "";
        Properties.GetChild(0).gameObject.SetActive(false);
        Properties.GetChild(1).gameObject.SetActive(false);
        Properties.GetChild(2).gameObject.SetActive(false);
    }

    private void GenerateChoiceUIAUX(string choiceButtonLetter)
    {
        TargetLetterCommand = choiceButtonLetter;
        Properties.GetChild(0).gameObject.SetActive(true);
        Properties.GetChild(1).gameObject.SetActive(true);
        Properties.GetChild(2).gameObject.SetActive(true);
        Properties.GetChild(0).GetComponent<TextMeshProUGUI>().text = Name;
        Properties.GetChild(1).GetComponent<Image>().sprite = UIMaster.LetterCommands[TargetLetterCommand];
        Properties.GetChild(2).GetComponent<Gauge>().Set(HP, Stats.MaxHP);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Initializers --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ClearTurnChoices()
    {
        SelectedSkill = null;
        SelectedItem = null;
        if (SelectedTeamSkillPartners != null) SelectedTeamSkillPartners.Clear();
        if (SelectedTargets != null) SelectedTargets.Clear();
        HitCritical = false;
        HitWeakness = false;
        HitResistant = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equip Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int EquipWeapon(Weapon weapon)
    {
        if (!weapon) return -1;
        Weapons.Add(weapon);
        return Weapons.Count - 1;
    }

    public Weapon UnequipWeapon(int index)
    {
        if (index < 0 || index >= Weapons.Count) return null;
        Weapon weapon = Weapons[index];
        Weapons.RemoveAt(index);
        return weapon;
    }

    public void ReplaceWeaponWith(Weapon weapon, int index)
    {
        if (index < 0 || index >= Weapons.Count || !weapon) return;
        Weapons[index] = weapon;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Add/Remove Passive Effect Components --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*public int AddPassiveSkill(List<PassiveSkill> pSkillsList, int id)
    {
        if (!ValidListInput(pSkillsList, id)) return -1;
        PassiveSkills.Add(new PassiveSkill(pSkillsList[id]));
        AddPassiveEffects(PassiveSkills.Last());
        return PassiveSkills.Last().Id;
    }

    public int RemovePassiveSkill(int listIndex)
    {
        if (!ValidListInput(PassiveSkills, listIndex)) return -1;
        PassiveSkill toRemove = PassiveSkills[listIndex];
        RemovePassiveEffects(toRemove);
        PassiveSkills.RemoveAt(listIndex);
        return toRemove.Id;
    }

    public int AddState(List<State> statesList, int id)
    {
        if (!ValidListInput(statesList, id)) return -1;
        State toAdd = new State(statesList[id]);
        foreach (State existingState in States)
        {
            if (toAdd.Id != existingState.Id) continue;
            if (existingState.Stack >= existingState.MaxStack) return -1;
            existingState.Stack++;
            existingState.TurnsLeft++;
            AddPassiveEffects(existingState);
            return existingState.Id;
        }
        toAdd.TurnsLeft = toAdd.TurnEnd2 > toAdd.TurnEnd1 ? RandInt(toAdd.TurnEnd1, toAdd.TurnEnd2) : -1;
        if (toAdd.KO || toAdd.Petrify) IsConscious = false;
        if (toAdd.Stun) CannotMove++;
        if (toAdd.ContactSpreadRate > 0) ContactSpread.AddRange(new int[] { toAdd.Id, toAdd.ContactSpreadRate });
        AddPassiveEffects(toAdd);
        States.Add(toAdd);
        return toAdd.Id;
    }
   
    public int RemoveState(int listIndex)
    {
        if (!ValidListInput(States, listIndex)) return -1;
        State toRemove = States[listIndex];
        if (toRemove.KO || toRemove.Petrify) IsConscious = true;
        if (toRemove.Stun) CannotMove--;
        for (int i = 0; i < ContactSpread.Count; i += 2)
            if (toRemove.Id == ContactSpread[i] && toRemove.ContactSpreadRate == ContactSpread[i + 1]) ContactSpread.RemoveRange(i, 2);
        for (int i = 0; i < States[listIndex].Stack; i++) RemovePassiveEffects(toRemove);
        States.RemoveAt(listIndex);
        return toRemove.Id;
    }

    public int AddPassiveEffects(PassiveEffect pe)
    {
        for (int i = 0; i < ElementRates.Length; i++) ElementRates[i] += pe.ElementRates[i];
        for (int i = 0; i < StateRates.Length; i++) StateRates[i] += pe.StateRates[i];
        StatModifiers.Add(pe.StatModifiers);
        SPConsumeRate += pe.SPConsumeRate;
        ComboDifficulty += pe.ComboDifficulty;
        Counter += pe.Counter;
        Reflect += pe.Reflect;
        if (pe.DisabledToolType1 > 0) DisabledToolTypes.Add(pe.DisabledToolType1);
        if (pe.DisabledToolType2 > 0) DisabledToolTypes.Add(pe.DisabledToolType2);
        if (pe.RemoveByHit > 0) RemoveByHit.AddRange(new int[] { pe.Id, pe.RemoveByHit });
        return pe.Id;
    }

    public int RemovePassiveEffects(PassiveEffect pe)
    {
        for (int i = 0; i < ElementRates.Length; i++) ElementRates[i] -= pe.ElementRates[i];
        for (int i = 0; i < StateRates.Length; i++) StateRates[i] -= pe.StateRates[i];
        if (pe.StatModifiers != null) StatModifiers.Subtract(pe.StatModifiers);
        SPConsumeRate -= pe.SPConsumeRate;
        ComboDifficulty -= pe.ComboDifficulty;
        Counter -= pe.Counter;
        Reflect -= pe.Reflect;
        bool d1 = false;
        bool d2 = false;
        for (int i = 0; i < DisabledToolTypes.Count; i += 2)
        {
            if (pe.DisabledToolType1 == DisabledToolTypes[i] && !d1) { DisabledToolTypes.RemoveAt(i); d1 = true; }
            if (pe.DisabledToolType2 == DisabledToolTypes[i] && !d2) { DisabledToolTypes.RemoveAt(i); d2 = true; }
        }
        List<int> rbh = RemoveByHit;
        for (int i = 0; i < rbh.Count; i += 2) if (pe.Id == rbh[i] && pe.RemoveByHit == rbh[i + 1]) rbh.RemoveRange(i, 2);
        return pe.Id;
    }*/

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Tool Pre-Actions --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void RedirectUnconsciousSelectedPartners(List<Battler> usersParty, List<Battler> opponentParty)
    {
        for (int i = SelectedTeamSkillPartners.Count - 1; i >= 0; i--)
        {
            if (SelectedTeamSkillPartners[i].Unconscious)
                SelectedTeamSkillPartners.RemoveAt(i);
        }
        for (int i = 0; i < usersParty.Count; i++)
        {
            Battler p = SelectedTeamSkillPartners[i];
            if (!p.Unconscious && p.GetType().Name == GetType().Name)
                SelectedTeamSkillPartners.Add(p);
        }
        DefaultToAttackRandom();
    }

    protected abstract Skill GetDefaultSkill();

    private void DefaultToAttackRandom()
    {
        ClearTurnChoices();
        SelectedSkill = GetDefaultSkill();
        SelectedToolMove = GetSelectedToolMove();
    }

    private void RedirectConsciousTargets(List<Battler> usersParty, List<Battler> opponentParty)
    {
        for (int i = SelectedTargets.Count - 1; i >= 0; i--)
            if (!SelectedTargets[i].Unconscious)
                SelectedTargets.RemoveAt(i);
        if (SelectedTargets.Count == 0)
            SetupRandomTargets(usersParty, opponentParty);
    }

    private void RedirectUnconsciousTargets(List<Battler> usersParty, List<Battler> opponentParty)
    {
        for (int i = SelectedTargets.Count - 1; i >= 0; i--)
            if (SelectedTargets[i].Unconscious)
                SelectedTargets.RemoveAt(i);
        if (SelectedTargets.Count == 0)
            SetupRandomTargets(usersParty, opponentParty);
    }

    private bool ScopeForUnconsciousTeammates()
    {
        return SelectedToolMove.Scope == Tool.ScopeType.OneKnockedOutTeammate || SelectedToolMove.Scope == Tool.ScopeType.AllKnockedOutTeammates;
    }

    private void SetupRandomTargets(List<Battler> usersParty, List<Battler> opponentParty)
    {
        Battler selected;
        switch (SelectedToolMove.Scope)
        {
            case Tool.ScopeType.OneEnemy:
            case Tool.ScopeType.SplashEnemies:
                do selected = opponentParty[Random.Range(0, opponentParty.Count)];
                while (selected.Unconscious);
                SelectedTargets.Add(selected);
                break;

            case Tool.ScopeType.OneRow:
                do selected = opponentParty[Random.Range(0, opponentParty.Count)];
                while (selected.Unconscious);
                foreach (Battler b in opponentParty)
                    if (!b.Unconscious && b.RowPosition == selected.RowPosition)
                        SelectedTargets.Add(selected);
                break;

            case Tool.ScopeType.OneColumn:
                do selected = opponentParty[Random.Range(0, opponentParty.Count)];
                while (selected.Unconscious);
                foreach (Battler b in opponentParty)
                    if (!b.Unconscious && b.ColumnPosition == selected.ColumnPosition)
                        SelectedTargets.Add(selected);
                break;

            case Tool.ScopeType.OneTeammate:
                do selected = usersParty[Random.Range(0, usersParty.Count)];
                while (selected.Unconscious);
                SelectedTargets.Add(selected);
                break;

            case Tool.ScopeType.OneKnockedOutTeammate:
                int potentialTargets = 0;
                foreach (Battler t in usersParty)
                    if (t.Unconscious)
                        potentialTargets++;
                if (potentialTargets == 0)      // User will skip their turn if everyone in their team is conscious, mid-turn
                    break;
                do selected = usersParty[Random.Range(0, usersParty.Count)];
                while (!selected.Unconscious);
                SelectedTargets.Add(selected);
                break;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Tool Actions --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ExecuteAction(List<Battler> usersParty, List<Battler> opponentParty)
    {
        if (!CanMove()) return;

        SelectedToolMove = GetSelectedToolMove();
        if (!SelectedToolMove) return;

        if (SelectedToolMove.RandomTarget)
        {
            SelectedTargets.Clear();
            SetupRandomTargets(usersParty, opponentParty);
        }
        
        if (ScopeForUnconsciousTeammates()) RedirectConsciousTargets(usersParty, opponentParty);
        else RedirectUnconsciousTargets(usersParty, opponentParty);

        if (SelectedTargets.Count == 0) DefaultToAttackRandom();
        if (SPToConsumeThisTurn > SP) SelectedSkill = GetDefaultSkill();
        
        ExecuteTool();
    }

    public Tool GetSelectedToolMove()
    {
        if (SelectedSkill)
        {
            if (SelectedSkill.WeaponDependent) SelectedSkill.ConvertToWeaponSettings(SelectedWeapon);
            return SelectedSkill;
        }
        if (SelectedItem) return SelectedItem;
        return null;
    }

    private void ShootProjectile()
    {
        foreach (Battler t in SelectedTargets)
        {
            Projectile p = Instantiate(SelectedToolMove.Projectile, transform.position, Quaternion.identity, gameObject.transform);
            p.Setup(10, GetDirection(transform.position, t.transform.position));
            p.GetBattleInfo(this, SelectedTargets, SelectedToolMove);
            p.GetComponent<SpriteRenderer>().sortingLayerName = "Battle";
        }
    }

    private Vector3 GetDirection(Vector3 userPos, Vector3 targetPos)
    {
        float distX = targetPos.x - userPos.x;
        float distY = targetPos.y - userPos.y;
        float norm = Mathf.Sqrt(distX * distX + distY * distY);
        if (norm == 0) return Vector3.zero;
        return new Vector3(distX / norm, distY / norm);
    }

    public void ExecuteTool()
    {
        switch (SelectedToolMove.GetType().Name)
        {
            case "Skill": ExecuteSkill(SelectedSkill); break;
            case "Item": ExecuteItem(); break;
        }
    }

    private S ExecuteSkill<S>(S skill) where S : Skill
    {
        skill.StartCharge();
        IsCharging = (skill.ChargeCount > 0);
        if (skill.ChargeCount > 0)
        {
            skill.Charge1Turn();
            return skill;
        }
        skill.DisableForCooldown();
        //sk.SummonPlayers();
        //sk.SummonEnemies();
        //ApplyToolEffectsPerTarget(sk, ExecuteSteal);
        ShootProjectile();
        return skill;
    }

    private List<int> ExecuteSteal(List<int> oneActResult, Battler target, double effectMagnitude)
    {
        //double willSteal = effectMagnitude * 100 * Luk() / target.Luk();
        //oneActResult.Add(SelectedSkill.Steal && Chance((int)willSteal) ? 1 : 0);
        return oneActResult;
    }

    private Item ExecuteItem()
    {
        Item it = SelectedItem;
        Stats.Add(it.PermantentStatChanges);
        //if (it.TurnsInto) Items[Items.FindIndex(x => x.Id == it.Id)] = Instantiate(it.TurnsInto, gameObject.transform);
        //else if (it.Consumable) Items.Remove(it);
        ShootProjectile();
        return it;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving Tool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public delegate void ApplyExtra(Battler user, Tool tool, float effectMagnitude);

    public void ReceiveToolEffects(Battler user, Tool tool, ApplyExtra extraFunc = null)
    {
        float effectMagnitude = 1.0f;
        if (tool.Hit(user, this, effectMagnitude))
        {
            int formulaOutput = tool.GetFormulaOutput(user, this, effectMagnitude);
            int directHPChange = tool.HPAmount + (Stats.MaxHP * tool.HPPercent / 100);
            
            int critRate = tool.GetCriticalHitRatio(user, this, effectMagnitude);       // UPDATES HitCritical
            float elementRate = tool.GetElementRateRatio(user, this);                   // UPDATES HitWeakness and HitResistant
            int ratesTotal = (int)(critRate * elementRate);

            int totalHPChange = (formulaOutput + directHPChange) * ratesTotal;
            int totalSPChange = (formulaOutput + tool.SPPecent) * ratesTotal;
            int realHPTotal = tool.GetTotalWithVariance(totalHPChange);
            int realSPTotal = tool.GetTotalWithVariance(totalSPChange);
            if (tool.HPModType != Tool.ModType.None && realHPTotal <= 0) realHPTotal = 1;
            if (tool.SPModType != Tool.ModType.None && realSPTotal <= 0) realSPTotal = 1;

            //List<int>[] states = tool.TriggeredStates(user, this, effectMagnitude);
            //oneTarget.Add(states[0].Count);
            //foreach (int stateGiveId in states[0]) oneTarget.Add(stateGiveId);
            //oneTarget.Add(states[1].Count);
            //foreach (int stateReceiveId in states[1]) oneTarget.Add(stateReceiveId);
            extraFunc?.Invoke(user, tool, effectMagnitude);

            ChangeAndDisplayPopup(user, realHPTotal, tool.HPModType, "HP", user.ChangeHP, ChangeHP);
            ChangeAndDisplayPopup(user, realSPTotal, tool.SPModType, "SP", user.ChangeSP, ChangeSP);
            CheckKO();
        }
        else
        {
            Popup popup = Instantiate(UIMaster.Popups["NoHit"], transform.position, Quaternion.identity);
            popup.GetComponent<TextMesh>().text = "MISS";
        }
    }

    // Helper for function above
    public delegate void ChangeFunc(int total);
    public void ChangeAndDisplayPopup(Battler user, int total, Tool.ModType modType, string HPorSP, ChangeFunc changeHPorSPForUser, ChangeFunc changeHPorSPForTarget)
    {
        Popup popup = null;
        switch (modType)
        {
            case Tool.ModType.None:
                break;
            case Tool.ModType.Damage:
                if (Time.timeScale > 0) 
                    popup = Instantiate(UIMaster.Popups[HPorSP + "Damage"], transform.position, Quaternion.identity);
                changeHPorSPForTarget(-total);
                break;
            case Tool.ModType.Drain:
                if (Time.timeScale > 0) 
                    popup = Instantiate(UIMaster.Popups[HPorSP + "Drain"], transform.position, Quaternion.identity);
                changeHPorSPForTarget(-total);
                changeHPorSPForUser(total);
                break;
            case Tool.ModType.Recover:
                if (Time.timeScale > 0)
                    popup = Instantiate(UIMaster.Popups[HPorSP + "Recover"], transform.position, Quaternion.identity);
                changeHPorSPForTarget(total);
                break;
        }
        if (popup) popup.GetComponent<TextMesh>().text = total.ToString();
    }

    private void CheckKO()
    {
        Unconscious = (HP <= 0 || Petrified);
        if (Unconscious) ClearTurnChoices();
        if (GetComponent<SpriteRenderer>()) GetComponent<SpriteRenderer>().enabled = !Unconscious;
        else if (GetComponent<Animator>()) GetComponent<Animator>().enabled = !Unconscious;
        Properties.gameObject.SetActive(!Unconscious);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- General HP/SP Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void MaxHPSP()
    {
        Unconscious = false;
        HP = Stats.MaxHP;
        SP = 100;
    }

    public void ChangeHP(int val)
    {
        HP += val;
        if (HP < 0) HP = 0;
        else if (HP > Stats.MaxHP) HP = Stats.MaxHP;
    }

    public void ChangeSP(int val)
    {
        SP += val;
        if (SP < 0) SP = 0;
        else if (SP > 100) SP = 100;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Applying Passive Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool CanMove()
    {
        return !Unconscious && CannotMove <= 0 && !IsCharging;
    }

    /*
    public void ApplyStartActionEffects(Environment e)
    {
        //foreach (State s in States) if (s.) ;
        //foreach (PassiveSkill p in PassiveSkills) if () ;
    }
    public void ApplyEndActionEffects(Environment e)
    {

    }
    public void ApplyEndTurnEffects(Environment e)
    {

    }

    private void ApplyEffects(Environment e)
    {

    }
    */

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Stat Management: TEMPORARY FUNCTIONS UNTIL ALL STATE MANAGEMENT HAS BEEN IMPLEMENTED --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Atk() { return Stats.Atk; }
    public int Def() { return Stats.Def; }
    public int Map() { return Stats.Map; }
    public int Mar() { return Stats.Mar; }
    public int Spd() { return Stats.Spd; }
    public int Tec() { return Stats.Tec; }
    public int Luk() { return Stats.Luk; }

    public int Acc() { return Stats.Acc; }
    public int Eva() { return Stats.Eva; }
    public int Crt() { return Stats.Crt; }
    public int Cev() { return Stats.Cev; }

    /*public int Atk() { return NaturalNumber((Stats.Atk + StatBoosts.Atk) * StatModifiers.Atk / 100); }
    public int Def() { return NaturalNumber((Stats.Def + StatBoosts.Def) * StatModifiers.Def / 100); }
    public int Map() { return NaturalNumber((Stats.Map + StatBoosts.Map) * StatModifiers.Map / 100); }
    public int Mar() { return NaturalNumber((Stats.Mar + StatBoosts.Mar) * StatModifiers.Mar / 100); }
    public int Spd() { return NaturalNumber((Stats.Spd + StatBoosts.Spd) * StatModifiers.Spd / 100); }
    public int Tec() { return NaturalNumber((Stats.Tec + StatBoosts.Tec) * StatModifiers.Tec / 100); }
    public int Luk() { return NaturalNumber((Stats.Luk + StatBoosts.Luk) * StatModifiers.Luk / 100); }

    public int Acc() { return (Stats.Acc + StatBoosts.Acc) * StatModifiers.Acc / 100; }
    public int Eva() { return (Stats.Eva + StatBoosts.Eva) * StatModifiers.Eva / 100; }
    public int Crt() { return (Stats.Crt + StatBoosts.Crt) * StatModifiers.Crt / 100; }
    public int Cev() { return (Stats.Cev + StatBoosts.Cev) * StatModifiers.Cev / 100; }*/

    public int NaturalNumber(int number)
    {
        return number <= 0 ? 0 : number;
    }
}
