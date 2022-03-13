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
}