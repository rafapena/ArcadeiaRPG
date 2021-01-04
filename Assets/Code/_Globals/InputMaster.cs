using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InputMaster : MonoBehaviour
{
    public static bool GoingBack()
    {
        return Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1);
    }
}
