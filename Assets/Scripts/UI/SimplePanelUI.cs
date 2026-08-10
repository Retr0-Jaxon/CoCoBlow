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

    [Header("Ending Panel")]
    [SerializeField] private Button endingCloseButton;

    private GameObject activePanel;
    private Action onNoteClosed;

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

        Button resolvedHairDryerButton = GetHairDryerUpgradeButton();
        if (resolvedHairDryerButton != null)
        {
            resolvedHairDryerButton.onClick.AddListener(HandleHairDryerUpgradeButtonClick);
        }

        if (coconutTreeUpgradeButton != null)
        {
            coconutTreeUpgradeButton.onClick.AddListener(HandleCoconutTreeUpgradeButtonClick);
        }

        if (upgradeCloseButton != null)
        {
            upgradeCloseButton.onClick.AddListener(CloseUpgradePanel);
        }

        if (endingCloseButton != null)
        {
            endingCloseButton.onClick.AddListener(CloseEndingPanel);
        }
    }

    private void Start()
    {
        HideAllPanelsImmediate();
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

    private void CloseUpgradePanel()
    {
        if (activePanel != upgradePanel)
        {
            return;
        }

        HideActivePanel();
    }

    private void RefreshUpgradeButtons()
    {
        if (gameManager == null)
        {
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
