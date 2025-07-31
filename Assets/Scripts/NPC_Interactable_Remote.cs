using UnityEngine;

public class NPC_Interactable_Remote : MonoBehaviour, IInteractable
{
    public NPC_Interactable npcOwner;
	private Animator anim;
	public bool canInteract { get; set; } = true;
	private void Start()
	{
		anim = GetComponent<Animator>();
	}
	public void Interact()
	{
		if (!canInteract)
			return;

		StartCoroutine(PickUp());
	}
	System.Collections.IEnumerator PickUp()
	{
		canInteract = false;

		npcOwner.OnTalkEnded += TalkEnded;

		anim.SetTrigger("Answer");
		yield return new WaitForSeconds(GetAnimationLength() -1f);
		npcOwner.Interact();
	}
	private void TalkEnded()
	{
		npcOwner.OnTalkEnded -= TalkEnded;

		anim.SetTrigger("HangUp");
		Invoke(nameof(ResetInteract), GetAnimationLength() + .4f);
	}
	private float GetAnimationLength() => anim.GetCurrentAnimatorStateInfo(0).length;
	private void ResetInteract() => canInteract = true;
	public void CallUp()
	{
		anim.SetTrigger("Call");
	}
}
