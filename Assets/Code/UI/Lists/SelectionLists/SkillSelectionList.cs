using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSelectionList : SelectionList_Super<Skill>
{
    public GameObject NoSkillsLabel;
    public GameObject CannotUseFrame;
    public TextMeshProUGUI CannotUseLabel;

    // Usability Ref
    private Battler ReferenceBattler;
    private List<Battler> BattlersGroup;
    private static string[] Unusabilities = new string[10];

    // Colors
    private Color NormalColor = Color.white;
    private Color NormalSPTextColor;
    private Color DisabledColor;

    public bool CanSelectSkill => !CannotUseFrame.activeSelf;

    private void Start()
    {
        CannotUseFrame.SetActive(false);
        NormalSPTextColor = transform.GetChild(0).GetChild(2).GetComponent<TextMeshProUGUI>().color;
        DisabledColor = transform.GetChild(0).GetComponent<Button>().colors.disabledColor;
    }

    public void Activate()
    {
        Container.GetComponent<MenuFrame>().Activate();
    }

    public void Deactivate()
    {
        Container.GetComponent<MenuFrame>().Deactivate();
    }

    public void Refresh(BattlePlayer b, List<Battler> bg)
    {
        BattlersGroup = bg;
        ReferenceBattler = b;
        ReferenceData.Clear();
        NoSkillsLabel.SetActive(!b.HasAnySkills);
        int i = 0;
        foreach (Skill dataEntry in b.Skills)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            GameObject entry = transform.GetChild(i).gameObject;
            AddToList(entry.transform, dataEntry);
            
            Unusabilities[i] = GetUnusability(dataEntry);
            bool usable = Unusabilities[i].Equals("");
            entry.transform.GetChild(0).GetComponent<Image>().color = usable ? NormalColor : DisabledColor;
            entry.transform.GetChild(1).GetComponent<TextMeshProUGUI>().color = usable ? NormalColor : DisabledColor;
            entry.transform.GetChild(2).GetComponent<TextMeshProUGUI>().color = usable ? NormalSPTextColor : DisabledColor;

            ListSelectable e = entry.GetComponent<ListSelectable>();
            e.Index = i;
            e.ClearHighlights();
            i++;
        }
        // If there are excess blank rows, make them invisible
        for (; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
    }

    private void AddToList(Transform entry, Skill dataEntry)
    {
        ReferenceData.Add(dataEntry);
        entry.GetChild(0).GetComponent<Image>().sprite = dataEntry.Image;
        entry.GetChild(1).GetComponent<TextMeshProUGUI>().text = dataEntry.Name.ToUpper();
        entry.GetChild(2).GetComponent<TextMeshProUGUI>().text = dataEntry.SPConsume > 0 ? dataEntry.SPConsume.ToString() : "";
        entry.GetChild(3).GetComponent<TextMeshProUGUI>().text = dataEntry.Cooldown > 0 ? dataEntry.Cooldown.ToString() : "";
    }

    public void HoverOverTool()
    {
        ListSelectable btn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        InfoFrame.SetActive(true);

        if (SelectedButton) SelectedButton.ClearHighlights();
        SelectedButton = btn;
        SelectedIndex = btn.Index;
        SelectedObject = ReferenceData[SelectedIndex];

        InfoFrame.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = SelectedObject.Name.ToUpper();
        InfoFrame.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = SelectedObject.Description;
        bool hasElement = UIMaster.ElementImages.ContainsKey(SelectedObject.Element);
        InfoFrame.transform.GetChild(2).gameObject.SetActive(hasElement);
        if (hasElement) InfoFrame.transform.GetChild(2).GetComponent<Image>().sprite = UIMaster.ElementImages[SelectedObject.Element];
        InfoFrame.transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = SelectedObject.Power.ToString();
        InfoFrame.transform.GetChild(6).GetComponent<TextMeshProUGUI>().text = "+" + SelectedObject.CriticalRateBoost + "%";
        InfoFrame.transform.GetChild(8).GetComponent<TextMeshProUGUI>().text = Regex.Replace(SelectedObject.Scope.ToString(), "(\\B[A-Z])", " $1").ToUpper();
        CheckDisplayCannotUseMessage(SelectedIndex);
    }

    private string GetUnusability(Skill skill)
    {
        string msg = "";
        if (!skill.CanUseOutsideOfBattle && !SceneMaster.InBattle) msg = "CANNOT USE OUTSIDE OF BATTLE";
        else if (!skill.EnoughSPFrom(ReferenceBattler)) msg = "NOT ENOUGH SP TO USE";
        else if (!skill.UsedByClassUser(ReferenceBattler)) msg = "MUST BE A " + skill.ClassExclusives[0].Name.ToUpper() + " TO USE";
        else if (!skill.CheckExclusiveWeapon(ReferenceBattler)) msg = "MUST HAVE A " + skill.WeaponExclusives[0].ToString().ToUpper() + " TO USE";
        else if (!skill.AvailableTeammateTargets(BattlersGroup)) msg = "NO AVAILABLE SELECTIONS WITHIN SCOPE";
        return msg;
    }

    private void CheckDisplayCannotUseMessage(int index)
    {
        string msg = Unusabilities[index];
        CannotUseFrame.SetActive(!msg.Equals(""));
        CannotUseLabel.text = msg;
    }
}
