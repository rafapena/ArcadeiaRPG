﻿using System.Collections.Generic;
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

    // Replace weapon
    protected virtual string ClassModelWeapon { get; set; } = "R_Arm_Item";
    protected const string CLASS_MODELS_LIST_NAME = "ClassModels";
    protected const string CLASS_MODEL_SPRITE_NAME = "Sprite";
    protected Transform CurrentClassModel;
    protected SpriteResolver Resolver;

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
    private BattleMenu BattleMenu;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Start()
    {
        base.Start();
        SetOutfitToClass();
        if (Weapons.Count > 0)
        {
            if (!SelectedWeapon) SelectedWeapon = Weapons[0];
            SetWeaponAppearance();
        }
        Direction = Vector3.right;
    }

    protected override void MapGameObjectsToHUD()
    {
        // StateEffects
    }

    public override void StatConversion()
    {
        Stats.SetTo(Class.BaseStats);
        Stats.ConvertFromBaseToActual(Level, NaturalStats);
        HP = Stats.MaxHP;
        SP = 100;
    }

    public void AddLearnedSkills()
    {
        IEnumerable<SkillLearnLevel> skills = SkillSet.Concat(Class.SkillSet).OrderBy(x => x.LearnLevel).Where(x => x.LearnLevel <= Level).ToList();
        foreach (SkillLearnLevel sk in skills) Skills.Add(sk.LearnedSkill);
    }

    public void SetBattleMenu(BattleMenu bm)
    {
        BattleMenu = bm;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Update()
    {
        if (IsDecidingAction) Movement = ArrowKeyMovement ? InputMaster.GetCustomMovementControls(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D) : Vector3.zero;
        base.Update();
    }

    public void EnableArrowKeyMovement() => ArrowKeyMovement = true;

    public void DisableArrowKeyMovement() => ArrowKeyMovement = false;

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Appearance --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetOutfitToClass()
    {
        if (!Class) return;
        Transform classModels = transform.Find(CLASS_MODELS_LIST_NAME);
        if (!classModels) return;
        foreach (Transform t in classModels) t.gameObject.SetActive(false);
        CurrentClassModel = classModels.Find(Class.Name);
        if (CurrentClassModel)
        {
            CurrentClassModel.gameObject.SetActive(true);
            Resolver = CurrentClassModel.Find(CLASS_MODEL_SPRITE_NAME).Find(ClassModelWeapon).GetComponent<SpriteResolver>();
            Properties.SpriteAppearanceList = CurrentClassModel.transform;
        }
    }

    public override void SetWeaponAppearance()
    {
        if (Resolver) Resolver.SetCategoryAndLabel(ClassModelWeapon, SelectedWeapon.Name);
    }

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
        SetOutfitToClass();
        AddLearnedSkills();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- General HP/SP Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void MaxHPSP()
    {
        base.MaxHPSP();
    }

    public override void ChangeHP(int val)
    {
        base.ChangeHP(val);
    }

    public override void ChangeSP(int val)
    {
        base.ChangeSP(val);
    }
}
