using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
	[SerializeField] private float maxDistance = 3f;

	private PlayerController playerController;

	public static InteractionHandler singleton;

	public Animator interactionKeyAnimator;

	private void Awake()
	{
		if (singleton != null)
			Destroy(gameObject);
		else
			singleton = this;
	}

	private void Start()
	{
		playerController = FindFirstObjectByType<PlayerController>();
	}

	Ray ray;
	void Update()
	{
		ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

		if (Input.GetKeyDown(KeyCode.E))
		{
			if (isTalking)
			{
				npc.ContinueTalk();
				return;
			}

			if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
				if (hit.transform.TryGetComponent<IInteractable>(out var interactable))
					interactable.Interact();
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
	}
	public void ExitInteraction_NPC()
	{
		npc = null;

		isTalking = false;
		playerController.canMove = true;
	}
}
