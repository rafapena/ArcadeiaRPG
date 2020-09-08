using UnityEngine;
using UnityEngine.UI;

public class Gauge : MonoBehaviour
{
    protected enum BarChangeModes { None, Increasing, Decreasing }

    public Image Bar;
    protected float CurrentAmount;
    protected BarChangeModes BarChangeMode;

    private float PrevTime;
    private float NextTime;

    protected virtual void Awake()
    {
        BarChangeMode = BarChangeModes.None;
    }

    protected virtual void Update()
    {
        switch (BarChangeMode)
        {
            case BarChangeModes.None:
                break;
            case BarChangeModes.Increasing:
                if (Bar.fillAmount >= CurrentAmount) SetBarAmount();
                else UpdateIncreaseInRealTime();
                break;
            case BarChangeModes.Decreasing:
                if (Bar.fillAmount <= CurrentAmount) SetBarAmount();
                else UpdateDecreaseInRealTime();
                break;
        }
    }
    
    protected virtual void SetBarAmount()
    {
        Bar.fillAmount = CurrentAmount;
        BarChangeMode = BarChangeModes.None;
    }

    protected virtual void UpdateIncreaseInRealTime()
    {
        Bar.fillAmount = Time.realtimeSinceStartup - PrevTime;
    }

    protected virtual void UpdateDecreaseInRealTime()
    {
        Bar.fillAmount = NextTime - Time.realtimeSinceStartup;
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- The actual operations --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public virtual void Set(float current, float max)
    {
        CurrentAmount = current / max;
        Bar.fillAmount = CurrentAmount;
    }

    public virtual void SetAndAnimate(float current, float max)
    {
        CurrentAmount = current / max;
        if (Bar.fillAmount > CurrentAmount) BarChangeMode = BarChangeModes.Decreasing;
        else if (Bar.fillAmount < CurrentAmount) BarChangeMode = BarChangeModes.Increasing;
        else BarChangeMode = BarChangeModes.None;
        PrevTime = Time.realtimeSinceStartup - Bar.fillAmount;
        NextTime = Time.realtimeSinceStartup + Bar.fillAmount;
    }

    public float GetPercent()
    {
        return Bar.fillAmount * 100f;
    }

    public virtual void Fill()
    {
        CurrentAmount = 1;
        Bar.fillAmount = 1;
    }

    public virtual void Empty()
    {
        CurrentAmount = 0;
        Bar.fillAmount = 0;
    }

    public bool IsChanging()
    {
        return BarChangeMode != BarChangeModes.None;
    }
}
