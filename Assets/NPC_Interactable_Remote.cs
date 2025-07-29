using UnityEngine;

public class NPC_Interactable_Remote : MonoBehaviour, IInteractable
{
    [SerializeField] private NPC_Interactable npcOwner;

	public bool canInteract { get ; set ; }

	private void OnEnable()
	{
		npcOwner.OnTalkEnded += TalkEnded;	
	}
	private void Start()
	{
		canInteract = npcOwner.canInteract;
	}
	public void Interact()
	{
		npcOwner.Interact();
	}
	private void TalkEnded()
	{

	}
}
