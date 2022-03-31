using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Crafter : MapNPC
{
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

    public void OpenWorkshop(int mode)
    {
        if (mode >= 0 && mode < System.Enum.GetNames(typeof(InventorySystem.ListType)).Length)
            SceneMaster.OpenCraftingMenu(Player.Party, this, (InventorySystem.ListType)mode);
    }

    public void CloseWorkshop(bool doneTransaction)
    {
        Cutscene.ForceJump(doneTransaction ? TransactionAddedJump : OnlyBrowseAddedJump);
    }
}