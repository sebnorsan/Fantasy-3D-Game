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

	private GameObject deathParticles;
	private GameObject hitParticles;
	private Coroutine currFlashCoroutine;

	private Material flashMaterial;
	private float flashDuration = .1f;


	private void Start()
	{
		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");
	}

	public void TakeDamage(int amount, Vector3 hitPoint)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		currentHealth = Mathf.Max(0, currentHealth - amount);

		DamageEffectsClientRpc(hitPoint);

		if (currentHealth == 0)
			DieServer();
	}

	public void Heal(int amount)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (currentHealth == maxHealth) return;
		currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
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

		StopAllCoroutines();

		localPlayer.canMove = false;

		// server despawns, clients will despawn automatically
		//if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
		//	nwo.Despawn(true);
		//else
		//	Destroy(gameObject);
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
	#endregion
	
}
