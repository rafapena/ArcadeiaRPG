using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractionField : MonoBehaviour
{
    public MapPlayer Avatar;
    public List<ItemBox> ItemsBoxesFound;
    public List<MapNPC> NPCsFound;

    private void Update()
    {
        if (!InputMaster.Interact) return;
        foreach (ItemBox b in ItemsBoxesFound.FindAll(x => !x.IsOpened))
        {
            if (!b.CloseToPlayer) continue;
            b.Open(Avatar);
            return;
        }
        foreach (MapNPC n in NPCsFound)
        {
            if (!n.CloseToPlayer) continue;
            n.InteractWith(Avatar);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        OnTrigger(collision, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        OnTrigger(collision, false);
    }

    private void OnTrigger(Collider2D collision, bool nearby)
    {
        switch (collision.gameObject.tag)
        {
            case "ItemBox":
                ItemBox b = collision.gameObject.GetComponent<ItemBox>();
                b.CloseToPlayer = nearby;
                if (nearby) ItemsBoxesFound.Add(b);
                break;
            case "NPC":
                MapNPC n = collision.gameObject.GetComponent<MapNPC>();
                n.CloseToPlayer = nearby;
                if (nearby) NPCsFound.Add(n);
                break;
        }
    }
}
