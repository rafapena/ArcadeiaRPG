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

    public static int SelectedFile;
    public static int Chapter;

    public static float TotalPlayTime;
    public static int InGameDay;
    public static int InGameTime;
    private static float InGameTimeCounter;
    public float InGameMinuteIncrementFrequency;

    private void Start()
    {
        if (!SceneMaster.InBattle) MapMaster.CurrentLocation = gameObject.scene.name.Replace("_", " ");
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

    public static bool NoFileSelected()
    {
        return SelectedFile < 0;
    }

    public static int GetLastManagedFile()
    {
        return PlayerPrefs.GetInt("LastManagedFile");
    }

    public static void SetLastManagedFile(int file)
    {
        PlayerPrefs.SetInt("LastManagedFile", file);
    }

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
