using UnityEngine;
using UnityEngine.Events;

public class InteractableProgressor : InteractionProgression, IInteractable
{
	public bool canInteract { get; set; } = true;

	[SerializeField] private UnityEvent eventsOnFinish;
	[SerializeField] private bool destroyOnFinish = false;
	[SerializeField] private bool disableInteractOnFinish = false;

	[Space(5)]

	[Header("Start, Cancel and Finish, trigger for hooked up Animator")]
	[SerializeField] private Animator anim;

	public void Interact()
	{
		if (!canInteract)
			return;

		StartProgression();
	}
	public override void StartProgression()
	{
		base.StartProgression();

		anim?.ResetTrigger("Start");
		anim?.SetTrigger("Start");
	}
	public override void CancelProgression()
	{
		base.CancelProgression();

		anim?.ResetTrigger("Cancel");
		anim?.SetTrigger("Cancel");
	}
	protected override void FinishAnimation()
	{
		base.FinishAnimation();

		if (destroyOnFinish)
			Destroy(gameObject);
		if (disableInteractOnFinish)
			canInteract = false;

		eventsOnFinish.Invoke();

		anim?.ResetTrigger("Finish");
		anim?.SetTrigger("Finish");
	}
}
