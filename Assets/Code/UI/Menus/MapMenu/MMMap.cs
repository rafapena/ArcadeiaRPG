using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

public class MMMap : MM_Super, IDragHandler
{
    public enum Selections { None, MoveMap, ConfirmTravel }

    // Child GameObjects
    public MenuFrame LocationFrame;
    public TextMeshProUGUI LocationName;
    public GameObject MapImage;
    public GameObject Tracker;
    public GameObject ConfirmTravelFrame;
    public TextMeshProUGUI ConfirmTravelLocation;
    public MenuFrame[] Borders;
    private Selections Selection;

    public GameObject MapLocationList;
    private ListSelectable SelectedLocationBtn;
    private int SelectedLocationIndex;
    //private LocationInfo SelectedLocation;

    protected override void Update()
    {
        base.Update();
        if (Selection != Selections.MoveMap) return;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveLocation(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) MoveLocation(1);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MapImage.transform.position += (Vector3)eventData.delta;
    }

    public override void Open()
    {
        base.Open();
        SetupMapLocations();
        MapImage.SetActive(true);
        Tracker.SetActive(true);
        Selection = Selections.MoveMap;
        foreach (MenuFrame f in Borders) f.Activate();
    }

    public override void Close()
    {
        base.Close();
    }

    public override void GoBack()
    {
        if (Selection == Selections.MoveMap)
        {
            ReturnToInitialStep();
            Selection = Selections.None;
            MenuManager.GoToMain();
        }
        else if (Selection == Selections.ConfirmTravel)
        {
            UndoConfirmTravel();
        }
    }

    protected override void ReturnToInitialStep()
    {
        MainComponent.Deactivate();
        LocationFrame.Deactivate();
        MapImage.SetActive(false);
        foreach (Transform t in MapLocationList.transform) t.gameObject.SetActive(false);
        Tracker.SetActive(false);
        foreach (MenuFrame f in Borders) f.Deactivate();
        ConfirmTravelFrame.SetActive(false);
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Moving on the Map --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupMapLocations()
    {
        if (MapLocationList.transform.childCount <= 0) return;
        MapLocationList.transform.GetChild(0).gameObject.SetActive(true);
        for (int i = 0; i < MapLocationList.transform.childCount; i++)
        {
            Transform current = MapLocationList.transform.GetChild(i);
            current.gameObject.SetActive(true);
            current.GetComponent<ListSelectable>().Index = i;
        }
        EventSystem.current.SetSelectedGameObject(MapLocationList.transform.GetChild(0).gameObject);
        if (!MapLocationList.transform.GetChild(0).gameObject.activeSelf) MoveLocation(1);
    }

    public void MoveLocation(int index)
    {
        int i = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index;
        bool foundNextEntry = false;
        while (true)
        {
            int iPlusIndex = i + index;
            if (iPlusIndex < 0 || iPlusIndex >= MapLocationList.transform.childCount) break;
            i = iPlusIndex;
            if (!MapLocationList.transform.GetChild(iPlusIndex).gameObject.activeSelf) continue;
            foundNextEntry = true;
            break;
        }
        if (foundNextEntry) EventSystem.current.SetSelectedGameObject(MapLocationList.transform.GetChild(i).gameObject);
    }

    public void HoverOverLocation()
    {
        if (Selection == Selections.ConfirmTravel) return;
        LocationFrame.Activate();
        LocateLocation();
    }

    private void LocateLocation()
    {
        SelectedLocationBtn = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        SelectedLocationIndex = SelectedLocationBtn.Index;
        LocationName.text = "asdfasdfasdf";
        Tracker.SetActive(true);
        Tracker.transform.position = SelectedLocationBtn.transform.position;
    }

    public void DeselectLocation()
    {
        LocationFrame.Deactivate();
        if (Selection == Selections.MoveMap) Tracker.SetActive(false);
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Confirm Travel --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupConfirmTravel()
    {
        Selection = Selections.ConfirmTravel;
        LocateLocation();
        ConfirmTravelFrame.SetActive(true);
        ConfirmTravelLocation.text = "TRAVEL TO " + "ASDFASDFDAF";
        EventSystem.current.SetSelectedGameObject(ConfirmTravelFrame.transform.GetChild(1).gameObject);
    }

    public void UndoConfirmTravel()
    {
        Selection = Selections.MoveMap;
        ConfirmTravelFrame.SetActive(false);
        EventSystem.current.SetSelectedGameObject(SelectedLocationBtn.gameObject);
    }

    public void Travel()
    {
        ConfirmTravelFrame.SetActive(false);
        Selection = Selections.None;
        ReturnToInitialStep();
        MenuManager.GoToMain();
        MenuManager.ExitAll();
        // Travel to location
    }
}