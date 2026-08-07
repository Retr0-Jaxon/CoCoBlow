using System.Collections.Generic;
using UnityEngine;

public class CoconutSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject coconutPrefab;
    [SerializeField] private CoconutSpawnPoint[] spawnPoints;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private int maxActiveCoconuts = 5;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Coconut Settings")]
    [SerializeField] private int coconutScoreValue = 1;
    [SerializeField] private float releaseWindForceThreshold = 120f;
    [SerializeField] private float releaseImpulse = 2f;

    private readonly List<Coconut> activeCoconuts = new List<Coconut>();
    private float spawnTimer;

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = GetComponentsInChildren<CoconutSpawnPoint>();
        }
    }

    private void Start()
    {
        spawnTimer = spawnInterval;

        if (spawnOnStart)
        {
            FillAvailableSpawnPoints();
        }
    }

    private void Update()
    {
        CleanupDestroyedCoconuts();

        if (activeCoconuts.Count >= maxActiveCoconuts)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        if (TrySpawnCoconut())
        {
            spawnTimer = spawnInterval;
        }
        else
        {
            spawnTimer = 1f;
        }
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.5f, interval);
    }

    public void SetMaxActiveCoconuts(int maxCount)
    {
        maxActiveCoconuts = Mathf.Max(1, maxCount);
    }

    public void UnregisterCoconut(Coconut coconut)
    {
        activeCoconuts.Remove(coconut);
    }

    private void FillAvailableSpawnPoints()
    {
        while (activeCoconuts.Count < maxActiveCoconuts && TrySpawnCoconut())
        {
        }
    }

    private bool TrySpawnCoconut()
    {
        if (coconutPrefab == null || activeCoconuts.Count >= maxActiveCoconuts)
        {
            return false;
        }

        CoconutSpawnPoint point = FindFreeSpawnPoint();
        if (point == null)
        {
            return false;
        }

        GameObject instance = Instantiate(coconutPrefab, point.transform.position, point.transform.rotation);
        Coconut coconut = instance.GetComponent<Coconut>();
        if (coconut == null)
        {
            coconut = instance.AddComponent<Coconut>();
        }

        if (!point.TryOccupy(coconut))
        {
            Destroy(instance);
            return false;
        }

        coconut.Initialize(this, point, coconutScoreValue, releaseWindForceThreshold, releaseImpulse);
        activeCoconuts.Add(coconut);
        return true;
    }

    private CoconutSpawnPoint FindFreeSpawnPoint()
    {
        foreach (CoconutSpawnPoint point in spawnPoints)
        {
            if (point != null && !point.IsOccupied)
            {
                return point;
            }
        }

        return null;
    }

    private void CleanupDestroyedCoconuts()
    {
        for (int i = activeCoconuts.Count - 1; i >= 0; i--)
        {
            if (activeCoconuts[i] == null)
            {
                activeCoconuts.RemoveAt(i);
            }
        }
    }
}
