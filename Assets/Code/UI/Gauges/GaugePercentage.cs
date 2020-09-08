using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class GaugePercentage : GaugeWithText
{
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
        base.Set(current, max);
        int value = (int)(current / max * 100);
        Label.text = value + "%";
    }

    public override void Fill()
    {
        base.Fill();
        Label.text = "100%";
    }

    public override void Empty()
    {
        base.Empty();
        Label.text = "0%";
    }

    private string CurrentBarPercentageText()
    {
        return Math.Round(Bar.fillAmount * 100) + "%";
    }
}
