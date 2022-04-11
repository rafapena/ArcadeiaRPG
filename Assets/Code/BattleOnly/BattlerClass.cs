using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BattlerClass : SkillUser
{
    public Stats BaseStats;
    public BattlerClass UpgradedClass1;
    public BattlerClass UpgradedClass2;
    public BattleMaster.WeaponTypes UsableWeapon1Type;
    public BattleMaster.WeaponTypes UsableWeapon2Type;
    public List<SkillLearnLevel> SkillSet;

    private int CurrentBasicAttackWeaponUsed;
    private Action[] UseBasicAttackLists;
    private Action[] AnimateBasicAttackLists;

    public bool IsBaseClass => UpgradedClass1 != null;
    
    public bool IsAdvancedClass => UpgradedClass1 == null && UpgradedClass2 == null;

    protected override void Awake()
    {
        base.Awake();
        CurrentBasicAttackWeaponUsed = -1;
        UseBasicAttackLists = new Action[] { UseBasicAttack_Weaponless, UseBasicAttack_Blade, UseBasicAttack_Hammer, UseBasicAttack_Charm, UseBasicAttack_Gun, UseBasicAttack_Tools, UseBasicAttack_Camera };
        AnimateBasicAttackLists = new Action[] { AnimateBasicAttack_Weaponless, AnimateBasicAttack_Blade, AnimateBasicAttack_Hammer, AnimateBasicAttack_Charm, AnimateBasicAttack_Gun, AnimateBasicAttack_Tools, AnimateBasicAttack_Camera };
    }

    protected override void Update()
    {
        if (CurrentBasicAttackWeaponUsed >= 0) AnimateBasicAttackLists[CurrentBasicAttackWeaponUsed].Invoke();
        else base.Update();
    }

    public override void ClearSkillExecution()
    {
        base.ClearSkillExecution();
        CurrentBasicAttackWeaponUsed = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Basic attack usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void UseBasicAttack(Weapon selectedWeapon)
    {
        ClearSkillExecution();
        CurrentBasicAttackWeaponUsed = selectedWeapon ? (int)selectedWeapon.WeaponType : 0;
        UseBasicAttackLists[CurrentBasicAttackWeaponUsed].Invoke();
    }

    protected virtual void UseBasicAttack_Weaponless() { }

    protected virtual void UseBasicAttack_Blade() { }

    protected virtual void UseBasicAttack_Hammer() { }

    protected virtual void UseBasicAttack_Charm() { }

    protected virtual void UseBasicAttack_Gun() { }

    protected virtual void UseBasicAttack_Tools() { }

    protected virtual void UseBasicAttack_Camera() { }

    protected virtual void AnimateBasicAttack_Weaponless() { }

    protected virtual void AnimateBasicAttack_Blade() { }

    protected virtual void AnimateBasicAttack_Hammer() { }

    protected virtual void AnimateBasicAttack_Charm() { }

    protected virtual void AnimateBasicAttack_Gun() { }

    protected virtual void AnimateBasicAttack_Tools() { }

    protected virtual void AnimateBasicAttack_Camera() { }
}
