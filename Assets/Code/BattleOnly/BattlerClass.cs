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
    private UnityAction[] UsingBasicAttackList;

    public bool IsBaseClass => UpgradedClass1 != null;
    
    public bool IsAdvancedClass => UpgradedClass1 == null && UpgradedClass2 == null;

    protected override void Awake()
    {
        base.Awake();
        if (CombatRangeType == CombatRangeTypes.Any) Debug.LogError("Battler class " + Name + " cannot have 'Any' as their combat range");
        UsingBasicAttackList = new UnityAction[] { UsingBasicAttack_Weaponless, UsingBasicAttack_Blade, UsingBasicAttack_Hammer, UsingBasicAttack_Staff, UsingBasicAttack_Gun, UsingBasicAttack_Tools, UsingBasicAttack_Camera };
    }

    protected override void Update()
    {
        if (CurrentBasicAttackWeaponUsed >= 0)
        {
            UsingBasicAttackList[CurrentBasicAttackWeaponUsed].Invoke();
            TryNotifyActionCompletion();
        }
        else base.Update();
    }

    protected override void ResetActionExecution()
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
        int mode = (int)(selectedWeapon?.WeaponType ?? 0);
        CurrentBasicAttackWeaponUsed = mode;
        User.Sprite.Animation.SetInteger(Battler.AnimParams.Action.ToString(), mode + 1);

        string[] modes = { "", "Blade", "Hammer", "Staff", "Gun", "Other", "Other", "Other", "Other" };
        CurrentAnimStateName = "BasicAttack" + modes[mode];
    }

    protected virtual void UsingBasicAttack_Weaponless() { }

    protected virtual void UsingBasicAttack_Blade() { }

    protected virtual void UsingBasicAttack_Hammer() { }

    protected virtual void UsingBasicAttack_Staff() { }

    protected virtual void UsingBasicAttack_Gun() { }

    protected virtual void UsingBasicAttack_Tools() { }

    protected virtual void UsingBasicAttack_Camera() { }
}
