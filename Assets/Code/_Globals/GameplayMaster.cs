using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayMaster : MonoBehaviour
{
    public enum Difficulties { Easy, Medium, Hard, Lunatic }
    public static Difficulties Difficulty;
    public MapMaster.Locations Location;

    public enum TextSpeeds { Slow, Medium, Fast }
    private static float[] TextSpeedNumbers = new float[] { 0.05f, 0.02f, 0.004f };

    public static int MAX_LOADED_FILE_CONTENT = 2;
    private static int LoadedFile;
    public static int SelectedFile;
    public static int Chapter;
    public const string OPTIONS_SETUP = "OptionsSetup";

    public const string MASTER_MUSIC_VOLUME = "MasterMusicVolume";
    public const string MASTER_SFX_VOLUME = "MasterSFXVolume";
    public GameObject BGM_List;

    public static float TextSpeed;
    public const string TEXT_DELAY_INDEX = "TextDelay";

    public static float TotalPlayTime;
    public static int InGameDay;
    public static int InGameTime;
    private static float InGameTimeCounter;
    public float InGameMinuteIncrementFrequency;

    public static PlayerParty Party;
    public static Shopkeeper Shop;

    private void Awake()
    {
        MapMaster.SetScene(gameObject);
        MapMaster.CurrentLocation = Location.ToString().Replace("_", " ");
        SetupAudio();
        SetTextSpeed(PlayerPrefs.GetInt(TEXT_DELAY_INDEX));
    }

    private void Update()
    {
        TotalPlayTime += Time.unscaledDeltaTime;
        if (SceneMaster.InBattle) return;
        InGameTimeCounter += Time.deltaTime;
        if (InGameTimeCounter > InGameMinuteIncrementFrequency)
        {
            InGameTimeCounter -= InGameMinuteIncrementFrequency;
            InGameTime++;
            if (InGameTime >= 1440)
            {
                InGameDay++;
                InGameTime = 0;
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Options --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupAudio()
    {
        float m = PlayerPrefs.GetFloat(MASTER_MUSIC_VOLUME);
        float s = PlayerPrefs.GetFloat(MASTER_SFX_VOLUME);
        foreach (Transform t in BGM_List.transform)
        {
            t.gameObject.GetComponent<AudioSource>().volume = m;
        }
    }

    public static void SetTextSpeed(int index)
    {
        TextSpeed = TextSpeedNumbers[index];
        PlayerPrefs.SetInt(TEXT_DELAY_INDEX, index);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- File Management --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void ResetLoad()
    {
        LoadedFile = 0;
    }

    public static void DeclareContentLoaded()
    {
        LoadedFile++;
    }

    public static bool FinishedLoadingContent()
    {
        return LoadedFile >= MAX_LOADED_FILE_CONTENT;
    }

    public static bool NoFileSelected()
    {
        return SelectedFile < 0;
    }

    public static int GetLastManagedFile()
    {
        SelectedFile = PlayerPrefs.GetInt("LastManagedFile");
        return SelectedFile;
    }

    public static void SetLastManagedFile(int file)
    {
        SelectedFile = file;
        PlayerPrefs.SetInt("LastManagedFile", file);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Time --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static string GetInGameTime()
    {
        int h = InGameTime / 60;
        int m = InGameTime % 60;
        string ampm = "AM";
        if (h >= 12)
        {
            h -= 12;
            ampm = "PM";
        }
        string hour = (h == 0) ? "12" : h.ToString();
        string minute = (m < 10) ? ("0" + m) : m.ToString();
        return hour + ":" + minute + " " + ampm;
    }

    public static string GetTotalPlayTime(int t = -1)
    {
        if (t < 0) t = (int)TotalPlayTime;
        int hour = t / 3600;
        int minute = (t % 3600) / 60;
        int second = (t % 3600) % 60;
        string min = (minute < 10) ? ("0" + minute) : minute.ToString();
        string sec = (second < 10) ? ("0" + second) : second.ToString();
        return hour + ":" + min + ":" + sec;
    }

    public static void FastForwardInGameTime(int minutesAdded)
    {
        if (minutesAdded < 0) return;
        InGameTime += minutesAdded;
        InGameTimeCounter = 0;
        while (InGameTime >= 1440)
        {
            InGameDay++;
            InGameTime -= 1440;
        }
    }

    public static void SetInGameTime(int hour, int minute)
    {
        if (hour >= 24 || hour < 0 || minute >= 60 || minute < 0) return;
        InGameTime = hour * 60 + minute;
        InGameTimeCounter = 0;
    }
}
