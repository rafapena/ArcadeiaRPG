using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public abstract class GaugeWithText : Gauge
{
    public TextMeshProUGUI Title;
    public TextMeshProUGUI Label;   // Used by child subclasses
    public bool KeepTextColorOnEmpty;

    protected static Color NormalColor;
    protected static Color EmptyBarTextColor;
    protected static Color SurpassedCapColor = new Color(0.8f, 0.2f, 0.2f);
    protected static Color KOdColor = new Color(0.8f, 0.1f, 0.1f);

    protected override void Awake()
    {
        base.Awake();
        NormalColor = Color.white;
        EmptyBarTextColor = KeepTextColorOnEmpty ? NormalColor : KOdColor;
    }

    protected override void SetBarAmount()
    {
        base.SetBarAmount();
        SetColors();
    }

    public override void Set(float current, float max)
    {
        base.Set(current, max);
        SetColors();
    }

    public override void Fill()
    {
        base.Fill();
        if (Title) Title.color = NormalColor;
    }

    public override void Empty()
    {
        base.Empty();
        if (Title) Title.color = EmptyBarTextColor;
    }

    protected abstract bool SurpassedCap();

    private void SetColors()
    {
        if (SurpassedCap())
        {
            if (Title) Title.color = SurpassedCapColor;
            Label.color = SurpassedCapColor;
            Bar.color = SurpassedCapColor;
        }
        else if (NotEmpty())
        {
            if (Title) Title.color = NormalColor;
            Label.color = NormalColor;
            Bar.color = NormalColor;
        }
        else
        {
            if (Title) Title.color = EmptyBarTextColor;
            Label.color = EmptyBarTextColor;
        }
    }
}
