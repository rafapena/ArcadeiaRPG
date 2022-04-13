using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.iOS;
using UnityEngine.UI;

public abstract class BattlerAI : Battler
{
    public List<AIAction> WeaponAI;
    public List<AIAction> SkillAI;
    public List<AIAction> ItemAI;

    [HideInInspector]
    public bool IsSummon;

    protected override void Awake()
    {
        base.Awake();
        HUDProperties.Name.text = Name.ToUpper();
    }

    public void SetUI(bool show)
    {
        HUDProperties.PropertiesList.gameObject.SetActive(show);
    }

    public void SetNextLabel(bool visible)
    {
        HUDProperties.Next.SetActive(visible);
    }

    protected override void MapGameObjectsToHUD()
    {
        // StateEffects
    }

    public override void StatConversion()
    {
        if (Class) Stats.SetTo(Class.BaseStats);
        Stats.ConvertFromBaseToActual(Level);
        HP = Stats.MaxHP;
        SP = 100;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Deciding action and target --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void MakeDecision<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        if (CanDoAction) return;
        SelectedWeapon = SelectWeapon(usersPartyMembers, opponentPartyMembers);
        ActiveTool t = SelectTool(usersPartyMembers, opponentPartyMembers);
        if (t != null) SelectTargets(usersPartyMembers, opponentPartyMembers);
    }

    protected Weapon SelectWeapon<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        return null;
    }

    protected ActiveTool SelectTool<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        return null;
    }

    protected void SelectTargets<T, U>(List<T> usersPartyMembers, List<U> opponentPartyMembers) where T : Battler where U : Battler
    {
        opponentPartyMembers[Random.Range(0, opponentPartyMembers.Count)].Select(true);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- General HP/SP Management --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void MaxHPSP()
    {
        base.MaxHPSP();
        HUDProperties.Gauge.Fill();
    }

    public override void ChangeHP(int val)
    {
        base.ChangeHP(val);
        HUDProperties.Gauge.SetAndAnimate(HP, Stats.MaxHP);
    }
}
