using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shopkeeper : MapNPC
{
    public PlayerParty Customer;
    public Sprite Image;
    public List<ToolForInventory> ToolsInStock;
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

    public void OpenShop(bool onlyBuying)
    {
        SceneMaster.OpenShop(onlyBuying, Customer, this);
    }

    public void CloseShop(bool doneTransaction)
    {
        Cutscene.ForceJump(doneTransaction ? TransactionAddedJump : OnlyBrowseAddedJump);
    }
}
