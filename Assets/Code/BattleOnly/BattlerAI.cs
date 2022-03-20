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
            if (!ait.Move) continue;
            switch (ait.Move.GetType().Name)
            {
                case "Weapon": Weapons.Add(ait.Move as Weapon); break;
                case "Skill": Skills.Add(ait.Move as Skill); break;
            }
        }
    }

    public override void StatConversion()
    {
        if (Class) Stats.SetTo(Class.BaseStats);
        Stats.ConvertFromBaseToActual(Level);
        HP = Stats.MaxHP;
        SP = 100;
    }

    protected override Skill GetDefaultSkill()
    {
        return Skills[0];   // Always assume that AI has at least one Solo Skill
    }

    public void MakeDecision(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        ExecutedAction = false;
        if (!CanMove()) return;
        SelectedWeapon = SelectWeapon(usersPartyMembers, opponentPartyMembers);
        ActiveTool t = SelectTool(usersPartyMembers, opponentPartyMembers);
        if (t == null) return;
        switch (t.GetType().Name)
        {
            case "Skill": SelectedSkill = t as Skill; break;
            case "Item": SelectedItem = t as Item; break;
        }
        SelectedTargets = SelectTargets(usersPartyMembers, opponentPartyMembers);
    }

    protected Weapon SelectWeapon(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
    {
        return null;
    }

    protected ActiveTool SelectTool(List<Battler> usersPartyMembers, List<Battler> opponentPartyMembers)
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
