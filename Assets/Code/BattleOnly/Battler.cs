using System.Collections;
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

public abstract class Battler : DataObject
{
    public Battle CurrentBattle { get; private set; }

    // Movement
    public Vector3 Position => SpriteInfo.BaseHitBox.position;
    [HideInInspector] public Rigidbody2D Figure;
    [HideInInspector] public Vector3 Direction;
    [HideInInspector] public Vector3 Movement;
    [HideInInspector] public float SpriteSpeed;
    [HideInInspector] public Vector3 ScopedTargetDestination;
    [HideInInspector] public Vector3 TurnDestination;
    private bool IsApproachingToTarget;
    private bool IsApproachingForNextTurn;
    private const float BATTLER_APPROACH_DISTANCE_THRESHOLD = 0.3f;
    private const float BATTLER_ESCAPE_SPEED = 0.7f;
    private const float BATTLER_APPROACH_SPEED = 2.5f;

    // Appearance
    public int ColumnOverlapRank { get; private set; }
    public Sprite MainImage;
    public Sprite FaceImage;
    [HideInInspector] public SpriteProperties SpriteInfo;
    private const float SIZE_CORRECTER = 0.7f;

    // Animation management
    public enum AnimParams { Action, Victory, Running, Block, Dodge, DoAction, DoneAction, GetHit, Recovered, KOd }
    private string CurrentAnimStateName;
    private const string BASIC_ATTACK_STATE_PREFIX = "BasicAttack";
    private const string CLASS_SKILL_STATE_PREFIX = "ClassSkill";
    private const string CHARACTER_SKILL_STATE_PREFIX = "CharacterSkill";
    private const string ITEM_STATE_PREFIX = "Item";
    private const int CLASS_PARAM_ACTION = 10;
    private const int CHARACTER_PARAM_ACTION = 20;
    private const int ITEM_PARAM_ACTION = 30;

    // General data
    public int Level = 1;
    public BattlerClass Class;
    public BattleMaster.CombatRangeTypes CombatRangeType;
    [HideInInspector] public Stats StatBoosts;
    public Stats Stats;
    public List<Accessory> Accessories;
    public int HP { get; private set; }
    public int SP { get; private set; }

    // Overall battle info
    [HideInInspector] public bool IsSelected { get; private set; }
    [HideInInspector] public bool LockSelectTrigger;
    [HideInInspector] public ActiveTool SelectedAction;
    [HideInInspector] public Weapon SelectedWeapon;
    [HideInInspector] public List<State> States = new List<State>();
    [HideInInspector] public int CurrentListIndex;
    protected Projectile LastHitProjectile;

    // Action execution info
    public bool UsingBasicAttack => SelectedAction == BasicAttackSkill;
    [HideInInspector] public Skill BasicAttackSkill;
    [HideInInspector] public Battler SingleSelectedTarget;
    [HideInInspector] public bool ExecutedAction;
    [HideInInspector] public bool HitCritical;
    [HideInInspector] public bool HitWeakness;
    [HideInInspector] public bool HitResistant;

    // PassiveEffect dependent info
    public ElementRate[] ChangedElementRates;
    public StateRate[] ChangedStateRates;
    [HideInInspector] public Stats StatModifiers;
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

    public bool KOd => HP <= 0 || Petrified;
    public bool HasLowHP => HP / (float)MaxHP <= LOW_HP_THRESHOLD;
    private float LOW_HP_THRESHOLD = 0.3f;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Awake()
    {
        base.Awake();

        Figure = gameObject.GetComponent<Rigidbody2D>();
        SpriteInfo = transform.GetChild(0)?.GetChild(0)?.GetComponent<SpriteProperties>();
        if (!SpriteInfo) Debug.LogError("Sprite must be set up in the correct hierarchy");

        SetupElementRates();
        SetupStateRates();
        if (CombatRangeType == BattleMaster.CombatRangeTypes.Any)
        {
            if (Class) CombatRangeType = Class.CombatRangeType;
            else Debug.LogError("Battler " + Name + " cannot have 'Any' as their combat range, unless they're in a class");
        }
    }

    protected virtual void Start()
    {
        transform.localScale = Vector3.one * SIZE_CORRECTER;
        SpriteInfo.ActionHitbox.SetBattler(this);
        SpriteInfo.ScopeHitbox.SetBattler(this);
    }

    public virtual void Setup(PlayerParty party = null)
    {
        if (party) Level = party.Level;
        StatConversion();
        for (int j = 0; j < States.Count; j++) States[j] = Instantiate(States[j], transform);

        var skills = gameObject.GetComponentsInChildren<Skill>();
        foreach (var skill in skills) skill.DisableForWarmup();
        if (Class)
        {
            Class = Instantiate(Class, transform);
            var classSkills = Class.gameObject.GetComponentsInChildren<Skill>();
            foreach (var skill in classSkills) skill.DisableForWarmup();
        }

        for (int i = 0; i < States.Count; i++) States[i] = Instantiate(States[i], transform);
        StatBoosts.SetToZero();
    }

    public virtual void StatConversion()
    {
        if (Class) Stats.Set(Class.BaseStats);
        Stats.ConvertFromBaseToActual(Level);
        HP = MaxHP;
        SP = BattleMaster.SP_CAP;
    }

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

    public void ResetAction()
    {
        SelectedAction = null;
        ExecutedAction = false;
        HitCritical = false;
        HitWeakness = false;
        HitResistant = false;
        SingleSelectedTarget = null;
    }

    public void SetBattle(Battle battle)
    {
        CurrentBattle = battle;
    }    

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Updating --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected virtual void Update()
    {
        SpriteInfo.Animation.SetBool(AnimParams.Running.ToString(), Movement != Vector3.zero);
        Figure.velocity = Movement * SpriteSpeed;
    }

    public void SetPosition(Vector3 newPosition)
    {
        var diff = Position - transform.position;
        transform.position = newPosition - diff;
    }

    public void SetColumnOverlapRank(int rank)
    {
        SpriteInfo.MoveForwardInOrder(rank - ColumnOverlapRank);
        ColumnOverlapRank = rank;
    }

    public void Mirror()
    {
        Direction = Direction == Vector3.left ? Vector3.right : Vector3.left;
        var vec = transform.localScale;
        vec.x *= -1;
        transform.localScale = vec;
    }

    public void PrintCurrentAnimationState()
    {
        var animStateList = new string[]
        {
            "Running", "Blocking", "Dodging", "Recovering", "GetHit", "GetKOd", "Idle", "Victory1", "Victory2", "Victory3",
            "BasicAttack", "BasicAttackBlade","BasicAttackHammer", "BasicAttackStaff", "BasicAttackGun", "BasicAttackOther",
            "ClassSkill0", "ClassSkill1", "ClassSkill2", "ClassSkill3", "ClassSkill4",
            "CharacterSkill0", "CharacterSkill1", "CharacterSkill2", "Item0", "Item1", "Item2"
        };
        Debug.Log(Name + " --> ");
        foreach (string s in animStateList)
        {
            if (SpriteInfo.Animation.GetCurrentAnimatorStateInfo(0).IsName(s)) Debug.Log(s.ToUpper());
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Approaching / Escaping --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void Select(bool selected)
    {
        IsSelected = selected;
    }

    public void ApproachTarget(Vector3 leftPoint, Vector3 rightPoint)
    {
        IsApproachingToTarget = true;
        float dist1 = Vector3.Distance(Position, leftPoint);
        float dist2 = Vector3.Distance(Position, rightPoint);
        ScopedTargetDestination = dist1 <= dist2 ? leftPoint : rightPoint;
        Movement = (ScopedTargetDestination - Position).normalized * BATTLER_APPROACH_SPEED;
    }

    public void ApproachForNextTurn()
    {
        if (KOd) return;
        IsApproachingForNextTurn = true;
        Movement = (TurnDestination - Position).normalized * BATTLER_APPROACH_SPEED * 2;
    }

    public void SetToEscapeMode(bool mode, bool mirror)
    {
        if (!CanEscape) return;
        if (mirror) Mirror();
        if (mode) TurnDestination = Position;
        Movement = mode ? Vector3.left * BATTLER_ESCAPE_SPEED : Vector3.zero;
    }

    public bool HasApproachedTarget() => CheckReachedNotifyDestination(ref IsApproachingToTarget, ScopedTargetDestination, SpriteSpeed / 6f);

    public bool HasApproachedNextTurnDestination() => CheckReachedNotifyDestination(ref IsApproachingForNextTurn, TurnDestination, SpriteSpeed / 2f);

    private bool CheckReachedNotifyDestination(ref bool isApproaching, Vector3 destination, float thresholdMod)
    {
        if (KOd || isApproaching && Vector3.Distance(Position, destination) < BATTLER_APPROACH_DISTANCE_THRESHOLD * thresholdMod)
        {
            isApproaching = false;
            SetPosition(destination);
            Movement = Vector3.zero;
            return true;
        }
        return false;
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
        //double willSteal = effectMagnitude * 100 * Rec() / target.Rec();
        //oneActResult.Add(SelectedSkill.Steal && Chance((int)willSteal) ? 1 : 0);
        return oneActResult;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Using ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseBasicAttack(Weapon weapon)
    {
        SpriteInfo.Animation.SetInteger(AnimParams.Action.ToString(), (int)(weapon?.WeaponType ?? 0) + 1);
        CurrentAnimStateName = BASIC_ATTACK_STATE_PREFIX + (weapon?.WeaponType.ToString() ?? string.Empty);
    }

    public void UseSkill(Skill skill)
    {
        skill.StartCharge();
        int paramAction = skill.ClassSkill ? CLASS_PARAM_ACTION : CHARACTER_PARAM_ACTION;
        CurrentAnimStateName = (skill.ClassSkill ? CLASS_SKILL_STATE_PREFIX : CHARACTER_SKILL_STATE_PREFIX) + skill.Id;
        SpriteInfo.Animation.SetInteger(AnimParams.Action.ToString(), paramAction + skill.Id);
    }

    public void UseItem(Item item)
    {
        int mode = (int)item.UseType;
        CurrentAnimStateName = ITEM_STATE_PREFIX + mode;
        SpriteInfo.Animation.SetInteger(AnimParams.Action.ToString(), ITEM_PARAM_ACTION + mode);
    }

    public bool ActionAnimationCompleted()
    {
        var animInfo = SpriteInfo.Animation.GetCurrentAnimatorStateInfo(0);
        if (animInfo.normalizedTime <= 1f || !animInfo.IsName(CurrentAnimStateName)) return false;
        SpriteInfo.Animation.SetBool(AnimParams.Running.ToString(), true);
        SpriteInfo.Animation.SetInteger(AnimParams.Action.ToString(), 0);
        SpriteInfo.Animation.SetTrigger(AnimParams.DoneAction.ToString());
        return true;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void ReceiveToolEffects(Battler user, ActiveTool activeTool, Projectile hitProjectile)
    {
        float effectMagnitude = 1.0f;
        if (activeTool.Hit(user, this, effectMagnitude))
        {
            LastHitProjectile = hitProjectile;

            int formulaOutput = activeTool.GetFormulaOutput(user, this, effectMagnitude);
            int directHPChange = activeTool.HPAmount + (MaxHP * activeTool.HPPercent / 100);
            
            int critRate = activeTool.GetCriticalHitRatio(user, this, effectMagnitude);       // UPDATES HitCritical
            float elementRate = activeTool.GetElementRateRatio(user, this);                   // UPDATES HitWeakness and HitResistant
            int ratesTotal = (int)(critRate * elementRate);

            float nerf = hitProjectile?.NerfPartition ?? 1f;
            int realHPTotal = (int)(activeTool.GetTotalWithVariance((formulaOutput + directHPChange) * ratesTotal) * nerf);
            int realSPTotal = (int)(activeTool.GetTotalWithVariance((formulaOutput + activeTool.SPPecent) * ratesTotal) * nerf);
            if (activeTool.HPModType != ActiveTool.ModType.None && realHPTotal <= 0) realHPTotal = 1;
            if (activeTool.SPModType != ActiveTool.ModType.None && realSPTotal <= 0) realSPTotal = 1;

            //List<int>[] states = ActiveTool.TriggeredStates(user, this, effectMagnitude);
            //oneTarget.Add(states[0].Count);
            //foreach (int stateGiveId in states[0]) oneTarget.Add(stateGiveId);
            //oneTarget.Add(states[1].Count);
            //foreach (int stateReceiveId in states[1]) oneTarget.Add(stateReceiveId);

            SetHPSPAndDisplayPopup(realHPTotal, activeTool.HPModType, "HP", user.AddHP, AddHP);
            SetHPSPAndDisplayPopup(realSPTotal, activeTool.SPModType, "SP", user.AddSP, AddSP);
        }
        else
        {
            Popup popup = Instantiate(UIMaster.Popups["NoHit"], SpriteInfo.TargetPoint, Quaternion.identity);
            popup.GetComponent<TextMesh>().text = "MISS";
        }
    }

    // Helper for function above
    public delegate void SetFunc(int total);
    public void SetHPSPAndDisplayPopup(int total, ActiveTool.ModType modType, string HPorSP, SetFunc setHPorSPForUser, SetFunc setHPorSPForTarget)
    {
        Popup popup = null;
        switch (modType)
        {
            case ActiveTool.ModType.Damage:
                if (SceneMaster.InBattle) popup = Instantiate(UIMaster.Popups[HPorSP + "Damage"], SpriteInfo.TargetPoint, Quaternion.identity);
                setHPorSPForTarget(-total);
                SpriteInfo.Animation?.SetTrigger(AnimParams.GetHit.ToString());
                break;

            case ActiveTool.ModType.Drain:
                if (SceneMaster.InBattle) popup = Instantiate(UIMaster.Popups[HPorSP + "Drain"], SpriteInfo.TargetPoint, Quaternion.identity);
                setHPorSPForTarget(-total);
                setHPorSPForUser(total);
                SpriteInfo.Animation?.SetTrigger(AnimParams.GetHit.ToString());
                break;

            case ActiveTool.ModType.Recover:
                if (SceneMaster.InBattle) popup = Instantiate(UIMaster.Popups[HPorSP + "Recover"], SpriteInfo.TargetPoint, Quaternion.identity);
                setHPorSPForTarget(total);
                SpriteInfo.Animation?.SetTrigger(AnimParams.Recovered.ToString());
                break;
        }
        if (popup) popup.Show(total.ToString());
    }

    protected virtual void Revive()
    {
        //
    }

    protected virtual void GetKOd()
    {
        HP = 0;
        ResetAction();
        SpriteInfo.Animation.SetTrigger(AnimParams.KOd.ToString());
        TurnDestination = Position;
    }

    protected IEnumerator ApplyKOEffect(ParticleSystem ps, float slowdownSeconds, bool knockAway)
    {
        var p = Instantiate(ps, SpriteInfo.ActionEffects);
        Destroy(p.gameObject, p.main.duration * 10);

        Time.timeScale = 0.1f;
        yield return new WaitForSecondsRealtime(slowdownSeconds);
        Time.timeScale = 1;
        
        SpriteInfo.SpritesList.gameObject.SetActive(knockAway);
        SpriteInfo.BaseHitBox.gameObject.SetActive(!knockAway);
        SpriteInfo.ScopeHitbox.gameObject.SetActive(knockAway);
        if (knockAway)
        {
            Movement = Vector3.left * 3;
            yield return new WaitForSeconds(1f);
            Movement = Vector3.zero;
        }
    }

    public virtual void AddHP(int val)
    {
        HP += val;
        if (HP <= 0 && (LastHitProjectile?.Finisher ?? true)) GetKOd();
        else if (HP > MaxHP) HP = MaxHP;
        if (HP <= val && HP > 0 && CurrentBattle.Turn > 0) Revive();
    }

    public virtual void AddSP(int val)
    {
        SP += val;
        if (SP < 0) SP = 0;
        else if (SP > BattleMaster.SP_CAP) SP = BattleMaster.SP_CAP;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Applying Passive Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool CanDoAction => CanEscape && !IsCharging;

    public bool CanEscape => !KOd && CannotMove <= 0;

    public bool IsCharging => ((SelectedAction as Skill)?.ChargeCount ?? 0) > 0;

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

    public int MaxHP => Stats.MaxHP;

    public int Atk => Stats.Atk;
    public int Def => Stats.Def;
    public int Map => Stats.Map;
    public int Mar => Stats.Mar;
    public int Rec => Stats.Rec;
    public int Spd => Stats.Spd;
    public int Tec => Stats.Tec;

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
    public int Rec => NaturalNumber((Stats.Rec + StatBoosts.Rec) * StatModifiers.Rec / 100);

    public int Acc => (Stats.Acc + StatBoosts.Acc) * StatModifiers.Acc / 100;
    public int Eva => (Stats.Eva + StatBoosts.Eva) * StatModifiers.Eva / 100;
    public int Crt => (Stats.Crt + StatBoosts.Crt) * StatModifiers.Crt / 100;
    public int Cev => (Stats.Cev + StatBoosts.Cev) * StatModifiers.Cev / 100;*/

    public int NaturalNumber(int number)
    {
        return number < 0 ? 0 : number;
    }
}
