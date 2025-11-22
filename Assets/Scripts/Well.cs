using UnityEngine;

public class Well : MonoBehaviour, IInteractable
{
	public bool canInteract { get; set; } = true;

	public void Interact()
	{
		if (!canInteract)
			return;
		canInteract = false;
		GetComponent<AudioSource>().Play();
		GetComponent<QuestObject>().FinishQuest();
	}
}
