using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;

// Source code (Interface interactions): https://forum.unity.com/threads/button-keyboard-and-mouse-highlighting.294147/#post-3080308

public class RelationBar : MonoBehaviour
{
    public TextMeshProUGUI Name;
    public Image Image;
    public Gauge Gauge;
    public Transform StarsList;
    public Sprite WholeStar;
    public Sprite EmptyStar;

    public void Refresh(BattlePlayer p, PlayerRelation relation)
    {
        Name.text = relation.GetOtherPlayerInRelationWith(p).Name;
        int min = relation.Level == 0 ? 0 : PlayerRelation.PointMarkers[relation.Level - 1];
        int current = relation.Points - min;
        int max = relation.Level == PlayerRelation.PointMarkers.Length ? current : (PlayerRelation.PointMarkers[relation.Level] - min);
        Gauge.Set(current, max);
        int i = 0;
        foreach (Transform star in StarsList) star.GetComponent<Image>().sprite = i++ < relation.Level ? WholeStar : EmptyStar;
    }
}