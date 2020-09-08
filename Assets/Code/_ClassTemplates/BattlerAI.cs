using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UI;

public abstract class BattlerAI : Battler
{
    public List<AITool> ToolAI;

    protected override void Awake()
    {
        base.Awake();
        foreach (AITool ait in ToolAI)
        {
            switch (ait.Move.GetType().Name)
            {
                case "Weapon": Weapons.Add(ait.Move as Weapon); break;
                case "SoloSkill": SoloSkills.Add(ait.Move as SoloSkill); break;
                case "TeamSkill": TeamSkills.Add(ait.Move as TeamSkill); break;
                case "Item": Items.Add(ait.Move as Item); break;
            }
        }
    }

    public override void StatConversion()
    {
        if (Class) Stats = Instantiate(Class.BaseStats, gameObject.transform);
        Stats.ConvertFromBaseToActual(Level);
        HP = Stats.MaxHP;
        SP = 100;
    }

    protected override SoloSkill GetDefaultSoloSkill()
    {
        return SoloSkills[0];   // Always assume that AI has at least one Solo Skill
    }

    public void MakeDecision(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        ExecutedAction = false;
        if (!CanMove()) return;
        SelectedWeapon = SelectWeapon(usersPartyMembers, opponentPartyMembers);
        Tool t = SelectTool(usersPartyMembers, opponentPartyMembers);
        if (t == null) return;
        switch (t.GetType().Name)
        {
            case "SoloSkill": SelectedSoloSkill = t as SoloSkill; break;
            case "TeamSkill": SelectedTeamSkill = t as TeamSkill; break;
            case "Item": SelectedItem = t as Item; break;
        }
        SelectedTeamSkillPartners = SelectTeammates(usersPartyMembers, opponentPartyMembers);
        SelectedTargets = SelectTargets(usersPartyMembers, opponentPartyMembers);
    }

    protected Weapon SelectWeapon(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        return null;
    }

    protected Tool SelectTool(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        return ToolAI[0].Move;
    }

    protected List<Battler> SelectTeammates(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        return null;
    }

    protected List<Battler> SelectTargets(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        List<Battler> targets = new List<Battler>();
        targets.Add(opponentPartyMembers[Random.Range(0, opponentPartyMembers.Count)]);
        return targets;
    }
}
