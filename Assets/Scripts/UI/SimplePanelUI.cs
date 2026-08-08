using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SimplePanelUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject notePanel;
    [SerializeField] private GameObject endingPanel;

    [Header("Note Panel")]
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private Button noteCloseButton;

    [Header("Ending Panel")]
    [SerializeField] private Button endingCloseButton;

    private readonly TechnologyId[] technologyIds =
    {
        TechnologyId.HairDryer,
        TechnologyId.ElectricFan,
        TechnologyId.FasterCoconutSpawn,
        TechnologyId.UnlockNote
    };

    private readonly EquipmentId[] equipmentIds = { EquipmentId.HairDryer, EquipmentId.ElectricFan };
    private readonly EquipmentStat[] equipmentStats = { EquipmentStat.WindForce, EquipmentStat.WindRange, EquipmentStat.WindAngle };

    private GameObject activePanel;
    private Action onNoteClosed;
    private GameObject technologyPage;
    private GameObject equipmentPage;
    private GameObject emptyEquipmentMessage;
    private Button technologyTab;
    private Button equipmentTab;
    private Button upgradeCloseButton;
    private TMP_Text[] technologyStatusTexts;
    private Button[] technologyButtons;
    private GameObject[] equipmentSections;
    private TMP_Text[,] equipmentStatusTexts;
    private Button[,] equipmentButtons;
    private bool showingTechnology = true;

    public bool IsOpen => activePanel != null;

    private void Awake()
    {
        if (noteCloseButton != null) noteCloseButton.onClick.AddListener(CloseNotePanel);
        if (endingCloseButton != null) endingCloseButton.onClick.AddListener(CloseEndingPanel);
        CacheUpgradePanelObjects();
        BindUpgradePanelButtons();
    }

    private void Start()
    {
        HideAllPanelsImmediate();
        if (GameManager.Instance != null) GameManager.Instance.ProgressChanged += RefreshUpgradePanel;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.ProgressChanged -= RefreshUpgradePanel;
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (activePanel == notePanel) CloseNotePanel();
        else if (activePanel == upgradePanel) CloseUpgradePanel();
        else if (activePanel == endingPanel) CloseEndingPanel();
    }

    public void ShowUpgradePanel()
    {
        SetUpgradeTab(true);
        ShowPanel(upgradePanel);
    }

    public void HideUpgradePanel() => CloseUpgradePanel();

    public void ShowNote(string text, Action onClosed = null)
    {
        onNoteClosed = onClosed;
        if (noteText != null) noteText.text = text ?? string.Empty;
        ShowPanel(notePanel);
    }

    public void HideNotePanel() => CloseNotePanel();
    public void ShowEndingPanel() => ShowPanel(endingPanel);
    public void HideEndingPanel() => CloseEndingPanel();

    private void CacheUpgradePanelObjects()
    {
        if (upgradePanel == null) return;
        Transform content = upgradePanel.transform.Find("Content");
        if (content == null) return;

        technologyPage = FindObject(content, "TechnologyPage");
        equipmentPage = FindObject(content, "EquipmentPage");
        emptyEquipmentMessage = FindObject(content, "EquipmentPage/EmptyMessage");
        technologyTab = FindObject(content, "TechnologyTab")?.GetComponent<Button>();
        equipmentTab = FindObject(content, "EquipmentTab")?.GetComponent<Button>();
        upgradeCloseButton = FindObject(content, "CloseButton")?.GetComponent<Button>();

        technologyStatusTexts = new TMP_Text[technologyIds.Length];
        technologyButtons = new Button[technologyIds.Length];
        string[] technologyNames = { "HairDryer", "ElectricFan", "FasterSpawn", "UnlockNote" };
        for (int i = 0; i < technologyNames.Length; i++)
        {
            Transform row = FindObject(content, $"TechnologyPage/{technologyNames[i]}Row")?.transform;
            technologyStatusTexts[i] = FindObject(row, "Status")?.GetComponent<TMP_Text>();
            technologyButtons[i] = FindObject(row, "BuyButton")?.GetComponent<Button>();
        }

        equipmentSections = new GameObject[equipmentIds.Length];
        equipmentStatusTexts = new TMP_Text[equipmentIds.Length, equipmentStats.Length];
        equipmentButtons = new Button[equipmentIds.Length, equipmentStats.Length];
        string[] equipmentNames = { "HairDryer", "ElectricFan" };
        string[] statNames = { "WindForce", "WindRange", "WindAngle" };
        for (int equipment = 0; equipment < equipmentNames.Length; equipment++)
        {
            Transform section = FindObject(content, $"EquipmentPage/{equipmentNames[equipment]}Section")?.transform;
            equipmentSections[equipment] = section != null ? section.gameObject : null;
            for (int stat = 0; stat < statNames.Length; stat++)
            {
                Transform row = FindObject(section, statNames[stat] + "Row")?.transform;
                equipmentStatusTexts[equipment, stat] = FindObject(row, "Status")?.GetComponent<TMP_Text>();
                equipmentButtons[equipment, stat] = FindObject(row, "UpgradeButton")?.GetComponent<Button>();
            }
        }
    }

    private void BindUpgradePanelButtons()
    {
        if (technologyTab != null) technologyTab.onClick.AddListener(() => SetUpgradeTab(true));
        if (equipmentTab != null) equipmentTab.onClick.AddListener(() => SetUpgradeTab(false));
        if (upgradeCloseButton != null) upgradeCloseButton.onClick.AddListener(CloseUpgradePanel);

        for (int i = 0; i < technologyButtons.Length; i++)
        {
            int index = i;
            if (technologyButtons[i] != null)
                technologyButtons[i].onClick.AddListener(() => GameManager.Instance?.TryPurchaseTechnology(technologyIds[index]));
        }

        for (int equipment = 0; equipment < equipmentIds.Length; equipment++)
        {
            for (int stat = 0; stat < equipmentStats.Length; stat++)
            {
                int equipmentIndex = equipment;
                int statIndex = stat;
                if (equipmentButtons[equipment, stat] != null)
                    equipmentButtons[equipment, stat].onClick.AddListener(() => GameManager.Instance?.TryUpgradeEquipment(equipmentIds[equipmentIndex], equipmentStats[statIndex]));
            }
        }
    }

    private void SetUpgradeTab(bool technology)
    {
        showingTechnology = technology;
        if (technologyPage != null) technologyPage.SetActive(technology);
        if (equipmentPage != null) equipmentPage.SetActive(!technology);
        SetTabColor(technologyTab, technology);
        SetTabColor(equipmentTab, !technology);
        RefreshUpgradePanel();
    }

    private void RefreshUpgradePanel()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || technologyStatusTexts == null) return;

        for (int i = 0; i < technologyIds.Length; i++)
        {
            TechnologyId technology = technologyIds[i];
            bool purchased = manager.IsTechnologyPurchased(technology);
            SetText(technologyStatusTexts[i], purchased ? "已购买" : $"{manager.GetTechnologyCost(technology)} 个椰子");
            if (technologyButtons[i] != null)
            {
                technologyButtons[i].interactable = manager.CanPurchaseTechnology(technology);
                SetButtonLabel(technologyButtons[i], purchased ? "已完成" : "购买");
            }
        }

        bool hasEquipment = false;
        for (int equipment = 0; equipment < equipmentIds.Length; equipment++)
        {
            bool owned = manager.HasEquipment(equipmentIds[equipment]);
            if (equipmentSections[equipment] != null) equipmentSections[equipment].SetActive(owned);
            hasEquipment |= owned;

            for (int stat = 0; stat < equipmentStats.Length; stat++)
            {
                if (!owned) continue;
                int level = manager.GetEquipmentLevel(equipmentIds[equipment], equipmentStats[stat]);
                int cost = manager.GetEquipmentUpgradeCost(equipmentIds[equipment], equipmentStats[stat]);
                string name = equipmentStats[stat] == EquipmentStat.WindForce ? "风力" : equipmentStats[stat] == EquipmentStat.WindRange ? "距离" : "角度";
                SetText(equipmentStatusTexts[equipment, stat], cost == 0
                    ? $"{name}  Lv.{level}/3  已满级"
                    : $"{name}  Lv.{level}/3  下一档：{manager.GetNextUpgradeDescription(equipmentIds[equipment], equipmentStats[stat])}");
                if (equipmentButtons[equipment, stat] != null)
                {
                    equipmentButtons[equipment, stat].interactable = cost > 0 && manager.CoconutCount >= cost;
                    SetButtonLabel(equipmentButtons[equipment, stat], cost == 0 ? "满级" : $"{cost} 椰子");
                }
            }
        }

        if (emptyEquipmentMessage != null) emptyEquipmentMessage.SetActive(!hasEquipment);
    }

    private static GameObject FindObject(Transform parent, string path)
    {
        if (parent == null) return null;
        Transform found = parent.Find(path);
        return found != null ? found.gameObject : null;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button == null) return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = value;
    }

    private static void SetTabColor(Button button, bool active)
    {
        if (button != null && button.targetGraphic is Image image)
            image.color = active ? new Color(0.18f, 0.62f, 0.66f, 1f) : new Color(0.16f, 0.42f, 0.48f, 1f);
    }

    private void CloseUpgradePanel()
    {
        if (activePanel == upgradePanel) HideActivePanel();
    }

    private void CloseNotePanel()
    {
        if (activePanel != notePanel) return;
        Action callback = onNoteClosed;
        onNoteClosed = null;
        HideActivePanel();
        callback?.Invoke();
    }

    private void CloseEndingPanel()
    {
        if (activePanel == endingPanel) HideActivePanel();
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;
        HideAllPanelsImmediate();
        activePanel = panel;
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HideActivePanel()
    {
        if (activePanel == null) return;
        activePanel.SetActive(false);
        activePanel = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HideAllPanelsImmediate()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        if (notePanel != null) notePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
        activePanel = null;
    }
}
