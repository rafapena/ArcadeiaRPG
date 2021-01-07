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
        return Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Backspace);
    }

    public static bool Interact()
    {
        return Input.GetKey(KeyCode.Z);
    }
}
