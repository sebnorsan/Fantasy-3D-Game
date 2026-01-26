using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemyWaveVisuals : NetworkBehaviour
{
	[Header("Skybox & Lighting")]
	[SerializeField] private Material skyboxMaterial;
	[SerializeField] private Light dirLight;

	[Space(3)]

	[SerializeField] private float dayIntensity = 2.2f;
	[SerializeField] private float nightIntensity = .5f;

	private Material runtimeSkybox;
	private Coroutine transitionRoutine;

	private WaveVisualType currentVisualType;

	private static readonly int CubemapTransitionID = Shader.PropertyToID("_CubemapTransition");

	public bool IsDownTime() => currentVisualType == WaveVisualType.Night;
	public override void OnNetworkSpawn()
	{
		if (skyboxMaterial == null)
			skyboxMaterial = RenderSettings.skybox;

		if (skyboxMaterial == null) return;

		runtimeSkybox = new Material(skyboxMaterial);
		RenderSettings.skybox = runtimeSkybox;
	}

	public void SetVisualType(WaveVisualType visualType, float dur)
	{
		if (!NetworkManager.IsServer) return;

		SetVisualTypeClientRpc(visualType, dur);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetVisualTypeClientRpc(WaveVisualType visualType, float dur)
	{
		if (runtimeSkybox == null) return;

		if (!runtimeSkybox.HasProperty(CubemapTransitionID)) return;

		if (transitionRoutine != null)
			StopCoroutine(transitionRoutine);

		transitionRoutine = StartCoroutine(ChangeWaveVisualMode(visualType, dur));
	}

	private IEnumerator ChangeWaveVisualMode(WaveVisualType visualType, float dur)
	{
		currentVisualType = visualType;

		float startValue = runtimeSkybox.GetFloat(CubemapTransitionID);
		float targetValue = (visualType == WaveVisualType.Night) ? 1f : 0f;

		float startLightValue = dirLight.intensity;
		float targetLightValue = (visualType == WaveVisualType.Night) ? nightIntensity : dayIntensity;

		float t = 0f;
		while (t < dur)
		{
			t += Time.deltaTime;
			float a = Mathf.Clamp01(t / dur);

			dirLight.intensity = Mathf.Lerp(startLightValue, targetLightValue, a);
			runtimeSkybox.SetFloat(CubemapTransitionID, Mathf.Lerp(startValue, targetValue, a));
			yield return null;
		}

		runtimeSkybox.SetFloat(CubemapTransitionID, targetValue);

		// If you rely on baked/reflection updates, you can enable this:
		// DynamicGI.UpdateEnvironment();

		transitionRoutine = null;
	}
}

public enum WaveVisualType
{
	Morning,
	Night
}
