using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;

public class MMStats : MM_Super
{
    public GameObject PartyList;

    public Transform CharacterInfo;
    public TextMeshProUGUI Level;
    public StatsList StatsList;

    public Transform RelationsList;
    public Transform SkillList;
    public Transform TeamSkillList;
    public Transform WeaponsList;
    public Transform ItemsList;
    public Transform WeaknessesList;
    public Transform StrengthsList;
    public Transform PassiveSkillsList;

    [HideInInspector] private BattlePlayer SelectedPlayer;
    [HideInInspector] private int SelectedPlayerIndex;

    public override void Open()
    {
        base.Open();
        SetupButtonList(MenuManager.PartyInfo.AllPlayers);
        SetupPlayer();
    }

    public override void Close()
    {
        base.Close();
    }

    public override void GoBack()
    {
        MenuManager.GoToMain();
    }

    protected override void ReturnToInitialSetup()
    {
        //
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Party Buttons List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void Setup(BattlePlayer player, int index)
    {
        SelectedPlayer = player;
        SelectedPlayerIndex = index;
    }

    public void SelectingOnButtonList()
    {
        SelectedPlayerIndex = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index;
        SelectedPlayer = MenuManager.PartyInfo.AllPlayers[SelectedPlayerIndex];
        SetupPlayer();
    }

    public void SetupButtonList(List<BattlePlayer> players)
    {
        int i = 0;
        foreach (BattlePlayer p in players)
        {
            Transform entry = PartyList.transform.GetChild(i);
            entry.GetChild(0).GetComponent<TextMeshProUGUI>().text = p.Name;
            entry.GetComponent<ListSelectable>().Index = i;
            entry.gameObject.SetActive(true);
            i++;
        }
        for (; i < PartyList.transform.childCount; i++)
        {
            Transform entry = PartyList.transform.GetChild(i);
            entry.GetComponent<ListSelectable>().Index = i;
            entry.gameObject.SetActive(false);
        }
    }

    public void SetupPlayer()
    {
        SetupCharacterInfo();
        Level.text = MenuManager.PartyInfo.Level.ToString();
        StatsList.Setup(SelectedPlayer);
        SetupRelations();
        SetupSkills();
        SetupEquipment();
        SetupRates();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup Lists --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupCharacterInfo()
    {
        //CharacterInfo.GetChild(0).GetComponent<Image>().sprite = SelectedPlayer.MainImage;
        CharacterInfo.GetChild(1).GetComponent<TextMeshProUGUI>().text = SelectedPlayer.Name.ToUpper();
        CharacterInfo.GetChild(2).GetComponent<TextMeshProUGUI>().text = SelectedPlayer.Class.Name.ToUpper();
        CharacterInfo.GetChild(3).GetComponent<Gauge>().Set(SelectedPlayer.HP, SelectedPlayer.Stats.MaxHP);
        CharacterInfo.GetChild(4).GetComponent<Gauge>().Set(SelectedPlayer.SP, 100);
        Transform states = CharacterInfo.GetChild(5).transform;
        int i = 0;
        for (; i < SelectedPlayer.States.Count; i++)
        {
            if (i == states.childCount) break;
            states.GetChild(i).gameObject.SetActive(true);
            states.GetChild(i).GetComponent<Image>().sprite = SelectedPlayer.States[i].GetComponent<Image>().sprite;
        }
        for (; i < states.childCount; i++) states.GetChild(i).gameObject.SetActive(false);
    }

    public void SetupRelations()
    {
        List<PlayerRelation> sortedRelations = SelectedPlayer.Relations;//.OrderByDescending(t => t.Points).ToList();
        int i = 0;
        foreach (PlayerRelation pr in sortedRelations)
        {
            if (pr == null) continue;
            else if (MenuManager.PartyInfo.AllPlayers.Contains(pr.Player))
            {
                Transform entry = RelationsList.transform.GetChild(i);
                RelationBar relBar = entry.GetChild(1).GetComponent<RelationBar>();
                entry.gameObject.SetActive(true);
                relBar.Setup(pr);
            }
            i++;
        }
        for (; i < RelationsList.transform.childCount; i++)
        {
            Transform entry = RelationsList.transform.GetChild(i);
            entry.gameObject.SetActive(false);
        }
    }

    public void SetupSkills()
    {
        bool moreThanZero = SelectedPlayer.Skills.Count > 0;
        SkillList.parent.gameObject.SetActive(moreThanZero);
        if (!moreThanZero) return;
        SetupTools(SkillList, SelectedPlayer.Skills);
    }

    public void SetupEquipment()
    {
        SetupTools(WeaponsList, SelectedPlayer.Weapons);
    }

    public void SetupRates()
    {
        int i = 0;
        int i0 = 0;
        foreach (ElementRate er in SelectedPlayer.ChangedElementRates)
        {
            if (i < WeaknessesList.childCount && er.Rate > 100)
            {
                WeaknessesList.GetChild(i).gameObject.SetActive(true);
                WeaknessesList.GetChild(i).GetComponent<Image>().sprite = UIMaster.ElementImages[er.Element];
                i++;
            }
            else if (i0 < StrengthsList.childCount && er.Rate < 100)
            {
                StrengthsList.GetChild(i0).gameObject.SetActive(true);
                StrengthsList.GetChild(i0).GetComponent<Image>().sprite = UIMaster.ElementImages[er.Element];
                i0++;
            }
        }
        foreach (StateRate sr in SelectedPlayer.ChangedStateRates)
        {
            if (i < WeaknessesList.childCount && sr.Rate > 100)
            {
                WeaknessesList.GetChild(i).gameObject.SetActive(true);
                WeaknessesList.GetChild(i).GetComponent<Image>().sprite = sr.State.GetComponent<SpriteRenderer>().sprite;
                i++;
            }
            else if (i0 < StrengthsList.childCount && sr.Rate < 100)
            {
                StrengthsList.GetChild(i0).gameObject.SetActive(true);
                StrengthsList.GetChild(i0).GetComponent<Image>().sprite = sr.State.GetComponent<SpriteRenderer>().sprite;
                i0++;
            }
        }
        WeaknessesList.gameObject.SetActive(i != 0);
        StrengthsList.gameObject.SetActive(i0 != 0);
        WeaknessesList.parent.gameObject.SetActive(i != 0 || i0 != 0);
        for (; i < WeaknessesList.childCount; i++) WeaknessesList.GetChild(i).gameObject.SetActive(false);
        for (; i0 < StrengthsList.childCount; i0++) StrengthsList.GetChild(i0).gameObject.SetActive(false);
    }

    private void SetupTools<T>(Transform listGO, List<T> listData) where T : BaseObject
    {
        int i = 0;
        foreach (T tool in listData)
        {
            Transform entry = listGO.GetChild(i++);
            entry.GetComponent<Image>().sprite = tool.GetComponent<SpriteRenderer>().sprite;
            if (entry.childCount > 0) entry.GetChild(0).GetComponent<TextMeshProUGUI>().text = tool.Name;
            entry.gameObject.SetActive(true);
        }
        for (; i < listGO.childCount; i++) listGO.GetChild(i).gameObject.SetActive(false);
    }
}
