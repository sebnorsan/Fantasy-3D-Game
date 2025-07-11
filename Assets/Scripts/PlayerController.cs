using System.Collections;
using System.Net.Http.Headers;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	// Player Settings
	[Header("Player Settings")]
	[SerializeField] public Transform playerCamera;
	[SerializeField, Range(1, 10)] float walkingSpeed = 3.0f;
	[SerializeField, Range(0.1f, 5)] float crouchSpeed = 1.0f;
	[SerializeField, Range(2, 20)] float runningSpeed = 4.0f;
	[SerializeField, Range(0, 20)] float jumpSpeed = 6.0f;
	[SerializeField, Range(0.5f, 10)] public float lookSpeed = 2.0f;
	[SerializeField, Range(10, 120)] float lookXLimit = 80.0f;

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

	[Header("Input")]
	[SerializeField] KeyCode crouchKey = KeyCode.LeftControl;
	public bool runToggle = false;
	public bool cameraLock = false;

	[HideInInspector] public CharacterController characterController;
	[HideInInspector] public Vector3 moveDirection = Vector3.zero;
	[HideInInspector] public bool isRunning = false;
	[HideInInspector] public bool Moving = false;

	// Private variables
	float initialWalkingSpeed;
	float runningValue;
	float rotationX = 0f;
	float initialCrouchHeight;
	float initialFOV;
	Camera cam;

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

	public float inputVertical;
	public float inputHorizontal;

	public bool dev = false;

	//private bool tooSteep = false;
	//private bool sliding = false;
	void Start()
	{
		characterController = GetComponent<CharacterController>();
		cam = GetComponentInChildren<Camera>();
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		initialCrouchHeight = transform.localScale.y;
		initialFOV = cam.fieldOfView;

		runningValue = runningSpeed;
		initialWalkingSpeed = walkingSpeed;
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
		float dist = Vector3.Distance(initFall, transform.position);

		if (dist > 90)
			EventManager.instance.BonusRoomTeleport();

		if (Time.time - lastLeftGroundTime >= landThreshold)
			GetComponent<Footsteps>().PlayOneOff();
	}

	private void PlayMovementSound(string clipName, float min, float max)
	{
		EventManager.instance.StopThisSound("Movement", clipName);
		EventManager.instance.PlayThisSound("Movement", clipName);
		EventManager.instance.RandomizePitchOnSound("Movement", clipName, min, max);
	}

	[SerializeField] private float footstepStopThreshold = 0.05f;

	// time at which we first noticed movement stopped
	private float footstepStopRequestTime;

	// whether a stop is currently pending
	private bool footstepStopPending = false;

	void Update()
	{
		if (dev)
		{
			if (Input.GetKeyDown(KeyCode.C)) SaveState();
			if (Input.GetKeyDown(KeyCode.V)) LoadState();
			if (Input.GetKeyDown(KeyCode.F))
			{
				moveDirection = Vector3.zero;
				isFlying = !isFlying;
			}
		}

		var footsteps = GetComponent<Footsteps>();

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
		if (Cursor.lockState == CursorLockMode.Locked && canMove && !cameraLock)
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
			if (!dev)
				isFlying = false;

			float flySpeed = 40f;
			float vertical = 0f;

			if (Input.GetKey(KeyCode.LeftShift))
				flySpeed = 7f;

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
		isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, LayerMask.GetMask("Ground", "MovingPlatform"));

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

			if (Vector3.Distance(new Vector3(transform.position.x, prevFramePos.y, transform.position.z),
								 transform.position) < 0.001f && !rebound)
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

			if(!isCrouching)
				characterController.stepOffset = 0.65f;
			canCoyote = true;
			CancelInvoke(nameof(StopCoyote));
			coyoteActive = false;
		}

		// Handle movement input
		Vector3 forward = transform.TransformDirection(Vector3.forward);
		Vector3 right = transform.TransformDirection(Vector3.right);

		isRunning = !isCrouching && canRun ? Input.GetKey(KeyCode.LeftShift) : false;
		if (runToggle && !isCrouching)
			isRunning = !isRunning;

		inputVertical = canMove ? Input.GetAxis("Vertical") : 0;
		inputHorizontal = canMove ? Input.GetAxis("Horizontal") : 0;
		float currentSpeed = isRunning ? runningValue : walkingSpeed;

		if (isRunning)
			runningValue = Mathf.Lerp(runningValue, runningSpeed, timeToRunning * Time.deltaTime);
		else
			runningValue = walkingSpeed;

		float verticalSpeed = moveDirection.y;
		moveDirection = forward * (currentSpeed * inputVertical) + right * (currentSpeed * inputHorizontal);
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

		bool crouchSphere = Physics.CheckSphere(transform.position + new Vector3(0, .66f, 0), .3f, LayerMask.GetMask("Ground", "MovingPlatform"));

		// Crouching
		if (Input.GetKeyDown(crouchKey))
			SetCrouchHeight(crouchHeight);
		else if (Input.GetKeyUp(crouchKey) && !crouchSphere)
			ResetSetCrouchHeight(initialCrouchHeight);

		if (Input.GetKey(crouchKey))
		{
			isCrouching = true;
			walkingSpeed = Mathf.Lerp(walkingSpeed, crouchSpeed, 6 * Time.deltaTime);
		}
		else if (!crouchSphere)
		{
			isCrouching = false;
			walkingSpeed = Mathf.Lerp(walkingSpeed, initialWalkingSpeed, 4 * Time.deltaTime);

			if (transform.localScale.y == crouchHeight)
				ResetSetCrouchHeight(initialCrouchHeight);
		}
	}

	void ResetRebound() => rebound = false;
	void StopCoyote() => coyoteActive = false;


	void SetCrouchHeight(float newHeight)
	{
		characterController.stepOffset = 0.1f;

		transform.localScale = new Vector3(transform.localScale.x, newHeight, transform.localScale.z);
		characterController.enabled = false;
		transform.position = new Vector3(transform.position.x, transform.position.y - ((initialCrouchHeight - newHeight) / 2), transform.position.z);
		characterController.enabled = true;
	}

	void ResetSetCrouchHeight(float newHeight)
	{
		characterController.stepOffset = 0.65f;

		transform.localScale = new Vector3(transform.localScale.x, newHeight, transform.localScale.z);
		characterController.enabled = false;
		transform.position = new Vector3(transform.position.x, transform.position.y + ((initialCrouchHeight - crouchHeight) / 2), transform.position.z);
		characterController.enabled = true;
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(groundCheck.position, checkRadius);

		Gizmos.DrawWireSphere(transform.position + new Vector3(0, .66f, 0), .3f);
	}


	private Vector3 savePos = Vector3.zero;
	private void SaveState() => savePos = transform.position;
	private void LoadState() => EventManager.instance.TeleportPlayer(savePos);
}
