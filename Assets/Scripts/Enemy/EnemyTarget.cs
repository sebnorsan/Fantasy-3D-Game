using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyTarget : NetworkBehaviour
{
	[Header("References")]
	[SerializeField] private SphereCollider enemyEnterTrigger;
	[SerializeField] private Animator anim;

	[Header("Stats")]
	[SerializeField] private float maxHealth = 1000;
	private float currentHealth;
	[SerializeField] private float targetRadius = 10f;

	[Header("UI")]
	[SerializeField] private Slider healthSlider;
	[SerializeField] private float sliderLerpSpeed = 5f;

	[Header("Effects")]
	[SerializeField] private ParticleSystem deathParticles;
	[SerializeField] private ParticleSystem damageParticles;
	[SerializeField] private AudioToPlay damageSound;

	private Material flashMaterial;
	private float flashDuration = .1f;

	[Space(15)]

	[SerializeField] private MeshRenderer[] allRenderers;

	private Coroutine currFlashCoroutine;

	private void OnValidate()
	{
		if (!Application.isEditor || Application.isPlaying) return;

		enemyEnterTrigger.isTrigger = true;
	}

	public override void OnNetworkSpawn()
	{
		// initialize health on all peers so UI has the right starting value
		currentHealth = maxHealth;

		if (healthSlider != null)
		{
			healthSlider.maxValue = maxHealth;
			healthSlider.value = maxHealth;
		}
	}

	private void Start()
	{
		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		if (allRenderers.Length <= 0)
			allRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);
	}

	private void Update()
	{
		if (healthSlider != null)
		{
			float target = currentHealth;
			healthSlider.value = Mathf.Lerp(
				healthSlider.value,
				target,
				Time.deltaTime * sliderLerpSpeed
			);
		}
	}
	

	// -------- DAMAGE & DEATH (SERVER AUTHORITATIVE) --------
	public void TakeDamage(float dmg)
	{
		if (!NetworkManager.Singleton.IsServer) return; // only server changes health

		currentHealth -= dmg;
		if (currentHealth < 0)
			currentHealth = 0;

		// tell everyone new health + play hit visuals
		DamageEffectsClientRpc(currentHealth);

		if (currentHealth <= 0)
			DieServer();
	}

	private void DieServer()
	{
		// run lose FX/anim on all clients + host
		DieClientRpc();

		// server destroys / despawns the crystal
		if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
			nwo.Despawn(true);
		else
			Destroy(gameObject);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamageEffectsClientRpc(float newHealth)
	{
		// sync health for UI lerp on all clients
		currentHealth = newHealth;

		// local hit FX + flash
		DamageEffects();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieClientRpc()
	{
		if (deathParticles != null)
		{
			var deathPfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			Destroy(deathPfx, deathPfx.totalTime);
		}
	}

	private void DamageEffects()
	{
		if (damageParticles != null)
		{
			var damagePfx = Instantiate(damageParticles, transform.position, Quaternion.identity);
			Destroy(damagePfx, damagePfx.totalTime);
		}

		AudioManagement.instance.PlayThisSound(damageSound, true, .6f, .9f);

		if (currFlashCoroutine == null)
			currFlashCoroutine = StartCoroutine(OnFlashMaterial());

		if (anim != null)
			anim.SetTrigger("TakeDamage");
	}

	private IEnumerator OnFlashMaterial()
	{
		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

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

	// -------- TRIGGERS (SERVER SIDE) --------

	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (other.TryGetComponent(out AbstractEnemyAttack enemy))
			enemy.StartAttack(); // AI / animation; damage still server-only
	}

	private void OnTriggerExit(Collider other)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (other.TryGetComponent(out AbstractEnemyAttack enemy))
			enemy.StopAttack();
	}

	// -------- REGENERATION (OPTIONAL, SERVER ONLY) --------

	public void Regeneration()
	{
		Invoke(nameof(RegenerationTickServerRpc), .5f);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void RegenerationTickServerRpc()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		currentHealth++;
		if (currentHealth > maxHealth)
			currentHealth = maxHealth;

		// update UI on everyone (no hit FX, just health)
		DamageEffectsClientRpc(currentHealth);

		// continue regenerating if desired
		Invoke(nameof(RegenerationTickServerRpc), .5f);
	}

	public float GetColliderRadius() => targetRadius;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, targetRadius);
	}
}
