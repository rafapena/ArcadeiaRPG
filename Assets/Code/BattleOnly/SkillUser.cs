using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class SkillUser : BaseObject
{
    protected Battle CurrentBattle;
    protected Battler User => CurrentBattle.ActingBattler;
    protected ActiveTool Action => CurrentBattle.ActingBattler.SelectedTool;

    private bool UsingUniqueBasicAttack;
    private int CurrentSkillUsed;
    private Action[] UseSkillsLists;
    private Action[] AnimateSkillsLists;

    protected override void Awake()
    {
        base.Awake();
        CurrentSkillUsed = -1;
        UseSkillsLists = new Action[] { UseSkill_0, UseSkill_1, UseSkill_2, UseSkill_3, UseSkill_4, UseSkill_5 };
        AnimateSkillsLists = new Action[] { AnimatingSkill_0, AnimatingSkill_1, AnimatingSkill_2, AnimatingSkill_3, AnimatingSkill_4, AnimatingSkill_5 };
    }

    protected virtual void Update()
    {
        if (UsingUniqueBasicAttack) AnimateUniqueBasicAttack();
        else if (CurrentSkillUsed >= 0) AnimateSkillsLists[CurrentSkillUsed].Invoke();
    }

    public void SetBattle(Battle battle)
    {
        CurrentBattle = battle;
    }

    public virtual void ClearSkillExecution()
    {
        UsingUniqueBasicAttack = false;
        CurrentSkillUsed = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Basic attack usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void UseBasicAttack()
    {
        ClearSkillExecution();
        UsingUniqueBasicAttack = true;
        UseUniqueBasicAttack();
    }

    protected virtual void UseUniqueBasicAttack() { }

    protected virtual void AnimateUniqueBasicAttack() { }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Skill usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseSkill(int skillNumber)
    {
        if (skillNumber < 0 || skillNumber >= UseSkillsLists.Length) return;
        ClearSkillExecution();
        CurrentSkillUsed = skillNumber;
        UseSkillsLists[CurrentSkillUsed].Invoke();
    }

    protected virtual void UseSkill_0() { }

    protected virtual void UseSkill_1() { }

    protected virtual void UseSkill_2() { }

    protected virtual void UseSkill_3() { }

    protected virtual void UseSkill_4() { }

    protected virtual void UseSkill_5() { }

    protected virtual void AnimatingSkill_0() { }

    protected virtual void AnimatingSkill_1() { }

    protected virtual void AnimatingSkill_2() { }

    protected virtual void AnimatingSkill_3() { }

    protected virtual void AnimatingSkill_4() { }

    protected virtual void AnimatingSkill_5() { }
}
