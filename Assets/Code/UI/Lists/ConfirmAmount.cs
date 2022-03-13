using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class ConfirmAmount : MonoBehaviour
{
    public GameObject Frame;
    public UnityEvent MoveUp;
    public UnityEvent MoveDown;
    public UnityEvent SelectAmount;
    public Button UpButton;
    public Button DownButton;
    public Button OKButton;

    public int Amount { get; private set; }
    public TextMeshProUGUI DisplayedAmount;
    public int StartingAmount;
    public int Increment = 1;
    
    public int MinValue { get; private set; }

    public int MaxValue { get; private set; }

    private bool InputGoingFast => Time.unscaledTime > RapidInputHoldTime;
    private float RapidInputHoldTime;
    private const float RAPID_INPUT_HOLD_TIME_BUFFER = 0.5f;

    private void Update()
    {
        if (!Frame.activeSelf) return;
        else if (Input.GetAxis("Mouse ScrollWheel") > 0) MoveUpInc();
        else if (Input.GetAxis("Mouse ScrollWheel") < 0) MoveDownInc();
        else KeyboardInputs();
    }

    private void KeyboardInputs()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) StartRapidHoldTime(MoveUpInc);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) StartRapidHoldTime(MoveDownInc);
        else if (Input.GetKey(KeyCode.UpArrow) && InputGoingFast) MoveUpInc();
        else if (Input.GetKey(KeyCode.DownArrow) && InputGoingFast) MoveDownInc();
    }

    private void StartRapidHoldTime(UnityAction func)
    {
        func.Invoke();
        RapidInputHoldTime = Time.unscaledTime + RAPID_INPUT_HOLD_TIME_BUFFER;
    }

    public void Activate(int minValue, int maxValue, bool useLastValue = false)
    {
        Frame.SetActive(true);
        if (!useLastValue) Amount = StartingAmount;

        MinValue = minValue;
        MaxValue = maxValue;
        if (MinValue > MaxValue) MinValue = MaxValue;
        if (StartingAmount > MaxValue) StartingAmount = MaxValue;
        if (StartingAmount < MinValue) StartingAmount = MinValue;

        bool overflow = Amount >= MaxValue;
        bool underflow = Amount <= MinValue;
        if (overflow) Amount = MaxValue;
        if (underflow) Amount = MinValue;
        UpButton.interactable = !overflow;
        DownButton.interactable = !underflow;

        DisplayedAmount.text = Amount.ToString();
        EventSystem.current.SetSelectedGameObject(OKButton.gameObject);
    }

    public void Deactivate()
    {
        Frame.SetActive(false);
    }

    public void MoveUpInc()
    {
        if (Amount == MaxValue) return;
        Amount += Increment;
        if (Amount >= MaxValue)
        {
            Amount = MaxValue;
            UpButton.interactable = false;
        }
        if (Amount > MinValue) DownButton.interactable = true;
        DisplayedAmount.text = Amount.ToString();
        MoveUp?.Invoke();
    }

    public void MoveDownInc()
    {
        if (Amount == MinValue) return;
        Amount -= Increment;
        if (Amount <= MinValue)
        {
            Amount = MinValue;
            DownButton.interactable = false;
        }
        if (Amount < MaxValue) UpButton.interactable = true;
        DisplayedAmount.text = Amount.ToString();
        MoveDown?.Invoke();
        return;
    }

    public void SelectAmountMain()
    {
        SelectAmount?.Invoke();
        Deactivate();
    }
}