public interface IInteractable
{
    bool CanInteract();
    void Interact();
    string GetHintText(); // MVP 可返回空
}