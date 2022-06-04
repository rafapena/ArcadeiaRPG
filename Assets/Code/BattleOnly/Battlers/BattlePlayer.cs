using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

public class BattlePlayer : Battler
{
    // Equipmet
    public List<Weapon> Weapons;
    public List<IToolEquippable> Equipment => Weapons.Cast<IToolEquippable>().Concat(Accessories.Cast<IToolEquippable>()).ToList();
    public bool MaxEquipment => Weapons.Count + Accessories.Count == BattleMaster.MAX_NUMBER_OF_EQUIPS;

    // Skills
    public bool HasAnySkills => Skills.Count > 0;
    public List<SkillLearnLevel> SkillSet;
    [HideInInspector] public List<Skill> Skills = new List<Skill>();
    [HideInInspector] public bool IsDecidingAction;

    // Stats
    public Stats NaturalStats;
    public List<BattlerClass> ClassSet;
    public int PartnerAccBoostRate = 100;
    public int PartnerCritBoostRate = 100;
    public int SavePartnerRate = 100;
    public int CounterattackRate = 100;
    public int AssistDamageRate = 100;
    public int StatBoostsRate = 100;
    [HideInInspector] public Stats PermanentStatsBoosts;

    private bool ArrowKeyMovement;
    private int CollideCounter;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Start()
    {
        base.Start();
        if (Class) SpriteInfo.WearAttire(Class.Name);
        if (Weapons.Count > 0)
        {
            if (!SelectedWeapon) SelectedWeapon = Weapons[0];
            SpriteInfo.RightArmHold(SelectedWeapon.Name);
        }
        Direction = Vector3.right;
    }

    public override void Setup(PlayerParty party)
    {
        base.Setup(party);
        AddLearnedSkills();
        if (Weapons.Count > 0 && !SelectedWeapon) SelectedWeapon = Weapons[0];
        for (int j = 0; j < Weapons.Count; j++) Weapons[j] = Instantiate(Weapons[j], transform);
    }

    public override void StatConversion()
    {
        base.StatConversion();
        Stats.Add(PermanentStatsBoosts);
    }

    public void AddLearnedSkills()
    {
        IEnumerable<SkillLearnLevel> skills = SkillSet.Concat(Class.SkillSet).OrderBy(x => x.LearnLevel).Where(x => x.LearnLevel <= Level).ToList();
        foreach (SkillLearnLevel sk in skills) Skills.Add(sk.LearnedSkill);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Update()
    {
        if (IsDecidingAction)
        {
            Movement = ArrowKeyMovement ? InputMaster.GetCustomMovementControls(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D) : Vector3.zero;
        }
        base.Update();
    }

    public void EnableArrowKeyMovement() => ArrowKeyMovement = true;

    public void DisableArrowKeyMovement() => ArrowKeyMovement = false;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (IsDecidingAction && collider.gameObject.CompareTag(Battle.SCOPE_HITBOX_TAG))
        {
            CollideCounter++;
            CurrentBattle.Menu.TargetFields.PositionRestrictor.gameObject.SetActive(CollideCounter > 0);
        }
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (IsDecidingAction && collider.gameObject.CompareTag(Battle.SCOPE_HITBOX_TAG))
        {
            CollideCounter--;
            CurrentBattle.Menu.TargetFields.PositionRestrictor.gameObject.SetActive(CollideCounter > 0);
        }
    }

    public void FinalizeDecision()
    {
        DisableArrowKeyMovement();
        Movement = Vector3.zero;
        TurnDestination = Position;
        IsDecidingAction = false;
        SpriteInfo.ActionHitbox.gameObject.SetActive(true);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Class/Equip Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Note: Must handle equipment management elsewhere (e.g. ChangeClass.cs)
    public void ChangeClass(BattlerClass newClass)
    {
        Destroy(Class.gameObject);
        Class = Instantiate(newClass, transform);
        StatConversion();
        SpriteInfo.WearAttire(Class.Name);
        Skills.Clear();
        AddLearnedSkills();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- ActiveTool scoping --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void Select(bool selected)
    {
        base.Select(selected);
        CurrentBattle?.Menu?.SetHighlightSelectedPlayerEntries(this, selected);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ReceiveToolEffects(Battler user, ActiveTool activeTool, Projectile hitProjectile)
    {
        base.ReceiveToolEffects(user, activeTool, hitProjectile);
        if (CurrentBattle?.Menu ?? false) CurrentBattle.Menu.UpdatePlayerEntry(this);
    }

    protected override void Revive()
    {
        base.Revive();
    }

    protected override void GetKOd()
    {
        base.GetKOd();
        StartCoroutine(ApplyKOEffect(CurrentBattle.PlayerKOParticles, 1f, true));
    }
}
