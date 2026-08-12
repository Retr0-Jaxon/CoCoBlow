using UnityEngine;
using UnityEngine.InputSystem;

public class HairDryer : MonoBehaviour
{
    [Header("Wind")]
    [SerializeField] private float windForce = 28f;
    [SerializeField] private float windRange = 8f;
    [SerializeField, Range(1f, 60f)] private float windAngle = 28f;
    [SerializeField] private bool blowOnLeftMouse = true;

    [Header("Pickup")]
    [SerializeField] private Vector3 pickupLocalPosition = new Vector3(0.42f, -0.28f, 0.72f);
    [SerializeField] private Vector3 pickupLocalEulerAngles = Vector3.zero;

    [Header("Visuals")]
    [SerializeField] private Transform nozzle;
    [SerializeField] private Vector3 windLocalDirection = Vector3.up;
    [SerializeField] private HairDryerRangeVisual rangeVisual;
    [SerializeField] private bool isHeld;

    public bool IsHeld => isHeld;
    public float WindRange => windRange;
    public float WindAngle => windAngle;
    public Transform Nozzle => nozzle;
    public bool ShouldShowRangeVisual => !isHeld || isBlowingPhysics;

    private bool isBlowingPhysics;
    private Collider pickupCollider;
    private Rigidbody pickupRigidbody;

    private void Awake()
    {
        pickupRigidbody = GetComponent<Rigidbody>();
        pickupCollider = GetComponent<Collider>();
        ApplyHeldState();
    }

    private void Start()
    {
        UpdateRangeVisual();
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
        if (pickupRigidbody != null)
        {
            pickupRigidbody.isKinematic = true;
        }
        ApplyHeldState();
    }

    public void Drop(Vector3 worldPosition, Quaternion worldRotation)
    {
        isHeld = false;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(worldPosition, worldRotation);
        if (pickupRigidbody != null)
        {
            pickupRigidbody.isKinematic = false;
        }
        ApplyHeldState();
    }

    public void GetWindOriginAndDirection(out Vector3 origin, out Vector3 direction)
    {
        Transform emitter = nozzle != null ? nozzle : transform;
        origin = emitter.position;
        direction = emitter.TransformDirection(GetNormalizedWindLocalDirection());
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
        bool mousePressed = IsMouseBlowPressed();
        isBlowingPhysics = isHeld && mousePressed;
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

    private Vector3 GetNormalizedWindLocalDirection()
    {
        if (windLocalDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.up;
        }

        return windLocalDirection.normalized;
    }

    private bool IsMouseBlowPressed()
    {
        return !blowOnLeftMouse || (Mouse.current != null && Mouse.current.leftButton.isPressed);
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

    private void ProcessSound(bool prevBlowing)
    {
        if (isBlowingPhysics && !prevBlowing)
        {
            AudioManager.PlayAudio("on", false);
        }
        else if (!isBlowingPhysics && prevBlowing)
        {
            AudioManager.PlayAudio("off", false);
        }

        if (isBlowingPhysics && prevBlowing)
        {
            AudioManager.PlayAudio("hair_dryer", true);
        }
        else if (!isBlowingPhysics)
        {
            AudioManager.StopAudio("hair_dryer");
        }
    }
}
