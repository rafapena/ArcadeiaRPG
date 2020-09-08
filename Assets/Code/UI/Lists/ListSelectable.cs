using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;

// Source code (Interface interactions): https://forum.unity.com/threads/button-keyboard-and-mouse-highlighting.294147/#post-3080308

[RequireComponent(typeof(Selectable))]
public class ListSelectable : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    // Used if data attached to the selectable is needed; not used, otherwise
    public int Index;

    // Input functions
    public UnityEvent OnHoverInput;
    public UnityEvent OnDeselectInput;

    // Main colors
    private Color NormalColor;
    private Color DisabledColor;

    public void Awake()
    {
        ColorBlock cb = GetComponent<Selectable>().colors;
        NormalColor = cb.normalColor;
        DisabledColor = cb.disabledColor;
    }

    public void SetMainAlpha(float a)
    {
        NormalColor.a = a;
        DisabledColor.a = a;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mouse won't bother with keyboard-only navigation
        bool IdleMouse = Input.GetAxis("Mouse X") == 0 && Input.GetAxis("Mouse Y") == 0;
        if (IdleMouse && Input.GetAxis("Mouse ScrollWheel") == 0f)
        {
            Selectable s = GetComponent<Selectable>();
            s.OnPointerExit(null);
            if (!s.interactable) DeselectDisabled();
            return;
        }

        if (EventSystem.current.alreadySelecting || !gameObject.GetComponent<Selectable>().interactable) return;
        EventSystem.current.SetSelectedGameObject(gameObject);
        if (OnHoverInput != null) OnHoverInput.Invoke();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!gameObject.GetComponent<Selectable>().interactable) SelectDisabled();
        else if (OnHoverInput != null) OnHoverInput.Invoke();
    }


    public void OnDeselect(BaseEventData eventData)
    {
        Selectable s = GetComponent<Selectable>();
        s.OnPointerExit(null);
        if (!s.interactable) DeselectDisabled();
        else if (OnDeselectInput != null) OnDeselectInput.Invoke();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Manage color --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private Color Blend(Color c1, Color c2)
    {
        float r = (c1.r + c2.r) * 0.5f;
        float g = (c1.g + c2.g) * 0.5f;
        float b = (c1.b + c2.b) * 0.5f;
        return new Color(r, g, b);
    }

    public void KeepSelected()
    {
        ColorBlock colors = GetComponent<Button>().colors;
        colors.normalColor = GetComponent<Button>().colors.highlightedColor;
        GetComponent<Button>().colors = colors;
    }

    public void KeepHighlighted()
    {
        ColorBlock colors = GetComponent<Button>().colors;
        colors.normalColor = GetComponent<Button>().colors.selectedColor;
        GetComponent<Button>().colors = colors;
    }

    public void ClearHighlights()
    {
        ColorBlock colors = GetComponent<Button>().colors;
        colors.normalColor = NormalColor;
        colors.disabledColor = DisabledColor;
        GetComponent<Button>().colors = colors;
    }

    private void SelectDisabled()
    {
        ColorBlock colors = GetComponent<Button>().colors;
        colors.disabledColor = Blend(colors.selectedColor, colors.disabledColor);
        GetComponent<Button>().colors = colors;
    }

    private void DeselectDisabled()
    {
        ColorBlock colors = GetComponent<Button>().colors;
        colors.disabledColor = DisabledColor;
        GetComponent<Button>().colors = colors;
    }
}