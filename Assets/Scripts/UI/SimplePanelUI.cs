using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SimplePanelUI : MonoBehaviour
{
    public enum UpgradeCategory
    {
        HairDryer,
        CoconutTree
    }

    [Header("Panels")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject notePanel;
    [SerializeField] private GameObject endingPanel;

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Note Panel")]
    [SerializeField] private TMP_Text noteText;
    [SerializeField] private Button noteCloseButton;

    [Header("Upgrade Panel")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button hairDryerUpgradeButton;
    [SerializeField] private Button coconutTreeUpgradeButton;
    [SerializeField] private Button upgradeCloseButton;
    [SerializeField] private string hairDryerUpgradeText = "升级吹风机";
    [SerializeField] private string coconutTreeUpgradeText = "升级椰子树";

    [Header("Upgrade Tree")]
    [SerializeField] private GameObject upgradeSubMenu;
    [SerializeField] private Image upgradeLayoutImage;
    [SerializeField] private RectTransform upgradePanelContent;
    [SerializeField] private TMP_Text upgradeTitleText;
    [SerializeField] private TMP_Text upgradeStatusText;
    [SerializeField] private TMP_Text upgradeDescriptionText;
    [SerializeField] private Button upgradeActionButton;
    [SerializeField] private UpgradeLayoutPreset[] layoutPresets;
    [SerializeField] private string hairDryerDescriptionText = "提升吹风机风力、射程和瞄准范围。";
    [SerializeField] private string coconutTreeDescriptionText = "提升椰子树生成速度和树上最大椰子数量。";

    [Header("Ending Panel")]
    [SerializeField] private Button endingCloseButton;

    private GameObject activePanel;
    private Action onNoteClosed;
    private UpgradeCategory selectedUpgradeCategory;
    private int selectedUpgradeNodeIndex = -1;
    private UpgradeSubMenuLayout selectedLayout;
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    public bool IsOpen => activePanel != null;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null
                ? GameManager.Instance
                : FindObjectOfType<GameManager>();
        }

        EnsureUpgradeButtons();

        if (noteCloseButton != null)
        {
            noteCloseButton.onClick.AddListener(CloseNotePanel);
        }

        if (upgradeActionButton != null)
        {
            upgradeActionButton.onClick.AddListener(HandleSelectedUpgradeButtonClick);
        }
        else
        {
            Button resolvedHairDryerButton = GetHairDryerUpgradeButton();
            if (resolvedHairDryerButton != null)
            {
                resolvedHairDryerButton.onClick.AddListener(HandleHairDryerUpgradeButtonClick);
            }

            if (coconutTreeUpgradeButton != null)
            {
                coconutTreeUpgradeButton.onClick.AddListener(HandleCoconutTreeUpgradeButtonClick);
            }
        }

        if (upgradeCloseButton != null)
        {
            upgradeCloseButton.onClick.AddListener(CloseUpgradePanel);
        }

        if (endingCloseButton != null)
        {
            endingCloseButton.onClick.AddListener(CloseEndingPanel);
        }

        SetUpgradeActionButtonActive(false);
    }

    private void Start()
    {
        HideAllPanelsImmediate();
        SetUpgradeActionButtonActive(false);
    }

    private void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (activePanel == notePanel)
        {
            CloseNotePanel();
        }
        else if (activePanel == upgradePanel)
        {
            CloseUpgradePanel();
        }
        else if (activePanel == endingPanel)
        {
            CloseEndingPanel();
        }
    }

    public void ShowUpgradePanel()
    {
        selectedUpgradeNodeIndex = -1;
        HideUpgradeSubMenu();
        SetUpgradeActionButtonActive(false);
        RefreshUpgradeButtons();
        ShowPanel(upgradePanel);
    }

    public void HideUpgradePanel()
    {
        CloseUpgradePanel();
    }

    private void HandleHairDryerUpgradeButtonClick()
    {
        if (gameManager != null && gameManager.TryUpgradeHairDryer())
        {
            RefreshUpgradeButtons();
        }
    }

    private void HandleCoconutTreeUpgradeButtonClick()
    {
        if (gameManager != null && gameManager.TryUpgradeCoconutTree())
        {
            RefreshUpgradeButtons();
        }
    }

    private void HandleSelectedUpgradeButtonClick()
    {
        if (gameManager == null || selectedUpgradeNodeIndex < 0 || !CanUpgradeSelectedNode())
        {
            return;
        }

        bool upgraded = selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? gameManager.TryUpgradeHairDryer()
            : gameManager.TryUpgradeCoconutTree();

        if (upgraded)
        {
            RefreshUpgradeButtons();
        }
    }

    private void CloseUpgradePanel()
    {
        if (activePanel != upgradePanel)
        {
            return;
        }

        HideUpgradeSubMenu();
        HideActivePanel();
    }

    public void SelectHairDryerUpgradeNode(int nodeIndex, UpgradeSubMenuLayout layout)
    {
        SelectUpgradeNode(UpgradeCategory.HairDryer, nodeIndex, layout);
    }

    public void SelectCoconutTreeUpgradeNode(int nodeIndex, UpgradeSubMenuLayout layout)
    {
        SelectUpgradeNode(UpgradeCategory.CoconutTree, nodeIndex, layout);
    }

    private void SelectUpgradeNode(UpgradeCategory category, int nodeIndex, UpgradeSubMenuLayout layout)
    {
        selectedUpgradeCategory = category;
        selectedUpgradeNodeIndex = Mathf.Max(0, nodeIndex);
        selectedLayout = layout;

        if (upgradeSubMenu != null)
        {
            upgradeSubMenu.SetActive(true);
            ApplySelectedLayout();
        }

        SetUpgradeActionButtonActive(true);
        RefreshUpgradeButtons();
    }

    private void ApplySelectedLayout()
    {
        if (!TryGetLayoutPreset(selectedLayout, out UpgradeLayoutPreset preset))
        {
            return;
        }

        if (upgradeLayoutImage != null)
        {
            upgradeLayoutImage.sprite = preset.layoutSprite;
            upgradeLayoutImage.color = Color.white;
            upgradeLayoutImage.raycastTarget = false;
        }

        if (upgradePanelContent != null)
        {
            ApplyTopLeftPixelRect(upgradePanelContent, preset.panelRect);
        }

        if (upgradeActionButton != null)
        {
            ApplyTopLeftPixelRect(upgradeActionButton.GetComponent<RectTransform>(), preset.buttonRect);
        }
    }

    private bool TryGetLayoutPreset(UpgradeSubMenuLayout layout, out UpgradeLayoutPreset preset)
    {
        if (layoutPresets != null)
        {
            foreach (UpgradeLayoutPreset candidate in layoutPresets)
            {
                if (candidate.layout == layout)
                {
                    preset = candidate;
                    return true;
                }
            }
        }

        preset = default;
        return false;
    }

    private static void ApplyTopLeftPixelRect(RectTransform rectTransform, Rect pixelRect)
    {
        if (rectTransform == null || pixelRect.width <= 0f || pixelRect.height <= 0f)
        {
            return;
        }

        float minX = pixelRect.x / ReferenceWidth;
        float maxX = (pixelRect.x + pixelRect.width) / ReferenceWidth;
        float minY = (ReferenceHeight - pixelRect.y - pixelRect.height) / ReferenceHeight;
        float maxY = (ReferenceHeight - pixelRect.y) / ReferenceHeight;

        rectTransform.anchorMin = new Vector2(minX, minY);
        rectTransform.anchorMax = new Vector2(maxX, maxY);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private void RefreshUpgradeButtons()
    {
        if (gameManager == null)
        {
            return;
        }

        if (upgradeActionButton != null)
        {
            RefreshSelectedUpgradeButton();
            return;
        }

        Button resolvedHairDryerButton = GetHairDryerUpgradeButton();
        if (resolvedHairDryerButton != null)
        {
            resolvedHairDryerButton.interactable = gameManager.CanUpgradeHairDryer;
            SetUpgradeButtonText(
                resolvedHairDryerButton,
                hairDryerUpgradeText,
                gameManager.HairDryerUpgradeLevel,
                gameManager.MaxHairDryerUpgradeLevel,
                gameManager.GetNextHairDryerUpgradeCost());
        }

        if (coconutTreeUpgradeButton != null)
        {
            coconutTreeUpgradeButton.interactable = gameManager.CanUpgradeCoconutTree;
            SetUpgradeButtonText(
                coconutTreeUpgradeButton,
                coconutTreeUpgradeText,
                gameManager.CoconutTreeUpgradeLevel,
                gameManager.MaxCoconutTreeUpgradeLevel,
                gameManager.GetNextCoconutTreeUpgradeCost());
        }
    }

    private void RefreshSelectedUpgradeButton()
    {
        if (selectedUpgradeNodeIndex < 0)
        {
            SetUpgradeTreeTexts("选择椰子节点", "点击任意椰子查看升级内容。", string.Empty);
            upgradeActionButton.interactable = false;
            SetButtonText(upgradeActionButton, "升级");
            return;
        }

        string label = GetSelectedUpgradeLabel();
        int currentLevel = GetSelectedCurrentLevel();
        int maxLevel = GetSelectedMaxLevel();
        int cost = GetSelectedUpgradeCost(selectedUpgradeNodeIndex);
        bool isCurrentNode = selectedUpgradeNodeIndex == currentLevel;
        bool canUpgrade = CanUpgradeSelectedNode();

        string title = $"{label} Lv.{selectedUpgradeNodeIndex + 1}";
        string status;
        if (selectedUpgradeNodeIndex < currentLevel)
        {
            status = "已升级";
        }
        else if (selectedUpgradeNodeIndex >= maxLevel)
        {
            status = "已满级";
        }
        else if (selectedUpgradeNodeIndex > currentLevel)
        {
            status = "需要先完成前置升级";
        }
        else if (!canUpgrade)
        {
            status = $"需要 {cost} 椰子";
        }
        else
        {
            status = $"消耗 {cost} 椰子升级";
        }

        SetUpgradeTreeTexts(title, status, GetSelectedUpgradeDescription());
        upgradeActionButton.interactable = canUpgrade && isCurrentNode;
        SetButtonText(upgradeActionButton, selectedUpgradeNodeIndex < currentLevel ? "已升级" : "升级");
    }

    private bool CanUpgradeSelectedNode()
    {
        if (gameManager == null || selectedUpgradeNodeIndex < 0)
        {
            return false;
        }

        return selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? selectedUpgradeNodeIndex == gameManager.HairDryerUpgradeLevel && gameManager.CanUpgradeHairDryer
            : selectedUpgradeNodeIndex == gameManager.CoconutTreeUpgradeLevel && gameManager.CanUpgradeCoconutTree;
    }

    private int GetSelectedCurrentLevel()
    {
        return selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? gameManager.HairDryerUpgradeLevel
            : gameManager.CoconutTreeUpgradeLevel;
    }

    private int GetSelectedMaxLevel()
    {
        return selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? gameManager.MaxHairDryerUpgradeLevel
            : gameManager.MaxCoconutTreeUpgradeLevel;
    }

    private int GetSelectedUpgradeCost(int nodeIndex)
    {
        return selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? gameManager.GetHairDryerUpgradeCost(nodeIndex)
            : gameManager.GetCoconutTreeUpgradeCost(nodeIndex);
    }

    private string GetSelectedUpgradeLabel()
    {
        return selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? hairDryerUpgradeText
            : coconutTreeUpgradeText;
    }

    private string GetSelectedUpgradeDescription()
    {
        return selectedUpgradeCategory == UpgradeCategory.HairDryer
            ? hairDryerDescriptionText
            : coconutTreeDescriptionText;
    }

    private void SetUpgradeTreeTexts(string title, string status, string description)
    {
        if (upgradeTitleText != null)
        {
            upgradeTitleText.text = title;
        }

        if (upgradeStatusText != null)
        {
            upgradeStatusText.text = status;
        }

        if (upgradeDescriptionText != null)
        {
            upgradeDescriptionText.text = description;
        }
    }

    private void HideUpgradeSubMenu()
    {
        if (upgradeSubMenu != null)
        {
            upgradeSubMenu.SetActive(false);
        }

        SetUpgradeActionButtonActive(false);
    }

    private void SetUpgradeActionButtonActive(bool active)
    {
        if (upgradeActionButton == null)
        {
            return;
        }

        upgradeActionButton.gameObject.SetActive(active);
        upgradeActionButton.interactable = false;
    }

    private Button GetHairDryerUpgradeButton()
    {
        return hairDryerUpgradeButton != null ? hairDryerUpgradeButton : upgradeButton;
    }

    private void EnsureUpgradeButtons()
    {
        Button resolvedHairDryerButton = GetHairDryerUpgradeButton();
        if (resolvedHairDryerButton == null || coconutTreeUpgradeButton != null)
        {
            return;
        }

        coconutTreeUpgradeButton = Instantiate(resolvedHairDryerButton, resolvedHairDryerButton.transform.parent);
        coconutTreeUpgradeButton.name = "CoconutTreeUpgradeButton";
        coconutTreeUpgradeButton.onClick.RemoveAllListeners();

        RectTransform rectTransform = coconutTreeUpgradeButton.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += Vector2.down * 56f;
        }

        SetButtonText(resolvedHairDryerButton, hairDryerUpgradeText);
        SetButtonText(coconutTreeUpgradeButton, coconutTreeUpgradeText);
    }

    private static void SetUpgradeButtonText(Button button, string label, int currentLevel, int maxLevel, int nextCost)
    {
        if (currentLevel >= maxLevel)
        {
            SetButtonText(button, $"{label}（已满级）");
            return;
        }

        SetButtonText(button, $"{label} Lv.{currentLevel + 1}（{nextCost} 椰子）");
    }

    private static void SetButtonText(Button button, string text)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
        if (label != null)
        {
            label.text = text;
        }
    }

    public void ShowNote(string text, Action onClosed = null)
    {
        onNoteClosed = onClosed;

        if (noteText != null)
        {
            noteText.text = text ?? string.Empty;
        }

        ShowPanel(notePanel);
    }

    public void HideNotePanel()
    {
        CloseNotePanel();
    }

    public void ShowEndingPanel()
    {
        ShowPanel(endingPanel);
    }

    public void HideEndingPanel()
    {
        CloseEndingPanel();
    }

    private void CloseNotePanel()//MARKER 将传进来的Action委托赋值给onNoteClosed，然后调用HideActivePanel，最后调用callback
    {
        if (activePanel != notePanel)
        {
            return;
        }

        Action callback = onNoteClosed;
        onNoteClosed = null;
        HideActivePanel();
        callback?.Invoke();
    }

    private void CloseEndingPanel()
    {
        if (activePanel != endingPanel)
        {
            return;
        }

        HideActivePanel();
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        HideAllPanelsImmediate();
        activePanel = panel;
        panel.SetActive(true);
        UnlockCursor();
    }

    private void HideActivePanel()
    {
        if (activePanel == null)
        {
            return;
        }

        activePanel.SetActive(false);
        activePanel = null;
        LockCursor();
    }

    private void HideAllPanelsImmediate()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }

        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }

        if (endingPanel != null)
        {
            endingPanel.SetActive(false);
        }

        activePanel = null;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
