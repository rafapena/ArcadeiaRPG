using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.iOS;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

public abstract class Battler : ToolUser
{
    [SerializeField]
    public BattlerProperties Properties { get; private set; }
    public enum VerticalPositions { Top, Center, Bottom }
    public enum HorizontalPositions { Left, Center, Right }

    // Battler position
    public VerticalPositions RowPosition;
    public HorizontalPositions ColumnPosition;
    public Vector3 ApproachPointLeft => Properties.ApproachPoints.transform.GetChild(0).position;
    public Vector3 ApproachPointRight => Properties.ApproachPoints.transform.GetChild(1).position;
    public Vector3 TargetPoint => Properties.ApproachPoints.transform.GetChild(2).position;

    // Movement
    private Vector3 Position => Properties.BaseHitBox.transform.position;
    [HideInInspector] public Rigidbody2D Figure;
    [HideInInspector] public Vector3 Direction;
    [HideInInspector] public Vector3 Movement;
    [HideInInspector] public float Speed;
    [HideInInspector] public Vector3 ApproachDestination;
    private bool IsApproaching;
    private const float BATTLER_APPROACH_DISTANCE_THRESHOLD = 0.3f;

    // Appearance
    public Sprite MainImage;
    public Sprite FaceImage;
    [HideInInspector] public SpriteAppearance DefaultSprite;

    // General data
    public int Level = 1;
    public BattlerClass Class;
    [HideInInspector] public int HP;
    [HideInInspector] public int SP;
    [HideInInspector] public Stats StatBoosts;
    public Stats Stats;
    public List<Accessory> Accessories;

    // Overall battle info
    [HideInInspector] public bool IsSelected { get; private set; }
    [HideInInspector] public bool LockSelectTrigger;
    [HideInInspector] public ActiveTool SelectedAction;
    [HideInInspector] public Weapon SelectedWeapon;
    [HideInInspector] public bool IsCharging;
    [HideInInspector] public List<State> States = new List<State>();
    private bool BlinkingEnabled;

    // Basic attack
    public bool UsingBasicAttack => SelectedAction == BasicAttackSkill;
    private const string BASIC_ATTACK_FILE_LOCATION = "Prefabs/BasicAttack";
    [HideInInspector] public Skill BasicAttackSkill;

    // Action execution info
    private Vector3 PopupSpawnPoint => Properties.ApproachPoints.transform.GetChild(2).position;
    [HideInInspector] public Battler SelectedSingleMeeleeTarget;
    [HideInInspector] public bool ExecutedAction;
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
        Properties = gameObject.GetComponent<BattlerProperties>();
        Properties.ActionHitBox.SetBattler(this);
        Properties.ScopeHitBox.SetBattler(this);
        Figure = gameObject.GetComponent<Rigidbody2D>();
        BasicAttackSkill = Resources.Load<Skill>(BASIC_ATTACK_FILE_LOCATION);
        MapGameObjectsToHUD();
        SetupElementRates();
        SetupStateRates();
        if (CombatRangeType == CombatRangeTypes.Any)
        {
            if (Class) CombatRangeType = Class.CombatRangeType;
            else Debug.LogError("Battler " + Name + " cannot have 'Any' as their combat range, unless they're in a class");
        }
    }

    protected virtual void Start()
    {
        Speed = 6;
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

    public void SetBattlePositions(VerticalPositions vp, HorizontalPositions hp)
    {
        RowPosition = vp;
        ColumnPosition = hp;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Updating --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Update()
    {
        if (IsApproaching && Vector3.Distance(Position, ApproachDestination) < BATTLER_APPROACH_DISTANCE_THRESHOLD)
        {
            IsApproaching = false;
            SetPosition(ApproachDestination);
            Movement = Vector3.zero;
            CurrentBattle.NotifyToSwitchInBattlePhase();
        }

        if (IsSelected && BlinkingEnabled && GetComponent<SpriteRenderer>())
            GetComponent<SpriteRenderer>().color = Color.Lerp(Color.black, Color.white, Time.time % 1.5f);

        base.Update();
        Figure.velocity = Movement * Speed;
    }

    public void SetPosition(Vector3 newPosition)
    {
        var diff = Position - transform.position;
        transform.position = newPosition - diff;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Appearance --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void SetWeaponAppearance()
    {
        //
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
            float dist = Vector3.Distance(Position, b.Position);
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

    public virtual void Select(bool selected)
    {
        SpriteRenderer sp = GetComponent<SpriteRenderer>();
        if (sp && !selected) sp.color = Color.white;
        IsSelected = selected;
    }

    public Vector3 GetApproachVector(ref Vector3 closestApproachPoint, Vector3 approachPointLeft, Vector3 approachPointRight)
    {
        float dist1 = Vector3.Distance(Position, approachPointLeft);
        float dist2 = Vector3.Distance(Position, approachPointRight);
        closestApproachPoint = dist1 <= dist2 ? approachPointLeft : approachPointRight;
        Vector3 distVector = closestApproachPoint - Position;
        return distVector.normalized * 2f;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Initializers --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ResetAction()
    {
        SelectedAction = null;
        ExecutedAction = false;
        HitCritical = false;
        HitWeakness = false;
        HitResistant = false;
        SelectedSingleMeeleeTarget = null;
    }

    public void ApproachTarget(Vector3 leftPoint, Vector3 rightPoint)
    {
        IsApproaching = true;
        Movement = GetApproachVector(ref ApproachDestination, leftPoint, rightPoint);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Add/Remove Passive Effect Components --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    /*
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

    public bool AimingForTeammates(ActiveTool tool = null)
    {
        switch (tool?.Scope ?? SelectedAction.Scope)
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

    public bool AimingForOnlyKnockedOutTeammates(ActiveTool tool = null)
    {
        return (tool ?? SelectedAction).Scope == ActiveTool.ScopeType.OneKnockedOutAlly || (tool ?? SelectedAction).Scope == ActiveTool.ScopeType.AllKnockedOutAllies;
    }

    public bool AimingForEnemies(ActiveTool tool = null)
    {
        switch (tool?.Scope ?? SelectedAction.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
            case ActiveTool.ScopeType.OneArea:
            case ActiveTool.ScopeType.AllEnemies:
            case ActiveTool.ScopeType.StraightThrough:
                return true;
        }
        return false;
    }

    public bool TryConvertSkillToWeaponSettings()
    {
        if (SelectedWeapon && SelectedAction is Skill sk && sk.WeaponDependent)
        {
            sk.ConvertToWeaponSettings(SelectedWeapon);
            return true;
        }
        return false;
    }

    private List<int> ExecuteSteal(List<int> oneActResult, Battler target, double effectMagnitude)
    {
        //double willSteal = effectMagnitude * 100 * Luk() / target.Luk();
        //oneActResult.Add(SelectedSkill.Steal && Chance((int)willSteal) ? 1 : 0);
        return oneActResult;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ReceiveToolEffects(Battler user, ActiveTool activeTool)
    {
        float effectMagnitude = 1.0f;
        if (activeTool.Hit(user, this, effectMagnitude))
        {
            int formulaOutput = activeTool.GetFormulaOutput(user, this, effectMagnitude);
            int directHPChange = activeTool.HPAmount + (Stats.MaxHP * activeTool.HPPercent / 100);
            
            int critRate = activeTool.GetCriticalHitRatio(user, this, effectMagnitude);       // UPDATES HitCritical
            float elementRate = activeTool.GetElementRateRatio(user, this);                   // UPDATES HitWeakness and HitResistant
            int ratesTotal = (int)(critRate * elementRate);

            int totalHPChange = (formulaOutput + directHPChange) * ratesTotal;
            int totalSPChange = (formulaOutput + activeTool.SPPecent) * ratesTotal;
            int realHPTotal = activeTool.GetTotalWithVariance(totalHPChange);
            int realSPTotal = activeTool.GetTotalWithVariance(totalSPChange);
            if (activeTool.HPModType != ActiveTool.ModType.None && realHPTotal <= 0) realHPTotal = 1;
            if (activeTool.SPModType != ActiveTool.ModType.None && realSPTotal <= 0) realSPTotal = 1;

            //List<int>[] states = ActiveTool.TriggeredStates(user, this, effectMagnitude);
            //oneTarget.Add(states[0].Count);
            //foreach (int stateGiveId in states[0]) oneTarget.Add(stateGiveId);
            //oneTarget.Add(states[1].Count);
            //foreach (int stateReceiveId in states[1]) oneTarget.Add(stateReceiveId);

            ChangeAndDisplayPopup(realHPTotal, activeTool.HPModType, "HP", user.ChangeHP, ChangeHP);
            ChangeAndDisplayPopup(realSPTotal, activeTool.SPModType, "SP", user.ChangeSP, ChangeSP);
            CheckKO();
        }
        else
        {
            Popup popup = Instantiate(UIMaster.Popups["NoHit"], PopupSpawnPoint, Quaternion.identity);
            popup.GetComponent<TextMesh>().text = "MISS";
        }
    }

    // Helper for function above
    public delegate void ChangeFunc(int total);
    public void ChangeAndDisplayPopup(int total, ActiveTool.ModType modType, string HPorSP, ChangeFunc changeHPorSPForUser, ChangeFunc changeHPorSPForTarget)
    {
        Popup popup = null;
        switch (modType)
        {
            case ActiveTool.ModType.None:
                break;
            case ActiveTool.ModType.Damage:
                if (Time.timeScale > 0) popup = Instantiate(UIMaster.Popups[HPorSP + "Damage"], PopupSpawnPoint, Quaternion.identity);
                changeHPorSPForTarget(-total);
                break;
            case ActiveTool.ModType.Drain:
                if (Time.timeScale > 0) popup = Instantiate(UIMaster.Popups[HPorSP + "Drain"], PopupSpawnPoint, Quaternion.identity);
                changeHPorSPForTarget(-total);
                changeHPorSPForUser(total);
                break;
            case ActiveTool.ModType.Recover:
                if (Time.timeScale > 0) popup = Instantiate(UIMaster.Popups[HPorSP + "Recover"], PopupSpawnPoint, Quaternion.identity);
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

    public virtual void MaxHPSP()
    {
        KOd = false;
        HP = Stats.MaxHP;
        SP = 100;
    }

    public virtual void ChangeHP(int val)
    {
        HP += val;
        if (HP < 0) HP = 0;
        else if (HP > Stats.MaxHP) HP = Stats.MaxHP;
    }

    public virtual void ChangeSP(int val)
    {
        SP += val;
        if (SP < 0) SP = 0;
        else if (SP > 100) SP = 100;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Applying Passive Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool CanDoAction => !KOd && CannotMove <= 0 && !IsCharging;

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

    /*public int Atk => NaturalNumber((Stats.Atk + StatBoosts.Atk) * StatModifiers.Atk / 100);
    public int Def => NaturalNumber((Stats.Def + StatBoosts.Def) * StatModifiers.Def / 100);
    public int Map => NaturalNumber((Stats.Map + StatBoosts.Map) * StatModifiers.Map / 100);
    public int Mar => NaturalNumber((Stats.Mar + StatBoosts.Mar) * StatModifiers.Mar / 100);
    public int Spd => NaturalNumber((Stats.Spd + StatBoosts.Spd) * StatModifiers.Spd / 100);
    public int Tec => NaturalNumber((Stats.Tec + StatBoosts.Tec) * StatModifiers.Tec / 100);
    public int Luk => NaturalNumber((Stats.Luk + StatBoosts.Luk) * StatModifiers.Luk / 100);

    public int Acc => (Stats.Acc + StatBoosts.Acc) * StatModifiers.Acc / 100;
    public int Eva => (Stats.Eva + StatBoosts.Eva) * StatModifiers.Eva / 100;
    public int Crt => (Stats.Crt + StatBoosts.Crt) * StatModifiers.Crt / 100;
    public int Cev => (Stats.Cev + StatBoosts.Cev) * StatModifiers.Cev / 100;*/

    public int NaturalNumber(int number)
    {
        return number < 0 ? 0 : number;
    }
}
