using UnityEngine;
using UnityEngine.InputSystem;

public class HairDryer : MonoBehaviour
{
    [Header("Wind")]
    [SerializeField] private float windForce = 28f;
    [SerializeField] private float windRange = 8f;
    [SerializeField, Range(1f, 60f)] private float windAngle = 28f;
    [SerializeField] private bool blowOnLeftMouse = true;

    [Header("Visuals")]
    [SerializeField] private Transform nozzle;
    [SerializeField] private ParticleSystem windEffect;
    [SerializeField] private bool isHeld;

    public bool IsHeld => isHeld;

    private bool isBlowing;
    private Collider pickupCollider;
    private Rigidbody pickupRigidbody;

    private void Awake()
    {
        pickupRigidbody =  GetComponent<Rigidbody>();
        pickupCollider = GetComponent<Collider>();
        ApplyHeldState();
    }

    public void PickUp(Transform handParent)
    {
        if (handParent == null)
        {
            return;
        }

        isHeld = true;
        transform.SetParent(handParent, false);
        transform.localPosition = new Vector3(0.42f, -0.28f, 0.72f);
        transform.localRotation = Quaternion.identity;
        pickupRigidbody.isKinematic = true;//MARKER 
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

    private void Update()
    {
        isBlowing = isHeld && (!blowOnLeftMouse || (Mouse.current != null && Mouse.current.leftButton.isPressed));

        if (windEffect != null)
        {
            if (isBlowing && !windEffect.isPlaying)
            {
                windEffect.Play();
            }
            else if (!isBlowing && windEffect.isPlaying)
            {
                windEffect.Stop();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isBlowing)
        {
            return;
        }

        Vector3 origin = nozzle != null ? nozzle.position : transform.position;
        Vector3 direction = nozzle != null ? nozzle.up : transform.forward;
        Collider[] hits = Physics.OverlapSphere(origin, windRange);

        foreach (Collider hit in hits)
        {
            TryToMakeCoconutDrop(hit);
            Rigidbody body = hit.attachedRigidbody;
            if (body == null || body.isKinematic || body.gameObject == gameObject)
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
            body.AddForce(direction * (windForce * distanceFalloff * angleFalloff), ForceMode.Force);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = nozzle != null ? nozzle.position : transform.position;
        Vector3 direction = nozzle != null ? nozzle.up : transform.forward;
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.3f);
        Gizmos.DrawWireSphere(origin, windRange);
        Gizmos.DrawRay(origin, direction * windRange);
    }

    private void ApplyHeldState()//TODO 如果开始游戏之后玩家发现地面上的吹风机在吹出粒子效果的风，光标移动识别，E键拾取停止吹风，按下左键再吹是不是这样有更好的引导作用
    {
        /*if (pickupCollider != null)
        {
            pickupCollider.enabled = !isHeld;
        }*/

        if (!isHeld && windEffect != null && windEffect.isPlaying)
        {
            windEffect.Stop();
        }
    }
    private void TryToMakeCoconutDrop(Collider hit)
    {
        Coconut coconut = hit.GetComponentInParent<Coconut>();
        if (coconut != null && !coconut.IsDropped)
        {
            coconut.Unfreeze();
            Debug.Log("椰子掉落了！", coconut);
        }
    }
}
