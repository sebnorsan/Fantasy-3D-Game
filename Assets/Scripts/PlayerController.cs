using EvolveGames;
using EZCameraShake;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
	// Player Settings
	[Header("Player Settings")]
	[SerializeField] public Transform playerCamera;
	[Range(1, 10)] public float walkingSpeed = 3.0f;
	[Range(0.1f, 5)] public float crouchSpeed = 1.0f;
	[Range(2, 20)] public float runningSpeed = 4.0f;
	[Range(0, 20)] public float jumpSpeed = 6.0f;
	[Range(0.5f, 10)] public float lookSpeed = 2.0f;
	[Range(10, 120)] public float lookXLimit = 80.0f;

	[Header("Advanced")]
	[SerializeField] float runningFOV = 65.0f;
	[SerializeField] float fovTransitionSpeed = 4.0f;
	[SerializeField] float crouchHeight = 1.0f;
	[SerializeField] float gravity = 20.0f;
	[SerializeField] float timeToRunning = 2.0f;
	[HideInInspector] public bool canMove = true;
	[HideInInspector] public bool canRun = true;

	[Header("Ground & Coyote Time")]
	public Transform groundCheck;
	public float checkRadius = 0.2f;
	[Range(0, 1)]
	public float coyoteTimeDuration = 0.2f;
	public float maxFallSpeed = -15f;

	[Header("CrouchCheck")]
	public Transform headCheck;
	public float headCheckRadius = 0.3f;

	[Header("Input")]
	[SerializeField] KeyCode crouchKey = KeyCode.LeftControl;
	public bool runToggle = false;

	[HideInInspector] public CharacterController characterController;
	[HideInInspector] public Vector3 moveDirection = Vector3.zero;
	[HideInInspector] public bool isRunning = false;
	[HideInInspector] public bool Moving = false;
	[HideInInspector] public Camera cam;

	// Private variables
	float initialWalkingSpeed;
	float runningValue;
	[HideInInspector]
	public float rotationX = 0f;
	float initialCrouchHeight;
	float initialFOV;
	float initialRunningFOV;

	bool isCrouching = false;
	[HideInInspector] public bool isGrounded;
	bool isClimbing = false;

	// Coyote time management
	bool canCoyote = false;
	bool coyoteActive = false;

	// Rebound control for "stuck" state
	bool rebound = false;
	Vector3 prevFramePos = Vector3.zero;
	bool resetVertical = false;

	[HideInInspector] public float inputVertical;
	[HideInInspector] public float inputHorizontal;

	public bool dev = false;
	private bool overrideDev = false;

	//private bool tooSteep = false;
	//private bool sliding = false;
	float runningFovMultiplier;
	private Footsteps footsteps;
	public ulong MyId => NetworkObject.OwnerClientId;

	private Vector3 spawnPos;
	private Quaternion spawnRot;

	//public void SetSpawnPoint(Vector3 pos, Quaternion rot)
	//{
	//	spawnPos = pos;
	//	spawnRot = rot;
	//}
	public override void OnNetworkSpawn()
	{
		if (!IsOwner)
		{
			gameObject.layer = LayerMask.NameToLayer("OtherGameController");

			foreach (var c in GetComponentsInChildren<Camera>(true))
				c.enabled = false;

			foreach (var a in GetComponentsInChildren<AudioListener>(true))
				a.enabled = false;

			//DisableIfExists<BowScript>();
			DisableIfExists<MovementEffects>();
			DisableIfExists<HandsSmooth>();
			DisableIfExists<HeadBob>();
			DisableIfExists<InteractionHandler>();
			DisableIfExists<HandsHolder>();
			DisableIfExists<EventAudioPlayer>();
			DisableIfExists<CameraShaker>();
		}
		//else
		//	TeleportToSpawnpoint();
	}
	//private void TeleportToSpawnpoint()
	//{
	//	if (IsSpawned && characterController && EventManager.instance != null)
	//		EventManager.instance.TeleportPlayer(this, spawnPos + new Vector3(0,1.5f,0));
	//	else
	//		Invoke(nameof(TeleportToSpawnpoint), .1f);
	//}
	private void DisableIfExists<T>() where T : Behaviour
	{
		var comps = GetComponentsInChildren<T>(true);
		foreach (var comp in comps)
			comp.enabled = false;
	}
	void Start()
	{
		cam = GetComponentInChildren<Camera>();
		initialFOV = cam.fieldOfView;
		runningFovMultiplier = runningFOV / initialFOV;
		footsteps = GetComponent<Footsteps>();

		characterController = GetComponent<CharacterController>();
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		initialCrouchHeight = characterController.height;

		runningValue = runningSpeed;
		initialWalkingSpeed = walkingSpeed;

		if (Application.isEditor)
			overrideDev = true;

		Debug.Log($"Player {OwnerClientId} start at {transform.position}");
	}
	public void ChangeFieldOfView(float newFov)
	{
		initialFOV = newFov;
		cam.fieldOfView = newFov;
		runningFOV = newFov * runningFovMultiplier;
	}

	private bool isFlying = false;

	private float lastLeftGroundTime = -Mathf.Infinity;
	private const float landThreshold = 0.25f;

	private Vector3 initFall;

	private void init_LeavingGrounded()
	{
		initFall = transform.position;
		lastLeftGroundTime = Time.time;
	}

	private void init_EnteringGrounded()
	{
		// new landing: reset the set of fired events
		_eventsFiredThisLanding.Clear();

		// check surfaces under feet for Event components that want collision activation
		TriggerLandingEvents();

		float dist = Vector3.Distance(new Vector3(transform.position.x, initFall.y, transform.position.z), transform.position);

		if (Time.time - lastLeftGroundTime >= landThreshold)
			footsteps.PlayOneOff();
	}

	private void PlayMovementSound(string clipName, float min, float max)
	{
		AudioManagement.instance.StopThisSound("Movement", clipName);
		AudioManagement.instance.PlayThisSound("Movement", clipName, true, min, max);
	}

	[SerializeField] private float footstepStopThreshold = 0.05f;

	// time at which we first noticed movement stopped
	private float footstepStopRequestTime;

	// whether a stop is currently pending
	private bool footstepStopPending = false;

	void Update()
	{
		if (!IsOwner) return;

		if (dev || overrideDev)
		{
			if (Input.GetKeyDown(KeyCode.C)) SaveState();
			if (Input.GetKeyDown(KeyCode.V)) LoadState();
			if (Input.GetKeyDown(KeyCode.F))
			{
				moveDirection = Vector3.zero;
				isFlying = !isFlying;

				isGrounded = false;
			}
		}

		if (Application.isEditor)
			if (Input.GetKeyDown(KeyCode.H)) overrideDev = !overrideDev;

		if (Moving && isGrounded)
		{
			footsteps.PlayFootsteps();

			if (footstepStopPending)
				footstepStopPending = false;
		}
		else
		{
			if (!footstepStopPending)
			{
				footstepStopPending = true;
				footstepStopRequestTime = Time.time;
			}
			else if (Time.time - footstepStopRequestTime >= footstepStopThreshold)
			{
				footsteps.StopFootsteps();
				footstepStopPending = false;
			}
			else if (!Moving)
			{
				footsteps.StopFootsteps();
				footstepStopPending = false;
			}
		}

		// Camera controls
		if (Cursor.lockState == CursorLockMode.Locked && canMove)
		{
			float mouseY = -Input.GetAxis("Mouse Y");
			float mouseX = Input.GetAxis("Mouse X");

			rotationX += mouseY * lookSpeed;
			rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
			playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
			transform.Rotate(0, mouseX * lookSpeed, 0);

			// Field of view adjustment
			if (isRunning && Moving)
				cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, runningFOV, fovTransitionSpeed * Time.deltaTime);
			else
				cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, initialFOV, fovTransitionSpeed * Time.deltaTime);
		}

		if (isFlying && canMove)
		{
			if (!dev && !overrideDev)
				isFlying = false;

			float flySpeed = 7f;
			float vertical = 0f;

			if (Input.GetKey(KeyCode.LeftShift))
				flySpeed = 60f;

			if (Input.GetKey(KeyCode.Space))
			{
				vertical = 1f;
			}
			else if (Input.GetKey(KeyCode.LeftControl))
			{
				vertical = -1f;
			}

			Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), vertical, Input.GetAxis("Vertical"));
			Vector3 move = transform.TransformDirection(direction) * flySpeed;

			characterController.Move(move * Time.deltaTime);

			return;
		}

		// Ground check
		isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, LayerMask.GetMask("Ground", "MovingPlatform", "OtherGameController"));

		// Handle vertical movement when not grounded
		if (!isGrounded)
		{
			if (resetVertical)
			{
				moveDirection.y = 0;
				resetVertical = false;

				init_LeavingGrounded();
			}
			transform.SetParent(null);

			if (!isClimbing)
			{
				moveDirection.y -= gravity * Time.deltaTime;
				if (moveDirection.y < maxFallSpeed)
					moveDirection.y = maxFallSpeed;
			}

			if (Vector3.Distance(new Vector3(transform.position.x, prevFramePos.y, transform.position.z), transform.position) < 0.001f && !rebound)
			{
				rebound = true;
				moveDirection.y = -1;
				Invoke(nameof(ResetRebound), 0.1f);
			}
			prevFramePos = transform.position;

			characterController.stepOffset = 0.1f;

			if (canCoyote)
			{
				canCoyote = false;
				Invoke(nameof(StopCoyote), coyoteTimeDuration);
				coyoteActive = true;
			}
		}
		else // When grounded
		{
			moveDirection.y = 0;

			if (!resetVertical)
				init_EnteringGrounded();

			resetVertical = true;

			// Parent to a moving platform if available
			Collider[] platformColliders = Physics.OverlapSphere(groundCheck.position, checkRadius,
												LayerMask.GetMask("MovingPlatform"));
			if (platformColliders.Length > 0)
				transform.SetParent(platformColliders[0].transform);

			if (!isCrouching)
				characterController.stepOffset = 0.65f;
			canCoyote = true;
			CancelInvoke(nameof(StopCoyote));
			coyoteActive = false;
		}

		// Handle movement input
		Vector3 forward = transform.TransformDirection(Vector3.forward);
		Vector3 right = transform.TransformDirection(Vector3.right);

		bool shift = Input.GetKey(KeyCode.LeftShift);

		bool runIntent = runToggle ? !shift : shift;

		bool runAllowedByStance = !(isCrouching && isGrounded);
		isRunning = runIntent && canRun && runAllowedByStance;


		inputVertical = canMove ? Input.GetAxis("Vertical") : 0f;
		inputHorizontal = canMove ? Input.GetAxis("Horizontal") : 0f;

		float baseSpeed =
			(isCrouching && isGrounded) ? crouchSpeed :
			(isRunning ? runningValue : walkingSpeed);

		// keep your ramp-up for running
		if (isRunning)
			runningValue = Mathf.Lerp(runningValue, runningSpeed, timeToRunning * Time.deltaTime);
		else
			runningValue = walkingSpeed;

		Vector3 desiredMove = (transform.TransformDirection(Vector3.forward) * inputVertical) +
							  (transform.TransformDirection(Vector3.right) * inputHorizontal);
		if (desiredMove.sqrMagnitude > 1f) desiredMove.Normalize();

		float verticalSpeed = moveDirection.y;
		moveDirection = desiredMove * baseSpeed;
		moveDirection.y = verticalSpeed;



		// Handle jumping
		if (Input.GetButton("Jump") && canMove && (isGrounded || coyoteActive) && !isClimbing)
		{
			if (coyoteActive)
				StopCoyote();

			moveDirection.y = jumpSpeed;

			PlayMovementSound("Slide", .65f, 1.35f);

			footsteps.StopFootsteps();
			footstepStopPending = false;
		}

		characterController.Move(moveDirection * Time.deltaTime);

		Moving = Mathf.Abs(inputVertical) > 0 || Mathf.Abs(inputHorizontal) > 0;

		bool crouchSphere = Physics.CheckSphere(headCheck.position, headCheckRadius, LayerMask.GetMask("Ground", "MovingPlatform", "OtherGameController"));

		// Crouching
		if (Input.GetKeyDown(crouchKey))
			SetCrouchHeight(crouchHeight);
		else if (Input.GetKeyUp(crouchKey) && !crouchSphere)
			ResetSetCrouchHeight(initialCrouchHeight);

		if (Input.GetKey(crouchKey))
		{
			isCrouching = true;
			if (isGrounded)
				walkingSpeed = Mathf.Lerp(walkingSpeed, crouchSpeed, 6 * Time.deltaTime);
		}
		else if (!crouchSphere)
		{
			isCrouching = false;
			walkingSpeed = Mathf.Lerp(walkingSpeed, initialWalkingSpeed, 4 * Time.deltaTime);

			if (characterController.height == crouchHeight)
				ResetSetCrouchHeight(initialCrouchHeight);
		}
	}
	void ResetRebound() => rebound = false;
	void StopCoyote() => coyoteActive = false;


	void SetCrouchHeight(float newHeight)
	{
		characterController.stepOffset = 0.1f;

		characterController.height = newHeight;

		//transform.localScale = new Vector3(transform.localScale.x, newHeight, transform.localScale.z);
		//characterController.enabled = false;
		EventManager.instance.TeleportPlayer(this, new Vector3(transform.position.x, transform.position.y - ((initialCrouchHeight - newHeight) / 2), transform.position.z));
		//characterController.enabled = true;
	}

	void ResetSetCrouchHeight(float newHeight)
	{
		characterController.stepOffset = 0.65f;

		characterController.height = newHeight;

		//transform.localScale = new Vector3(transform.localScale.x, newHeight, transform.localScale.z);
		//characterController.enabled = false;
		EventManager.instance.TeleportPlayer(this, new Vector3(transform.position.x, transform.position.y + ((initialCrouchHeight - crouchHeight) / 2), transform.position.z));
		//characterController.enabled = true;
	}

	private readonly HashSet<global::AbstractEvent> _eventsFiredThisLanding = new HashSet<global::AbstractEvent>();
	private void TriggerLandingEvents()
	{
		// Grab everything around feet. We include triggers, since you might have trigger volumes.
		Collider[] hits = Physics.OverlapSphere(
			groundCheck.position,
			checkRadius,
			LayerMask.GetMask("Ground", "MovingPlatform", "OtherGameController"),
			QueryTriggerInteraction.Collide
		);

		for (int i = 0; i < hits.Length; i++)
		{
			var col = hits[i];
			if (!col) continue;

			// We search on this collider's object or up its parents,
			// so it still works if the collider is on a child.
			var evt = col.GetComponentInParent<global::AbstractEvent>();
			if (evt == null) continue;

			// If we've already fired this exact Event for this landing, skip
			if (_eventsFiredThisLanding.Contains(evt))
				continue;

			// - only react to events that are intended to react to collision contact
			//   (that's what you described as "they have collision bool activated")
			if (evt.eventActivation != EventActivation.Collision 
				|| evt.eventActivation == EventActivation.Trigger 
				|| evt.eventActivation == EventActivation.Remote)
				continue;

			// Looks valid. Fire it and remember so we don't spam.
			_eventsFiredThisLanding.Add(evt);
			evt.CallEvent();
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
		Gizmos.DrawWireSphere(headCheck.position, headCheckRadius);
	}


	private Vector3 savePos = Vector3.zero;
	private void SaveState() => savePos = transform.position;
	private void LoadState() => EventManager.instance.TeleportPlayer(this ,savePos);
}
