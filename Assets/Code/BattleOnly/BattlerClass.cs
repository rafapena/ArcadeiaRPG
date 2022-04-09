using System.Collections.Generic;
using UnityEngine;

public class BattlerClass : BaseObject
{
    public Stats BaseStats;
    public BattlerClass UpgradedClass1;
    public BattlerClass UpgradedClass2;
    public BattleMaster.WeaponTypes UsableWeapon1Type;
    public BattleMaster.WeaponTypes UsableWeapon2Type;
    public List<SkillLearnLevel> SkillSet;
    public int AttackTimes;

    public bool IsBaseClass => UpgradedClass1 != null;
    
    public bool IsAdvancedClass => UpgradedClass1 == null && UpgradedClass2 == null;

    private Battler ActingBattler;

    private void Update()
    {
        //
    }
}
