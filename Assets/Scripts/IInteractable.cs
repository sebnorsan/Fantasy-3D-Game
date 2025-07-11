using UnityEngine;

public interface IInteractable
{
    public bool canInteract { get; set; }
    void Interact();
}
