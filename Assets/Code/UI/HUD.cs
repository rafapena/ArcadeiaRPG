using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public MenuFrame LocationFrame;
    public TextMeshProUGUI LocationName;

    private int LocationWindowStage;
    private float LocationWindowTimer;
    private float LOCATION_WINDOW_SPAWN_TIME = 2f;
    private float LOCATION_WINDOW_DESPAWN_TIME = 3f;

    // Start is called before the first frame update
    void Start()
    {
        if (SceneMaster.InBattle) LocationWindowStage = -1;
        LocationName.text = MapMaster.CurrentLocation;
        LocationWindowTimer = Time.unscaledTime + LOCATION_WINDOW_SPAWN_TIME;
    }

    // Update is called once per frame
    void Update()
    {
        if (LocationWindowStage == 0 && Time.unscaledTime > LocationWindowTimer)
        {
            LocationWindowTimer = Time.unscaledTime + LOCATION_WINDOW_DESPAWN_TIME;
            LocationFrame.Activate();
            LocationWindowStage++;
        }
        else if (LocationWindowStage == 1 && Time.unscaledTime > LocationWindowTimer)
        {
            LocationFrame.Deactivate();
            LocationWindowStage++;
        }
    }
}
