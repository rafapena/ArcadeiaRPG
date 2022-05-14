using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

public abstract class BattlePlayer : Battler
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

    private bool ArrowKeyMovement;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Start()
    {
        base.Start();
        if (Class) Sprite.WearAttire(Class.Name);
        if (Weapons.Count > 0)
        {
            if (!SelectedWeapon) SelectedWeapon = Weapons[0];
            Sprite.RightArmHold(SelectedWeapon.Name);
        }
        Direction = Vector3.right;
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

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Class/Equip Management --
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

    // Note: Must handle equipment management elsewhere (e.g. ChangeClass.cs)
    public void ChangeClass(BattlerClass newClass)
    {
        Skills.Clear();
        foreach (var sk in SkillSet) Skills.Add(sk.LearnedSkill);
        Class = newClass;
        StatConversion();
        Sprite.WearAttire(Class.Name);
        AddLearnedSkills();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Receiving ActiveTool Effects --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void ReceiveToolEffects(Battler user, ActiveTool activeTool, float nerfPartition)
    {
        base.ReceiveToolEffects(user, activeTool, nerfPartition);
        if (CurrentBattle?.BattleMenu ?? false) CurrentBattle.BattleMenu.UpdatePlayerEntry(this);
    }
}
