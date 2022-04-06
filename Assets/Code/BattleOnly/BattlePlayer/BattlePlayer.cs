using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayer : Battler
{
    public Stats NaturalStats;
    public int Companionship = 100;
    public int SavePartnerRate = 100;
    public int CounterattackRate = 100;
    public int AssistDamageRate = 100;
    public List<BattlerClass> ClassSet;
    public List<SkillLearnLevel> SkillSet;

    [HideInInspector] public Skill BasicAttackSkill;

    public bool UsingBasicAttack => SelectedTool is Skill && SelectedTool.Id == BasicAttackSkill.Id;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected new void Awake()
    {
        base.Awake();
        BasicAttackSkill = Resources.Load<Skill>("Prefabs/Skills/Attack");
    }

    protected override void Start()
    {
        base.Start();
        Speed = 6;
    }
    
    protected override Skill GetDefaultSkill()
    {
        Skill sk = BasicAttackSkill;
        sk.ConvertToWeaponSettings(SelectedWeapon);
        return sk;
    }

    public void SetBattlePositions(VerticalPositions vp, HorizontalPositions hp)
    {
        RowPosition = vp;
        ColumnPosition = hp;
    }

    public override void StatConversion()
    {
        Stats.SetTo(Class.BaseStats);
        Stats.ConvertFromBaseToActual(Level, NaturalStats);
        HP = Stats.MaxHP;
        SP = 100;
    }

    public void AddLearnedSkills()
    {
        IEnumerable<SkillLearnLevel> skills = SkillSet.Concat(Class.SkillSet).OrderBy(x => x.LearnLevel).Where(x => x.LearnLevel <= Level);
        foreach (SkillLearnLevel sk in skills) Skills.Add(sk.LearnedSkill);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Update --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Update()
    {
        Movement = CanMove ? new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector3.zero;
        base.Update();
    }

    // Note: Must handle equipment management elsewhere (e.g. ChangeClass.cs)
    public void ChangeClass(BattlerClass newClass)
    {
        IEnumerable<SkillLearnLevel> toKeep = Class.SkillSet.Where(x => x.Permanent);
        Skills.Clear();
        foreach (SkillLearnLevel sk in toKeep) Skills.Add(sk.LearnedSkill);
        Class = newClass;
        StatConversion();
        AddLearnedSkills();
    }

    public void SetRelation(int id, int level)
    {
        //PlayerCompanionships[id] = 0;
        //for (int i = 1; i <= level; i++) PlayerCompanionships[id] += 10 * i;
    }
}
