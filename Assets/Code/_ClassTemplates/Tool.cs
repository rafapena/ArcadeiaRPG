using System.Collections.Generic;
using UnityEngine;

public abstract class Tool : BaseObject
{
    public enum ModType { None, Damage, Recover, Drain }

    public enum ScopeType {
        None, OneEnemy, SplashEnemies, OneRow, OneColumn, AllEnemies,
        Self, OneTeammate, OneKnockedOutTeammate, AllTeammates, AllKnockedOutTeammates,
        EveryoneButSelf, Everyone }

    public BattleMaster.ToolTypes Type;
    public BattleMaster.ToolFormulas Formula;
    public float ActionTime;
    public ModType HPModType;
    public ModType SPModType;
    public int HPAmount;
    public int HPPercent;
    public int SPPecent;
    public int HPRecoil;
    public ScopeType Scope;
    public int ConsecutiveActs = 1;
    public bool RandomTarget;
    public BattleMaster.Elements Element;
    public bool AlwaysHits;
    public int Power = 10;
    public int Accuracy = 100;
    public int CritcalRate = 2;
    public int Variance;
    public int Priority;
    public Projectile Projectile;
    public List<BattlerClass> ClassExclusives;
    public StateRate[] ChangedStatesGiveRate;
    public StateRate[] ChangedStatesReceiveRate;

    [HideInInspector] public bool Disabled;
    private int ElementMagnitude;
    private int[] StatesGiveRate;
    private int[] StatesReceiveRate;

    protected void Awake()
    {
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
        float weaponAcc = u.SelectedWeapon != null ? u.SelectedWeapon.Accuracy : 100;
        float toolAcc = Accuracy * weaponAcc / 10000f;
        float result = BattleMaster.BASE_ACCURACY * toolAcc * u.Acc() / t.Eva();
        //Debug.Log("HIT OR MISS => " + toolAcc + " " + u.Acc() + " " + t.Eva() + " => " + result);
        return Chance(result * effectMagnitude);
    }

    public int GetFormulaOutput(Battler u, Battler t, float effectMagnitude = 1.0f)
    {
        float weaponPower = u.SelectedWeapon != null ? u.SelectedWeapon.Power : 5;
        float power = Power * weaponPower / 100f;

        float formulaTotal = 0;
        float offMod = 1.5f;
        float defMod = 1.25f;
        float tecMod = 3;
        float gunOffenceReduce = 4f;
        switch (Formula)
        {
            case BattleMaster.ToolFormulas.PhysicalStandard:
                formulaTotal = offMod * u.Atk() - defMod * t.Def();
                break;
            case BattleMaster.ToolFormulas.MagicalStandard:
                formulaTotal = offMod * u.Map() - defMod * t.Mar();
                break;
            case BattleMaster.ToolFormulas.PhysicalGun:
                float physicalOffense = (u.Atk() + u.Tec() * tecMod) / gunOffenceReduce;
                formulaTotal = offMod * physicalOffense - defMod * t.Def();
                break;
            case BattleMaster.ToolFormulas.MagicalGun:
                float magicalOffense = (u.Map() + u.Tec() * tecMod) / gunOffenceReduce;
                formulaTotal = offMod * magicalOffense  - defMod * t.Mar();
                break;
        }
        return (int)(formulaTotal * power * effectMagnitude);
    }

    // WRITES TO THE USER
    public int GetCriticalHitRatio(Battler u, Battler t, float effectMagnitude = 1.0f)
    {
        float weaponCritRate = u.SelectedWeapon != null ? u.SelectedWeapon.CritcalRate : 100;
        float toolCrt = CritcalRate * weaponCritRate / 10000f;
        float def = t.Tec() * t.Cev();
        float critExponent = 1.1f;
        float result = 2 * Mathf.Pow(u.Tec() * toolCrt, critExponent) * u.Crt() / (def != 0 ? def : 0.01f);
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
        for (int i = 0; i < StatesGiveRate.Length; i++)
        {
            if (StatesGiveRate[i] <= 0) continue;
            float tAttr = (t.StateRates[i] + 100) * u.Luk() / (100 * t.Luk());
            float result = (StatesGiveRate[i] + 100) * tAttr / 100f * effectMagnitude;
            if (Chance((int)result)) stateIds[0].Add(i);
        }
        for (int i = 0; i < StatesReceiveRate.Length; i++)
        {
            if (StatesReceiveRate[i] <= 0) continue;
            float result = (StatesReceiveRate[i] + 100) * (u.StateRates[i] + 100) / 10000f;
            if (Chance((int)result)) stateIds[1].Add(i);
        }
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
        return ClassExclusives.Count == 0 || ClassExclusives.Contains(battler.Class);
    }
}