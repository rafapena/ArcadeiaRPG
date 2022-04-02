using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class NumberUpdater : MonoBehaviour
{
    private TextMeshProUGUI NumberDisplay;
    private int RealValue;
    private int DisplayedValue;
    private float UpdateBuffer;
    private int BoostRate;
    private const int BOOST_RATE_FPS = 60;

    void Start()
    {
        NumberDisplay = gameObject.GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        NumberDisplay.text = DisplayedValue.ToString();
        if (Time.unscaledTime < UpdateBuffer) return;
        DisplayedValue += BoostRate;
        if (DisplayedValue < RealValue && BoostRate < 0 || DisplayedValue > RealValue && BoostRate > 0)
        {
            DisplayedValue = RealValue;
            BoostRate = 0;
        }
    }

    public void Initialize(int value)
    {
        DisplayedValue = value;
        RealValue = value;
    }

    public void Add(ref int number, int value, float updateBufferTime = 0)
    {
        number += value;
        RealValue = number;
        UpdateBuffer = Time.unscaledTime + updateBufferTime;
        int br = (RealValue - DisplayedValue) / BOOST_RATE_FPS;
        if (br == 0)
        {
            if (RealValue > DisplayedValue) BoostRate = 1;
            else if (RealValue < DisplayedValue) BoostRate = -1;
            else BoostRate = 0;
        }
        else BoostRate = br;
    }
}
