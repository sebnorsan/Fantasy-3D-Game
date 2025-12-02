using UnityEngine;

public class NPC_Interactable_Remote : MonoBehaviour, IInteractable
{
    [HideInInspector] public NPC_Interactable npcOwner;
	private Animator anim;
	public bool canInteract { get; set; } = true;
	private PlayerController playerController;
	private void Start()
	{
		anim = GetComponent<Animator>();
		playerController = GetComponentInParent<PlayerController>();
		npcOwner = FindFirstObjectByType<NPC_Interactable>();

		canInteract = false;
		if (playerController.IsOwner)
			canInteract = true;
	}
	public void Interact()
	{
		if (!playerController.IsOwner) return;

		if (!canInteract)
			return;

		StartCoroutine(PickUp());
	}
	System.Collections.IEnumerator PickUp()
	{
		canInteract = false;

		npcOwner.OnTalkEnded += TalkEnded;

		anim.SetTrigger("Answer");
		yield return new WaitForEndOfFrame();
		yield return new WaitForSeconds(GetTransitioningAnimationLength());
		npcOwner.InteractRemotely();
	}
	private void TalkEnded()
	{
		npcOwner.OnTalkEnded -= TalkEnded;

		anim.SetTrigger("HangUp");
		Invoke(nameof(ResetInteract), GetCurrentAnimationLength() + .4f);
	}
	private float GetCurrentAnimationLength() => anim.GetCurrentAnimatorStateInfo(0).length;
	private float GetTransitioningAnimationLength() => anim.GetNextAnimatorStateInfo(0).length;
	private void ResetInteract() => canInteract = true;
	public void CallUp()
	{
		anim.SetTrigger("Call");
	}
}
