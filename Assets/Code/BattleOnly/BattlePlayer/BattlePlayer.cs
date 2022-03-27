using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using UnityEngine;
using UnityEngine.UI;

public class BattlePlayer : Battler
{
    public Sprite FaceImage;
    public Stats NaturalStats;
    public int Companionship = 100;
    public int SavePartnerRate = 100;
    public int CounterattackRate = 100;
    public int AssistDamageRate = 100;
    public List<BattlerClass> ClassSet;
    public List<SkillLearnLevel> SkillSet;
    public PreExistingRelation[] PreExistingRelations;

    [HideInInspector] public Skill AttackSkill;
    [HideInInspector] public List<PlayerRelation> Relations;

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected new void Awake()
    {
        base.Awake();
        AttackSkill = Resources.Load<Skill>("Prefabs/Skills/Attack");
    }

    protected override void Start()
    {
        base.Start();
    }
    
    protected override Skill GetDefaultSkill()
    {
        Skill sk = AttackSkill;
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
        base.Update();
    }

    /*public new void SetAllStats(int level)
    {
        base.SetAllStats(level);
        if (Class != null) Stats = new Stats(level, Class.BaseStats, NaturalStats);
    }

    public void SetCurrentClass(List<BattlerClass> classesData, int classId)
    {
        if (ValidListInput(classesData, classId)) Class = classesData[classId];
    }

    public void SetRelation(int id, int level)
    {
        PlayerCompanionships[id] = 0;
        for (int i = 1; i <= level; i++) PlayerCompanionships[id] += 10 * i;
    }

    public void AddSkillsFromLevel()
    {
        for (int i = 0; i < SkillSet.Count; i++)
        {
            if (Level < SkillSetLevels[i]) continue;
            if (SkillSet[i].NumberOfUsers > 1) ComboSkills.Add(new Skill(SkillSet[i]));
            else Skills.Add(new Skill(SkillSet[i]));
        }
    }*/
}
