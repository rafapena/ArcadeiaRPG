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

    protected Color NormalTextColor;
    protected Color NormalBarColor;
    protected Color EmptyBarTextColor;
    protected static Color SurpassedCapTextColor = new Color(0.8f, 0.2f, 0.2f);
    protected static Color SurpassedCapBarColor = new Color(0.6f, 0.2f, 0.2f);
    protected static Color KOdColor = new Color(0.6f, 0.1f, 0.1f);

    protected override void Awake()
    {
        base.Awake();
        NormalTextColor = Color.white;
        NormalBarColor = Bar.color;
        EmptyBarTextColor = KeepTextColorOnEmpty ? NormalTextColor : KOdColor;
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
        if (Title) Title.color = NormalTextColor;
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
            if (Title) Title.color = SurpassedCapTextColor;
            Label.color = SurpassedCapTextColor;
            Bar.color = SurpassedCapBarColor;
        }
        else if (!IsEmpty)
        {
            if (Title) Title.color = NormalTextColor;
            Label.color = NormalTextColor;
            Bar.color = NormalBarColor;
        }
        else
        {
            if (Title) Title.color = EmptyBarTextColor;
            Label.color = EmptyBarTextColor;
        }
    }
}
