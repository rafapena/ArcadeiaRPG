using UnityEngine;

/// <summary>
/// Helper component for a single image/part in a cutscene. Changes what happens when you advance to a specific part of the scene
/// </summary>
public class FlowJumper : MonoBehaviour
{
    // Tells which section to jump to in the cutscene
    // If either is 0 or lower, the cutscene ends: the other variables below need to be set up, beforehand
    public int ToImage;


    // The following are only relevant when ToImage is == 0 (Also indicating the end of a cutscene)

    // Where the player wil be positioned, on gameplay, right after the cutscene, assuming ToGamePlay is true
    public float StartingPlayerX;
    public float StartingPlayerY;

    // Player goes to scene based on the order of precedence
    public bool ToGamePlay;
    public bool ToYouWinScreen;
    public bool ToGameOverScreen;
    
    // Level
    public int GoToLevel;
}
