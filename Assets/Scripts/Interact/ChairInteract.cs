using UnityEngine;

public class ChairInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private SimplePanelUI panelUI;

    private void Awake()
    {
        if (panelUI == null)
        {
            panelUI = FindObjectOfType<SimplePanelUI>();
        }
    }

    public bool CanInteract()
    {
        return isActiveAndEnabled && panelUI != null && !panelUI.IsOpen;
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        panelUI.ShowUpgradePanel(() => GameManager.Instance != null && GameManager.Instance.TryUpgrade());
    }

    public string GetHintText()
    {
        return string.Empty;
    }
}
