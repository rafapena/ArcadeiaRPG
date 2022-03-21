using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Shopkeeper : MapNPC
{
    public Sprite Image;
    public int TransactionAddedJump;
    public int OnlyBrowseAddedJump;

    public Item[] ItemsInStock;
    public Weapon[] WeaponsInStock;
    public Accessory[] AccessoriesInStock;

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

    public List<IToolForInventory> GetAllToolsInStock()
    {
        return MenuMaster.GroupInventoryToolsToList(ItemsInStock, WeaponsInStock, AccessoriesInStock);
    }

    protected override void AnimateDirection()
    {
        //
    }

    public void OpenShop(bool onlyBuying)
    {
        SceneMaster.OpenShop(onlyBuying, Player.Party, this);
    }

    public void CloseShop(bool doneTransaction)
    {
        Cutscene.ForceJump(doneTransaction ? TransactionAddedJump : OnlyBrowseAddedJump);
    }

    public void OpenClassChange()
    {
        //
    }

    public void CloseClassChange()
    {
        //
    }

    public void OpenSkillChange()
    {
        //
    }

    public void CloseSkillChange()
    {
        //
    }
}