using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractionField : MonoBehaviour
{
    public MapPlayer Avatar;
    public List<ItemBox> ItemsBoxesFound;
    //public List<NPC> NPCsFound;

    private void Update()
    {
        if (!InputMaster.Interact()) return;
        foreach (ItemBox b in ItemsBoxesFound)
            if (b.CloseToPlayer) b.Open(Avatar);
        /*foreach (NPC n in NPCsFound)
        {
            if (!n.ClosingPlayer) return;
            Avatar.PointToDirectionOf(n);
            n.InteractWith(Avatar);
        }*/
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
                //NPC n = collision.gameObject.GetComponent<NPC>();
                //n.ClosingPlayer = nearby ? Avatar : null;
                //NPCsFound.Add(n)
                break;
        }
    }
}
