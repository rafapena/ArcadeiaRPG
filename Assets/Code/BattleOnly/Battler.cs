using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.iOS;
using UnityEngine.UI;

public abstract class Battler : SkillUser
{
    [SerializeField]
    public BattlerHUD HUDProperties { get; private set; }
    public enum VerticalPositions { Top, Center, Bottom }
    public enum HorizontalPositions { Left, Center, Right }
    public enum Phases { None, DecidingAction, Charging, PreparingAction, UsingAction, ExecutedAction }

    // Battler position
    public VerticalPositions RowPosition;
    public HorizontalPositions ColumnPosition;

    // Movement
    [HideInInspector] public Rigidbody2D Figure;
    [HideInInspector] public Phases Phase;
    protected Vector3 MainLocation;
    protected Vector3 Movement;
    [HideInInspector] public float Speed;
    private Vector3 ActionPrepDestination;
    private const float BATTLER_APPROACH_DISTANCE_THRESHOLD = 1.25f;

    // General battler data
    public Sprite MainImage;
    public Sprite FaceImage;
    public int Level = 1;
    public BattlerClass Class;
    [HideInInspector] public int HP;
    [HideInInspector] public int SP;
    public Stats Stats;
    [HideInInspector] public Stats StatBoosts;
    [HideInInspector] public List<Skill> Skills;
    public bool HasAnySkills => Skills.Count > 0;

    // Equipment
    public List<Weapon> Weapons;
    public List<Accessory> Accessories;
    public List<IToolEquippable> Equipment => Weapons.Cast<IToolEquippable>().Concat(Accessories.Cast<IToolEquippable>()).ToList();
    public bool MaxEquipment => Weapons.Count + Accessories.Count == BattleMaster.MAX_NUMBER_OF_EQUIPS;
    public bool UsingFists => Weapons.Count == 0;

    // Overall battle info
    [HideInInspector] public bool IsSelected { get; private set; }
    [HideInInspector] public bool LockSelectTrigger;
    [HideInInspector] public ActiveTool SelectedTool;
    [HideInInspector] public Weapon SelectedWeapon;
    [HideInInspector] public List<State> States = new List<State>();
    private bool BlinkingEnabled;

    // Basic attack
    public bool UsingBasicAttack => SelectedTool is Skill && SelectedTool.Id == BasicAttackSkill.Id;
    [HideInInspector] public Skill BasicAttackSkill;

    // Action execution info
    [HideInInspector] public bool HitCritical;
    [HideInInspector] public bool HitWeakness;
    [HideInInspector] public bool HitResistant;

    // PassiveEffect dependent info
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;
    public bool Flying;
    [HideInInspector] public Stats StatModifiers;
    [HideInInspector] public bool KOd;
    [HideInInspector] public bool Petrified;
    [HideInInspector] public int CannotMove;
    [HideInInspector] public int SPConsumeRate;
    [HideInInspector] public int Counter;
    [HideInInspector] public int Reflect;
    [HideInInspector] public List<BattleMaster.ToolTypes> DisabledToolTypes;
    [HideInInspector] public List<int> RemoveByHit = new List<int>();
    [HideInInspector] public List<int> ContactSpread = new List<int>();

    // Size of array == All elements and states, respectively. Takes values from ChangedElementRates and ChangedStateRates
    [HideInInspector] public int[] ElementRates;
    [HideInInspector] public int[] StateRates;


    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Awake()
    {
        base.Awake();
        HUDProperties = gameObject.GetComponent<BattlerHUD>();
        HUDProperties.ActionHitBox.SetBattler(this);
        HUDProperties.ScopeHitBox.SetBattler(this);
        Figure = gameObject.GetComponent<Rigidbody2D>();
        MainLocation = transform.position;
        BasicAttackSkill = ResourcesMaster.Skills[0];
        MapGameObjectsToHUD();
        SetupElementRates();
        SetupStateRates();
    }

    protected virtual void Start()
    {
        //
    }

    protected abstract void MapGameObjectsToHUD();

    public abstract void StatConversion();

    private void SetupElementRates()
    {
        ElementRates = new int[System.Enum.GetNames(typeof(BattleMaster.Elements)).Length];
        for (int i = 0; i < ElementRates.Length; i++) ElementRates[i] = BattleMaster.DEFAULT_RATE;
        foreach (ElementRate er in ChangedElementRates) ElementRates[(int)er.Element] = er.Rate;
    }

    private void SetupStateRates()
    {
        StateRates = new int[ResourcesMaster.States.Length];
        for (int i = 0; i < StateRates.Length; i++) StateRates[i] = BattleMaster.DEFAULT_RATE;
        foreach (StateRate sr in ChangedStateRates) StateRates[sr.State.Id] = sr.Rate;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Updating --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Update()
    {
        if (Phase == Phases.PreparingAction && Vector3.Distance(transform.position, ActionPrepDestination) < BATTLER_APPROACH_DISTANCE_THRESHOLD)
        {
            Movement = Vector3.zero;
            Phase = Phases.UsingAction;
        }

        if (IsSelected && BlinkingEnabled && GetComponent<SpriteRenderer>())
            GetComponent<SpriteRenderer>().color = Color.Lerp(Color.black, Color.white, Time.time % 1.5f);

        base.Update();
        Figure.velocity = Movement * Speed;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- UI --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public T GetNearestTarget<T>(IEnumerable<T> battlers) where T : Battler
    {
        T minB = battlers.First();
        float minDist = float.MaxValue;
        foreach (T b in battlers)
        {
            float dist = Vector3.Distance(transform.position, b.transform.position);
            if (dist >= minDist) continue;
            minB = b;
            minDist = dist;
        }
        return minB;
    }

    public void SetBlinkingBattler(bool blinking)
    {
        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        if (sp && !blinking) sp.color = Color.white;
        BlinkingEnabled = blinking;
    }

    public void Select(bool selected)
    {
        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        if (sp && !selected) sp.color = Color.white;
        IsSelected = selected;
    }

    public Vector3 GetApproachVector(ref Vector3 closestApproachPoint, Vector3 approachPointLeft, Vector3 approachPointRight)
    {
        float dist1 = Vector3.Distance(approachPointLeft, transform.position);
        float dist2 = Vector3.Distance(approachPointRight, transform.position);
        closestApproachPoint = dist1 <= dist2 ? approachPointLeft : approachPointRight;
        Vector3 distVector = closestApproachPoint - transform.position;
        return distVector.normalized * 2f;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Initializers --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ResetAction()
    {
        Phase = Phases.None;
        SelectedTool = null;
        HitCritical = false;
        HitWeakness = false;
        HitResistant = false;
    }

    public void ApproachTarget(Transform leftPoint, Transform rightPoint)
    {
        Movement = GetApproachVector(ref ActionPrepDestination, leftPoint.position, rightPoint.position);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Equip Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public int Equip<T>(T equipment) where T : IToolEquippable
    {
        if (MaxEquipment) return -1;
        else if (equipment is Weapon wp)
        {
            Weapons.Add(wp);
            return Weapons.Count - 1;
        }
        else // equipment is Accessory
        {
            Accessories.Add(equipment as Accessory);
            return Accessories.Count - 1;
        }
    }

    public IToolEquippable Unequip<T>(int index) where T : IToolEquippable
    {
        if (typeof(T).Name.Equals("Weapon") && index >= 0 && index < Weapons.Count)
        {
            Weapon weapon = Weapons[index];
            Weapons.RemoveAt(index);
            return weapon;
        }
        else if (typeof(T).Name.Equals("Accessory") && index >= 0 && index < Accessories.Count)
        {
            Accessory accessory = Accessories[index];
            Accessories.RemoveAt(index);
            return accessory;
        }
        else return default(T);
    }

    public int Unequip<T>(T tool) where T : IToolEquippable
    {
        int index = 0;
        if (tool is Weapon && Weapons.Count > 0)
        {
            index = Weapons.FindIndex(x => x.Id == tool.Info.Id && x.Name.Equals(tool.Info.Name));
            Weapons.RemoveAt(index);
        }
        else if (tool is Accessory && Accessories.Count > 0)
        {
            index = Accessories.FindIndex(x => x.Id == tool.Info.Id && x.Name.Equals(tool.Info.Name));
            Accessories.RemoveAt(index);
        }
        else return -1;
        return index;
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
    /// -- ActiveTool scoping --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected abstract Skill GetDefaultSkill();

    public bool AimingForTeammates()
    {
        switch (SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneAlly:
            case ActiveTool.ScopeType.OneKnockedOutAlly:
            case ActiveTool.ScopeType.AllAllies:
            case ActiveTool.ScopeType.AllKnockedOutAllies:
            case ActiveTool.ScopeType.Self:
                return true;
        }
        return false;
    }

    public bool AimingForEnemies()
    {
        switch (SelectedTool.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
            case ActiveTool.ScopeType.OneArea:
            case ActiveTool.ScopeType.AllEnemies:
            case ActiveTool.ScopeType.StraightThrough:
            case ActiveTool.ScopeType.Widespread:
                return true;
        }
        return false;
    }

    public bool TryConvertSkillToWeaponSettings()
    {
        if (SelectedTool is Skill sk && sk.WeaponDependent)
        {
            sk.ConvertToWeaponSettings(SelectedWeapon);
            return true;
        }
        return false;
    }

    private void ShootProjectile()
    {
        /*foreach (Battler t in SelectedTargets)
        {
            Projectile p = Instantiate(SelectedTool.Projectile, transform.position, Quaternion.identity, gameObject.transform);
            p.Setup(10, GetDirection(transform.position, t.transform.position));
            p.GetBattleInfo(this, SelectedTargets, SelectedTool);
            p.GetComponent<SpriteRenderer>().sortingLayerName = "Battle";
        }*/
    }

    private Vector3 GetDirection(Vector3 userPos, Vector3 targetPos)
    {
        float distX = targetPos.x - userPos.x;
        float distY = targetPos.y - userPos.y;
        float norm = Mathf.Sqrt(distX * distX + distY * distY);
        if (norm == 0) return Vector3.zero;
        return new Vector3(distX / norm, distY / norm);
    }

    private S ExecuteSkill<S>(S skill) where S : Skill
    {
        skill.StartCharge();
        if (skill.ChargeCount > 0)
        {
            Phase = Phases.Charging;
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

    private Item ExecuteItem(Item it)
    {
        Stats.Add(it.PermantentStatChanges);
        //if (it.TurnsInto) Items[Items.FindIndex(x => x.Id == it.Id)] = Instantiate(it.TurnsInto, gameObject.transform);
        //else if (it.Consumable) Items.Remove(it);
        ShootProjectile();
        return it;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public delegate void ApplyExtra(Battler user, ActiveTool ActiveTool, float effectMagnitude);

    public void ReceiveToolEffects(Battler user, ActiveTool ActiveTool, ApplyExtra extraFunc = null)
    {
        float effectMagnitude = 1.0f;
        if (ActiveTool.Hit(user, this, effectMagnitude))
        {
            int formulaOutput = ActiveTool.GetFormulaOutput(user, this, effectMagnitude);
            int directHPChange = ActiveTool.HPAmount + (Stats.MaxHP * ActiveTool.HPPercent / 100);
            
            int critRate = ActiveTool.GetCriticalHitRatio(user, this, effectMagnitude);       // UPDATES HitCritical
            float elementRate = ActiveTool.GetElementRateRatio(user, this);                   // UPDATES HitWeakness and HitResistant
            int ratesTotal = (int)(critRate * elementRate);

            int totalHPChange = (formulaOutput + directHPChange) * ratesTotal;
            int totalSPChange = (formulaOutput + ActiveTool.SPPecent) * ratesTotal;
            int realHPTotal = ActiveTool.GetTotalWithVariance(totalHPChange);
            int realSPTotal = ActiveTool.GetTotalWithVariance(totalSPChange);
            if (ActiveTool.HPModType != ActiveTool.ModType.None && realHPTotal <= 0) realHPTotal = 1;
            if (ActiveTool.SPModType != ActiveTool.ModType.None && realSPTotal <= 0) realSPTotal = 1;

            //List<int>[] states = ActiveTool.TriggeredStates(user, this, effectMagnitude);
            //oneTarget.Add(states[0].Count);
            //foreach (int stateGiveId in states[0]) oneTarget.Add(stateGiveId);
            //oneTarget.Add(states[1].Count);
            //foreach (int stateReceiveId in states[1]) oneTarget.Add(stateReceiveId);
            extraFunc?.Invoke(user, ActiveTool, effectMagnitude);

            ChangeAndDisplayPopup(user, realHPTotal, ActiveTool.HPModType, "HP", user.ChangeHP, ChangeHP);
            ChangeAndDisplayPopup(user, realSPTotal, ActiveTool.SPModType, "SP", user.ChangeSP, ChangeSP);
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
    public void ChangeAndDisplayPopup(Battler user, int total, ActiveTool.ModType modType, string HPorSP, ChangeFunc changeHPorSPForUser, ChangeFunc changeHPorSPForTarget)
    {
        Popup popup = null;
        switch (modType)
        {
            case ActiveTool.ModType.None:
                break;
            case ActiveTool.ModType.Damage:
                if (Time.timeScale > 0) 
                    popup = Instantiate(UIMaster.Popups[HPorSP + "Damage"], transform.position, Quaternion.identity);
                changeHPorSPForTarget(-total);
                break;
            case ActiveTool.ModType.Drain:
                if (Time.timeScale > 0) 
                    popup = Instantiate(UIMaster.Popups[HPorSP + "Drain"], transform.position, Quaternion.identity);
                changeHPorSPForTarget(-total);
                changeHPorSPForUser(total);
                break;
            case ActiveTool.ModType.Recover:
                if (Time.timeScale > 0)
                    popup = Instantiate(UIMaster.Popups[HPorSP + "Recover"], transform.position, Quaternion.identity);
                changeHPorSPForTarget(total);
                break;
        }
        if (popup) popup.GetComponent<TextMesh>().text = total.ToString();
    }

    private void CheckKO()
    {
        KOd = (HP <= 0 || Petrified);
        if (KOd) ResetAction();
        if (GetComponent<SpriteRenderer>()) GetComponent<SpriteRenderer>().enabled = !KOd;
        else if (GetComponent<Animator>()) GetComponent<Animator>().enabled = !KOd;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- General HP/SP Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void MaxHPSP()
    {
        KOd = false;
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

    public bool CanDoAction => !KOd && CannotMove <= 0 && Phase != Phases.Charging;

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

    public int Atk => Stats.Atk;
    public int Def => Stats.Def;
    public int Map => Stats.Map;
    public int Mar => Stats.Mar;
    public int Spd => Stats.Spd;
    public int Tec => Stats.Tec;
    public int Luk => Stats.Luk;

    public int Acc => Stats.Acc;
    public int Eva => Stats.Eva;
    public int Crt => Stats.Crt;
    public int Cev => Stats.Cev;

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
