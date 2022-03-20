using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Crafter : MapNPC
{
    public PlayerParty Customer;
    public Sprite Image;
    public int TransactionAddedJump;
    public int OnlyBrowseAddedJump;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void AnimateDirection()
    {
        //
    }

    public void OpenWorkshop()
    {
        SceneMaster.OpenCraftingMenu(Customer, this);
    }

    public void CloseWorkshop(bool doneTransaction)
    {
        Cutscene.ForceJump(doneTransaction ? TransactionAddedJump : OnlyBrowseAddedJump);
    }
}