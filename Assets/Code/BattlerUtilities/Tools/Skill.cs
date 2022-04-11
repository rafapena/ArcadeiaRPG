using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Skill : ActiveTool
{
    public bool Basic;
    public int SPConsume;
    public int Charge;
    public int Warmup;
    public int Cooldown;
    public bool Steal;
    public bool CanUseOutsideOfBattle;
    public bool WeaponDependent;
    public List<BattleMaster.WeaponTypes> WeaponExclusives;
    public List<Command> Commands;
    public List<SummonBattler> SummonChances;

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
        CriticalRate = w.CriticalRate;
        Projectile = w.Projectile;
        ClassExclusives = w.ClassExclusives;
        ChangedStatesGiveRate = w.ChangedStatesGiveRate;
        ChangedStatesReceiveRate = w.ChangedStatesReceiveRate;
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

    public List<int> SummonPlayers()
    {
        List<int> summonedIds = new List<int>();
        //for (int i = 0; i < SummonedPlayers.Count; i++) if (Chance(SummonPlayerChances[i])) summonedIds.Add(SummonedPlayers[i].Id);
        return summonedIds;
    }

    public List<int> SummonEnemies()
    {
        List<int> summonedIds = new List<int>();
        //for (int i = 0; i < SummonedEnemies.Count; i++) if (Chance(SummonEnemyChances[i])) summonedIds.Add(SummonedEnemies[i].Id);
        return summonedIds;
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

    public bool UsedByWeaponUser(Battler battler)
    {
        return WeaponExclusives.Count == 0 || WeaponExclusives.Contains(battler.SelectedWeapon.WeaponType);
    }
}
