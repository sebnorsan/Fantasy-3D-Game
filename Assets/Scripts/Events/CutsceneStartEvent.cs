using System.Collections;
using UnityEngine;
// using Unity.Cinemachine; // not used anymore, can be removed if you like

public class CutsceneStartEvent : AbstractEvent
{
	public bool canInteract { get; set; } = true;

	[SerializeField] private GameObject initializationCamera;
	[SerializeField] private Camera cutsceneCam;

	[SerializeField] private GameObject[] objectsBeforeCutscene;
	[SerializeField] private GameObject[] objectsAfterCutscene;

	[Space(10)]
	[SerializeField] private float camInitializationTime = 1f;

	// NEW: curves for camera in/out
	[Header("Camera Movement Curves")]
	[Tooltip("Curve for camera movement INTO the cutscene. X=0..1 (time), Y=0..1 (lerp factor).")]
	[SerializeField] private AnimationCurve cameraInitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

	[Tooltip("Curve for camera movement OUT OF the cutscene.")]
	[SerializeField] private AnimationCurve cameraUninitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

	private PlayerController player;

	private Vector3 storedMainPos;
	private Quaternion storedMainRot;
	private Vector3 storedCutscenePos;
	private Quaternion storedCutsceneRot;

	private Camera playerCam;

	[SerializeField] private bool dynamicCamera = false;
	[SerializeField] private bool playerCanMoveWhile = false;

	public override void CallEvent()
	{
		//player = FindFirstObjectByType<PlayerController>();
		//playerCam = player.cam;
		
		StartCoroutine(InitializeCameraPosition());
	}

	private IEnumerator InitializeCameraPosition()
	{
		if (!playerCanMoveWhile) player.canMove = false;

		foreach (var obj in objectsBeforeCutscene)
			obj.SetActive(true);

		GameObject cam = Instantiate(initializationCamera);
		cutsceneCam.enabled = false;

		storedCutscenePos = cutsceneCam.transform.position;
		storedCutsceneRot = cutsceneCam.transform.rotation;

		storedMainPos = playerCam.transform.position;
		storedMainRot = playerCam.transform.rotation;

		cam.transform.SetPositionAndRotation(storedMainPos, storedMainRot);

		float elapsed = 0f;
		float duration = camInitializationTime <= 0f ? 0.0001f : camInitializationTime;

		if (eventActivation == EventActivation.Awake)
			duration = 0f;

		// If duration is 0, snap instantly
		if (duration <= 0f)
		{
			if (!dynamicCamera)
			{
				cam.transform.SetPositionAndRotation(storedCutscenePos, storedCutsceneRot);
			}
			else
			{
				cam.transform.SetPositionAndRotation(cutsceneCam.transform.position, cutsceneCam.transform.rotation);
			}
		}
		else
		{
			while (elapsed < duration)
			{
				float linearT = elapsed / duration;                          // 0..1 linearly
				float curvedT = cameraInitCurve.Evaluate(Mathf.Clamp01(linearT)); // curve-controlled 0..1

				if (!dynamicCamera)
				{
					cam.transform.position = Vector3.LerpUnclamped(storedMainPos, storedCutscenePos, curvedT);
					cam.transform.rotation = Quaternion.SlerpUnclamped(storedMainRot, storedCutsceneRot, curvedT);
				}
				else
				{
					// dynamic target follows cutsceneCam transform
					cam.transform.position = Vector3.LerpUnclamped(storedMainPos, cutsceneCam.transform.position, curvedT);
					cam.transform.rotation = Quaternion.SlerpUnclamped(storedMainRot, cutsceneCam.transform.rotation, curvedT);
				}

				elapsed += Time.deltaTime;
				yield return null;
			}
		}

		cam.transform.SetPositionAndRotation(storedCutscenePos, storedCutsceneRot);

		cutsceneCam.enabled = true;

		if (TryGetComponent(out Animation anim))
		{
			if (anim.clip != null)
				anim.Play();
		}

		Destroy(cam);
	}

	public void UnInitializeCameraPosition()
	{
		StartCoroutine(UninitializeRoutine());
	}

	private IEnumerator UninitializeRoutine()
	{
		GameObject cam = Instantiate(initializationCamera);
		cam.transform.SetPositionAndRotation(cutsceneCam.transform.position, cutsceneCam.transform.rotation);

		Vector3 targetPos = storedMainPos;
		Quaternion targetRot = storedMainRot;

		Vector3 fromPos = cam.transform.position;
		Quaternion fromRot = cam.transform.rotation;

		float elapsed = 0f;
		float duration = camInitializationTime <= 0f ? 0.0001f : camInitializationTime;

		if (duration <= 0f)
		{
			if (!playerCanMoveWhile)
			{
				cam.transform.SetPositionAndRotation(targetPos, targetRot);
			}
			else
			{
				cam.transform.SetPositionAndRotation(playerCam.transform.position, playerCam.transform.rotation);
			}
		}
		else
		{
			while (elapsed < duration)
			{
				float linearT = elapsed / duration;                             // 0..1 linearly
				float curvedT = cameraUninitCurve.Evaluate(Mathf.Clamp01(linearT)); // curve-controlled

				if (!playerCanMoveWhile)
				{
					cam.transform.position = Vector3.LerpUnclamped(fromPos, targetPos, curvedT);
					cam.transform.rotation = Quaternion.SlerpUnclamped(fromRot, targetRot, curvedT);
				}
				else
				{
					// Player might be moving; lerp back toward the current player cam
					cam.transform.position = Vector3.LerpUnclamped(fromPos, playerCam.transform.position, curvedT);
					cam.transform.rotation = Quaternion.SlerpUnclamped(fromRot, playerCam.transform.rotation, curvedT);
				}

				elapsed += Time.deltaTime;
				yield return null;
			}
		}

		foreach (var obj in objectsAfterCutscene)
			obj.SetActive(true);

		cam.transform.SetPositionAndRotation(targetPos, targetRot);

		if (TryGetComponent(out Animation anim))
		{
			if (!playerCanMoveWhile) player.canMove = true;

			if (anim.clip != null)
				Destroy(gameObject);
			else
				cutsceneCam.enabled = false;
		}
		else
		{
			cutsceneCam.enabled = false;
			if (!playerCanMoveWhile) player.canMove = true;
		}

		Destroy(cam);
	}
}
