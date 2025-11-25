using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
	[SerializeField] private float maxDistance = 3f;

	private PlayerController playerController;

	private Animator interactionKeyAnimator;
	Camera cam;

	private void Start()
	{
		playerController = GetComponentInParent<PlayerController>(includeInactive: true);
		if (!playerController.IsOwner) return;

		Invoke(nameof(StartChecks), 1f);
	}

	private void StartChecks()
	{
		interactionKeyAnimator = EventManager.instance.interactionAnimator;
		cam = playerController.cam;
	}

	Ray ray;
	void Update()
	{
		if (playerController == null || !playerController.IsOwner || cam == null || interactionKeyAnimator == null) return;

		ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

		if (Input.GetKeyDown(KeyCode.E))
		{
			FindFirstObjectByType<NPC_Interactable>().SetInteractionHandler(this);

			if (isTalking)
			{
				npc.ContinueTalk();
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
				if (hit.transform.TryGetComponent<IInteractable>(out var interactable))
				{
					//if (interactable is NPC_Interactable npcInteractable)
					//	npcInteractable.SetInteractionHandler(this);

					interactable.Interact();
				}
		}

		CheckForVisual();
	}
	private void CheckForVisual()
	{
		bool isLooking = false;

		if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
			if (hit.transform.TryGetComponent<IInteractable>(out var interactable))
				if (interactable.canInteract)
					isLooking = true;

		interactionKeyAnimator.SetBool("LookingAtInteractable", isLooking);
	}

	private NPC_Interactable npc;

	[HideInInspector] public bool isTalking;
	public void EnterInteraction_NPC(NPC_Interactable temp_npc)
	{
		npc = temp_npc;

		isTalking = true;
		playerController.canMove = false;
		playerController.GetComponentInChildren<HandsSmooth>().enabled = false;
		FindFirstObjectByType<BowScript>().enabled = false;
	}
	public void ExitInteraction_NPC()
	{
		npc = null;

		isTalking = false;
		playerController.canMove = true;
		playerController.GetComponentInChildren<HandsSmooth>().enabled = true;
		FindFirstObjectByType<BowScript>().enabled = true;
	}
}
