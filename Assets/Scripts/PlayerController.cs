using EvolveGames;
using EZCameraShake;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
	#region Inspector: Player Settings

	public PlayerReferences pRef;
	public PlayerInputs pInput;

	[Header("Player Settings")]
	[SerializeField] public Transform playerCamera;
	[Range(1, 10)] public float walkingSpeed = 3.0f;
	[Range(0.1f, 5)] public float crouchSpeed = 1.0f;
	[Range(2, 20)] public float runningSpeed = 4.0f;
	[Range(0, 20)] public float jumpSpeed = 6.0f;
	[Range(0.5f, 10)] public float lookSpeed = 2.0f;
	[Range(10, 120)] public float lookXLimit = 80.0f;

	[Header("Advanced")]
	[SerializeField] private float runningFOV = 65.0f;
	[SerializeField] private float fovTransitionSpeed = 4.0f;
	[SerializeField] private float crouchHeight = 1.0f;
	[SerializeField] private float gravity = 20.0f;
	[SerializeField] private float timeToRunning = 2.0f;
	[HideInInspector] public bool canMove = true;
	[HideInInspector] public bool canRun = true;

	[Header("Ground & Coyote Time")]
	public Transform groundCheck;
	public float checkRadius = 0.2f;
	[Range(0, 1)] public float coyoteTimeDuration = 0.2f;
	public float maxFallSpeed = -15f;

	[Header("CrouchCheck")]
	public Transform headCheck;
	public float headCheckRadius = 0.3f;
	public CapsuleCollider playerDamageCollider;

	private Vector3 standCamLocalPos;
	[SerializeField] private float crouchCamLerpSpeed = 8f;


	[Header("Input")]
	public bool runToggle = false;

	[Header("Knockback")]
	[SerializeField] private float knockbackDecay = 5f;

	[Header("Footsteps")]
	[SerializeField] private float footstepStopThreshold = 0.05f;

	#endregion

	#region Components

	private Footsteps footsteps;

	#endregion
	#region Movement State

	[HideInInspector] public Vector3 moveDirection = Vector3.zero;
	[HideInInspector] public bool isRunning = false;
	[HideInInspector] public bool isMoving = false;
	[HideInInspector] public float rotationX = 0f;
	[HideInInspector] public bool isGrounded;
	[HideInInspector] public float inputVertical;
	[HideInInspector] public float inputHorizontal;

	private float initialWalkingSpeed;
	private float runningValue;
	private float initialCrouchHeight;
	private float initialFOV;
	private float runningFovMultiplier;

	private bool isCrouching = false;
	private bool isFlying = false;

	private bool canCoyote = false;
	private bool coyoteActive = false;

	private bool rebound = false;
	private Vector3 prevFramePos = Vector3.zero;
	private bool resetVertical = false;

	private float lastLeftGroundTime = -Mathf.Infinity;
	private const float landThreshold = 0.25f;
	private Vector3 initFall;

	private float footstepStopRequestTime;
	private bool footstepStopPending = false;

	private Vector3 knockbackVelocity = Vector3.zero;

	#endregion
	#region Debug & Dev

	[Header("Debugging")]

	[SerializeField] private bool dev = false;
	[SerializeField] private float flySpeed = 7f;
	[SerializeField] private float flySpeedFast = 60f;
	[SerializeField] private float flySpeedVertical = 5;
	private float flySpeedToUse = 0f;
	private bool overrideDev = false;
	private Vector3 savePos = Vector3.zero;

	#endregion
	#region Networking & Events

	public ulong MyId => NetworkObject.OwnerClientId;

	#endregion
	#region Network Lifecycle

	public override void OnNetworkSpawn()
	{
		if (!IsOwner)
		{
			gameObject.layer = LayerMask.NameToLayer("OtherGameController");

			foreach (var c in GetComponentsInChildren<Camera>(true))
				c.enabled = false;

			foreach (var a in GetComponentsInChildren<AudioListener>(true))
				a.enabled = false;

			DisableIfExists<MovementEffects>();
			DisableIfExists<HandsSmooth>();
			DisableIfExists<HeadBob>();
			DisableIfExists<InteractionHandler>();
			DisableIfExists<HandsHolder>();
			DisableIfExists<EventAudioPlayer>();
			DisableIfExists<CameraShaker>();
		}
	}

	private void DisableIfExists<T>() where T : Behaviour
	{
		var comps = GetComponentsInChildren<T>(true);
		foreach (var comp in comps)
			comp.enabled = false;
	}

	#endregion

	#region Unity Lifecycle

	private void Start()
	{
		initialFOV = pRef.playerCam.fieldOfView;
		runningFovMultiplier = runningFOV / initialFOV;
		footsteps = GetComponent<Footsteps>();

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		initialCrouchHeight = pRef.playerCharacterController.height;

		runningValue = runningSpeed;
		initialWalkingSpeed = walkingSpeed;

		if (pRef.playerCam != null)
			standCamLocalPos = playerCamera.transform.localPosition;

		if (Application.isEditor)
			overrideDev = true;
	}

	private void Update()
	{
		if (!IsOwner) return;

		HandleLookingAround();
		HandleGrounded();
		HandleInput();
		HandleJumpingInput();   
		HandleCrouchingInput();
		HandleCrouchCamera();
		HandleMovement();       
		HandleKnockback();
		HandleFootsteps();
	}

	private void HandleFlying()
	{
		flySpeedToUse = 0f;

		if (isFlying && canMove)
		{
			if (!dev && !overrideDev)
				isFlying = false;

			flySpeedToUse = flySpeed;

			if (Input.GetKey(pInput.runningKey))
				flySpeedToUse = flySpeedFast;

			if (Input.GetKey(pInput.jumpKey))
				moveDirection.y = flySpeedVertical;
			else if (Input.GetKey(pInput.crouchKey))
				moveDirection.y = -flySpeedVertical;
			else
				moveDirection.y = 0f;
		}
	}
	private void HandleFootsteps()
	{
		if (isMoving && isGrounded)
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
			else if (!isMoving)
			{
				footsteps.StopFootsteps();
				footstepStopPending = false;
			}
		}
	}
	private void HandleLookingAround()
	{
		if (Cursor.lockState == CursorLockMode.Locked && canMove)
		{
			float mouseY = -Input.GetAxis("Mouse Y");
			float mouseX = Input.GetAxis("Mouse X");

			rotationX += mouseY * lookSpeed;
			rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
			playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
			transform.Rotate(0, mouseX * lookSpeed, 0);

			if (isRunning && isMoving)
				pRef.playerCam.fieldOfView = Mathf.Lerp(pRef.playerCam.fieldOfView, runningFOV, fovTransitionSpeed * Time.deltaTime);
			else
				pRef.playerCam.fieldOfView = Mathf.Lerp(pRef.playerCam.fieldOfView, initialFOV, fovTransitionSpeed * Time.deltaTime);
		}
	}
	private void HandleGrounded()
	{
		if (isFlying) return;

		isGrounded = Physics.CheckSphere(
			groundCheck.position,
			checkRadius,
			pRef.groundLayerMask
		);

		if (!isGrounded)
		{
			if (resetVertical)
			{
				moveDirection.y = 0;
				resetVertical = false;

				init_LeavingGrounded();
			}

			transform.SetParent(null);

			HandleGravity();
			
			pRef.playerCharacterController.stepOffset = 0.1f;

			if (canCoyote)
			{
				canCoyote = false;
				Invoke(nameof(StopCoyote), coyoteTimeDuration);
				coyoteActive = true;
			}
		}
		else
		{
			moveDirection.y = 0;

			if (!resetVertical)
				init_EnteringGrounded();

			resetVertical = true;

			Collider[] platformColliders = Physics.OverlapSphere(
				groundCheck.position,
				checkRadius,
				LayerMask.GetMask("MovingPlatform")
			);

			if (platformColliders.Length > 0)
				transform.SetParent(platformColliders[0].transform);

			if (!isCrouching)
				pRef.playerCharacterController.stepOffset = 0.65f;

			canCoyote = true;
			CancelInvoke(nameof(StopCoyote));
			coyoteActive = false;
		}
	}
	private void HandleGravity()
	{
		moveDirection.y -= gravity * Time.deltaTime; if (moveDirection.y < maxFallSpeed) moveDirection.y = maxFallSpeed;

		if (Vector3.Distance(
					new Vector3(transform.position.x, prevFramePos.y, transform.position.z),
					transform.position) < 0.001f && !rebound)
		{
			rebound = true;
			moveDirection.y = -1;
			Invoke(nameof(ResetRebound), 0.1f);

			prevFramePos = transform.position;
		}
	}
	private void HandleKnockback()
	{
		knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
	}
	private void HandleMovement()
	{
		HandleFlying();

		Vector3 forward = transform.TransformDirection(Vector3.forward);
		Vector3 right = transform.TransformDirection(Vector3.right);

		float baseSpeed =
			(isCrouching && isGrounded) ? crouchSpeed :
			(isRunning ? runningValue : walkingSpeed);

		if (isRunning)
			runningValue = Mathf.Lerp(runningValue, runningSpeed, timeToRunning * Time.deltaTime);
		else
			runningValue = walkingSpeed;

		Vector3 desiredMove =
			(transform.TransformDirection(Vector3.forward) * inputVertical) +
			(transform.TransformDirection(Vector3.right) * inputHorizontal);

		if (desiredMove.sqrMagnitude > 1f)
			desiredMove.Normalize();

		float verticalSpeed = moveDirection.y;

		Vector3 horizontalMove = desiredMove * (baseSpeed + flySpeedToUse) + knockbackVelocity;
		moveDirection = new Vector3(horizontalMove.x, verticalSpeed, horizontalMove.z);

		if (pRef.playerCharacterController.enabled)
			pRef.playerCharacterController?.Move(moveDirection * Time.deltaTime);

		isMoving = Mathf.Abs(inputVertical) > 0 || Mathf.Abs(inputHorizontal) > 0;

		if (isMoving && isGrounded)
			pRef.playerGraphics.PlayParticle(PfxToPlay.Run, true);
		else
			pRef.playerGraphics.PlayParticle(PfxToPlay.Run, false);
	}
	private void HandleInput()
	{
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

		bool shift = Input.GetKey(pInput.runningKey);
		bool runIntent = runToggle ? !shift : shift;
		bool runAllowedByStance = !(isCrouching && isGrounded);
		isRunning = runIntent && canRun && runAllowedByStance;

		inputVertical = canMove ? Input.GetAxis("Vertical") : 0f;
		inputHorizontal = canMove ? Input.GetAxis("Horizontal") : 0f;
	}
	private void HandleJumpingInput()
	{
		if (Input.GetKey(pInput.jumpKey) && canMove && (isGrounded || coyoteActive))
		{
			pRef.playerGraphics.PlayParticle(PfxToPlay.Halo);
			pRef.playerGraphics.PlayParticle(PfxToPlay.Stripe);

			if (coyoteActive)
				StopCoyote();

			moveDirection.y = jumpSpeed;

			PlayMovementSound("Slide", .65f, 1.35f);

			footsteps.StopFootsteps();
			footstepStopPending = false;

			pRef.playerAnimator.A_Jump();
		}
	}
	private void HandleCrouchingInput()
	{
		bool crouchSphere = Physics.CheckSphere(
			headCheck.position,
			headCheckRadius,
			pRef.groundLayerMask
		);

		if (Input.GetKeyDown(pInput.crouchKey))
		{
			SetCrouchHeight(crouchHeight);                // instant local
			SetCrouchHeightRpc(crouchHeight);            // sync others
		}
		else if (Input.GetKeyUp(pInput.crouchKey) && !crouchSphere)
		{
			ResetSetCrouchHeight(initialCrouchHeight);   // instant local
			ResetCrouchHeightRpc(initialCrouchHeight);   // sync others
		}

		if (Input.GetKey(pInput.crouchKey))
		{
			isCrouching = true;
			if (isGrounded)
				walkingSpeed = Mathf.Lerp(walkingSpeed, crouchSpeed, 6 * Time.deltaTime);
		}
		else if (!crouchSphere)
		{
			isCrouching = false;
			walkingSpeed = Mathf.Lerp(walkingSpeed, initialWalkingSpeed, 4 * Time.deltaTime);

			if (pRef.playerCharacterController?.height == crouchHeight)
				ResetSetCrouchHeight(initialCrouchHeight);
		}
	}
	private void HandleCrouchCamera()
	{
		if (playerCamera == null || pRef.crouchCamPoint == null) return;

		var camTransform = playerCamera.transform;

		Vector3 targetPos = isCrouching
			? pRef.crouchCamPoint.localPosition   // crouched anchor
			: standCamLocalPos;                   // original stand pos

		camTransform.localPosition = Vector3.Lerp(
			camTransform.localPosition,
			targetPos,
			crouchCamLerpSpeed * Time.deltaTime
		);
	}
	#endregion

	#region Camera

	public void ChangeFieldOfView(float newFov)
	{
		initialFOV = newFov;
		pRef.playerCam.fieldOfView = newFov;
		runningFOV = newFov * runningFovMultiplier;
	}

	#endregion
	#region Grounding & Landing

	private void init_LeavingGrounded()
	{
		initFall = transform.position;
		lastLeftGroundTime = Time.time;
	}

	private readonly HashSet<global::AbstractEvent> _eventsFiredThisLanding =
		new HashSet<global::AbstractEvent>();
	private void init_EnteringGrounded()
	{
		//slight delay as enteringgrounded happens immediatly when groundCheck enters ground radius
		pRef.playerGraphics.PlayParticle(PfxToPlay.Halo, true, .05f);

		_eventsFiredThisLanding.Clear();
		TriggerLandingEvents();

		float dist = Vector3.Distance(
			new Vector3(transform.position.x, initFall.y, transform.position.z),
			transform.position
		);

		if (Time.time - lastLeftGroundTime >= landThreshold)
			footsteps.PlayOneOff();

		pRef.playerAnimator.A_Land();
	}
	private void ResetRebound() => rebound = false;
	private void StopCoyote() => coyoteActive = false;

	private void TriggerLandingEvents()
	{
		Collider[] hits = Physics.OverlapSphere(
			groundCheck.position,
			checkRadius,
			pRef.groundLayerMask,
			QueryTriggerInteraction.Collide
		);

		for (int i = 0; i < hits.Length; i++)
		{
			var col = hits[i];
			if (!col) continue;

			var evt = col.GetComponentInParent<global::AbstractEvent>();
			if (evt == null) continue;

			if (_eventsFiredThisLanding.Contains(evt))
				continue;

			if (evt.eventActivation != EventActivation.Collision
				|| evt.eventActivation == EventActivation.Trigger
				|| evt.eventActivation == EventActivation.Remote)
				continue;

			_eventsFiredThisLanding.Add(evt);
			evt.CallEvent();
		}
	}

	#endregion
	#region Crouch

	private void SetCrouchHeight(float newHeight)
	{
		playerDamageCollider.height = newHeight;

		pRef.playerAnimator.A_SetCrouch(true);

		pRef.playerCharacterController.stepOffset = 0.1f;
		pRef.playerCharacterController.height = newHeight;

		EventManager.instance.TeleportPlayer(
			this,
			new Vector3(
				transform.position.x,
				transform.position.y - ((initialCrouchHeight - newHeight) / 2),
				transform.position.z),
			false
		);
	}

	private void ResetSetCrouchHeight(float newHeight)
	{
		playerDamageCollider.height = newHeight;

		pRef.playerAnimator.A_SetCrouch(false);

		pRef.playerCharacterController.stepOffset = 0.65f;
		pRef.playerCharacterController.height = newHeight;

		EventManager.instance.TeleportPlayer(
			this,
			new Vector3(
				transform.position.x,
				transform.position.y + ((initialCrouchHeight - crouchHeight) / 2),
				transform.position.z),
			false
		);
	}

	// Owner -> Server
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
	private void SetCrouchHeightRpc(float newHeight)
	{
		// server forwards to all non-owner clients
		SetCrouchHeightClientRpc(newHeight);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
	private void ResetCrouchHeightRpc(float newHeight)
	{
		ResetCrouchHeightClientRpc(newHeight);
	}

	// Server -> all non-owner clients
	[Rpc(SendTo.NotOwner)]
	private void SetCrouchHeightClientRpc(float newHeight)
	{
		SetCrouchHeight(newHeight);
	}

	[Rpc(SendTo.NotOwner)]
	private void ResetCrouchHeightClientRpc(float newHeight)
	{
		ResetSetCrouchHeight(newHeight);
	}


	#endregion
	#region Audio & Footsteps

	private void PlayMovementSound(string clipName, float min, float max)
	{
		AudioManagement.instance.StopThisSound("Movement", clipName);
		AudioManagement.instance.PlayThisSound("Movement", clipName, true, min, max);
	}

	#endregion
	#region Knockback

	public void AddKnockback(Vector3 force)
	{
		if (!IsOwner) return;
		knockbackVelocity += force;
	}

	#endregion
	#region Dev Tools

	private void SaveState() => savePos = transform.position;

	private void LoadState() =>
		EventManager.instance.TeleportPlayer(this, savePos);

	#endregion

	private void OnDrawGizmos()
	{
		if (groundCheck != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
		}

		if (headCheck != null)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(headCheck.position, headCheckRadius);
		}
	}
}
