using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
	public static EventManager instance;
	public Animator interactionAnimator;
	public void Awake()
	{
		if (instance != null)
			Destroy(this);
		else
			instance = this;
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
	public void TeleportPlayer(PlayerController player, Vector3 tpPos)
	{
		player.characterController.enabled = false;
		player.transform.position = tpPos;
		player.characterController.enabled = true;

		player.moveDirection = Vector3.zero;
	}

	#region UI-Events

	[SerializeField] private GameObject screenSummon;

	public void SummonScreen(Color screenColor, float lerpTime, bool transToFilled)
	{
		StartCoroutine(SummonScreenNumerator(screenColor, lerpTime, transToFilled));
	}

	private IEnumerator SummonScreenNumerator(Color screenColor, float lerpTime, bool transToFilled)
	{
		GameObject temp = Instantiate(screenSummon);
		Image img = temp.GetComponentInChildren<Image>();

		float startAlpha = transToFilled ? 0f : 1f;
		float endAlpha = transToFilled ? 1f : 0f;

		float timeElapsed = 0f;

		while (timeElapsed < lerpTime)
		{
			timeElapsed += Time.deltaTime;
			float t = timeElapsed / lerpTime;

			float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
			img.color = new Color(screenColor.r, screenColor.g, screenColor.b, currentAlpha);

			yield return null;
		}

		img.color = new Color(screenColor.r, screenColor.g, screenColor.b, endAlpha);

		if (!transToFilled)
			Destroy(temp);
	}


	#endregion
}