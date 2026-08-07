using UnityEngine;

public class Coconut : MonoBehaviour
{
    private int scoreValue = 1;
    private float releaseWindForceThreshold = 120f;
    private float releaseImpulse = 2f;
    [SerializeField] private float windIgnoreDurationAfterRelease = 0.3f;
    private float windForceAccumulator;
    private float windIgnoreUntilTime;

    private Rigidbody body;
    private CoconutSpawner spawner;
    private CoconutSpawnPoint spawnPoint;

    public int ScoreValue => scoreValue;
    public bool IsSubmitted { get; private set; }
    public bool IsAttached { get; private set; }
    public bool CanReceiveWindForce => Time.time >= windIgnoreUntilTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
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

        transform.SetParent(spawnPoint.transform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

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
            return false;
        }

        ReleaseFromTree(CalculateReleaseImpulse(forceDirection));
        return true;
    }

    public void ReleaseFromTree(Vector3 initialImpulse)
    {
        if (!IsAttached)
        {
            return;
        }

        IsAttached = false;
        windIgnoreUntilTime = Time.time + windIgnoreDurationAfterRelease;
        transform.SetParent(null, true);

        if (spawnPoint != null)
        {
            spawnPoint.ClearOccupant();
            spawnPoint = null;
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.AddForce(initialImpulse, ForceMode.Impulse);
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
