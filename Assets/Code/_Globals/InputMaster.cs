using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InputMaster
{
    public static bool ProceedInMenu => ReadyForMenuInput && (Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0));

    public static bool GoingBack => ReadyForMenuInput && (Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape));

    public static bool Interact => ReadyForGameplayInput && (Input.GetKeyDown(KeyCode.Z) || Input.GetMouseButtonDown(0));

    public static bool Pause => ReadyForGameplayInput && Input.GetKeyDown(KeyCode.P);

    public static bool FileSelect => ReadyForGameplayInput && Input.GetKeyDown(KeyCode.Q);

    public static bool MapMenu => ReadyForGameplayInput && Input.GetKeyDown(KeyCode.Space);

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Setup for input --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private static bool ReadyForGameplayInput => MenuMaster.ReadyToSelectInMenu && MenuMaster.ReadyToSelectInGameplay && !SceneMaster.InMenu;

    private static bool ReadyForMenuInput => MenuMaster.ReadyToSelectInMenu && SceneMaster.InMenu;
}
