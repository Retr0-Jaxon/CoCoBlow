using UnityEngine;

public class NoteInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private SimplePanelUI panelUI;
    [TextArea(3, 8)]
    [SerializeField] private string noteContent = "这是一张测试纸条。\n后续会接入正式剧情内容。";

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

        panelUI.ShowNote(noteContent);
    }

    public string GetHintText()
    {
        return string.Empty;
    }
}
