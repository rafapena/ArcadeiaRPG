using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class ActiveTool : BaseObject
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

    public bool IsOffense()
    {
        return Type == BattleMaster.ToolTypes.PhysicalOffense || Type == BattleMaster.ToolTypes.MagicalOffense || Type == BattleMaster.ToolTypes.GeneralOffense;
    }

    public bool IsDefense()
    {
        return Type == BattleMaster.ToolTypes.PhysicalDefense || Type == BattleMaster.ToolTypes.MagicalDefense || Type == BattleMaster.ToolTypes.GeneralDefense;
    }

    public bool Hit(Battler u, Battler t, float effectMagnitude = 1.0f)
    {
        if (AlwaysHits) return true;
        float weaponAcc = u.SelectedWeapon?.Accuracy ?? 100;
        float toolAcc = Accuracy * weaponAcc / 100f;
        
        float statsAcc = u.Tec / t.Spd;
        float result = toolAcc;// * statsAcc * u.Acc / t.Eva;
        return Chance(result * effectMagnitude);
    }

    public int GetFormulaOutput(Battler u, Battler t, float effectMagnitude = 1.0f)
    {
        float power = Power * (u.SelectedWeapon?.Power ?? 5) / 100f;

        float formulaTotal = 0;
        float offMod = 2f;
        float defMod = 1f;
        float tecMod = 3;
        float gunOffenseReduce = 4f;
        switch (Formula)
        {
            case BattleMaster.ToolFormulas.PhysicalStandard:
                formulaTotal = offMod * u.Atk - defMod * t.Def;
                break;
            case BattleMaster.ToolFormulas.MagicalStandard:
                formulaTotal = offMod * u.Map - defMod * t.Mar;
                break;
            case BattleMaster.ToolFormulas.PhysicalGun:
                float physicalOffense = (u.Atk + u.Tec * tecMod) / gunOffenseReduce;
                formulaTotal = offMod * physicalOffense - defMod * t.Def;
                break;
            case BattleMaster.ToolFormulas.MagicalGun:
                float magicalOffense = (u.Map + u.Tec * tecMod) / gunOffenseReduce;
                formulaTotal = offMod * magicalOffense  - defMod * t.Mar;
                break;
        }
        return (int)(formulaTotal * power * effectMagnitude);
    }

    // WRITES TO THE USER
    public int GetCriticalHitRatio(Battler u, Battler t, float effectMagnitude = 1.0f)
    {
        float weaponCritRate = u.SelectedWeapon != null ? u.SelectedWeapon.CriticalRateBoost : 100;
        float toolCrt = CriticalRateBoost * weaponCritRate / 10000f;
        float def = t.Tec * t.Cev;
        float critExponent = 1.1f;
        float result = 2 * Mathf.Pow(u.Tec * toolCrt, critExponent) * u.Crt / (def != 0 ? def : 0.01f);
        int cRate = Chance(result * effectMagnitude) ? 3 : 1;
        u.HitCritical = (cRate == 3);
        return cRate;
    }

    // WRITES TO THE THE USER
    public float GetElementRateRatio(Battler u, Battler t)
    {
        int eRate = t.ElementRates[(int)Element];
        u.HitWeakness = eRate > 120;
        u.HitWeakness = eRate < 80;
        return eRate / 100f;
    }

    public int GetTotalWithVariance(int total)
    {
        float variance = total * (Variance / 100f);
        return (int)(total + Random.Range(-variance, variance));
    }

    public List<int>[] TriggeredStates(Battler u, Battler t, float effectMagnitude = 1.0f)
    {
        List<int>[] stateIds = new List<int>[] { new List<int>(), new List<int>() };
        /*for (int i = 0; i < StatesGiveRate.Length; i++)
        {
            if (StatesGiveRate[i] <= 0) continue;
            float tAttr = (t.StateRates[i] + 100) * u.Rec() / (100 * t.Rec());
            float result = (StatesGiveRate[i] + 100) * tAttr / 100f * effectMagnitude;
            if (Chance((int)result)) stateIds[0].Add(i);
        }
        for (int i = 0; i < StatesReceiveRate.Length; i++)
        {
            if (StatesReceiveRate[i] <= 0) continue;
            float result = (StatesReceiveRate[i] + 100) * (u.StateRates[i] + 100) / 10000f;
            if (Chance((int)result)) stateIds[1].Add(i);
        }*/
        return stateIds;
    }

    private bool Chance(float chance)
    {
        return Random.Range(0f, 99.99f) < chance;
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