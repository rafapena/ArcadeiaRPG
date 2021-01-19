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
    private const float DEFAULT_TEXT_DELAY = 0.02f;

    public static BattleAlly[] Allies { get; private set; }

    public static BattlerClass[] Classes { get; private set; }
    
    public static BattleEnemy[] Enemies { get; private set; }

    public static BattlePlayer[] Players { get; private set; }

    public static EnemyParty[] EnemyParties { get; private set; }
    
    public static Environment[] Environments { get; private set; }
    
    public static Item[] Items { get; private set; }
    
    public static Objective[] Objectives { get; private set; }

    public static PassiveSkill[] PassiveSkills { get; private set; }
    
    public static SoloSkill[] SoloSkills { get; private set; }
    
    public static State[] States { get; private set; }
    
    public static TeamSkill[] TeamSkills { get; private set; }
    
    public static Weapon[] Weapons { get; private set; }


    private void Awake()
    {
        if (IsSetup) return;
        IsSetup = true;
        SetupOptions();
        SetupLists();   
    }

    private void SetupOptions()
    {
        if (PlayerPrefs.HasKey(GameplayMaster.OPTIONS_SETUP)) return;
        PlayerPrefs.SetInt(GameplayMaster.OPTIONS_SETUP, 1);
        PlayerPrefs.SetFloat(GameplayMaster.MASTER_MUSIC_VOLUME, DEFAULT_MASTER_MUSIC_VOLUME);
        PlayerPrefs.SetFloat(GameplayMaster.MASTER_SFX_VOLUME, DEFAULT_MASTER_SFX_VOLUME);
        PlayerPrefs.SetFloat(GameplayMaster.TEXT_DELAY, DEFAULT_TEXT_DELAY);
    }

    private void SetupLists()
    {
        Allies = SortById(Resources.LoadAll<BattleAlly>("Prefabs/Allies"));
        Players = SortById(Resources.LoadAll<BattlePlayer>("Prefabs/BattlePlayers"));
        Classes = SortById(Resources.LoadAll<BattlerClass>("Prefabs/BattlerClasses"));
        Enemies = SortById(Resources.LoadAll<BattleEnemy>("Prefabs/BattleEnemies"));
        EnemyParties = SortById(Resources.LoadAll<EnemyParty>("Prefabs/EnemyParties"));
        Environments = SortById(Resources.LoadAll<Environment>("Prefabs/Environments"));
        Items = SortById(Resources.LoadAll<Item>("Prefabs/Items"));
        Objectives = SortById(Resources.LoadAll<Objective>("Prefabs/Objectives"));
        PassiveSkills = SortById(Resources.LoadAll<PassiveSkill>("Prefabs/PassiveSkills"));
        SoloSkills = SortById(Resources.LoadAll<SoloSkill>("Prefabs/SoloSkills"));
        States = SortById(Resources.LoadAll<State>("Prefabs/States"));
        TeamSkills = SortById(Resources.LoadAll<TeamSkill>("Prefabs/TeamSkills"));
        Weapons = SortById(Resources.LoadAll<Weapon>("Prefabs/Weapons"));
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Sorting --
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static T[] SortById<T>(T[] arr) where T : BaseObject
    {
        QSort(ref arr, 0, arr.Length - 1);
        return arr;
    }

    private static void QSort<T>(ref T[] arr, int low, int high) where T : BaseObject
    {
        if (low >= high) return;
        int pi = RandomizedPartition(ref arr, low, high);
        QSort(ref arr, low, pi - 1);
        QSort(ref arr, pi + 1, high);
    }

    private static int RandomizedPartition<T>(ref T[] arr, int low, int high) where T : BaseObject
    {
        int i = Random.Range(low, high);
        T pivot = arr[i];
        arr[i] = arr[high];
        arr[high] = pivot;
        return Partition(ref arr, low, high);
    }

    private static int Partition<T>(ref T[] arr, int low, int high) where T : BaseObject
    {
        T pivot = arr[high];
        int i = low;
        for (int j = low; j < high; j++)
        {
            if (arr[j].Id > pivot.Id) continue;
            T temp = arr[j];
            arr[j] = arr[i];
            arr[i] = temp;
            i++;
        }
        arr[high] = arr[i];
        arr[i] = pivot;
        return i;
    }
}
