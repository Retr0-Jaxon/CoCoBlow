using System.Collections;
using UnityEngine;

public class Coconut : MonoBehaviour
{
    private int scoreValue = 1;
    private float releaseWindForceThreshold = 45f;
    private float releaseImpulse = 2f;
    [SerializeField] private float windIgnoreDurationAfterRelease = 0.3f;

    [Header("Wobble (attached only)")]
    [SerializeField] private float maxWobbleAngle = 12f;
    [SerializeField] private float wobbleFrequency = 10f;
    [SerializeField] private float wobbleIntensityGain = 0.35f;
    [SerializeField] private float wobbleDecaySpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float progressWobbleWeight = 0.45f;

    private float windForceAccumulator;
    private float windIgnoreUntilTime;
    private float wobbleIntensity;
    private float wobblePhase;
    private Vector3 wobbleWindDirection = Vector3.forward;
    private Vector3 intendedWorldScale = Vector3.one;
    private bool hasTriggeredLandAnomaly;

    private Rigidbody body;
    private CoconutSpawner spawner;
    private CoconutSpawnPoint spawnPoint;
    private Light landFlashLight;

    public int ScoreValue => scoreValue;
    public bool IsSubmitted { get; private set; }
    public bool IsAttached { get; private set; }
    public bool CanReceiveWindForce => Time.time >= windIgnoreUntilTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        intendedWorldScale = transform.lossyScale;
        if (intendedWorldScale.sqrMagnitude < 0.0001f)
        {
            intendedWorldScale = Vector3.one;
        }
    }

    public void Initialize(
        CoconutSpawner owner,
        CoconutSpawnPoint point,
        int score,
        float releaseThreshold,
        float impulse)
    {
        spawner = owner;
        spawnPoint = point;
        scoreValue = score;
        releaseWindForceThreshold = releaseThreshold;
        releaseImpulse = impulse;
        hasTriggeredLandAnomaly = false;
        AttachToTree();
    }

    public void AttachToTree()
    {
        if (spawnPoint == null || body == null)
        {
            return;
        }

        IsAttached = true;
        windForceAccumulator = 0f;
        windIgnoreUntilTime = 0f;
        ResetWobbleState();

        transform.SetParent(spawnPoint.transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        ApplyCompensatedLocalScale();

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
    }

    public bool ApplyWindForce(Vector3 forceDirection, float forceMagnitude)
    {
        if (!IsAttached)
        {
            return false;
        }

        windForceAccumulator += forceMagnitude * Time.fixedDeltaTime;
        if (windForceAccumulator < releaseWindForceThreshold)
        {
            FeedWobble(forceDirection, forceMagnitude);
            return false;
        }

        ReleaseFromTree(CalculateReleaseImpulse(forceDirection));
        return true;
    }

    private void Update()
    {
        if (!IsAttached)
        {
            return;
        }

        UpdateWobbleVisual();
    }

    public void ReleaseFromTree(Vector3 initialImpulse)
    {
        if (!IsAttached)
        {
            return;
        }

        IsAttached = false;
        windIgnoreUntilTime = Time.time + windIgnoreDurationAfterRelease;
        ResetWobbleState();

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.SetParent(null, true);

        if (spawnPoint != null)
        {
            spawnPoint.ClearOccupant();
            spawnPoint = null;
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = false;
        body.useGravity = true;
        body.AddForce(initialImpulse, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsAttached || IsSubmitted || hasTriggeredLandAnomaly || body == null || body.isKinematic)
        {
            return;
        }

        bool hitGround = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.35f)
            {
                hitGround = true;
                break;
            }
        }

        if (!hitGround)
        {
            return;
        }

        hasTriggeredLandAnomaly = true;
        if (AnomalyController.Instance != null)
        {
            AnomalyController.Instance.HandleCoconutLanded(this);
        }
    }

    private Coroutine landFlashRoutine;
    private Coroutine landBurstRoutine;

    public void PlayLandFlash(float duration, float intensity, float range)
    {
        if (landFlashRoutine != null)
        {
            StopCoroutine(landFlashRoutine);
        }

        landFlashRoutine = StartCoroutine(LandFlashRoutine(duration, intensity, range));
    }

    public void ApplyLandBurstToward(float speed, Transform target)
    {
        if (body == null || body.isKinematic || speed <= 0f || target == null)
        {
            return;
        }

        if (landBurstRoutine != null)
        {
            StopCoroutine(landBurstRoutine);
        }

        landBurstRoutine = StartCoroutine(LandBurstRoutine(speed, target));
    }

    private IEnumerator LandBurstRoutine(float speed, Transform target)
    {
        yield return new WaitForFixedUpdate();

        if (body == null || body.isKinematic || IsSubmitted || target == null)
        {
            landBurstRoutine = null;
            yield break;
        }

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            landBurstRoutine = null;
            yield break;
        }

        body.velocity = toTarget.normalized * speed;
        body.angularVelocity = Vector3.zero;
        landBurstRoutine = null;
    }

    private IEnumerator LandFlashRoutine(float duration, float intensity, float range)
    {
        if (landFlashLight == null)
        {
            landFlashLight = gameObject.AddComponent<Light>();
        }

        landFlashLight.enabled = true;
        landFlashLight.type = LightType.Point;
        landFlashLight.color = Color.white;
        landFlashLight.intensity = intensity;
        landFlashLight.range = range;
        landFlashLight.shadows = LightShadows.None;

        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        if (landFlashLight != null)
        {
            landFlashLight.enabled = false;
        }
    }

    private void ApplyCompensatedLocalScale()
    {
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            intendedWorldScale.x / Mathf.Max(0.0001f, parentScale.x),
            intendedWorldScale.y / Mathf.Max(0.0001f, parentScale.y),
            intendedWorldScale.z / Mathf.Max(0.0001f, parentScale.z));
    }

    private Vector3 CalculateReleaseImpulse(Vector3 forceDirection)
    {
        Vector3 horizontalDirection = Vector3.ProjectOnPlane(forceDirection, Vector3.up);
        if (horizontalDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.down * releaseImpulse;
        }

        Vector3 releaseDirection = (horizontalDirection.normalized + Vector3.down * 0.35f).normalized;
        return releaseDirection * releaseImpulse;
    }

    public void MarkSubmitted()
    {
        IsSubmitted = true;
    }

    private void FeedWobble(Vector3 forceDirection, float forceMagnitude)
    {
        Vector3 horizontalDirection = Vector3.ProjectOnPlane(forceDirection, Vector3.up);
        if (horizontalDirection.sqrMagnitude > 0.0001f)
        {
            wobbleWindDirection = horizontalDirection.normalized;
        }

        float windContribution = Mathf.Clamp01(forceMagnitude * wobbleIntensityGain);
        float progressContribution = releaseWindForceThreshold > 0f
            ? windForceAccumulator / releaseWindForceThreshold * progressWobbleWeight
            : 0f;
        float targetIntensity = Mathf.Clamp01(windContribution + progressContribution);
        wobbleIntensity = Mathf.Max(wobbleIntensity, targetIntensity);
    }

    private void UpdateWobbleVisual()
    {
        wobbleIntensity = Mathf.MoveTowards(wobbleIntensity, 0f, wobbleDecaySpeed * Time.deltaTime);
        if (wobbleIntensity <= 0.001f)
        {
            transform.localRotation = Quaternion.identity;
            return;
        }

        wobblePhase += Time.deltaTime * wobbleFrequency * Mathf.PI * 2f;
        float swingAngle = Mathf.Sin(wobblePhase) * wobbleIntensity * maxWobbleAngle;
        float secondaryAngle = Mathf.Sin(wobblePhase * 0.7f + 0.6f) * wobbleIntensity * maxWobbleAngle * 0.35f;

        Vector3 primaryAxis = Vector3.Cross(wobbleWindDirection, Vector3.up);
        if (primaryAxis.sqrMagnitude <= 0.0001f)
        {
            primaryAxis = Vector3.right;
        }

        Vector3 secondaryAxis = Vector3.Cross(primaryAxis, Vector3.up);
        if (secondaryAxis.sqrMagnitude <= 0.0001f)
        {
            secondaryAxis = Vector3.forward;
        }

        Quaternion wobbleRotation =
            Quaternion.AngleAxis(swingAngle, primaryAxis.normalized) *
            Quaternion.AngleAxis(secondaryAngle, secondaryAxis.normalized);
        transform.localRotation = wobbleRotation;
    }

    private void ResetWobbleState()
    {
        wobbleIntensity = 0f;
        wobblePhase = 0f;
        wobbleWindDirection = Vector3.forward;
    }

    private void OnDestroy()
    {
        if (spawnPoint != null)
        {
            spawnPoint.ClearOccupant();
        }

        if (spawner != null)
        {
            spawner.UnregisterCoconut(this);
        }
    }
}
