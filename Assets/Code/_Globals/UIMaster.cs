using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIMaster : MonoBehaviour
{
    private static bool Setup;

    public static Dictionary<string, Popup> Popups { get; private set; }
    private static readonly string[] PopupsListFileNames = new string[]
    {
        "HPDamage", "SPDamage", "HPRecover", "SPRecover", "HPDrain", "SPDrain",
        "NoHit", "ExBalloon", "QBalloon", "Button", "LevelUp"
    };

    public static Dictionary<BattleMaster.WeaponTypes, Sprite> WeaponImages { get; private set; }
    private static int IgnoreFirstNElementsW = 1;

    public static Dictionary<BattleMaster.Elements, Sprite> ElementImages { get; private set; }
    private static int IgnoreFirstNElementsE = 1;

    public static Dictionary<string, Sprite> LetterCommands { get; private set; }
    private static readonly string[] LetterCommandFileNames = new string[]
    {
        "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
        "A", "S", "D", "F", "G", "H", "J", "K", "L",
        "Z", "X", "C", "V", "B", "N", "M",
        "1", "2", "3", "4", "5", "6", "7", "8", "9", "0"
    };

    void Start()
    {
        if (Setup) return;
        Setup = true;

        Popups = new Dictionary<string, Popup>();
        foreach (string s in PopupsListFileNames)
            Popups.Add(s, Resources.Load<Popup>("Prefabs/UI/Popups/" + s));

        WeaponImages = new Dictionary<BattleMaster.WeaponTypes, Sprite>();
        var wt = (BattleMaster.WeaponTypes[])System.Enum.GetValues(typeof(BattleMaster.WeaponTypes));
        Sprite[] wi = Resources.LoadAll<Sprite>("UIContent/Menus/WeaponTypes");
        for (int i = 0; i < wi.Length; i++)
            WeaponImages.Add(wt[i + IgnoreFirstNElementsW], wi[i]);

        ElementImages = new Dictionary<BattleMaster.Elements, Sprite>();
        var et = (BattleMaster.Elements[])System.Enum.GetValues(typeof(BattleMaster.Elements));
        Sprite[] ei = Resources.LoadAll<Sprite>("UIContent/Menus/Elements");
        for (int i = 0; i < ei.Length; i++)
            ElementImages.Add(et[i + IgnoreFirstNElementsE], ei[i]);

        LetterCommands = new Dictionary<string, Sprite>();
        Sprite[] kbi = Resources.LoadAll<Sprite>("UIContent/Menus/Keyboard1");
        for (int i = 0; i < kbi.Length; i++)
            LetterCommands.Add(LetterCommandFileNames[i], kbi[i]);
    }
}
