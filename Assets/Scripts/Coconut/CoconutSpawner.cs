using System.Collections.Generic;
using UnityEngine;

public class CoconutSpawner : MonoBehaviour
{
    [SerializeField] private GameObject coconutPrefab;
    [SerializeField] private CoconutSpawnPoint[] spawnPoints;
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private int maxActiveCoconuts = 5;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private int coconutScoreValue = 1;
    [SerializeField] private float releaseWindForceThreshold = 120f;
    [SerializeField] private float releaseImpulse = 6f;

    [SerializeField] private float spawnTimer;

    private readonly List<TreeCoconut> activeTreeCoconuts = new List<TreeCoconut>();

    private class TreeCoconut
    {
        public CoconutSpawnPoint SpawnPoint;
        public Coconut Coconut;
        public Rigidbody Body;
        public float AccumulatedWind;
        public bool Released;
    }

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = GetComponentsInChildren<CoconutSpawnPoint>();
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            spawnTimer = spawnInterval;
        }
    }

    private void Update()
    {
        if (activeTreeCoconuts.Count < maxActiveCoconuts)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                TrySpawnCoconut();
                spawnTimer = spawnInterval;
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = activeTreeCoconuts.Count - 1; i >= 0; i--)
        {
            TreeCoconut treeCoconut = activeTreeCoconuts[i];
            if (treeCoconut.Released || treeCoconut.Coconut == null)
            {
                activeTreeCoconuts.RemoveAt(i);
                continue;
            }

            if (treeCoconut.Body == null || !treeCoconut.Body.isKinematic)
            {
                treeCoconut.SpawnPoint?.ClearOccupant();
                activeTreeCoconuts.RemoveAt(i);
            }
        }
    }

    public void RegisterWindForce(Coconut coconut, float forceMagnitude)
    {
        if (forceMagnitude <= 0f || coconut == null)
        {
            return;
        }

        for (int i = 0; i < activeTreeCoconuts.Count; i++)
        {
            TreeCoconut treeCoconut = activeTreeCoconuts[i];
            if (treeCoconut.Released || treeCoconut.Coconut != coconut)
            {
                continue;
            }

            treeCoconut.AccumulatedWind += forceMagnitude * Time.fixedDeltaTime;
            if (treeCoconut.AccumulatedWind >= releaseWindForceThreshold)
            {
                ReleaseCoconut(treeCoconut);
            }

            return;
        }
    }

    private void TrySpawnCoconut()
    {
        if (coconutPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        CoconutSpawnPoint spawnPoint = GetFreeSpawnPoint();
        if (spawnPoint == null)
        {
            return;
        }

        GameObject coconutObject = Instantiate(coconutPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, spawnPoint.transform);
        Coconut coconut = coconutObject.GetComponent<Coconut>();
        if (coconut == null)
        {
            Destroy(coconutObject);
            return;
        }

        if (!spawnPoint.TryOccupy(coconut))
        {
            Destroy(coconutObject);
            return;
        }

        Rigidbody body = coconutObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = coconutObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;

        activeTreeCoconuts.Add(new TreeCoconut
        {
            SpawnPoint = spawnPoint,
            Coconut = coconut,
            Body = body
        });
    }

    private CoconutSpawnPoint GetFreeSpawnPoint()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && !spawnPoints[i].IsOccupied)
            {
                return spawnPoints[i];
            }
        }

        return null;
    }

    private void ReleaseCoconut(TreeCoconut treeCoconut)
    {
        if (treeCoconut.Released || treeCoconut.Body == null)
        {
            return;
        }

        treeCoconut.Released = true;
        treeCoconut.SpawnPoint?.ClearOccupant();

        Transform coconutTransform = treeCoconut.Coconut.transform;
        coconutTransform.SetParent(null, true);

        treeCoconut.Body.isKinematic = false;
        treeCoconut.Body.useGravity = true;

        Vector3 releaseDirection = -Physics.gravity.normalized;
        treeCoconut.Body.AddForce(releaseDirection * releaseImpulse, ForceMode.VelocityChange);
    }
}
