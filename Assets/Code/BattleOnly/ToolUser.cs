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
    protected string CurrentAnimStateName;

    protected override void Awake()
    {
        base.Awake();
        ResetActionExecution();
        UsingSkillsList = new UnityAction[] { UsingSkill_0, UsingSkill_1, UsingSkill_2, UsingSkill_3, UsingSkill_4, UsingSkill_5 };
        UsingItemsList = new UnityAction[] { UsingItem_0, UsingItem_1, UsingItem_2 };
    }

    protected virtual void Update()
    {
        if (UniqueBasicAttackUsed)
        {
            UsingUniqueBasicAttack();
            TryNotifyActionCompletion();
        }
        else if (CurrentSkillUsed >= 0)
        {
            UsingSkillsList[CurrentSkillUsed].Invoke();
            TryNotifyActionCompletion();
        }
        else if (CurrentItemModeUsed >= 0)
        {
            UsingItemsList[CurrentItemModeUsed].Invoke();
            TryNotifyActionCompletion();
        }
    }

    public void SetBattle(Battle battle)
    {
        CurrentBattle = battle;
    }

    public void TryNotifyActionCompletion()
    {
        if (CurrentActionTimer < 1f || !User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).IsName(CurrentAnimStateName)) return;
        ResetActionExecution();
        User.Sprite.Animation.SetBool(Battler.AnimParams.Running.ToString(), true);
        User.Sprite.Animation.SetInteger(Battler.AnimParams.Action.ToString(), 0);
        User.Sprite.Animation.SetTrigger(Battler.AnimParams.DoneAction.ToString());
        StartCoroutine(CurrentBattle.NotifyActionCompletion());
    }

    protected virtual void ResetActionExecution()
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
        CurrentAnimStateName = "BasicAttack";
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
        CurrentAnimStateName = (this is BattlerClass ? "ClassSkill" : "CharacterSkill") + Action.Id;
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
        CurrentAnimStateName = "Item" + mode;
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
        if (ActionSwitch == actionSwitchNumber && CurrentActionTimer > time && User.Sprite.Animation.GetCurrentAnimatorStateInfo(0).IsName(CurrentAnimStateName))
        {
            ActionSwitch++;
            return true;
        }
        return false;
    }
}
