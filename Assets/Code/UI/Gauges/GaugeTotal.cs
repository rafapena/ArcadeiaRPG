using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GaugeTotal : GaugeWithText
{
    public int Current { get; private set; }

    public int Max { get; private set; }

    protected override void SetBarAmount()
    {
        base.SetBarAmount();
        Label.text = Current + " / " + Max;
    }

    protected override void UpdateIncreaseInRealTime()
    {
        base.UpdateIncreaseInRealTime();
        Label.text = Math.Round(Bar.fillAmount * Max) + " / " + Max;
    }

    protected override void UpdateDecreaseInRealTime()
    {
        base.UpdateDecreaseInRealTime();
        Label.text = Math.Round(Bar.fillAmount * Max) + " / " + Max;
    }

    public override void Set(float current, float max)
    {
        Current = (int)current;
        Max = (int)max;
        Label.text = Current + " / " + Max;
        base.Set(current, max);
    }

    public override void SetAndAnimate(float current, float max)
    {
        Current = (int)current;
        Max = (int)max;
        base.SetAndAnimate(current, max);
    }

    public override void Fill()
    {
        base.Fill();
        Label.text = Max + " / " + Max;
    }

    public override void Empty()
    {
        base.Empty();
        Label.text = "0 / " + Max;
    }

    protected override bool SurpassedCap()
    {
        return Current > Max;
    }
}
