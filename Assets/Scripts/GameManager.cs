using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SimpleHUD simpleHud;
    [SerializeField] private HairDryer hairDryer;
    [SerializeField] private CoconutSpawner coconutSpawner;

    [Header("Upgrade")]
    [SerializeField] private int upgradeRequiredCoconuts = 3;
    [SerializeField] private float windForceAfterUpgrade = 35f;
    [SerializeField] private float windRangeAfterUpgrade = 10f;
    [SerializeField, Range(1f, 60f)] private float windAngleAfterUpgrade = 28f;
    [SerializeField] private float spawnIntervalAfterUpgrade = 5f;

    private bool hasUpgraded;

    public int CoconutCount { get; private set; }

    public bool CanUpgrade => !hasUpgraded && CoconutCount >= upgradeRequiredCoconuts;

    public bool HasUpgraded => hasUpgraded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (simpleHud == null)
        {
            simpleHud = FindObjectOfType<SimpleHUD>();
        }

        if (hairDryer == null)
        {
            hairDryer = FindObjectOfType<HairDryer>();
        }

        if (coconutSpawner == null)
        {
            coconutSpawner = FindObjectOfType<CoconutSpawner>();
        }
    }

    private void Start()
    {
        RefreshHud();
        UpdateUpgradeHint();
    }

    public void AddCoconut(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        CoconutCount += amount;
        Debug.Log($"椰子提交成功！+{amount}，当前椰子数量：{CoconutCount}", this);
        RefreshHud();
        UpdateUpgradeHint();
    }

    public bool TryUpgrade()
    {
        if (hasUpgraded || CoconutCount < upgradeRequiredCoconuts)
        {
            return false;
        }

        CoconutCount -= upgradeRequiredCoconuts;
        hasUpgraded = true;
        ApplyUpgradeEffects();
        UpdateUpgradeHint();
        RefreshHud();
        Debug.Log($"升级成功！消耗 {upgradeRequiredCoconuts} 个椰子，剩余：{CoconutCount}", this);
        return true;
    }

    private void ApplyUpgradeEffects()
    {
        if (hairDryer != null)
        {
            hairDryer.ApplyUpgrade(windForceAfterUpgrade, windRangeAfterUpgrade, windAngleAfterUpgrade);
        }

        if (coconutSpawner != null)
        {
            coconutSpawner.SetSpawnInterval(spawnIntervalAfterUpgrade);
        }
    }

    private void UpdateUpgradeHint()
    {
        if (simpleHud == null)
        {
            return;
        }

        simpleHud.ShowUpgradeHint(CanUpgrade);
    }

    private void RefreshHud()
    {
        if (simpleHud != null)
        {
            simpleHud.RefreshAll();
        }
    }
}
