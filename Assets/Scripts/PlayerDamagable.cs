using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using EZCameraShake;

public class PlayerDamagable : NetworkBehaviour, IDamagable
{

	[SerializeField] private PlayerAnimator animator;
	[SerializeField] private PlayerController localPlayer;

	[Space(15)]

	public int maxHealth = 100;
	[SerializeField] private int currentHealth = 100;

	[Space(15)]

	[SerializeField] private float deathTime;
	[SerializeField] private GameObject deathCam;

	[Space(15)]

	[SerializeField] private GameObject deathParticles;
	[SerializeField] private GameObject hitParticles;

	[Space(15)]

	[Header("Health UI")]
	[SerializeField] private Slider healthSlider;
	[SerializeField] private float sliderLerpSpeed = 10f;

	private float displayedHealth;

	private Coroutine currFlashCoroutine;

	private Material flashMaterial;
	private float flashDuration = .1f;

	private ulong lastHitByClientId = ulong.MaxValue;

	private Vector3 deathPosition = new Vector3(9999, 9999, 9999);

	private void Start()
	{
		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		displayedHealth = currentHealth;
		if (healthSlider != null)
		{
			healthSlider.maxValue = maxHealth;
			healthSlider.value = currentHealth;
		}
	}
	private void Update()
	{
		if (healthSlider == null) return;

		displayedHealth = Mathf.Lerp(
			displayedHealth,
			currentHealth,
			Time.deltaTime * sliderLerpSpeed
		);

		healthSlider.value = displayedHealth;
	}

	public void TakeDamage(int amount, Vector3 hitPoint)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		Debug.Log("i hit a player");

		currentHealth = Mathf.Max(0, currentHealth - amount);

		UpdateHealthClientRpc(currentHealth);

		DamageEffectsClientRpc(hitPoint);

		if (currentHealth <= 0)
			DieServer();
	}

	public void SetLastHitBy(ulong shooterClientId)
	{
		lastHitByClientId = shooterClientId;
	}

	public void Heal(int amount)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (currentHealth == maxHealth) return;
		currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

		UpdateHealthClientRpc(currentHealth);

		if (currentHealth > 0)
			SetAliveStateClientRpc();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetAliveStateClientRpc()
	{
		var pc = GetComponent<PlayerController>();
		if (pc != null)
			pc.canMove = true;
	}

	#region DamageEffects

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamageEffectsClientRpc(Vector3 hitPoint)
	{
		DamageEffects(hitPoint);

		if (IsOwner)
			CameraShaker.Instance.ShakeOnce(7f, 3f, .1f, 1f);
	}
	public void DamageEffects(Vector3 hitPoint)
	{
		//if (currKnockbackCoroutine != null)
		//	StopCoroutine(currKnockbackCoroutine);
		//currKnockbackCoroutine = StartCoroutine(OnKnockback(hitPoint));

		if (currFlashCoroutine == null)
			currFlashCoroutine = StartCoroutine(OnFlashMaterial());

		OnSpawnDamagePFX();
		OnPlayDamageAnimation();
	}
	private void OnSpawnDamagePFX()
	{
		if (hitParticles == null) return;

		var pfx = Instantiate(hitParticles, transform.position, Quaternion.identity);
		Destroy(pfx, 5);
	}
	private void OnPlayDamageAnimation()
	{
		if (animator != null)
			animator.A_TakeDamage();
	}
	private IEnumerator OnFlashMaterial()
	{
		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		var allRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

		var affected = new List<(MeshRenderer renderer, Material[] originals)>(allRenderers.Length);
		foreach (var rend in allRenderers)
		{
			affected.Add((rend, rend.materials));

			var flashMats = new Material[rend.materials.Length];
			for (int i = 0; i < flashMats.Length; i++)
				flashMats[i] = flashMaterial;

			rend.materials = flashMats;
		}

		yield return new WaitForSeconds(flashDuration);

		foreach (var (renderer, originals) in affected)
			renderer.materials = originals;

		currFlashCoroutine = null;
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void UpdateHealthClientRpc(int newHealth)
	{
		currentHealth = newHealth;
	}

	#endregion
	#region Dying
	private void DieServer()
	{
		//pfx
		DieParticlesClientRpc();

		// server-side state
		SetDeadStateClientRpc();

		// tell clients; only the dead player's client will actually spawn the cam
		SpawnDeathCamClientRpc(lastHitByClientId);

		StartCoroutine(DeathFlowServer()); // teleports + heal etc on server
		localPlayer.canMove = false;
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SpawnDeathCamClientRpc(ulong killerClientId)
	{
		if (!IsOwner) return;           // only this player's client

		StartCoroutine(DeathCamFlow(killerClientId));
	}
	
	private IEnumerator DeathCamFlow(ulong killerClientId)
	{
		var dCam = Instantiate(
			deathCam,
			localPlayer.cam.transform.position,
			localPlayer.cam.transform.rotation);

		if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerClientId, out var killerCc))
		{
			var killerPc = killerCc.PlayerObject.GetComponent<PlayerController>();
			dCam.GetComponent<DeathCam>().playerWhoKilled = killerPc;
		}

		EventManager.instance.TeleportPlayer(localPlayer, deathPosition);

		yield return new WaitForSeconds(deathTime);
		Destroy(dCam);

		EventManager.instance.TeleportPlayer(
			localPlayer,
			FindFirstObjectByType<GameSceneSpawnManager>().FindValidSpawnPoint());
	}

	private IEnumerator DeathFlowServer()
	{
		yield return new WaitForSeconds(deathTime);
		Heal(maxHealth); // still server-side
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetDeadStateClientRpc()
	{
		var pc = GetComponent<PlayerController>();
		if (pc != null)
			pc.canMove = false;

		if (IsOwner)
		{
			// e.g. show death screen, disable bow input, etc.
		}
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieParticlesClientRpc()
	{
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			Destroy(pfx, 5);
		}
	
	}
	//private IEnumerator DeathFlow()
	//{
	//	EventManager.instance.TeleportPlayer(localPlayer, deathPosition);

	//	var dCam = Instantiate(deathCam, localPlayer.cam.transform.position, localPlayer.cam.transform.rotation);

	//	if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastHitByClientId, out var killerCc))
	//	{
	//		var killerPc = killerCc.PlayerObject.GetComponent<PlayerController>();
	//		dCam.GetComponent<DeathCam>().playerWhoKilled = killerPc;
	//	}

	//	yield return new WaitForSeconds(deathTime);

	//	Destroy(dCam);

	//	Heal(maxHealth); // direct, no RPC
	//	EventManager.instance.TeleportPlayer(localPlayer, FindFirstObjectByType<GameSceneSpawnManager>().FindValidSpawnPoint());
	//}

	#endregion

}
