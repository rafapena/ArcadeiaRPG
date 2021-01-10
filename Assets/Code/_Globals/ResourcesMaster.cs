using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResourcesMaster : MonoBehaviour
{
    private bool IsSetup;
    private const float DEFAULT_MASTER_MUSIC_VOLUME = 1f;
    private const float DEFAULT_MASTER_SFX_VOLUME = 0.8f;

    public static List<BattleAlly> Allies;
    public static List<BattlerClass> Classes;
    public static List<BattleEnemy> Enemies;
    public static List<EnemyParty> EnemyParties;
    public static List<Environment> Environments;
    public static List<Item> Items;
    public static List<PassiveEffect> PassiveEffects;
    public static List<BattlePlayer> BattlePlayers;
    public static List<SoloSkill> SoloSkills;
    public static List<State> States; 
    public static List<TeamSkill> TeamSkills;
    public static List<Weapon> Weapon;

    private void Awake()
    {
        if (IsSetup) return;
        IsSetup = true;
        SetupAudio();
        SetupLists();   
    }

    private void SetupAudio()
    {
        if (PlayerPrefs.GetInt(GameplayMaster.AUDIO_SETUP) != 0) return;
        PlayerPrefs.SetInt(GameplayMaster.AUDIO_SETUP, 1);
        PlayerPrefs.SetFloat(GameplayMaster.MASTER_MUSIC_VOLUME, DEFAULT_MASTER_MUSIC_VOLUME);
        PlayerPrefs.SetFloat(GameplayMaster.MASTER_SFX_VOLUME, DEFAULT_MASTER_SFX_VOLUME);
    }

    private void SetupLists()
    {
        Allies = new List<BattleAlly>();
        Classes = new List<BattlerClass>();
        Enemies = new List<BattleEnemy>();
        EnemyParties = new List<EnemyParty>();
        Environments = new List<Environment>();
        Items = new List<Item>();
        PassiveEffects = new List<PassiveEffect>();
        BattlePlayers = new List<BattlePlayer>();
        SoloSkills = new List<SoloSkill>();
        States = new List<State>();
        TeamSkills = new List<TeamSkill>();
        Weapon = new List<Weapon>();
    }
}
