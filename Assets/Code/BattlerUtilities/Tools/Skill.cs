using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Skill : ActiveTool
{
    public Sprite Image;
    public bool Basic;
    public bool ClassSkill;
    public int SPConsume;
    public int Charge;
    public int Warmup;
    public int Cooldown;
    public bool Steal;
    public bool CanUseOutsideOfBattle;
    public bool WeaponDependent;
    public List<BattleMaster.WeaponTypes> WeaponExclusives;

    [HideInInspector] public int ChargeCount;
    [HideInInspector] public int DisabledCount;

    // Shallow copy: Prefab widely changes
    public void ConvertToWeaponSettings(Weapon w)
    {
        if (!w) return;
        Type = w.Type;
        Formula = w.Formula;
        ActionTime = w.ActionTime;
        HPModType = w.HPModType;
        SPModType = w.SPModType;
        HPAmount = w.HPAmount;
        HPPercent = w.HPPercent;
        SPPecent = w.SPPecent;
        HPRecoil = w.HPRecoil;
        Scope = w.Scope;
        Element = w.Element;
        Power = w.Power;
        Range = w.Range;
        Accuracy = w.Accuracy;
        CriticalRateBoost = w.CriticalRateBoost;
        ClassExclusives = w.ClassExclusives;
        StatesGiveRate = w.StatesGiveRate;
        StatesReceiveRate = w.StatesReceiveRate;
    }

    public void DisableForWarmup()
    {
        DisabledCount = Warmup;
    }
    public void DisableForCooldown()
    {
        DisabledCount = Cooldown;
    }
    public void StartCharge()
    {
        ChargeCount = Charge;
    }
    public void Charge1Turn()
    {
        ChargeCount--;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Use conditions --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool DisabledFromWarmupOrCooldown()
    {
        return DisabledCount > 0;
    }

    public bool EnoughSPFrom(Battler battler)
    {
        return battler.SP >= SPConsume;
    }

    public bool CheckExclusiveWeapon(Battler battler)
    {
        return WeaponExclusives.Count == 0 || CheckExclusiveWeapon(battler.SelectedWeapon);
    }

    public bool CheckExclusiveWeapon(Weapon weapon)
    {
        return WeaponExclusives.Contains(weapon.WeaponType);
    }
}
