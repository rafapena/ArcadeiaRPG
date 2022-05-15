using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UI;

public abstract class BattlerAI : Battler
{
    [SerializeField] private AIAction[] AIActions;

    protected IEnumerable<AIAction> WeaponOptions;
    protected IEnumerable<AIAction> ActionOptions;
    protected List<Battler> PossibleTargets = new List<Battler>();
    private static AIAction AI_ACTION_NONE = new AIAction() { ClassSkillId = -1 };

    [HideInInspector]
    public bool IsSummon;

    protected override void Awake()
    {
        base.Awake();
        CleanupAIActionList();
        BasicAttackSkill = ActionOptions.Any() ? ActionOptions.Select(x => x.Action as Skill).FirstOrDefault(y => y == ResourcesMaster.BasicAttackSkill) : null;
        SelectedWeapon = WeaponOptions.Any() ? (WeaponOptions.First().Action as Weapon) : null;
    }

    private void CleanupAIActionList()
    {
        for (int i = 0; i < AIActions.Length; i++)
        {
            var a = AIActions[i];
            if (!a.Action && Class) a.Action = Class.SkillSet.Find(x => x.LearnedSkill.Id == a.ClassSkillId).LearnedSkill;
        }
        WeaponOptions = AIActions.Where(x => x.Action is Weapon wp && (!Class || wp.CanEquipWith(Class)));
        ActionOptions = AIActions.Where(x =>
            x.Action is Item it && it.UsedByClassUser(this) ||
            x.Action is Skill sk && sk.UsedByClassUser(this) && (sk.WeaponExclusives.Count == 0 || WeaponOptions.Select(y => y.Action as Weapon).Any(y => (x.Action as Skill).CheckExclusiveWeapon(y))) );
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Deciding action and target --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void MakeDecision<T, U>(IList<T> usersPartyMembers, IList<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        PossibleTargets.Clear();

        SelectedAction = SelectAction(usersPartyMembers, opponentPartyMembers);
        if (!SelectedAction) return;

        SelectedWeapon = SelectWeapon(usersPartyMembers, opponentPartyMembers); 
        TryConvertSkillToWeaponSettings();
        
        SelectTargets(usersPartyMembers, opponentPartyMembers);

        TurnDestination = Position;
    }

    protected ActiveTool SelectAction<T, U>(IList<T> usersPartyMembers, IList<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        IEnumerable<Battler> upm = usersPartyMembers.Cast<Battler>();
        IEnumerable<AIAction> actions = ActionOptions.Where(x =>
            x.Action is Skill sk && sk.AvailableTeammateTargets(upm) && !sk.DisabledFromWarmupOrCooldown() && sk.EnoughSPFrom(this) ||
            x.Action is Item it && it.AvailableTeammateTargets(upm) && this is BattleEnemy e && e.CurrentBattle.EnemyParty.ItemInventory.Find(x => x.Id == it.Id));
        return DecideTool(actions, usersPartyMembers, opponentPartyMembers);
    }

    protected Weapon SelectWeapon<T, U>(IList<T> usersPartyMembers, IList<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        Skill skill = SelectedAction as Skill;
        if (!skill) return SelectedWeapon;
        if (skill.WeaponExclusives.Count == 0) return DecideTool(WeaponOptions, usersPartyMembers, opponentPartyMembers) as Weapon;

        IEnumerable<AIAction> weapons = WeaponOptions.Where(x => skill.CheckExclusiveWeapon(x.Action as Weapon));
        return DecideTool(weapons, usersPartyMembers, opponentPartyMembers) as Weapon;
    }

    private ActiveTool DecideTool<T, U>(IEnumerable<AIAction> actions0, IList<T> usersPartyMembers, IList<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        if (!actions0.Any()) return null;
        List<AIAction> actions = new List<AIAction>();

        // Establish priorties
        foreach (var a in actions0)
        {
            var initialTargetsList = GetTargetsList(usersPartyMembers, opponentPartyMembers, a.Action);
            bool metRequirements = a.MetRequirements(this, initialTargetsList);
            if (!metRequirements) a.SetPossibleTargets(initialTargetsList);
            a.SetPriority(metRequirements ? a.Priority : a.PriorityOnFail);
            actions.Add(a);
        }
        if (actions.Count == 1) return actions[0].Action;;
        actions = actions.OrderByDescending(x => x.CurrentPriority).ToList();

        // Select action based on priorities and probabilities
        int i = 0;
        foreach (var a in actions)
        {
            var a0 = actions[i + 1];
            bool chance = Random.Range(0, 100) < (a.CurrentPriority - a0.CurrentPriority) * 10 + 50;
            AIAction selectedAIAction = a0.Equals(actions.Last()) ? (chance ? a : a0) : (chance ? a : AI_ACTION_NONE);
            if (!selectedAIAction.Unusable())
            {
                if (selectedAIAction.Action is not Weapon || selectedAIAction.Action is Skill sk && sk.WeaponDependent) PossibleTargets = selectedAIAction.PossibleTargets;
                return selectedAIAction.Action;
            }
            else i++;
        }
        return null;
    }

    private IEnumerable<Battler> GetTargetsList<T, U>(IList<T> usersPartyMembers, IList<U> opponentPartyMembers, ActiveTool action) where T : Battler where U : Battler
    {
        if (AimingForOnlyKnockedOutTeammates(action)) return usersPartyMembers.Where(x => x.KOd);
        if (AimingForTeammates(action)) return usersPartyMembers.Where(x => !x.KOd);
        if (AimingForEnemies(action)) return opponentPartyMembers.Where(x => !x.KOd);
        switch (action.Scope)
        {
            case ActiveTool.ScopeType.Planting:
            case ActiveTool.ScopeType.TrapSetup:
                return Random.Range(0, 2) < 1 ? usersPartyMembers.Where(x => !x.KOd) : opponentPartyMembers.Where(x => !x.KOd);
        }
        return null;
    }

    protected void SelectTargets<T, U>(IList<T> usersPartyMembers, IList<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        switch (SelectedAction.Scope)
        {
            case ActiveTool.ScopeType.OneEnemy:
            case ActiveTool.ScopeType.OneArea:
            case ActiveTool.ScopeType.WideFrontal:
            case ActiveTool.ScopeType.StraightThrough:
            case ActiveTool.ScopeType.OneAlly:
                var pt = PossibleTargets.Where(x => !x.KOd);
                Battler target = pt.ElementAt(Random.Range(0, pt.Count()));
                SelectedSingleMeeleeTarget = SelectedAction.Ranged ? null : target;
                target.Select(true);
                break;

            case ActiveTool.ScopeType.AllEnemies:
                TargetAll(opponentPartyMembers.Where(x => !x.KOd));
                break;

            case ActiveTool.ScopeType.Self:
                Select(true);
                break;

            case ActiveTool.ScopeType.AllAllies:
                TargetAll(usersPartyMembers.Where(x => !x.KOd));
                break;

            case ActiveTool.ScopeType.OneKnockedOutAlly:
                var pt0 = PossibleTargets.Where(x => x.KOd);
                pt0.ElementAt(Random.Range(0, pt0.Count())).Select(true);
                break;

            case ActiveTool.ScopeType.AllKnockedOutAllies:
                TargetAll(usersPartyMembers.Where(x => x.KOd));
                break;

            case ActiveTool.ScopeType.TrapSetup:
            case ActiveTool.ScopeType.Planting:
                break;

            case ActiveTool.ScopeType.EveryoneButSelf:
                TargetAll(CurrentBattle.AllBattlers.Where(x => !x.KOd && x != this));
                break;

            case ActiveTool.ScopeType.Everyone:
                TargetAll(CurrentBattle.AllBattlers.Where(x => !x.KOd));
                break;
        }
    }

    private void TargetAll<T>(IEnumerable<T> battlers) where T : Battler
    {
        foreach (T b in battlers) b.Select(true);
    }

    private void PositionIn()
    {
        //
    }
}
