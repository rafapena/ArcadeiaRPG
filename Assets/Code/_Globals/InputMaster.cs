using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InputMaster
{
    public static bool GoingBack()
    {
        return ReadyToGoBack && (Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape));
    }

    public static bool Interact()
    {
        return ReadyForInput && (Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0));
    }

    public static bool Pause()
    {
        return ReadyForInput && Input.GetKeyDown(KeyCode.P);
    }

    public static bool FileSelect()
    {
        return ReadyForInput && Input.GetKeyDown(KeyCode.Q);
    }

    public static bool MapMenu()
    {
        return ReadyForInput && Input.GetKeyDown(KeyCode.Space);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup for input --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static bool ReadyForInput => MenuMaster.ReadyToSelectInMenu && MenuMaster.ReadyToSelectInGameplay && !SceneMaster.InMenu;

    private static bool ReadyToGoBack => MenuMaster.ReadyToSelectInMenu;
}
