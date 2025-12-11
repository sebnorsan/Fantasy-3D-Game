using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
	public static EventManager instance;

	public Animator interactionAnimator;
	public NetHelper netHelper;

	public void Awake()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;
	}

	private void Start()
	{
		netHelper = GetComponent<NetHelper>();
	}

	#region ===== Activate-Deactivate-GameObject/Colliders =====
	public void ActivateObjects(GameObject[] objs)
	{
		foreach (var o in objs)
			if (o) o.SetActive(true);
	}
	public void DeactivateObjects(GameObject[] objs)
	{
		foreach (var o in objs)
			if (o) o.SetActive(false);
	}
	public void ActivateColliders(Collider[] cols)
	{
		foreach (var c in cols)
			if (c) c.enabled = true;
	}
	public void DeactivateColliders(Collider[] cols)
	{
		foreach (var c in cols)
			if (c) c.enabled = false;
	}
	#endregion
	#region ===== Fog / Skybox (utilities) =====
	public void CallDensityChange(float changedDensity)
	{
		CoroutineRunner.instance.StartCoroutine(DensityChangeLerp(changedDensity));
	}
	public void CallDensityChange(float changedDensity, Color newFogColor)
	{
		RenderSettings.fogColor = newFogColor;
		CoroutineRunner.instance.StartCoroutine(DensityChangeLerp(changedDensity));
	}
	private IEnumerator DensityChangeLerp(float target)
	{
		while (Mathf.Abs(RenderSettings.fogDensity - target) > 0.001f)
		{
			RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, target, 5f * Time.deltaTime);
			yield return null;
		}
		RenderSettings.fogDensity = target;
	}

	public void CallSkyboxChange(Material newSkybox)
	{
		if (newSkybox) RenderSettings.skybox = newSkybox;
	}
	public void CallSkyboxChange(Material newSkybox, Color mainCameraViewportColor)
	{
		if (newSkybox) RenderSettings.skybox = newSkybox;
		if (Camera.main) Camera.main.backgroundColor = mainCameraViewportColor;
	}
	public void CallSkyboxOff()
	{
		if (Camera.main) Camera.main.clearFlags = CameraClearFlags.SolidColor;
	}
	public void CallSkyboxOn()
	{
		if (Camera.main) Camera.main.clearFlags = CameraClearFlags.Skybox;
	}
	#endregion
	public bool IsCameraPlayerMode()
	{
		if (Cursor.lockState == CursorLockMode.Locked)
			return true;
		else return false;
	}
	public void ToggleCameraMode()
	{
		Cursor.visible = !Cursor.visible;

		if (Cursor.lockState == CursorLockMode.Confined)
			Cursor.lockState = CursorLockMode.Locked;
		else
			Cursor.lockState = CursorLockMode.Confined;
	}
	public void TeleportPlayer(PlayerController player, Vector3 tpPos, bool resetDir = true)
	{
		player.pRef.playerCharacterController.enabled = false;
		player.transform.position = tpPos;
		player.pRef.playerCharacterController.enabled = true;

		if (resetDir)
			player.moveDirection = Vector3.zero;
	}
}