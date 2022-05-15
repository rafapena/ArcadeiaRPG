using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BattlerClass : BaseObject
{
    public BattleMaster.CombatRangeTypes CombatRangeType;
    public Stats BaseStats;
    public BattlerClass UpgradedClass1;
    public BattlerClass UpgradedClass2;
    public BattleMaster.WeaponTypes UsableWeapon1Type;
    public BattleMaster.WeaponTypes UsableWeapon2Type;
    public List<SkillLearnLevel> SkillSet;

    public bool IsBaseClass => UpgradedClass1 != null;
    
    public bool IsAdvancedClass => UpgradedClass1 == null && UpgradedClass2 == null;
}
