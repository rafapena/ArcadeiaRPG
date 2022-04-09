using System.Collections.Generic;
using System.Linq;
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
        HUDProperties.Name.text = Name.ToUpper();

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

    public void SetUI(bool show)
    {
        HUDProperties.PropertiesList.gameObject.SetActive(show);
    }

    public void SetNextLabel(bool visible)
    {
        HUDProperties.Next.SetActive(visible);
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

    public void MakeDecision<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        if (CanDoAction) return;
        SelectedWeapon = SelectWeapon(usersPartyMembers, opponentPartyMembers);
        ActiveTool t = SelectTool(usersPartyMembers, opponentPartyMembers);
        if (t != null) SelectedTargets = SelectTargets(usersPartyMembers, opponentPartyMembers);
    }

    protected Weapon SelectWeapon<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        return null;
    }

    protected ActiveTool SelectTool<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        return ToolAI[0].Move;
    }

    protected List<Battler> SelectTargets<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        List<Battler> targets = new List<Battler>();
        targets.Add(opponentPartyMembers[Random.Range(0, opponentPartyMembers.Count)]);
        return targets;
    }

    protected override void MapGameObjectsToHUD()
    {
        // StateEffects
    }
}
