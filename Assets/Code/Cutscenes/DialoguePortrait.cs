using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct DialoguePortrait
{
    public MenuFrame Frame;
    public Image Image;

    public static Color HighlightedColor = Color.white;
    public static Color UnhighlightedColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
}
