using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GaugePercentage : GaugeWithText
{
    public int Percentage { get; private set; }

    protected override void SetBarAmount()
    {
        base.SetBarAmount();
        Label.text = CurrentBarPercentageText();
    }

    protected override void UpdateIncreaseInRealTime()
    {
        base.UpdateIncreaseInRealTime();
        Label.text = CurrentBarPercentageText();
    }

    protected override void UpdateDecreaseInRealTime()
    {
        base.UpdateDecreaseInRealTime();
        Label.text = CurrentBarPercentageText();
    }

    public override void Set(float current, float max)
    {
        Percentage = (int)(current / max * 100);
        base.Set(current, max);
        Label.text = Percentage + "%";
    }

    public override void Fill()
    {
        Percentage = 100;
        base.Fill();
        Label.text = "100%";
    }

    public override void Empty()
    {
        base.Empty();
        Percentage = 0;
        Label.text = "0%";
    }

    protected override bool SurpassedCap()
    {
        return Percentage > 100;
    }

    private string CurrentBarPercentageText()
    {
        return Percentage + "%";
    }
}
