﻿using System.Collections;
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
}
