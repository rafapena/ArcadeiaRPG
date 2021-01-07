using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuMaster : MonoBehaviour
{
    public static PlayerParty PartyInfo;

    public static void DisableSelection(ref GameObject button)
    {
        Color c = Color.white;
        c.a = 0.2f;
        button.GetComponent<Button>().interactable = false;
        button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = c;
        if (button.transform.childCount > 1) button.transform.GetChild(1).GetComponent<Image>().color = c;
    }
}