using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class StatsList : MonoBehaviour
{
    private PlayerParty Party;
    private const int NUMBER_OF_STATS = 8;

    public TextMeshProUGUI MaxHP;
    public TextMeshProUGUI Str;
    public TextMeshProUGUI Def;
    public TextMeshProUGUI Map;
    public TextMeshProUGUI Mar;
    public TextMeshProUGUI Rec;
    public TextMeshProUGUI Spd;
    public TextMeshProUGUI Tec;
    public TextMeshProUGUI MaxHPBoost;
    public TextMeshProUGUI StrBoost;
    public TextMeshProUGUI DefBoost;
    public TextMeshProUGUI MapBoost;
    public TextMeshProUGUI MarBoost;
    public TextMeshProUGUI RecBoost;
    public TextMeshProUGUI SpdBoost;
    public TextMeshProUGUI TecBoost;

    private int[] StatsListBoosts;

    private void Awake()
    {
        StatsListBoosts = new int[NUMBER_OF_STATS];
    }

    public void Setup(BattlePlayer player, PlayerParty party = null)
    {
        Party = party;
        SetValues(player);
        SetRelationStatBoosts(player);
        SetStatBoosts();
    }

    public void Setup(BattlePlayer player, Stats otherStats, PlayerParty party = null)
    {
        Party = party;
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
        Rec.text = player.Stats.Rec.ToString();
        Spd.text = player.Stats.Spd.ToString();
        Tec.text = player.Stats.Tec.ToString();
    }

    private void SetStatBoosts()
    {
        MenuMaster.SetNumberBoost(MaxHPBoost, StatsListBoosts[0]);
        MenuMaster.SetNumberBoost(StrBoost, StatsListBoosts[1]);
        MenuMaster.SetNumberBoost(DefBoost, StatsListBoosts[2]);
        MenuMaster.SetNumberBoost(MapBoost, StatsListBoosts[3]);
        MenuMaster.SetNumberBoost(MarBoost, StatsListBoosts[4]);
        MenuMaster.SetNumberBoost(RecBoost, StatsListBoosts[5]);
        MenuMaster.SetNumberBoost(SpdBoost, StatsListBoosts[6]);
        MenuMaster.SetNumberBoost(TecBoost, StatsListBoosts[7]);
    }

    private void SetDiffStatBoosts(BattlePlayer player, Stats other)
    {
        StatsListBoosts[0] = other.MaxHP - player.Stats.MaxHP;
        StatsListBoosts[1] = other.Atk - player.Stats.Atk;
        StatsListBoosts[2] = other.Def - player.Stats.Def;
        StatsListBoosts[3] = other.Map - player.Stats.Map;
        StatsListBoosts[4] = other.Mar - player.Stats.Mar;
        StatsListBoosts[5] = other.Rec - player.Stats.Rec;
        StatsListBoosts[6] = other.Spd - player.Stats.Spd;
        StatsListBoosts[7] = other.Tec - player.Stats.Tec;
    }

    private void SetRelationStatBoosts(BattlePlayer player)
    {
        if (Party == null) return;
        IEnumerable<PlayerRelation> relations = Party.Relations.Where(x => x.PlayerInRelation(player));
        foreach (PlayerRelation pr in relations)
        {
            BattlePlayer p = pr.GetOtherPlayerInRelationWith(player);
            StatsListBoosts[0] += SetRelationStatBoost(pr, p.NaturalStats.MaxHP);
            StatsListBoosts[1] += SetRelationStatBoost(pr, p.NaturalStats.Atk);
            StatsListBoosts[2] += SetRelationStatBoost(pr, p.NaturalStats.Def);
            StatsListBoosts[3] += SetRelationStatBoost(pr, p.NaturalStats.Map);
            StatsListBoosts[4] += SetRelationStatBoost(pr, p.NaturalStats.Mar);
            StatsListBoosts[5] += SetRelationStatBoost(pr, p.NaturalStats.Rec);
            StatsListBoosts[6] += SetRelationStatBoost(pr, p.NaturalStats.Spd);
            StatsListBoosts[7] += SetRelationStatBoost(pr, p.NaturalStats.Tec);
        }
    }

    private int SetRelationStatBoost(PlayerRelation pc, int stat)
    {
        if (stat < 0) return (int)((pc.Level - 1) * stat / 3f);
        return (pc.Level - 1) * stat;
    }
}