using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class StatsList : MonoBehaviour
{
    private const int NUMBER_OF_STATS = 8;

    public TextMeshProUGUI MaxHP;
    public TextMeshProUGUI Str;
    public TextMeshProUGUI Def;
    public TextMeshProUGUI Map;
    public TextMeshProUGUI Mar;
    public TextMeshProUGUI Spd;
    public TextMeshProUGUI Tec;
    public TextMeshProUGUI Luk;
    public TextMeshProUGUI MaxHPBoost;
    public TextMeshProUGUI StrBoost;
    public TextMeshProUGUI DefBoost;
    public TextMeshProUGUI MapBoost;
    public TextMeshProUGUI MarBoost;
    public TextMeshProUGUI SpdBoost;
    public TextMeshProUGUI TecBoost;
    public TextMeshProUGUI LukBoost;

    private int[] StatsListBoosts;

    private void Awake()
    {
        StatsListBoosts = new int[NUMBER_OF_STATS];
    }

    public void Setup(BattlePlayer player)
    {
        SetValues(player);
        SetRelationStatBoosts(player);
        SetStatBoosts();
    }

    public void Setup(BattlePlayer player, Stats otherStats)
    {
        SetValues(player);
        SetDiffStatBoosts(player, otherStats);
        SetStatBoosts();
    }

    private void SetValues(BattlePlayer player)
    {
        MaxHP.text = player.Stats.MaxHP.ToString();
        Str.text = player.Stats.Atk.ToString();
        Def.text = player.Stats.Def.ToString();
        Map.text = player.Stats.Map.ToString();
        Mar.text = player.Stats.Mar.ToString();
        Spd.text = player.Stats.Spd.ToString();
        Tec.text = player.Stats.Tec.ToString();
        Luk.text = player.Stats.Luk.ToString();
    }

    private void SetStatBoosts()
    {
        MenuMaster.SetNumberBoost(MaxHPBoost, StatsListBoosts[0]);
        MenuMaster.SetNumberBoost(StrBoost, StatsListBoosts[1]);
        MenuMaster.SetNumberBoost(DefBoost, StatsListBoosts[2]);
        MenuMaster.SetNumberBoost(MapBoost, StatsListBoosts[3]);
        MenuMaster.SetNumberBoost(MarBoost, StatsListBoosts[4]);
        MenuMaster.SetNumberBoost(SpdBoost, StatsListBoosts[5]);
        MenuMaster.SetNumberBoost(TecBoost, StatsListBoosts[6]);
        MenuMaster.SetNumberBoost(LukBoost, StatsListBoosts[7]);
    }

    private void SetDiffStatBoosts(BattlePlayer player, Stats other)
    {
        StatsListBoosts[0] = other.MaxHP - player.Stats.MaxHP;
        StatsListBoosts[1] = other.Atk - player.Stats.Atk;
        StatsListBoosts[2] = other.Def - player.Stats.Def;
        StatsListBoosts[3] = other.Map - player.Stats.Map;
        StatsListBoosts[4] = other.Mar - player.Stats.Mar;
        StatsListBoosts[5] = other.Spd - player.Stats.Spd;
        StatsListBoosts[6] = other.Tec - player.Stats.Tec;
        StatsListBoosts[7] = other.Luk - player.Stats.Luk;
    }

    private void SetRelationStatBoosts(BattlePlayer player)
    {
        foreach (PlayerRelation pr in player.Relations)
        {
            if (pr == null) continue;
            StatsListBoosts[0] += SetRelationStatBoost(pr, pr.Player.NaturalStats.MaxHP);
            StatsListBoosts[1] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Atk);
            StatsListBoosts[2] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Def);
            StatsListBoosts[3] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Map);
            StatsListBoosts[4] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Mar);
            StatsListBoosts[5] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Spd);
            StatsListBoosts[6] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Tec);
            StatsListBoosts[7] += SetRelationStatBoost(pr, pr.Player.NaturalStats.Luk);
        }
    }

    private int SetRelationStatBoost(PlayerRelation pc, int stat)
    {
        if (stat < 0) return (int)((pc.Level - 1) * stat / 3f);
        return (pc.Level - 1) * stat;
    }
}