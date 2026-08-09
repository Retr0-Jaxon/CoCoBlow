using System;
using UnityEngine;

[Serializable]
public class NoteEntry
{
    public GameObject noteObject;
    [TextArea(3, 8)] public string content;
    [HideInInspector] public bool unlocked;
    [HideInInspector] public bool readForEnding;
}

public enum TechnologyId { HairDryer, ElectricFan, FasterCoconutSpawn, UnlockNote }
public enum EquipmentId { HairDryer, ElectricFan }
public enum EquipmentStat { WindForce, WindRange, WindAngle }

public class GameManager : MonoBehaviour
{
    [Serializable]
    private class EquipmentProgress
    {
        public HairDryer item;
        public float baseWindForce;
        public float baseWindRange;
        public float baseWindAngle;
        public int windForceLevel;
        public int windRangeLevel;
        public int windAngleLevel;
    }

    public static GameManager Instance { get; private set; }
    public event Action ProgressChanged;

    [Header("References")]
    [SerializeField] private SimpleHUD simpleHud;
    [SerializeField] private SimplePanelUI simplePanelUI;
    [SerializeField] private CoconutSpawner coconutSpawner;
    [SerializeField] private Transform itemOutlet;
    [SerializeField] private GameObject hairDryerPrefab;
    [SerializeField] private GameObject electricFanPrefab;

    [Header("Notes")]
    [SerializeField] private NoteEntry[] notes;

    [Header("Technology Costs")]
    [SerializeField] private int hairDryerCost = 3;
    [SerializeField] private int electricFanCost = 5;
    [SerializeField] private int fasterSpawnCost = 3;
    [SerializeField] private int unlockNoteCost = 3;
    [SerializeField] private float fasterSpawnInterval = 5f;

    [Header("Equipment Upgrades")]
    [SerializeField] private int[] equipmentUpgradeCosts = { 3, 5, 8 };
    [SerializeField] private float windForceIncreasePerLevel = 0.25f;
    [SerializeField] private float windRangeIncreasePerLevel = 2f;
    [SerializeField] private float windAngleIncreasePerLevel = 5f;
    [SerializeField] private float maxWindAngle = 60f;

    private bool hairDryerPurchased;
    private bool electricFanPurchased;
    private bool fasterSpawnPurchased;
    private bool notePurchased;
    private readonly EquipmentProgress hairDryerProgress = new EquipmentProgress();
    private readonly EquipmentProgress electricFanProgress = new EquipmentProgress();

    public int CoconutCount { get; private set; }
    public bool CanUpgrade => CanPurchaseAnyTechnology();
    public bool HasUpgraded => hairDryerPurchased || electricFanPurchased || fasterSpawnPurchased || notePurchased;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (simpleHud == null) simpleHud = FindObjectOfType<SimpleHUD>();
        if (simplePanelUI == null) simplePanelUI = FindObjectOfType<SimplePanelUI>();
        if (coconutSpawner == null) coconutSpawner = FindObjectOfType<CoconutSpawner>();
    }

    private void Start()
    {
        HideAllNotes();
        RefreshPresentation();
    }

    public void AddCoconut(int amount = 1)
    {
        if (amount <= 0) return;
        CoconutCount += amount;
        RefreshPresentation();
    }

    public bool TryPurchaseTechnology(TechnologyId technology)
    {
        if (!CanPurchaseTechnology(technology)) return false;
        int cost = GetTechnologyCost(technology);
        CoconutCount -= cost;

        switch (technology)
        {
            case TechnologyId.HairDryer:
                hairDryerPurchased = SpawnEquipment(hairDryerPrefab, hairDryerProgress);
                break;
            case TechnologyId.ElectricFan:
                electricFanPurchased = SpawnEquipment(electricFanPrefab, electricFanProgress);
                break;
            case TechnologyId.FasterCoconutSpawn:
                fasterSpawnPurchased = true;
                if (coconutSpawner != null) coconutSpawner.SetSpawnInterval(fasterSpawnInterval);
                break;
            case TechnologyId.UnlockNote:
                notePurchased = true;
                UnlockNote(0);
                break;
        }

        if (!IsTechnologyPurchased(technology))
        {
            CoconutCount += cost;
            return false;
        }

        RefreshPresentation();
        return true;
    }

    public bool CanPurchaseTechnology(TechnologyId technology)
    {
        if (IsTechnologyPurchased(technology) || CoconutCount < GetTechnologyCost(technology)) return false;
        return technology switch
        {
            TechnologyId.HairDryer => hairDryerPrefab != null && itemOutlet != null,
            TechnologyId.ElectricFan => electricFanPrefab != null && itemOutlet != null,
            _ => true
        };
    }

    public bool IsTechnologyPurchased(TechnologyId technology) => technology switch
    {
        TechnologyId.HairDryer => hairDryerPurchased,
        TechnologyId.ElectricFan => electricFanPurchased,
        TechnologyId.FasterCoconutSpawn => fasterSpawnPurchased,
        TechnologyId.UnlockNote => notePurchased,
        _ => false
    };

    public int GetTechnologyCost(TechnologyId technology) => technology switch
    {
        TechnologyId.HairDryer => hairDryerCost,
        TechnologyId.ElectricFan => electricFanCost,
        TechnologyId.FasterCoconutSpawn => fasterSpawnCost,
        TechnologyId.UnlockNote => unlockNoteCost,
        _ => 0
    };

    public bool HasEquipment(EquipmentId equipment) => GetProgress(equipment).item != null;

    public int GetEquipmentLevel(EquipmentId equipment, EquipmentStat stat)
    {
        EquipmentProgress progress = GetProgress(equipment);
        return stat switch
        {
            EquipmentStat.WindForce => progress.windForceLevel,
            EquipmentStat.WindRange => progress.windRangeLevel,
            EquipmentStat.WindAngle => progress.windAngleLevel,
            _ => 0
        };
    }

    public int GetEquipmentUpgradeCost(EquipmentId equipment, EquipmentStat stat)
    {
        int level = GetEquipmentLevel(equipment, stat);
        return level >= equipmentUpgradeCosts.Length ? 0 : equipmentUpgradeCosts[level];
    }

    public bool TryUpgradeEquipment(EquipmentId equipment, EquipmentStat stat)
    {
        EquipmentProgress progress = GetProgress(equipment);
        int cost = GetEquipmentUpgradeCost(equipment, stat);
        if (progress.item == null || cost <= 0 || CoconutCount < cost) return false;
        CoconutCount -= cost;
        if (stat == EquipmentStat.WindForce) progress.windForceLevel++;
        else if (stat == EquipmentStat.WindRange) progress.windRangeLevel++;
        else progress.windAngleLevel++;
        ApplyEquipmentStats(progress);
        RefreshPresentation();
        return true;
    }

    public string GetNextUpgradeDescription(EquipmentId equipment, EquipmentStat stat)
    {
        EquipmentProgress progress = GetProgress(equipment);
        if (GetEquipmentUpgradeCost(equipment, stat) <= 0) return "已满级";
        return stat switch
        {
            EquipmentStat.WindForce => $"风力 {progress.baseWindForce * (1f + windForceIncreasePerLevel * (progress.windForceLevel + 1)):0.#}",
            EquipmentStat.WindRange => $"距离 {progress.baseWindRange + windRangeIncreasePerLevel * (progress.windRangeLevel + 1):0.#}m",
            EquipmentStat.WindAngle => $"角度 {Mathf.Min(maxWindAngle, progress.baseWindAngle + windAngleIncreasePerLevel * (progress.windAngleLevel + 1)):0.#}°",
            _ => string.Empty
        };
    }

    public bool CanReadNote(int index) => IsValidNoteIndex(index) && simplePanelUI != null && !simplePanelUI.IsOpen && notes[index].unlocked;
    public string GetNoteContent(int index) => IsValidNoteIndex(index) ? notes[index].content ?? string.Empty : string.Empty;

    public void OnNoteRead(int index)
    {
        if (!IsValidNoteIndex(index) || !notes[index].unlocked || notes[index].readForEnding) return;
        notes[index].readForEnding = true;
        if (simplePanelUI != null) simplePanelUI.ShowEndingPanel();
    }

    private bool SpawnEquipment(GameObject prefab, EquipmentProgress progress)
    {
        if (prefab == null || itemOutlet == null) return false;
        GameObject instance = Instantiate(prefab, itemOutlet.position, itemOutlet.rotation);
        HairDryer item = instance.GetComponent<HairDryer>();
        if (item == null) { Destroy(instance); return false; }
        progress.item = item;
        progress.baseWindForce = item.WindForce;
        progress.baseWindRange = item.WindRange;
        progress.baseWindAngle = item.WindAngle;
        ApplyEquipmentStats(progress);
        return true;
    }

    private void ApplyEquipmentStats(EquipmentProgress progress)
    {
        if (progress.item == null) return;
        float force = progress.baseWindForce * (1f + windForceIncreasePerLevel * progress.windForceLevel);
        float range = progress.baseWindRange + windRangeIncreasePerLevel * progress.windRangeLevel;
        float angle = Mathf.Min(maxWindAngle, progress.baseWindAngle + windAngleIncreasePerLevel * progress.windAngleLevel);
        progress.item.ApplyUpgrade(force, range, angle);
    }

    private EquipmentProgress GetProgress(EquipmentId equipment) => equipment == EquipmentId.HairDryer ? hairDryerProgress : electricFanProgress;

    private bool CanPurchaseAnyTechnology()
    {
        foreach (TechnologyId technology in Enum.GetValues(typeof(TechnologyId)))
            if (CanPurchaseTechnology(technology)) return true;
        return false;
    }

    private void UnlockNote(int index)
    {
        if (!IsValidNoteIndex(index)) return;
        notes[index].unlocked = true;
        if (notes[index].noteObject != null) notes[index].noteObject.SetActive(true);
    }

    private void HideAllNotes()
    {
        if (notes == null) return;
        foreach (NoteEntry note in notes)
        {
            if (note == null) continue;
            note.unlocked = false;
            note.readForEnding = false;
            if (note.noteObject != null) note.noteObject.SetActive(false);
        }
    }

    private bool IsValidNoteIndex(int index) => notes != null && index >= 0 && index < notes.Length && notes[index] != null;

    private void RefreshPresentation()
    {
        if (simpleHud != null)
        {
            simpleHud.RefreshAll();
            simpleHud.ShowUpgradeHint(CanUpgrade);
        }
        ProgressChanged?.Invoke();
    }
}
