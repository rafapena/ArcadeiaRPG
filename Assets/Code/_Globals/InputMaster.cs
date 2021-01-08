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
        return Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape);
    }

    public static bool Interact()
    {
        return !SceneMaster.InMenu() && Input.GetKeyDown(KeyCode.Z);
    }

    public static bool Pause()
    {
        return !SceneMaster.InMenu() && Input.GetKeyDown(KeyCode.P);
    }

    public static bool MapMenu()
    {
        return !SceneMaster.InMenu() && Input.GetKeyDown(KeyCode.Space);
    }
}
