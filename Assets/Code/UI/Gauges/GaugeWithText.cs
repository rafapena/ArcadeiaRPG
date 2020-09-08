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
    
    private Color NormalColor;
    private Color EmptyBarTextColor;
    private Color KOdColor = new Color(1f, 0.4f, 0.4f);

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
        Title.color = NormalColor;
    }

    public override void Empty()
    {
        base.Empty();
        Title.color = EmptyBarTextColor;
    }

    private void SetColors()
    {
        if (CurrentAmount > 0)
        {
            Title.color = NormalColor;
            Label.color = NormalColor;
        }
        else
        {
            Title.color = EmptyBarTextColor;
            Label.color = EmptyBarTextColor;
        }
    }
}
