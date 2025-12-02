using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CrystalScript : NetworkBehaviour
{
	[SerializeField] private GameObject crystalShield;
	[SerializeField] private float radius;
	public int maxHealth = 1000;
	[SerializeField] private MeshRenderer[] renderersToFlash;
	public int currentHealth;
	private List<(MeshRenderer renderer, Material[] originals)> affected =
		new List<(MeshRenderer renderer, Material[] originals)>();

	public bool thorns, deadly;

	[Header("UI")]
	[Tooltip("Slider showing health")]
	public Slider healthSlider;
	[Tooltip("Speed at which the slider value lerps")]
	[SerializeField] private float sliderLerpSpeed = 5f;

	private Material flashMaterial;
	private float flashDuration = .1f;

	public ParticleSystem explosionpfx;

	public override void OnNetworkSpawn()
	{
		crystalShield.SetActive(true);

		affected = new List<(MeshRenderer renderer, Material[] originals)>(renderersToFlash.Length);
		foreach (var rend in renderersToFlash)
			affected.Add((rend, rend.materials));

		// initialize health on all peers so UI has the right starting value
		currentHealth = maxHealth;

		if (healthSlider != null)
		{
			healthSlider.maxValue = maxHealth;
			healthSlider.value = maxHealth;
		}
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

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position, radius);
	}

	// -------- DAMAGE & DEATH (SERVER AUTHORITATIVE) --------
	public void SetThorns()
	{
		SetThornsServerRpc();
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetThornsServerRpc()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		thorns = true;
	}
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
		Debug.Log("Crystal has been destroyed!!");

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
		StartCoroutine(DamageEffects());
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

		var waveCtrl = FindFirstObjectByType<WaveController>();
		if (waveCtrl != null)
			waveCtrl.GetComponent<Animator>().SetTrigger("Lose");
	}

	private IEnumerator DamageEffects()
	{
		var ps = GetComponentInChildren<ParticleSystem>();
		var audio = GetComponentInChildren<AudioSource>();

		if (ps != null)
			ps.Play();
		if (audio != null)
		{
			audio.Play();
			//EventManager.instance.RandomizePitchOnSound(audio.clip, .6f, .9f);
		}

		var anim = GetComponent<Animator>();
		if (anim != null)
			anim.SetTrigger("TakeDamage");

		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		foreach (var rend in renderersToFlash)
		{
			var flashMats = new Material[rend.materials.Length];
			for (int i = 0; i < flashMats.Length; i++)
				flashMats[i] = flashMaterial;
			rend.materials = flashMats;
		}

		yield return new WaitForSeconds(flashDuration);

		foreach (var (renderer, originals) in affected)
			renderer.materials = originals;
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
}
