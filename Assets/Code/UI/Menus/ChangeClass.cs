using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

public class ChangeClass : MonoBehaviour
{
    private enum Selections { None, SelectCharacter, SelectClass, ChangingClass, ChangeClass }

    public Item ScrollToUse;

    private void Start()
    {
        //
    }

    void Update()
    {
        //
    }
}