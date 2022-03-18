using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class StatsList : MonoBehaviour
{
    private int NUMBER_OF_STATS = 7;
    private Color PositiveColor;
    private Color NegativeColor = new Color(0.9f, 0.4f, 0.4f);

    public TextMeshProUGUI Str;
    public TextMeshProUGUI Def;
    public TextMeshProUGUI Map;
    public TextMeshProUGUI Mar;
    public TextMeshProUGUI Spd;
    public TextMeshProUGUI Tec;
    public TextMeshProUGUI Luk;
    public TextMeshProUGUI StrBoost;
    public TextMeshProUGUI DefBoost;
    public TextMeshProUGUI MapBoost;
    public TextMeshProUGUI MarBoost;
    public TextMeshProUGUI SpdBoost;
    public TextMeshProUGUI TecBoost;
    public TextMeshProUGUI LukBoost;

    private void Awake()
    {
        PositiveColor = StrBoost.color;
    }

    public void Setup(BattlePlayer player)
    {
        Str.text = player.Stats.Atk.ToString();
        Def.text = player.Stats.Def.ToString();
        Map.text = player.Stats.Map.ToString();
        Mar.text = player.Stats.Mar.ToString();
        Spd.text = player.Stats.Spd.ToString();
        Tec.text = player.Stats.Tec.ToString();
        Luk.text = player.Stats.Luk.ToString();
        int[] statsListBoosts = GetCurrentStatBoosts(player);
        StrBoost = SetStatText(StrBoost, statsListBoosts[0]);
        DefBoost = SetStatText(DefBoost, statsListBoosts[1]);
        MapBoost = SetStatText(MapBoost, statsListBoosts[2]);
        MarBoost = SetStatText(MarBoost, statsListBoosts[3]);
        SpdBoost = SetStatText(SpdBoost, statsListBoosts[4]);
        TecBoost = SetStatText(TecBoost, statsListBoosts[5]);
        LukBoost = SetStatText(LukBoost, statsListBoosts[6]);
    }

    private int[] GetCurrentStatBoosts(BattlePlayer player)
    {
        int[] stats = new int[NUMBER_OF_STATS];
        foreach (PlayerRelation pr in player.Relations)
        {
            if (pr == null) continue;
            stats[0] += GetStatBoost(pr, pr.Player.NaturalStats.Atk);
            stats[1] += GetStatBoost(pr, pr.Player.NaturalStats.Def);
            stats[2] += GetStatBoost(pr, pr.Player.NaturalStats.Map);
            stats[3] += GetStatBoost(pr, pr.Player.NaturalStats.Mar);
            stats[4] += GetStatBoost(pr, pr.Player.NaturalStats.Spd);
            stats[5] += GetStatBoost(pr, pr.Player.NaturalStats.Tec);
            stats[6] += GetStatBoost(pr, pr.Player.NaturalStats.Luk);
        }
        return stats;
    }

    private int GetStatBoost(PlayerRelation pc, int stat)
    {
        if (stat < 0) return (int)((pc.Level - 1) * stat / 3f);
        return (pc.Level - 1) * stat;
    }

    private TextMeshProUGUI SetStatText(TextMeshProUGUI boost, int stat)
    {
        if (stat < 0)
        {
            boost.color = NegativeColor;
            boost.text = stat.ToString();
        }
        else if (stat > 0)
        {
            boost.color = PositiveColor;
            boost.text = "+" + stat;
        }
        else boost.text = "";
        return boost;
    }
}