using UnityEngine;
using System;
using UnityEngine.Events;

public abstract class ToolUser : BaseObject
{
    public enum CombatRangeTypes { Any, Close, Medium, Far }
    public CombatRangeTypes CombatRangeType;

    protected Battle CurrentBattle;
    protected Battler User => CurrentBattle?.ActingBattler;
    protected ActiveTool Action => User?.SelectedAction;
    protected float CurrentActionTimer => User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).normalizedTime;

    private bool UniqueBasicAttackUsed;
    private int CurrentSkillUsed;
    private int CurrentItemModeUsed;
    private UnityAction[] UsingSkillsList;
    private UnityAction[] UsingItemsList;
    protected int ActionSwitch;

    protected override void Awake()
    {
        base.Awake();
        ResetActionExecution();
        UsingSkillsList = new UnityAction[] { UsingSkill_0, UsingSkill_1, UsingSkill_2, UsingSkill_3, UsingSkill_4, UsingSkill_5 };
        UsingItemsList = new UnityAction[] { UsingItem_0, UsingItem_1, UsingItem_2 };
    }

    protected virtual void Update()
    {
        if (UniqueBasicAttackUsed) UsingUniqueBasicAttack();
        else if (CurrentSkillUsed >= 0) UsingSkillsList[CurrentSkillUsed].Invoke();
        else if (CurrentItemModeUsed >= 0) UsingItemsList[CurrentItemModeUsed].Invoke();
    }

    public void SetBattle(Battle battle)
    {
        CurrentBattle = battle;
    }

    public bool NotifyActionCompletion()
    {
        /*Debug.Log(CurrentActionTimer);
        if (User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).IsName("Idle")) Debug.Log("IDLE");
        else if (User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).IsName("Running")) Debug.Log("RUNNING");
        else if (User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).IsName("BasicAttack")) Debug.Log("ATTACK PUNCH");
        else if (User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).IsName("BasicAttackBlade")) Debug.Log("ATTACK SWORD");*/

        if (CurrentActionTimer < 1f) return false;
        ResetActionExecution();
        User.Sprite.Animation.SetBool(Battler.AnimParams.Running.ToString(), true);
        User.Sprite.Animation.SetInteger(Battler.AnimParams.Action.ToString(), 0);
        User.Sprite.Animation.SetTrigger(Battler.AnimParams.DoneAction.ToString());
        return true;
    }

    public virtual void ResetActionExecution()
    {
        ActionSwitch = 0;
        UniqueBasicAttackUsed = false;
        CurrentSkillUsed = -1;
        CurrentItemModeUsed = -1;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Basic attack usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseBasicAttack()
    {
        ResetActionExecution();
        UniqueBasicAttackUsed = true;
        User.Sprite.Animation.SetInteger(Battler.AnimParams.Action.ToString(), 1);
    }

    protected virtual void UsingUniqueBasicAttack() { }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Skill usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseSkill()
    {
        ResetActionExecution();
        CurrentSkillUsed = Action.Id;
        int paramAction = this is BattlerClass ? Battler.CLASS_PARAM_ACTION : Battler.CHARACTER_PARAM_ACTION;
        User.Sprite.Animation.SetInteger(Battler.AnimParams.Action.ToString(), paramAction + Action.Id);
    }

    protected virtual void UsingSkill_0() { }

    protected virtual void UsingSkill_1() { }

    protected virtual void UsingSkill_2() { }

    protected virtual void UsingSkill_3() { }

    protected virtual void UsingSkill_4() { }

    protected virtual void UsingSkill_5() { }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Item usage --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void UseItem(Item selectedItem)
    {
        ResetActionExecution();
        int mode = (int)selectedItem.UseType;
        CurrentItemModeUsed = mode;
        User.Sprite.Animation.SetInteger(Battler.AnimParams.Action.ToString(), Battler.ITEM_PARAM_ACTION + mode);
    }

    protected virtual void UsingItem_0()
    {
        //
    }

    protected virtual void UsingItem_1()
    {
        //
    }

    protected virtual void UsingItem_2()
    {
        //
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Action utilities --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected Projectile SpawnProjectile(Projectile p0, float nerfPartition = 1f)
    {
        Projectile p = Instantiate(p0, User.transform);
        p.transform.position = User.transform.position;
        p.SetBattleInfo(User, Action, nerfPartition);
        p.Direct(User.Direction);
        return p;
    }

    protected void SummonBattler(BattlerAI b)
    {
        if (b is BattleAlly a0)
        {
            var a = CurrentBattle.InstantiateBattler(a0, User.TargetDestination);
            a.StatConversion();
            CurrentBattle.PlayerParty.Allies.Add(a);
            a.IsSummon = true;
        }
        else if (b is BattleEnemy e0)
        {
            var e = CurrentBattle.InstantiateBattler(e0, User.TargetDestination);
            CurrentBattle.EnemyParty.Enemies.Add(e);
            e.IsSummon = true;
        }
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
