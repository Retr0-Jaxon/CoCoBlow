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

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private SimpleHUD simpleHud;
    [SerializeField] private SimplePanelUI simplePanelUI;
    [SerializeField] private HairDryer hairDryer;
    [SerializeField] private CoconutSpawner coconutSpawner;

    [Header("Notes")]
    [SerializeField] private NoteEntry[] notes;

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
    }

    private void Start()
    {
        HideAllNotes();
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
        UnlockNotesAfterUpgrade();
        UpdateUpgradeHint();
        RefreshHud();
        Debug.Log($"升级成功！消耗 {upgradeRequiredCoconuts} 个椰子，剩余：{CoconutCount}", this);
        return true;
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

        if (simplePanelUI != null)
        {
            simplePanelUI.ShowEndingPanel();
        }
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

    private void UnlockNotesAfterUpgrade()
    {
        if (notes == null || notes.Length == 0)
        {
            return;
        }

        UnlockNote(0);
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
