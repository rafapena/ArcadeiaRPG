using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleWin : MonoBehaviour
{
    // Battle data, including parties, levelling/exp and inventory info
    public Battle FinishedBattle;

    // Child GameObjects
    public MenuFrame VictoryBanner;
    public MenuFrame LevelEXPFrame;
    public MenuFrame GoldFrame;
    public MenuFrame NewSkillsFrame;
    public MenuFrame ProceedButton;

    // Level and EXP
    private int LevelUps;
    public TextMeshProUGUI LevelLabel;
    public Gauge EXPGauge;
    public NumberUpdater TotalEXPLabel;
    public TextMeshProUGUI EXPEarnedLabel;
    private int EXPEarned;

    // Gold
    public NumberUpdater TotalGoldLabel;
    public TextMeshProUGUI GoldEarnedLabel;
    private int GoldEarned;

    // New Skills
    private float DefaultNewSkillsListEntryHeight;
    public GameObject NewSkillsListCover;
    public Transform NewSkillsList;

    private void Start()
    {
        DefaultNewSkillsListEntryHeight = NewSkillsListCover.GetComponent<RectTransform>().sizeDelta.y;
    }

    private void Update()
    {
        if (!VictoryBanner.Activated) return;
        if (Input.GetKeyDown(KeyCode.Z) && ProceedButton.Activated) ExitBattle();
    }

    public void Setup()
    {
        VictoryBanner.Activate();
        LevelEXPFrame.Activate();
        GoldFrame.Activate();
        SetupEnemyInfo();
        SetupLevelEXPInfo();

        TotalEXPLabel.Initialize(FinishedBattle.PlayerParty.EXP);
        EXPEarnedLabel.text = "+" + EXPEarned;
        TotalGoldLabel.Initialize(FinishedBattle.PlayerParty.Inventory.Gold);
        GoldEarnedLabel.text = "+" + GoldEarned;

        StartCoroutine(AnimateMenu());
    }

    private IEnumerator AnimateMenu()
    {
        yield return new WaitForSecondsRealtime(1f);

        int gCurr = FinishedBattle.PlayerParty.EXP + EXPEarned - FinishedBattle.PlayerParty.LastEXPToNext;
        int gMax = FinishedBattle.PlayerParty.EXPToNext - FinishedBattle.PlayerParty.LastEXPToNext;
        EXPGauge.SetAndAnimate(gCurr, gMax);
        TotalEXPLabel.Add(ref FinishedBattle.PlayerParty.EXP, EXPEarned);
        TotalGoldLabel.Add(ref FinishedBattle.PlayerParty.Inventory.Gold, GoldEarned);

        while (FinishedBattle.PlayerParty.EXP >= FinishedBattle.PlayerParty.EXPToNext)
        {
            yield return new WaitUntil(() => EXPGauge.IsBarFull);
            LevelUp();
        }
        yield return new WaitUntil(() => !EXPGauge.IsUpdating);

        HandleNewSkills();
        if (NewSkillsFrame.Activated) yield return new WaitForSecondsRealtime(1f);
        ProceedButton.Activate();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- First Win Screen --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void SetupEnemyInfo()
    {
        foreach (BattleEnemy e in FinishedBattle.EnemyParty.Enemies)
        {
            EXPEarned += e.Exp;
            GoldEarned += e.Gold;
        }
    }

    private void SetupLevelEXPInfo()
    {
        int gCurr = FinishedBattle.PlayerParty.EXP - FinishedBattle.PlayerParty.LastEXPToNext;
        int gMax = FinishedBattle.PlayerParty.EXPToNext - FinishedBattle.PlayerParty.LastEXPToNext;
        EXPGauge.Set(gCurr, gMax);
        LevelLabel.text = FinishedBattle.PlayerParty.Level.ToString();
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Level up --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void LevelUp()
    {
        LevelUps++;
        FinishedBattle.PlayerParty.LevelUp();
        LevelLabel.text = FinishedBattle.PlayerParty.Level.ToString();
        if (LevelUps > 0) LevelLabel.color = new Color(0.7f, 1, 0.5f);

        int lastExp = FinishedBattle.PlayerParty.LastEXPToNext;
        int newExpTotal = FinishedBattle.PlayerParty.EXP;
        EXPGauge.Empty();
        EXPGauge.SetAndAnimate(newExpTotal - lastExp, FinishedBattle.PlayerParty.EXPToNext - lastExp);

        Instantiate(UIMaster.Popups["LevelUp"], transform.position, Quaternion.identity);
    }

    private void HandleNewSkills()
    {
        int battlerCount = 0;
        int oldLevel = FinishedBattle.PlayerParty.Level - LevelUps;
        int maxSkills = NewSkillsList.GetChild(battlerCount).GetChild(1).childCount;

        foreach (var b in FinishedBattle.PlayerParty.AllPlayers)
        {
            int skillCount = 0;
            foreach (var s in b.SkillSet)
            {
                if (oldLevel >= s.LearnLevel || s.LearnLevel > FinishedBattle.PlayerParty.Level) continue;
                NewSkillsList.GetChild(battlerCount).GetChild(0).GetChild(0).GetComponent<Image>().sprite = b.FaceImage;
                if (skillCount < maxSkills)
                {
                    NewSkillsList.GetChild(battlerCount).GetChild(1).GetChild(skillCount).GetComponent<TextMeshProUGUI>().text = s.LearnedSkill.Name;
                    NewSkillsList.GetChild(battlerCount).GetChild(1).GetChild(skillCount).gameObject.SetActive(true);
                }
                skillCount++;
            }
            if (skillCount > 0) battlerCount++;
        }
        for (int i = battlerCount; i < NewSkillsList.childCount; i++) NewSkillsList.GetChild(i).gameObject.SetActive(false);
        if (battlerCount > 0) NewSkillsFrame.Activate();

        Vector2 size0 = NewSkillsListCover.GetComponent<RectTransform>().sizeDelta;
        Vector2 size1 = NewSkillsFrame.GetComponent<RectTransform>().sizeDelta;
        size0.y += DefaultNewSkillsListEntryHeight * battlerCount;
        size1.y += DefaultNewSkillsListEntryHeight * battlerCount;
        NewSkillsListCover.GetComponent<RectTransform>().sizeDelta = size0;
        NewSkillsFrame.GetComponent<RectTransform>().sizeDelta = size1;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Exit screen --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void ExitBattle()
    {
        ProceedButton.Deactivate();
        SceneMaster.EndBattle();
    }
}
