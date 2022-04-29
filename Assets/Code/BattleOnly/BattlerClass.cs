using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class BattlerClass : ToolUser
{
    public Stats BaseStats;
    public BattlerClass UpgradedClass1;
    public BattlerClass UpgradedClass2;
    public BattleMaster.WeaponTypes UsableWeapon1Type;
    public BattleMaster.WeaponTypes UsableWeapon2Type;
    public List<SkillLearnLevel> SkillSet;

    private int CurrentBasicAttackWeaponUsed;
    private UnityAction[] UseBasicAttackLists;
    private UnityAction[] AnimateBasicAttackLists;

    public bool IsBaseClass => UpgradedClass1 != null;
    
    public bool IsAdvancedClass => UpgradedClass1 == null && UpgradedClass2 == null;

    protected override void Awake()
    {
        base.Awake();
        if (CombatRangeType == CombatRangeTypes.Any) Debug.LogError("Battler class " + Name + " cannot have 'Any' as their combat range");
        UseBasicAttackLists = new UnityAction[] { UseBasicAttack_Weaponless, UseBasicAttack_Blade, UseBasicAttack_Hammer, UseBasicAttack_Staff, UseBasicAttack_Gun, UseBasicAttack_Tools, UseBasicAttack_Camera };
        AnimateBasicAttackLists = new UnityAction[] { AnimateBasicAttack_Weaponless, AnimateBasicAttack_Blade, AnimateBasicAttack_Hammer, AnimateBasicAttack_Staff, AnimateBasicAttack_Gun, AnimateBasicAttack_Tools, AnimateBasicAttack_Camera };
    }

    protected override void Update()
    {
        if (CurrentBasicAttackWeaponUsed >= 0)
        {
            AnimateBasicAttackLists[CurrentBasicAttackWeaponUsed].Invoke();
            TryNotifyActionCompletion();
        }
        else base.Update();
    }

    public override void ResetActionExecution()
    {
        base.ResetActionExecution();
        CurrentBasicAttackWeaponUsed = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Basic attack usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseBasicAttack(Weapon selectedWeapon)
    {
        ResetActionExecution();
        StartUseTimer();
        int mode = (int)(selectedWeapon?.WeaponType ?? 0);
        UseBasicAttackLists[mode].Invoke();
        CurrentBasicAttackWeaponUsed = mode;
    }

    protected virtual void UseBasicAttack_Weaponless() { }

    protected virtual void UseBasicAttack_Blade() { }

    protected virtual void UseBasicAttack_Hammer() { }

    protected virtual void UseBasicAttack_Staff() { }

    protected virtual void UseBasicAttack_Gun() { }

    protected virtual void UseBasicAttack_Tools() { }

    protected virtual void UseBasicAttack_Camera() { }

    protected virtual void AnimateBasicAttack_Weaponless() { }

    protected virtual void AnimateBasicAttack_Blade() { }

    protected virtual void AnimateBasicAttack_Hammer() { }

    protected virtual void AnimateBasicAttack_Staff() { }

    protected virtual void AnimateBasicAttack_Gun() { }

    protected virtual void AnimateBasicAttack_Tools() { }

    protected virtual void AnimateBasicAttack_Camera() { }
}
