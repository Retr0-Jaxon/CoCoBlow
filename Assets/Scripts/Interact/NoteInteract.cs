using UnityEngine;

public class NoteInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private SimplePanelUI panelUI;
    [SerializeField] private int noteIndex;

    private void Awake()
    {
        if (panelUI == null)
        {
            panelUI = FindObjectOfType<SimplePanelUI>();
        }
    }

    public bool CanInteract()
    {
        return isActiveAndEnabled
            && GameManager.Instance != null
            && GameManager.Instance.CanReadNote(noteIndex);
    }

    public void Interact()
    {
        if (!CanInteract() || GameManager.Instance == null)
        {
            return;
        }

        panelUI.ShowNote(
            GameManager.Instance.GetNoteContent(noteIndex),
            () => GameManager.Instance.OnNoteRead(noteIndex));
        AudioManager.PlayAudio("click", false);
    }

    public string GetHintText()
    {
        return string.Empty;
    }
}
