using UnityEngine;
using UnityEngine.InputSystem;

public class HairDryer : MonoBehaviour
{
    public enum WindActivationMode
    {
        HoldLeftMouse = 0,
        ClickLeftMouse = 1,
        Automatic = 2
    }

    [Header("Wind")]
    [SerializeField] private float windForce = 28f;
    [SerializeField] private float windRange = 8f;
    [SerializeField, Range(1f, 60f)] private float windAngle = 28f;
    [SerializeField] private WindActivationMode activationMode = WindActivationMode.HoldLeftMouse;
    [SerializeField] private bool blowOnLeftMouse = true;
    [SerializeField, Min(0.05f)] private float clickBurstDuration = 0.18f;

    [Header("Visuals")]
    [SerializeField] private Transform nozzle;
    [SerializeField] private HairDryerRangeVisual rangeVisual;
    [SerializeField] private bool isHeld;
    [SerializeField] private bool canBePickedUp = true;

    [Header("Pickup")]
    [SerializeField] private Vector3 pickupLocalPosition = new Vector3(0.42f, -0.28f, 0.72f);
    [SerializeField] private Vector3 pickupLocalEulerAngles = Vector3.zero;

    public bool IsHeld => isHeld;
    public bool CanBePickedUp => canBePickedUp;
    public float WindForce => windForce;
    public float WindRange => windRange;
    public float WindAngle => windAngle;
    public Transform Nozzle => nozzle;
    public bool ShouldShowRangeVisual => activationMode == WindActivationMode.Automatic || !isHeld || isBlowingPhysics;

    private bool isBlowingPhysics;
    private float clickBurstTimer;
    private Collider pickupCollider;
    private Rigidbody pickupRigidbody;

    private void Awake()
    {
        pickupRigidbody = GetComponent<Rigidbody>();
        pickupCollider = GetComponent<Collider>();
        ApplyNameBasedPreset();
        ApplyHeldState();
    }

    private void Start()
    {
        UpdateRangeVisual();
    }

    private void OnValidate()
    {
        ApplyNameBasedPreset();
    }

    public void PickUp(Transform handParent)
    {
        if (handParent == null)
        {
            return;
        }

        isHeld = true;
        transform.SetParent(handParent, false);
        transform.localPosition = pickupLocalPosition;
        transform.localRotation = Quaternion.Euler(pickupLocalEulerAngles);
        pickupRigidbody.isKinematic = true;
        ApplyHeldState();
    }

    public void Drop(Vector3 worldPosition, Quaternion worldRotation)
    {
        isHeld = false;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(worldPosition, worldRotation);
        pickupRigidbody.isKinematic = false;
        ApplyHeldState();
    }

    public void GetWindOriginAndDirection(out Vector3 origin, out Vector3 direction)
    {
        origin = nozzle != null ? nozzle.position : transform.position;
        direction = nozzle != null ? nozzle.up : transform.forward;
    }

    public void ApplyUpgrade(float force, float range, float angle)
    {
        windForce = force;
        windRange = range;
        windAngle = angle;
        UpdateRangeVisual();
    }

    private void Update()
    {
        bool prevBlowing = isBlowingPhysics;
        UpdateBlowingState();
        UpdateRangeVisual();
        ProcessSound(prevBlowing);
    }

    private void FixedUpdate()
    {
        if (!isBlowingPhysics)
        {
            return;
        }

        GetWindOriginAndDirection(out Vector3 origin, out Vector3 direction);
        Collider[] hits = Physics.OverlapSphere(origin, windRange);

        foreach (Collider hit in hits)
        {
            Rigidbody body = hit.attachedRigidbody;
            if (body == null || body.gameObject == gameObject)
            {
                continue;
            }

            Vector3 toTarget = body.worldCenterOfMass - origin;
            float distance = toTarget.magnitude;
            if (distance <= 0.01f || Vector3.Angle(direction, toTarget) > windAngle)
            {
                continue;
            }

            float distanceFalloff = 1f - Mathf.Clamp01(distance / windRange);
            float angleFalloff = Mathf.InverseLerp(windAngle, 0f, Vector3.Angle(direction, toTarget));
            float forceMagnitude = windForce * distanceFalloff * angleFalloff;

            Coconut coconut = body.GetComponent<Coconut>();
            if (coconut != null && coconut.IsAttached)
            {
                coconut.ApplyWindForce(direction, forceMagnitude);
                continue;
            }

            if (coconut != null && !coconut.CanReceiveWindForce)
            {
                continue;
            }

            if (body.isKinematic)
            {
                continue;
            }

            body.AddForce(direction * forceMagnitude, ForceMode.Force);
        }
    }

    private void OnDrawGizmosSelected()
    {
        GetWindOriginAndDirection(out Vector3 origin, out Vector3 direction);
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.3f);
        Gizmos.DrawWireSphere(origin, windRange);
        Gizmos.DrawRay(origin, direction * windRange);
    }

    private void ApplyHeldState()
    {
        UpdateRangeVisual();
    }

    private void ApplyNameBasedPreset()
    {
        if (gameObject.name.Contains("HandFan"))
        {
            activationMode = WindActivationMode.ClickLeftMouse;
            canBePickedUp = true;
            return;
        }

        /*if (gameObject.name.Contains("ElectricFan"))
        {
            activationMode = WindActivationMode.Automatic;
            canBePickedUp = false;
            isHeld = false;
        }*/
    }

    private bool IsMouseBlowPressed()
    {
        if (activationMode == WindActivationMode.Automatic)
        {
            return true;
        }

        if (Mouse.current == null)
        {
            return false;
        }

        if (!blowOnLeftMouse)
        {
            return activationMode != WindActivationMode.ClickLeftMouse || Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (activationMode == WindActivationMode.ClickLeftMouse)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }

        return Mouse.current.leftButton.isPressed;
    }

    private void UpdateBlowingState()
    {
        if (activationMode == WindActivationMode.Automatic)
        {
            isBlowingPhysics = true;
            return;
        }

        if (!isHeld)
        {
            isBlowingPhysics = false;
            clickBurstTimer = 0f;
            return;
        }

        if (activationMode == WindActivationMode.ClickLeftMouse)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clickBurstTimer = clickBurstDuration;
            }

            clickBurstTimer = Mathf.Max(0f, clickBurstTimer - Time.deltaTime);
            isBlowingPhysics = clickBurstTimer > 0f;
            return;
        }

        isBlowingPhysics = IsMouseBlowPressed();
    }

    private void UpdateRangeVisual()
    {
        if (rangeVisual == null)
        {
            return;
        }

        if (ShouldShowRangeVisual)
        {
            rangeVisual.Play();
        }
        else
        {
            rangeVisual.StopFade();
        }
    }

    //ParaNoite留言：别删呗，我留着处理声音的
    private void ProcessSound(bool prevBlowing)
    {
        if (isBlowingPhysics && !prevBlowing)
            AudioManager.PlayAudio("dryer_on", false);
        else if (!isBlowingPhysics && prevBlowing)
            AudioManager.PlayAudio("dryer_off", false);

        if (isBlowingPhysics && prevBlowing)
            AudioManager.PlayAudio("dryer_loop", true);
        else if (!isBlowingPhysics)
            AudioManager.StopAudio("dryer_loop");
    }
}
