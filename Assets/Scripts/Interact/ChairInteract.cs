using UnityEngine;

public class ChairInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private SimplePanelUI panelUI;

    private void Awake()
    {
        if (panelUI == null) panelUI = FindObjectOfType<SimplePanelUI>();
    }

    public bool CanInteract() => isActiveAndEnabled && panelUI != null && !panelUI.IsOpen;

    public void Interact()
    {
        if (CanInteract()) panelUI.ShowUpgradePanel();
    }

    public string GetHintText() => string.Empty;
}
