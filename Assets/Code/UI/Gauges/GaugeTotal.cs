using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GaugeTotal : GaugeWithText
{
    private int Current;
    private int Max;

    private Color PastCapColor = new Color(1f, 0.8f, 0.2f);

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
        base.Set(current, max);
        Current = (int)current;
        Max = (int)max;
        Label.text = Current + " / " + Max;
    }

    public override void SetAndAnimate(float current, float max)
    {
        base.SetAndAnimate(current, max);
        Current = (int)current;
        Max = (int)max;
    }

    public override void Fill()
    {
        base.Fill();
        string max = GetMax();
        Label.text = max + " / " + max;
    }

    public override void Empty()
    {
        base.Empty();
        Label.text = "0 / " + GetMax();
    }

    private string GetMax()
    {
        string[] s = Label.text.Split(' ');
        return s.Length > 1 ? s[2] : s[0];
    }

    protected override void SetColors()
    {
        base.SetColors();
        /*if (CurrentAmount > Max)
        {
            Title.color = PastCapColor;
            Label.color = PastCapColor;
        }*/
    }

    public bool SurpassedCap()
    {
        return CurrentAmount > Max;
    }
}
