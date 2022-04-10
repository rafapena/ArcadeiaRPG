using System.Collections.Generic;
using UnityEngine;

public class TargetFieldHitBox : MonoBehaviour
{
    public TargetField TargetField;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TargetField.NotifyTriggerEnter(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        TargetField.NotifyTriggerExit(collision);
    }
}
