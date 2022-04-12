using UnityEngine;
using System;
using UnityEngine.Events;

public abstract class ToolUser : BaseObject
{
    protected Battle CurrentBattle;
    protected Battler User => CurrentBattle.ActingBattler;
    protected ActiveTool Action => CurrentBattle.ActingBattler.SelectedTool;

    private bool UsingUniqueBasicAttack;
    private int CurrentSkillUsed;
    private int CurrentItemModeUsed;
    private UnityAction[] UseSkillsLists;
    private UnityAction[] AnimateSkillsLists;
    private UnityAction[] ItemUseList;
    private UnityAction[] AnimateItemUseList;

    public float CurrentActionTimer => (Time.time - UsingActionStartingTimer) / Action.ActionTime;      // Must always be in range [0.0, 1.0)
    protected float UsingActionStartingTimer;
    protected int ActionSwitch;

    protected override void Awake()
    {
        base.Awake();
        ResetActionExecution();
        UseSkillsLists = new UnityAction[] { UseSkill_0, UseSkill_1, UseSkill_2, UseSkill_3, UseSkill_4, UseSkill_5 };
        AnimateSkillsLists = new UnityAction[] { AnimatingSkill_0, AnimatingSkill_1, AnimatingSkill_2, AnimatingSkill_3, AnimatingSkill_4, AnimatingSkill_5 };
        ItemUseList = new UnityAction[] { ItemUse_0, ItemUse_1, ItemUse_2 };
        AnimateItemUseList = new UnityAction[] { AnimatingItemUse_0, AnimatingItemUse_1, AnimatingItemUse_2 };
    }

    protected virtual void Update()
    {
        if (UsingUniqueBasicAttack)
        {
            AnimateUniqueBasicAttack();
            TryNotifyActionCompletion();
        }
        else if (CurrentSkillUsed >= 0)
        {
            AnimateSkillsLists[CurrentSkillUsed].Invoke();
            TryNotifyActionCompletion();
        }
        else if (CurrentItemModeUsed >= 0)
        {
            AnimateItemUseList[CurrentItemModeUsed].Invoke();
            TryNotifyActionCompletion();
        }
    }

    public void SetBattle(Battle battle)
    {
        CurrentBattle = battle;
    }

    protected void TryNotifyActionCompletion()
    {
        if (CurrentActionTimer >= 1f)
        {
            CurrentBattle.NotifyDoneUsingAction();
            ResetActionExecution();
        }
    }

    public void StartUseTimer()
    {
        UsingActionStartingTimer = Time.time;
        ActionSwitch = 0;
    }

    public virtual void ResetActionExecution()
    {
        UsingUniqueBasicAttack = false;
        CurrentSkillUsed = -1;
        CurrentItemModeUsed = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Basic attack usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseBasicAttack()
    {
        ResetActionExecution();
        StartUseTimer();
        UsingUniqueBasicAttack = true;
        UseUniqueBasicAttack();
    }

    protected virtual void UseUniqueBasicAttack() { }

    protected virtual void AnimateUniqueBasicAttack() { }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Skill usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseSkill()
    {
        ResetActionExecution();
        StartUseTimer();
        CurrentSkillUsed = Action.Id;
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

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Item usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseItem(Item selectedItem)
    {
        ResetActionExecution();
        StartUseTimer();
        CurrentItemModeUsed = (int)selectedItem.UseType;
        ItemUseList[CurrentItemModeUsed].Invoke();
    }

    protected virtual void ItemUse_0()
    {
        //
    }

    protected virtual void ItemUse_1()
    {
        //
    }

    protected virtual void ItemUse_2()
    {
        //
    }

    protected virtual void AnimatingItemUse_0()
    {
        //
    }

    protected virtual void AnimatingItemUse_1()
    {
        //
    }

    protected virtual void AnimatingItemUse_2()
    {
        //
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action utilities --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected Projectile SpawnProjectile(Projectile p0)
    {
        Projectile p = Instantiate(p0, User.transform);
        p.transform.position = User.transform.position;
        p.SetBattleInfo(User, Action);
        p.Direct(Vector3.right);
        return p;
    }

    protected bool PassedTime(float time, int actionSwitchNumber)
    {
        if (ActionSwitch == actionSwitchNumber && CurrentActionTimer > time)
        {
            ActionSwitch++;
            return true;
        }
        return false;
    }
}
