using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NoteEntry
{
    public GameObject noteObject;
    [TextArea(3, 8)] public string content;
    [HideInInspector] public bool unlocked;
    [HideInInspector] public bool readForEnding;
}

[Serializable]
public class UpgradeStage
{
    public int cost = 1;

    [Header("Hair Dryer")]
    public float windForce = 28f;
    public float windRange = 8f;
    [Range(1f, 60f)] public float windAngle = 28f;
    public int noteIndex = -1;

    [Header("Hair Dryer Model")]
    public HairDryer hairDryerPrefab;

    [Header("Coconut Tree")]
    public float spawnInterval = 3f;
    public int maxActiveCoconuts = 1;
    public bool spawnExtraTree;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SimpleHUD simpleHud;
    [SerializeField] private SimplePanelUI simplePanelUI;
    [SerializeField] private HairDryer hairDryer;
    [SerializeField] private CoconutSpawner coconutSpawner;
    [SerializeField] private Transform secondTreeAnchor;

    [Header("Notes")]
    [SerializeField] private NoteEntry[] notes;

    [Header("Hair Dryer Upgrades")]
    [SerializeField] private UpgradeStage[] hairDryerUpgradeStages =
    {
        new UpgradeStage { cost = 5, windForce = 120f, windRange = 5.5f, windAngle = 20f, noteIndex = 0 },
        new UpgradeStage { cost = 12, windForce = 150f, windRange = 7f, windAngle = 19f, noteIndex = 1 },
        new UpgradeStage { cost = 20, windForce = 180f, windRange = 8f, windAngle = 21f, noteIndex = 2 }
    };

    [Header("Coconut Tree Upgrades")]
    [SerializeField] private UpgradeStage[] coconutTreeUpgradeStages =
    {
        new UpgradeStage { cost = 8, spawnInterval = 3f, maxActiveCoconuts = 4 },
        new UpgradeStage { cost = 15, spawnInterval = 1.5f, maxActiveCoconuts = 4 },
        new UpgradeStage { cost = 25, spawnInterval = 1.5f, maxActiveCoconuts = 4, spawnExtraTree = true }
    };

    private readonly List<CoconutSpawner> coconutSpawners = new List<CoconutSpawner>();
    private int hairDryerUpgradeLevel;
    private int coconutTreeUpgradeLevel;
    private bool hasSpawnedSecondTree;

    public int CoconutCount { get; private set; }

    public int HairDryerUpgradeLevel => hairDryerUpgradeLevel;

    public int CoconutTreeUpgradeLevel => coconutTreeUpgradeLevel;

    public int MaxHairDryerUpgradeLevel => hairDryerUpgradeStages != null ? hairDryerUpgradeStages.Length : 0;

    public int MaxCoconutTreeUpgradeLevel => coconutTreeUpgradeStages != null ? coconutTreeUpgradeStages.Length : 0;

    public bool CanUpgradeHairDryer => CanUpgradeStage(hairDryerUpgradeStages, hairDryerUpgradeLevel);

    public bool CanUpgradeCoconutTree => CanUpgradeStage(coconutTreeUpgradeStages, coconutTreeUpgradeLevel);

    public bool CanUpgrade => CanUpgradeHairDryer || CanUpgradeCoconutTree;

    public bool HasUpgraded => hairDryerUpgradeLevel > 0;

    [Header("Cheat Mode")]
    [SerializeField] private bool enableCheatMode = false;

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

        if (simplePanelUI == null)
        {
            simplePanelUI = FindObjectOfType<SimplePanelUI>();
        }

        if (hairDryer == null)
        {
            hairDryer = FindObjectOfType<HairDryer>();
        }

        if (coconutSpawner == null)
        {
            coconutSpawner = FindObjectOfType<CoconutSpawner>();
        }

        RegisterSpawner(coconutSpawner);
    }

    private void Start()
    {
        HideAllNotes();
        RefreshHud();
        UpdateUpgradeHint();
    }

    private void Update()
    {
        if (enableCheatMode && Input.GetKeyDown(KeyCode.O))
        {
            AddCoconut(6);
        }
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
        return TryUpgradeHairDryer();
    }

    public bool TryUpgradeHairDryer()
    {
        if (!TrySpendForUpgrade(hairDryerUpgradeStages, hairDryerUpgradeLevel, out UpgradeStage stage))
        {
            return false;
        }

        hairDryerUpgradeLevel++;
        ApplyHairDryerUpgrade(stage);
        UnlockNote(stage.noteIndex);
        OnHairDryerUpgradeEvent(hairDryerUpgradeLevel);
        AfterUpgradeChanged();
        Debug.Log($"吹风机升级成功！等级：{hairDryerUpgradeLevel}，消耗 {stage.cost} 个椰子，剩余：{CoconutCount}", this);
        return true;
    }

    public bool TryUpgradeCoconutTree()
    {
        if (!TrySpendForUpgrade(coconutTreeUpgradeStages, coconutTreeUpgradeLevel, out UpgradeStage stage))
        {
            return false;
        }

        coconutTreeUpgradeLevel++;
        ApplyCoconutTreeUpgrade(stage);
        AfterUpgradeChanged();
        Debug.Log($"椰子树升级成功！等级：{coconutTreeUpgradeLevel}，消耗 {stage.cost} 个椰子，剩余：{CoconutCount}", this);
        return true;
    }

    public int GetNextHairDryerUpgradeCost()
    {
        return GetNextUpgradeCost(hairDryerUpgradeStages, hairDryerUpgradeLevel);
    }

    public int GetHairDryerUpgradeCost(int level)
    {
        return GetUpgradeCost(hairDryerUpgradeStages, level);
    }

    public int GetNextCoconutTreeUpgradeCost()
    {
        return GetNextUpgradeCost(coconutTreeUpgradeStages, coconutTreeUpgradeLevel);
    }

    public int GetCoconutTreeUpgradeCost(int level)
    {
        return GetUpgradeCost(coconutTreeUpgradeStages, level);
    }

    public bool CanReadNote(int index)
    {
        if (!IsValidNoteIndex(index) || simplePanelUI == null || simplePanelUI.IsOpen)
        {
            return false;
        }

        return notes[index].unlocked;
    }

    public string GetNoteContent(int index)
    {
        if (!IsValidNoteIndex(index))
        {
            return string.Empty;
        }

        return notes[index].content ?? string.Empty;
    }

    public void OnNoteRead(int index)
    {
        if (!IsValidNoteIndex(index) || !notes[index].unlocked)
        {
            return;
        }

        if (notes[index].readForEnding)
        {
            return;
        }

        notes[index].readForEnding = true;

        if (notes[index].noteObject != null)
        {
            notes[index].noteObject.SetActive(false);
        }

        if (index == GetEndingNoteIndex() && simplePanelUI != null)
        {
            simplePanelUI.ShowEndingPanel();
        }
    }

    private bool CanUpgradeStage(UpgradeStage[] stages, int level)
    {
        return TryGetUpgradeStage(stages, level, out UpgradeStage stage) && CoconutCount >= stage.cost;
    }

    private bool TrySpendForUpgrade(UpgradeStage[] stages, int level, out UpgradeStage stage)
    {
        if (!TryGetUpgradeStage(stages, level, out stage) || CoconutCount < stage.cost)
        {
            return false;
        }

        CoconutCount -= stage.cost;
        return true;
    }

    private bool TryGetUpgradeStage(UpgradeStage[] stages, int level, out UpgradeStage stage)
    {
        stage = null;
        if (stages == null || level < 0 || level >= stages.Length)
        {
            return false;
        }

        stage = stages[level];
        return stage != null && stage.cost > 0;
    }

    private int GetNextUpgradeCost(UpgradeStage[] stages, int level)
    {
        return GetUpgradeCost(stages, level);
    }

    private int GetUpgradeCost(UpgradeStage[] stages, int level)
    {
        return TryGetUpgradeStage(stages, level, out UpgradeStage stage) ? stage.cost : 0;
    }

    private void ApplyHairDryerUpgrade(UpgradeStage stage)
    {
        if (stage == null)
        {
            return;
        }

        if (stage.hairDryerPrefab != null)
        {
            SwapHairDryer(stage.hairDryerPrefab, stage);
            return;
        }

        if (hairDryer != null)
        {
            hairDryer.ApplyUpgrade(stage.windForce, stage.windRange, stage.windAngle);
        }
    }

    private void SwapHairDryer(HairDryer prefab, UpgradeStage stage)
    {
        if (prefab == null || stage == null)
        {
            return;
        }

        HairDryer oldDryer = hairDryer;
        if (oldDryer == null)
        {
            oldDryer = FindObjectOfType<HairDryer>();
        }

        bool wasHeld = oldDryer != null && oldDryer.IsHeld;
        Transform handParent = wasHeld ? oldDryer.transform.parent : null;
        Vector3 worldPosition = oldDryer != null ? oldDryer.transform.position : Vector3.zero;
        Quaternion worldRotation = oldDryer != null ? oldDryer.transform.rotation : Quaternion.identity;

        HairDryer newDryer = Instantiate(prefab);
        newDryer.name = prefab.name;
        newDryer.ApplyUpgrade(stage.windForce, stage.windRange, stage.windAngle);

        if (wasHeld && handParent != null)
        {
            newDryer.PickUp(handParent);
        }
        else
        {
            newDryer.transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        hairDryer = newDryer;

        if (oldDryer != null && oldDryer != newDryer)
        {
            Destroy(oldDryer.gameObject);
        }
    }

    private void ApplyCoconutTreeUpgrade(UpgradeStage stage)
    {
        if (stage == null)
        {
            return;
        }

        if (stage.spawnExtraTree)
        {
            SpawnSecondTree();
        }

        for (int i = 0; i < coconutSpawners.Count; i++)
        {
            CoconutSpawner spawner = coconutSpawners[i];
            if (spawner == null)
            {
                continue;
            }

            spawner.SetSpawnInterval(stage.spawnInterval);
            spawner.SetMaxActiveCoconuts(stage.maxActiveCoconuts);
        }
    }

    private void SpawnSecondTree()
    {
        if (hasSpawnedSecondTree || coconutSpawner == null)
        {
            return;
        }

        hasSpawnedSecondTree = true;

        Vector3 spawnPosition = secondTreeAnchor != null
            ? secondTreeAnchor.position
            : coconutSpawner.transform.position + coconutSpawner.transform.right * 8f;
        Quaternion spawnRotation = secondTreeAnchor != null
            ? secondTreeAnchor.rotation
            : coconutSpawner.transform.rotation;

        GameObject clone = Instantiate(coconutSpawner.gameObject, spawnPosition, spawnRotation);
        clone.name = coconutSpawner.gameObject.name + "_2";

        CoconutSpawner extraSpawner = clone.GetComponent<CoconutSpawner>();
        if (extraSpawner == null)
        {
            return;
        }

        extraSpawner.CopyGenerationSettingsFrom(coconutSpawner);

        Coconut[] clonedCoconuts = extraSpawner.GetComponentsInChildren<Coconut>();
        for (int i = 0; i < clonedCoconuts.Length; i++)
        {
            DestroyImmediate(clonedCoconuts[i].gameObject);
        }

        CoconutSpawnPoint[] clonedPoints = extraSpawner.GetComponentsInChildren<CoconutSpawnPoint>();
        for (int i = 0; i < clonedPoints.Length; i++)
        {
            clonedPoints[i].ClearOccupant();
        }

        RegisterSpawner(extraSpawner);
    }

    private void RegisterSpawner(CoconutSpawner spawner)
    {
        if (spawner != null && !coconutSpawners.Contains(spawner))
        {
            coconutSpawners.Add(spawner);
        }
    }

    private void OnHairDryerUpgradeEvent(int level)
    {
        if (AnomalyController.Instance != null)
        {
            AnomalyController.Instance.OnHairDryerUpgraded(level);
        }
    }

    private void AfterUpgradeChanged()
    {
        UpdateUpgradeHint();
        RefreshHud();
    }

    private void UnlockNote(int index)
    {
        if (!IsValidNoteIndex(index))
        {
            return;
        }

        notes[index].unlocked = true;

        if (notes[index].noteObject != null)
        {
            notes[index].noteObject.SetActive(true);
        }
    }

    private void HideAllNotes()
    {
        if (notes == null)
        {
            return;
        }

        foreach (NoteEntry note in notes)
        {
            if (note == null)
            {
                continue;
            }

            note.unlocked = false;
            note.readForEnding = false;

            if (note.noteObject != null)
            {
                note.noteObject.SetActive(false);
            }
        }
    }

    private bool IsValidNoteIndex(int index)
    {
        return notes != null && index >= 0 && index < notes.Length && notes[index] != null;
    }

    private int GetEndingNoteIndex()
    {
        int endingNoteIndex = -1;

        if (hairDryerUpgradeStages != null)
        {
            foreach (UpgradeStage stage in hairDryerUpgradeStages)
            {
                if (stage != null && IsValidNoteIndex(stage.noteIndex))
                {
                    endingNoteIndex = stage.noteIndex;
                }
            }
        }

        if (endingNoteIndex >= 0)
        {
            return endingNoteIndex;
        }

        return notes != null ? notes.Length - 1 : -1;
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
