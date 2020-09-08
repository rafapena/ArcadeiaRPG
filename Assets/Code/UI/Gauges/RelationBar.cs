using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;

// Source code (Interface interactions): https://forum.unity.com/threads/button-keyboard-and-mouse-highlighting.294147/#post-3080308

public class RelationBar : MonoBehaviour
{
    public TextMeshProUGUI Status;
    public Image Bar;
    public GridLayoutGroup Circles;

    private Color PastCircleColor = new Color(0.6f, 1f, 0.6f);

    public void Setup(PlayerCompanionship companionShip)
    {
        Status.text = companionShip.Player.Name;    // BattleMaster.CompanionshipLevels[companionShip.Level];
        RectTransform rt = Bar.transform.GetComponent<RectTransform>();
        float xLength = (Circles.cellSize.x + Circles.spacing.x) * (BattleMaster.CompanionshipLevels.Length - 1);
        rt.sizeDelta = new Vector3(xLength, rt.sizeDelta.y);
        for (int i = 0; i < companionShip.Level; i++)
            Circles.transform.GetChild(i).GetComponent<Image>().color = PastCircleColor;
    }
}