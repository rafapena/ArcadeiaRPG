using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class ActiveTool : DataObject
{
    public enum ModType { None, Damage, Recover, Drain }

    public enum RangeTypes { None, Meelee, Ranged, Anywhere }

    public enum ScopeType
    {
        None, OneEnemy, OneArea, WideFrontal, StraightThrough, AllEnemies,
        Self, OneAlly, OneKnockedOutAlly, AllAllies, AllKnockedOutAllies,
        TrapSetup, Planting,
        EveryoneButSelf, Everyone
    }

    public BattleMaster.ToolTypes Type;
    public BattleMaster.ToolFormulas Formula;
    public ModType HPModType;
    public ModType SPModType;
    public int HPAmount;
    public int HPPercent;
    public int SPPecent;
    public int HPRecoil;
    public ScopeType Scope;
    public RangeTypes Range;
    public BattleMaster.Elements Element;
    public bool AlwaysHits;
    public int Power = 10;
    public int CriticalRateBoost = 0;
    public int Accuracy = 100;
    public int Variance;
    public List<BattlerClass> ClassExclusives;
    public StateRate[] StatesGiveRate;
    public StateRate[] StatesReceiveRate;

    public bool Ranged => Range == RangeTypes.Ranged || Range == RangeTypes.Anywhere;

    protected override void Awake()
    {
        base.Awake();
        // Set up states give and receive rates
    }

    public bool IsOffense => Type == BattleMaster.ToolTypes.PhysicalOffense || Type == BattleMaster.ToolTypes.MagicalOffense || Type == BattleMaster.ToolTypes.GeneralOffense;

    public bool IsDefense() => Type == BattleMaster.ToolTypes.PhysicalDefense || Type == BattleMaster.ToolTypes.MagicalDefense || Type == BattleMaster.ToolTypes.GeneralDefense;

    public bool Hit(Battler u, Battler t)
    {
        if (AlwaysHits) return true;
        float weaponAcc = u.SelectedWeapon?.Accuracy ?? 100;
        float result = Accuracy * weaponAcc / 100f;
        return Chance((int)result);
    }

    public int GetFormulaOutput(Battler u, Battler t)
    {
        float power = Power * (u.SelectedWeapon?.Power ?? 5) / 100f;
        float formulaTotal = 0;
        float offMod = 2f;
        float defMod = 1f;
        float tecMod = 3;
        float gunOffenseReduce = 4f;
        switch (Formula)
        {
            case BattleMaster.ToolFormulas.PhysicalStandard: formulaTotal = offMod * u.Atk - defMod * t.Def; break;
            case BattleMaster.ToolFormulas.MagicalStandard:  formulaTotal = offMod * u.Map - defMod * t.Mar; break;
            case BattleMaster.ToolFormulas.GunStandard:      formulaTotal = offMod * ((u.Atk + u.Tec * tecMod) / gunOffenseReduce) - defMod * t.Def; break;
        }
        return (int)(formulaTotal * power);
    }

    public int GetCriticalHitRatio(Battler u, Battler t)
    {
        float weaponCritRate = u.SelectedWeapon != null ? u.SelectedWeapon.CriticalRateBoost : 100;
        float toolCrt = CriticalRateBoost * weaponCritRate / 10000f;
        float def = t.Tec * t.Cev;
        float critExponent = 1.1f;
        float result = 2 * Mathf.Pow(u.Tec * toolCrt, critExponent) * u.Crt / (def != 0 ? def : 0.01f);
        int cRate = Chance((int)result) ? 3 : 1;
        return cRate;
    }

    public float GetElementRateRatio(Battler u, Battler t)
    {
        return t.ElementRates[(int)Element] / 100f;
    }

    public int GetTotalWithVariance(int total)
    {
        float variance = total * (Variance / 100f);
        return (int)(total + Random.Range(-variance, variance));
    }

    public static bool Chance(int chance) => Random.Range(0, 100) < chance;

    public virtual void ApplyActionEndEffects()
    {
        // Add receive state rates
        // Add recoil damage
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Use conditions --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool UsedByClassUser(Battler battler)
    {
        return !battler.Class || ClassExclusives.Count == 0 || battler.Class && ClassExclusives.Contains(battler.Class);
    }

    public bool AvailableTeammateTargets(IEnumerable<Battler> battlersGroup)
    {
        switch (Scope)
        {
            case ScopeType.OneKnockedOutAlly:
            case ScopeType.AllKnockedOutAllies:
                return battlersGroup.Where(x => x.KOd).Any();
            case ScopeType.EveryoneButSelf:
                return battlersGroup.Where(x => !x.KOd).Any();
        }
        return true;
    }
}