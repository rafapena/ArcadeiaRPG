using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapPlayer : MapExplorer
{
    public Animator Animate;
    private int Mode;

    public PlayerParty Party;
    public Transform BattlersListDump;
    public Transform ItemsListDump;
    public Transform ObjectivesListDump;

    protected override void Awake()
    {
        base.Awake();
        MenuMaster.SetupSelectionBufferInGameplay();
    }

    protected override void Start()
    {
        base.Start();
        Party.Setup();
        TEST_SETUP();
        //if (GameplayMaster.NoFileSelected()) SetupPartyNew();
        //else if (!GameplayMaster.FinishedLoadingContent()) Party.LoadFromFile(GameplayMaster.SelectedFile, this);
        //else Setup();
        GameplayMaster.Party = Party;
    }

    private void SetupPartyNew()
    {
        List<Battler> all = Party.GetWholeParty();
        for (int i = 0; i < all.Count; i++)
        {
            all[i] = Instantiate(all[i], BattlersListDump);
            Battler b = all[i];
            b.Level = Party.Level;
            if (b is BattlePlayer p && p.Weapons.Count > 0) p.SelectedWeapon = p.Weapons[0];
            b.StatConversion();
            b.gameObject.SetActive(false);
        }
        Party.UpdateAll(all);
        for (int i = 0; i < Party.LoggedObjectives.Count; i++)
        {
            Party.LoggedObjectives[i] = Instantiate(Party.LoggedObjectives[i], ObjectivesListDump);
        }
    }

    private void Setup()
    {
        List<Battler> all = Party.GetWholeParty();
        for (int i = 0; i < all.Count; i++)
        {
            all[i] = Instantiate(all[i], gameObject.transform);
            Battler b = all[i];
            b.Level = Party.Level;
            b.StatConversion();
            b.HP = b.MaxHP;
            b.SP = 100;
            b.gameObject.SetActive(false);
            if (b is BattlePlayer p)
            {
                p.AddLearnedSkills();
                if (p.Weapons.Count > 0) p.SelectedWeapon = p.Weapons[0];
                for (int j = 0; j < p.Weapons.Count; j++) p.Weapons[j] = Instantiate(p.Weapons[j], p.transform);
            }
            for (int j = 0; j < b.States.Count; j++) b.States[j] = Instantiate(b.States[j], b.transform);
        }
        Party.UpdateAll(all);
        for (int i = 0; i < Party.LoggedObjectives.Count; i++)
        {
            Party.LoggedObjectives[i] = Instantiate(Party.LoggedObjectives[i], ObjectivesListDump);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // FOR TESTING PURPOSES ONLY
    private void TEST_SETUP()
    {
        List<Battler> all = Party.GetWholeParty();
        for (int i = 0; i < all.Count; i++)
        {
            all[i] = Instantiate(all[i], gameObject.transform);
            Battler b = all[i];
            b.Level = Party.Level;
            b.StatConversion();
            b.HP = b.MaxHP / 2;
            b.SP = 100 / 2;
            b.gameObject.SetActive(false);
            if (b is BattlePlayer p)
            {
                p.AddLearnedSkills();
                if (p.Weapons.Count > 0) p.SelectedWeapon = p.Weapons[0];
                for (int j = 0; j < p.Weapons.Count; j++) p.Weapons[j] = Instantiate(p.Weapons[j], p.transform);
            }
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
        if (SceneMaster.InMenu || SceneMaster.InCutscene)
        {
            Figure.velocity = Vector3.zero;
            return;
        }
        if (!SceneMaster.InBattle)
        {
            if (InputMaster.FileSelect) SceneMaster.OpenFileSelect(FileSelect.FileMode.Save, Party);
            else if (InputMaster.MapMenu) SceneMaster.OpenMapMenu(Party);
            else if (InputMaster.Pause) SceneMaster.OpenPauseMenu(Party);
            else if (Input.GetKeyDown(KeyCode.T)) SceneMaster.OpenStorage(Party);   // TEST MODE
        }
        base.Update();
        if (gameObject.layer == NON_COLLIDABLE_EXPLORER_LAYER && !IsBlinking()) gameObject.layer = MAP_PLAYER_LAYER;
        Movement = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Figure.velocity = Movement * Speed;
        AnimateDirection();
    }

    protected override void AnimateDirection()
    {
        if (Movement.x == 0 && Movement.y == 0)
        {
            return;
        }
        else if (Movement.x != 0 && Movement.y != 0)
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

    public void PointToDirectionOf(MonoBehaviour target)
    {
        float xDist = transform.position.x - target.transform.position.x;
        float yDist = transform.position.y - target.transform.position.y;
        bool r = xDist < 0 && Mathf.Abs(xDist) > Mathf.Abs(yDist);
        bool l = xDist > 0 && Mathf.Abs(xDist) > Mathf.Abs(yDist);
        bool t = yDist < 0 && Mathf.Abs(xDist) < Mathf.Abs(yDist);
        bool b = yDist > 0 && Mathf.Abs(xDist) < Mathf.Abs(yDist);
        AnimationDirectionAUX(r, 6, "R");
        AnimationDirectionAUX(l, 2, "L");
        AnimationDirectionAUX(t, 4, "T");
        AnimationDirectionAUX(b, 0, "B");
    }
}
