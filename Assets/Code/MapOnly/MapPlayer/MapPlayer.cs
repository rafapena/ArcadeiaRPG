using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapPlayer : MapExplorer
{
    public Animator Animate;
    private int Mode;

    public PlayerParty Party;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        TEST_SETUP();
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // FOR TESTING PURPOSES ONLY
    private void TEST_SETUP()
    {
        List<Battler> all = Party.GetWholeParty();
        for (int i = 0; i < all.Count; i++)
        {
            all[i] = Instantiate(all[i], gameObject.transform);
            all[i].Level = Party.Level;
            if (all[i].Weapons.Count > 0) all[i].SelectedWeapon = all[i].Weapons[0];
            all[i].StatConversion();
            all[i].HP = all[i].Stats.MaxHP;
            all[i].SP = 100;
            all[i].gameObject.SetActive(false);
            if (all[i].GetType().Name == "BattlePlayer")
            {
                BattlePlayer p = all[i] as BattlePlayer;
                p.AddLearnedSkills();
                p.MapUsableSkills = new List<SoloSkill>();
                foreach (SoloSkill s in p.SoloSkills)
                    if (s.CanUseOutsideOfBattle) p.MapUsableSkills.Add(s);
            }
            Battler b = all[i];
            for (int j = 0; j < b.SoloSkills.Count; j++)
            {
                b.SoloSkills[j] = Instantiate(b.SoloSkills[j], b.transform);
                //b.SoloSkills[j].DisableForWarmup();
            }
            for (int j = 0; j < b.TeamSkills.Count;j++)
            {
                b.TeamSkills[j] = Instantiate(b.TeamSkills[j], b.transform);
                //b.TeamSkills[j].DisableForWarmup();
            }
            for (int j = 0; j < b.Weapons.Count; j++) b.Weapons[j] = Instantiate(b.Weapons[j], b.transform);
            for (int j = 0; j < b.Items.Count; j++) b.Items[j] = Instantiate(b.Items[j], b.transform);
            for (int j = 0; j < b.PassiveSkills.Count; j++) b.PassiveSkills[j] = Instantiate(b.PassiveSkills[j], b.transform);
            for (int j = 0; j < b.States.Count; j++) b.States[j] = Instantiate(b.States[j], b.transform);
        }
        Party.UpdateAll(all);
        for (int i = 0; i < Party.LoggedObjectives.Count; i++)
        {
            Party.LoggedObjectives[i] = Instantiate(Party.LoggedObjectives[i], gameObject.transform);
        }
    }
    // FOR TESTING PURPOSES ONLY
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void Update()
    {
        if (SceneMaster.InMapMenu) return;      // Player input should not work, while in the menu
        if (Input.GetKeyDown(KeyCode.Space)) SceneMaster.OpenMenu(Party);
        base.Update();
        if (gameObject.layer == NON_COLLIDABLE_EXPLORER_LAYER && !IsBlinking()) gameObject.layer = PLAYER_LAYER;
        Movement = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        transform.position += Movement * Speed * Time.deltaTime;
        AnimateDirection();
    }

    protected override void AnimateDirection()
    {
        if (Movement.x != 0 && Movement.y != 0)
        {
            AnimationDirectionAUX(Movement.x > 0 && Movement.y > 0, 5, "TR");
            AnimationDirectionAUX(Movement.x > 0 && Movement.y < 0, 7, "BR");
            AnimationDirectionAUX(Movement.x < 0 && Movement.y > 0, 3, "TL");
            AnimationDirectionAUX(Movement.x < 0 && Movement.y < 0, 1, "BL");
        }
        else   // Horizontal/Vertical movement
        {
            AnimationDirectionAUX(Movement.x > 0, 6, "R");
            AnimationDirectionAUX(Movement.x < 0, 2, "L");
            AnimationDirectionAUX(Movement.y > 0, 4, "T");
            AnimationDirectionAUX(Movement.y < 0, 0, "B");
        }
    }

    void AnimationDirectionAUX(bool moveDir, int mode, string dir)
    {
        if (Mode != mode && moveDir)
        {
            Animate.SetTrigger(dir);
            Mode = mode;
        }
    }
}
