using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuMaster : MonoBehaviour
{
    public static Color POSITIVE_NUMBER_COLOR = new Color(0.5f, 1f, 0.5f);
    public static Color NEGATIVE_NUMBER_COLOR = new Color(0.9f, 0.4f, 0.4f);

    // For menu selection
    private static float SelectionTimer = 0;
    private const float SELECTION_BUFFER = 0.6f;

    // For gameplay
    private static float SelectionTimerGP = 0;
    private const float SELECTION_BUFFER_GP = 0.5f;

    public static bool ReadyToSelectInMenu => Time.unscaledTime > SelectionTimer;
    public static bool ReadyToSelectInGameplay => Time.time > SelectionTimerGP;

    public static void SetupSelectionBufferInMenu(float change = 1f)
    {
        SelectionTimer = Time.unscaledTime + SELECTION_BUFFER * change;
    }

    public static void SetupSelectionBufferInGameplay(float change = 1f)
    {
        SelectionTimerGP = Time.time + SELECTION_BUFFER_GP * change;
    }

    public static void KeepHighlightedSelected(ref ListSelectable selectedListBtn)
    {
        if (selectedListBtn) selectedListBtn.ClearHighlights();
        selectedListBtn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        selectedListBtn.KeepSelected();
    }

    public static void DisableSelection(ref GameObject button)
    {
        Color c = Color.white;
        c.a = 0.2f;
        button.GetComponent<Button>().interactable = false;
        button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = c;
        if (button.transform.childCount > 1) button.transform.GetChild(1).GetComponent<Image>().color = c;
    }

    public static void SetNumberBoost(TextMeshProUGUI boost, int number, string zeroText = "", string postText = "")
    {
        if (number < 0)
        {
            boost.color = NEGATIVE_NUMBER_COLOR;
            boost.text = number + postText;
        }
        else if (number > 0)
        {
            boost.color = POSITIVE_NUMBER_COLOR;
            boost.text = "+" + number + postText;
        }
        else boost.text = zeroText;
    }

    public static void EnableSelection(ref GameObject button)
    {
        Color c = Color.white;
        c.a = 1f;
        button.GetComponent<Button>().interactable = true;
        button.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = c;
        if (button.transform.childCount > 1) button.transform.GetChild(1).GetComponent<Image>().color = c;
    }

    public static List<IToolForInventory> GroupInventoryToolsToList(Item[] it, Weapon[] wp, Accessory[] ac)
    {
        List<IToolForInventory> toolsInStock = new List<IToolForInventory>();
        if (it != null) toolsInStock.AddRange(it);
        if (wp != null) toolsInStock.AddRange(wp);
        if (ac != null) toolsInStock.AddRange(ac);
        return toolsInStock;
    }

    public static IToolForInventory[] GroupInventoryToolsToArray(Item[] it, Weapon[] wp, Accessory[] ac)
    {
        IToolForInventory[] toolsInStock = new IToolForInventory[it.Length + wp.Length + ac.Length];
        int i = 0;
        foreach (Item t in it) toolsInStock[i++] = t;
        foreach (Weapon w in wp) toolsInStock[i++] = w;
        foreach (Accessory a in ac) toolsInStock[i++] = a;
        return toolsInStock;
    }
}