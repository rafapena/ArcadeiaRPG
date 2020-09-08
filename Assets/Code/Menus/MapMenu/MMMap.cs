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

    public GameObject BlackBackground;
    public MenuFrame LocationFrame;
    public TextMeshProUGUI LocationName;
    public GameObject MapImage;
    public GameObject Tracker;
    public GameObject ConfirmTravelFrame;
    public TextMeshProUGUI ConfirmTravelLocation;
    public MenuFrame[] Borders;
    private Selections Selection;

    public float NavigationSpeed;
    private float Zoom = 1f;
    public float ZoomSpeed;
    public float ZoomMin;
    public float ZoomMax;
    public float DefaultZoom;

    public const float NAVIGATION_RIGHT_BOUND = 800;
    public const float NAVIGATION_LEFT_BOUND = -1600;
    public const float NAVIGATION_UPPER_BOUND = 800;
    public const float NAVIGATION_LOWER_BOUND = -1200;

    private Color TrackerColor;
    private Color HighlightedTrackerColor = new Color(1f, 1f, 0);

    protected override void Update()
    {
        base.Update();
        if (Selection != Selections.MoveMap) return;
        
        // Zooming
        Zoom = Input.GetAxis("Mouse ScrollWheel") * Time.unscaledDeltaTime * ZoomSpeed;
        MapImage.transform.localScale += new Vector3(MapImage.transform.localScale.x * Zoom, MapImage.transform.localScale.y * Zoom);
        MapImage.transform.localScale = new Vector3(Mathf.Clamp(MapImage.transform.localScale.x, ZoomMin, ZoomMax), Mathf.Clamp(MapImage.transform.localScale.y, ZoomMin, ZoomMax));
        Vector3 scale = MapImage.transform.localScale;
        MapImage.transform.localScale = scale;

        // Navigating with keyboard
        if (Selection == Selections.MoveMap)
        {
            Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            MapImage.transform.position -= movement * Time.unscaledDeltaTime * NavigationSpeed;
            CheckBounds();
        }
    }

    private void CheckBounds()
    {
        if (MapImage.transform.position.x > NAVIGATION_RIGHT_BOUND) MapImage.transform.position = new Vector3(NAVIGATION_RIGHT_BOUND, MapImage.transform.position.y);
        else if (MapImage.transform.position.x < NAVIGATION_LEFT_BOUND) MapImage.transform.position = new Vector3(NAVIGATION_LEFT_BOUND, MapImage.transform.position.y);
        if (MapImage.transform.position.y > NAVIGATION_UPPER_BOUND) MapImage.transform.position = new Vector3(MapImage.transform.position.x, NAVIGATION_UPPER_BOUND);
        else if (MapImage.transform.position.y < NAVIGATION_LOWER_BOUND) MapImage.transform.position = new Vector3(MapImage.transform.position.x, NAVIGATION_LOWER_BOUND);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MapImage.transform.position += (Vector3)eventData.delta;
    }

    public override void Open()
    {
        base.Open();
        BlackBackground.SetActive(true);
        MapImage.SetActive(true);
        Tracker.SetActive(true);
        Selection = Selections.MoveMap;
        MapImage.transform.localScale = new Vector3(DefaultZoom, DefaultZoom);
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
        BlackBackground.SetActive(false);
        MapImage.SetActive(false);
        Tracker.SetActive(false);
        foreach (MenuFrame f in Borders) f.Deactivate();
        ConfirmTravelFrame.SetActive(false);
        TrackerColor = Tracker.GetComponent<Image>().color;
    }

    public void HoverOverLocation()
    {
        LocationFrame.Activate();
        LocationName.text = "asdfasdfasdf";
    }

    public void TrackerOverLocation()
    {
        Tracker.GetComponent<Image>().color = TrackerColor;
    }

    public void DeselectLocation()
    {
        LocationFrame.Deactivate();
    }

    public void TrackerDeselectedLocation()
    {
        Tracker.GetComponent<Image>().color = HighlightedTrackerColor;
    }

    public void SetupConfirmTravel()
    {
        Selection = Selections.ConfirmTravel;
        ConfirmTravelFrame.SetActive(true);
        ConfirmTravelLocation.text = "TRAVEL TO " + "ASDFASDFDAF";
        EventSystem.current.SetSelectedGameObject(ConfirmTravelFrame.transform.GetChild(1).gameObject);
    }

    public void UndoConfirmTravel()
    {
        Selection = Selections.MoveMap;
        ConfirmTravelFrame.SetActive(false);
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