using UnityEngine;

public class NPC_Interactable_Remote : MonoBehaviour, IInteractable
{
	public static NPC_Interactable_Remote instance;
    [SerializeField] private NPC_Interactable npcOwner;
	private Animator anim;

	public bool canInteract { get; set; } = true;

	private void Awake()
	{
		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}
	private void Start()
	{
		anim = GetComponent<Animator>();
	}
	private void OnEnable()
	{
		npcOwner.OnTalkEnded += TalkEnded;
	}
	public void Interact()
	{
		StartCoroutine(PickUp());
	}
	System.Collections.IEnumerator PickUp()
	{
		canInteract = false;

		anim.SetTrigger("Answer");
		yield return new WaitForSeconds(GetAnimationLength() + .4f);
		npcOwner.Interact();
	}
	private void TalkEnded()
	{
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
