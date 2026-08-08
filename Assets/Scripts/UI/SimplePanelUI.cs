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

    [Header("Upgrade Panel")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button upgradeCloseButton;

    [Header("Ending Panel")]
    [SerializeField] private Button endingCloseButton;

    private GameObject activePanel;
    private Action onNoteClosed;

    public bool IsOpen => activePanel != null;

    private void Awake()
    {
        if (noteCloseButton != null)
        {
            noteCloseButton.onClick.AddListener(CloseNotePanel);
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
        ShowPanel(upgradePanel);
    }

    public void HideUpgradePanel()
    {
        CloseUpgradePanel();
    }

    private void CloseUpgradePanel()
    {
        if (activePanel != upgradePanel)
        {
            return;
        }

        HideActivePanel();
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
