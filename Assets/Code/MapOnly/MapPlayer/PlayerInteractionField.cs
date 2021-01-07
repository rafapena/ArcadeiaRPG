using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInteractionField : MonoBehaviour
{
    public MapPlayer Avatar;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (InputMaster.Interact())
        {
            switch (collision.gameObject.tag)
            {
                case "ItemBox":
                    if (collision.gameObject.tag == "ItemBox") collision.gameObject.GetComponent<ItemBox>().Open(Avatar);
                    break;
                case "NPC":
                    break;
            }
        }
    }
}
