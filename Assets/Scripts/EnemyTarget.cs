using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Unity.AppUI.Navigation;

public class EnemyTarget : NetworkBehaviour
{
	[SerializeField] private SphereCollider enemyEnterTrigger;

	[SerializeField] private GameObject crystalShield;
	public int maxHealth = 1000;
	public int currentHealth;
	
	public bool thorns, deadly;

	[Header("UI")]
	[Tooltip("Slider showing health")]
	public Slider healthSlider;
	[Tooltip("Speed at which the slider value lerps")]
	[SerializeField] private float sliderLerpSpeed = 5f;

	public ParticleSystem explosionpfx;

	[SerializeField] private AudioToPlay damageSound;
	[SerializeField] private ParticleSystem damageParticles;

	[Space(15)]

	[SerializeField] private MeshRenderer[] allRenderers;

	private Material flashMaterial;
	private float flashDuration = .1f;

	private Coroutine currFlashCoroutine;

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
	public void TakeDamage(int dmg)
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
	private void DamageEffectsClientRpc(int newHealth)
	{
		// sync health for UI lerp on all clients
		currentHealth = newHealth;

		// local hit FX + flash
		DamageEffects();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieClientRpc()
	{
		if (explosionpfx != null)
		{
			explosionpfx.Play();
			var audio = explosionpfx.gameObject.GetComponent<AudioSource>();
			if (audio != null)
				audio.Play();
		}
	}

	private void DamageEffects()
	{
		damageParticles.Play();
		AudioManagement.instance.PlayThisSound(damageSound, true, .6f, .9f);

		if (currFlashCoroutine == null)
			currFlashCoroutine = StartCoroutine(OnFlashMaterial());

		var anim = GetComponent<Animator>();
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

		if (other.TryGetComponent(out AbstractEnemy enemy))
			enemy.StartAttack(); // AI / animation; damage still server-only
	}

	private void OnTriggerExit(Collider other)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (other.TryGetComponent(out AbstractEnemy enemy))
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

	public float GetColliderRadius() => enemyEnterTrigger.radius;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, enemyEnterTrigger.radius);
	}
}
