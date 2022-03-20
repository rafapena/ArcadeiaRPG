using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class AITool : MonoBehaviour
{
    public ActiveTool Move;
    public int Priority;
    public int Quantity;
    public int HPLow;
    public int HPHigh;
    public int SPLow;
    public int SPHigh;
    public int AllyCondition;
    public int FoeCondition;
    public int UserCondition;
    public int TargetToolElement;
    public State[] ActiveStates;
    public State[] InactiveStates;

    public ElementRate[] TargetElementRate;
    public StateRate[] TargetStateRates;
    public Stats TargetStatConditions;
}