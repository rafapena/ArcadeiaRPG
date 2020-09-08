using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    public static Dictionary<string, AudioSource> SFX { get; private set; }
    private bool SFXSetup;
    private static readonly string[] SoundEffects = new string[]
    {
        "ButtonClick",
        "BattleStart"
    };

    public static Dictionary<string, AudioSource> BGM { get; private set; }
    private bool BGMSetup;
    private static readonly string[] BackgroundMusic = new string[]
    {
        "OpeningTheme",
        "GameOver",
    };
}
