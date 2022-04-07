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

    private const int CURRENT_TEXT_SIZE = 125;
    private const int MAX_TEXT_SIZE = 75;

    protected override void SetBarAmount()
    {
        base.SetBarAmount();
        SetLabelText(Current);
    }

    protected override void UpdateIncreaseInRealTime()
    {
        base.UpdateIncreaseInRealTime();
        SetLabelText((int)Math.Round(Bar.fillAmount * Max));
    }

    protected override void UpdateDecreaseInRealTime()
    {
        base.UpdateDecreaseInRealTime();
        SetLabelText((int)Math.Round(Bar.fillAmount * Max));
    }

    public override void Set(float current, float max)
    {
        Current = (int)current;
        Max = (int)max;
        SetLabelText(Current);
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
        SetLabelText(Max);
    }

    public override void Empty()
    {
        base.Empty();
        SetLabelText(0);
    }

    protected override bool SurpassedCap()
    {
        return Current > Max;
    }

    private void SetLabelText(int current)
    {
        Label.text = "<size=" + CURRENT_TEXT_SIZE + "%>" + current + "</size><size=" + MAX_TEXT_SIZE + "%>/" + Max + "</size>";
    }
}
