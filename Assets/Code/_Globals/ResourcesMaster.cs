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

    public static BattleAlly[] Allies { get; private set; }

    public static BattlerClass[] Classes { get; private set; }
    
    public static BattleEnemy[] Enemies { get; private set; }

    public static BattlePlayer[] Players { get; private set; }

    public static EnemyParty[] EnemyParties { get; private set; }
    
    public static Environment[] Environments { get; private set; }
    
    public static Item[] Items { get; private set; }
    
    public static Objective[] Objectives { get; private set; }

    public static PassiveEffect[] PassiveEffects { get; private set; }
    
    public static SoloSkill[] SoloSkills { get; private set; }
    
    public static State[] States { get; private set; }
    
    public static TeamSkill[] TeamSkills { get; private set; }
    
    public static Weapon[] Weapons { get; private set; }


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
        Allies = Resources.LoadAll<BattleAlly>("Prefabs/Allies");
        Players = Resources.LoadAll<BattlePlayer>("Prefabs/BattlePlayers");
        Classes = Resources.LoadAll<BattlerClass>("Prefabs/BattlerClasses");
        Enemies = Resources.LoadAll<BattleEnemy>("Prefabs/BattleEnemies");
        EnemyParties = Resources.LoadAll<EnemyParty>("Prefabs/EnemyParties");
        Environments = Resources.LoadAll<Environment>("Prefabs/Environments");
        Items = Resources.LoadAll<Item>("Prefabs/Items");
        Objectives = Resources.LoadAll<Objective>("Prefabs/Objectives");
        PassiveEffects = Resources.LoadAll<PassiveEffect>("Prefabs/PassiveEffects");
        SoloSkills = Resources.LoadAll<SoloSkill>("Prefabs/SoloSkills");
        States = Resources.LoadAll<State>("Prefabs/States");
        TeamSkills = Resources.LoadAll<TeamSkill>("Prefabs/TeamSkills");
        Weapons = Resources.LoadAll<Weapon>("Prefabs/Weapons");
    }
}
