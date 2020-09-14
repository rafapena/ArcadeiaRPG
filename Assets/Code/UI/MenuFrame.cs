using UnityEngine;
using UnityEngine.UI;

public class MenuFrame : MonoBehaviour
{
    public enum DirEnum { Left, Right, Top, Bottom }

    public enum SpeedTypes { Fast, Med, Slow }
    private float[] SPEED_TYPES = new float[] { 3.5f, 1.5f, 0.7f };   // Ensure the length of this list == the number of SpeedTypes

    // Activation related
    public bool Activated { get; private set; }
    private bool Shifted;

    // Direction + Destination locations
    public DirEnum ComeFrom;
    private Vector3 ActivatedSpot;
    private Vector3 DeactivatedSpot;

    // Movement related
    public SpeedTypes ApproachSpeed;
    private Vector3 Movement;
    private float DistanceFromPlacement;
    private float Speed;
    [SerializeField] private bool Flexible;

    private void Awake()
    {
        ActivatedSpot = new Vector3(transform.position.x, transform.position.y);
        float dDist = 5;
        switch (ComeFrom)
        {
            case DirEnum.Left:
                Movement = Vector3.right;
                DistanceFromPlacement = GetComponent<RectTransform>().rect.width * dDist * 0.5f;
                DeactivatedSpot = new Vector3(ActivatedSpot.x - DistanceFromPlacement, ActivatedSpot.y);
                break;
            case DirEnum.Right:
                Movement = Vector3.left;
                DistanceFromPlacement = GetComponent<RectTransform>().rect.width * dDist * 0.5f;
                DeactivatedSpot = new Vector3(ActivatedSpot.x + DistanceFromPlacement, ActivatedSpot.y);
                break;
            case DirEnum.Top:
                Movement = Vector3.down;
                DistanceFromPlacement = GetComponent<RectTransform>().rect.height * dDist;
                DeactivatedSpot = new Vector3(ActivatedSpot.x, ActivatedSpot.y + DistanceFromPlacement);
                break;
            case DirEnum.Bottom:
                Movement = Vector3.up;
                DistanceFromPlacement = GetComponent<RectTransform>().rect.height * dDist;
                DeactivatedSpot = new Vector3(ActivatedSpot.x, ActivatedSpot.y - DistanceFromPlacement);
                break;
        }
        DeactivateNonAnimate();
        Speed = DistanceFromPlacement * SPEED_TYPES[(int)ApproachSpeed];
    }

    private void Update()
    {
        if (!Shifted) return;   // Just here to prevent multiple checks from the statements, below

        if (Activated)
        {
            transform.position += Movement * Speed * Time.unscaledDeltaTime;
            if (ComeFrom == DirEnum.Left && transform.position.x >= ActivatedSpot.x ||
                ComeFrom == DirEnum.Right && transform.position.x <= ActivatedSpot.x ||
                ComeFrom == DirEnum.Top && transform.position.y <= ActivatedSpot.y ||
                ComeFrom == DirEnum.Bottom && transform.position.y >= ActivatedSpot.y)
            {
                LandOnSpot(ActivatedSpot);
            }
        }
        else
        {
            transform.position -= Movement * Speed * Time.unscaledDeltaTime;
            if (ComeFrom == DirEnum.Left && transform.position.x <= DeactivatedSpot.x ||
                ComeFrom == DirEnum.Right && transform.position.x >= DeactivatedSpot.x ||
                ComeFrom == DirEnum.Top && transform.position.y >= DeactivatedSpot.y ||
                ComeFrom == DirEnum.Bottom && transform.position.y <= DeactivatedSpot.y)
            {
                LandOnSpot(DeactivatedSpot);
            }
        }
    }

    private void LandOnSpot(Vector3 Spot)
    {
        if (Flexible) transform.position = (ComeFrom == DirEnum.Left || ComeFrom == DirEnum.Right) ? (new Vector3(Spot.x, transform.position.y)) : (new Vector3(transform.position.x, Spot.y));
        else transform.position = Spot;
        Shifted = false;
    }

    public void Activate()
    {
        if (Activated) return;
        Shifted = true;
        Activated = true;
    }

    public void Deactivate()
    {
        if (!Activated) return;
        Shifted = true;
        Activated = false;
    }

    public void ActivateNonAnimate()
    {
        transform.position = ActivatedSpot;
        Activated = true;
    }

    public void DeactivateNonAnimate()
    {
        transform.position = DeactivatedSpot;
        Activated = false;
    }
}
