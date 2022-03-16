using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shopkeeper : MapNPC
{
    public PlayerParty Customer;
    public List<ToolForInventory> ToolsInStock;
    [TextArea] public string TransactionMessage;
    [TextArea] public string OnlyBrowsedMessage;

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
        //
    }
}
