using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapPlayer : MapExplorer
{
    public Animator Animate;
    private int Mode;

    public GameObject Container;
    public PlayerParty Party;

    protected override void Awake()
    {
        base.Awake();
        MenuMaster.SetupSelectionBufferInGameplay();
    }

    protected override void Start()
    {
        base.Start();
        Party.Setup();
        //if (GameplayMaster.NoFileSelected) Party.Setup();
        //else if (!GameplayMaster.FinishedLoadingContent) Party.LoadFromFile(GameplayMaster.SelectedFile);
        //else Party.Setup();
        GameplayMaster.PlayerContainer = Container;
    }

    protected override void Update()
    {
        if (SceneMaster.InMenu || SceneMaster.InCutscene)
        {
            Figure.velocity = Vector3.zero;
            return;
        }
        if (!SceneMaster.InBattle)
        {
            if (InputMaster.FileSelect) SceneMaster.OpenFileSelect(FileSelect.FileMode.Save);
            else if (InputMaster.MapMenu) SceneMaster.OpenMapMenu();
            else if (InputMaster.Pause) SceneMaster.OpenPauseMenu();
            else if (Debug.isDebugBuild && Input.GetKeyDown(KeyCode.T)) SceneMaster.OpenStorage();
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
