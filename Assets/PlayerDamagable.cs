using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

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

	private Coroutine currFlashCoroutine;

	private Material flashMaterial;
	private float flashDuration = .1f;

	private ulong lastHitByClientId = ulong.MaxValue;

	private void Start()
	{
		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");
	}

	public void TakeDamage(int amount, Vector3 hitPoint)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		Debug.Log("i hit a player");

		currentHealth = Mathf.Max(0, currentHealth - amount);

		DamageEffectsClientRpc(hitPoint);

		if (currentHealth == 0)
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

		if (!localPlayer.canMove && currentHealth > 0)
			localPlayer.canMove = true;
	}

	#region DamageEffects

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamageEffectsClientRpc(Vector3 hitPoint)
	{
		DamageEffects(hitPoint);
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
	#endregion
	#region Dying
	private void DieServer()
	{
		DieClientRpc();
		SetDeadStateClientRpc();
		//StopAllCoroutines();

		StartCoroutine(DeathFlow());

		localPlayer.canMove = false;

		// server despawns, clients will despawn automatically
		//if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
		//	nwo.Despawn(true);
		//else
		//	Destroy(gameObject);
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
	private void DieClientRpc()
	{
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			Destroy(pfx, 5);
		}
	
	}
	Vector3 deathPosition;
	private IEnumerator DeathFlow()
	{
		EventManager.instance.TeleportPlayer(localPlayer, deathPosition);

		var dCam = Instantiate(deathCam, localPlayer.cam.transform.position, localPlayer.cam.transform.rotation);

		if (NetworkManager.Singleton.ConnectedClients.TryGetValue(lastHitByClientId, out var killerCc))
		{
			var killerPc = killerCc.PlayerObject.GetComponent<PlayerController>();
			dCam.GetComponent<DeathCam>().playerWhoKilled = killerPc;
		}

		yield return new WaitForSeconds(deathTime);

		Destroy(dCam);

		Heal(maxHealth); // direct, no RPC
		EventManager.instance.TeleportPlayer(localPlayer, FindFirstObjectByType<GameSceneSpawnManager>().FindValidSpawnPoint());
	}

	#endregion

}
