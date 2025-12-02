using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class KillableObject : NetworkBehaviour, IDamagable
{
	[Header("Health Settings")]
	[SerializeField] private int maxHealth = 5;

	private NetworkVariable<int> currentHealth = new(
		0,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server
	);

	public int CurrentHealth => currentHealth.Value;
	public int MaxHealth => maxHealth;


	[Header("UI")]
	[SerializeField] private Slider healthSlider;
	[SerializeField] private float sliderLerpSpeed = 5f;

	[Header("Effects")]
	public GameObject hitParticles;
	public GameObject deathParticles;

	[Header("XP")]
	public int xpGain = 1;

	[Header("Flash Settings")]
	private Material flashMaterial;
	private float flashDuration = .1f;

	public bool parentHitFx = false;
	public bool bigguy = false;

	public override void OnNetworkSpawn()
	{
		if (NetworkManager.Singleton.IsServer)
			currentHealth.Value = maxHealth;

		currentHealth.OnValueChanged += OnHealthChanged;

		// init UI for late joiners too
		SetupSlider(currentHealth.Value);
	}
	private void SetupSlider(int value)
	{
		if (healthSlider == null) return;
		healthSlider.maxValue = maxHealth;
		healthSlider.value = value;
	}

	private void OnHealthChanged(int oldValue, int newValue)
	{
		SetupSlider(newValue);
	}

	private void Update()
	{
		if (healthSlider != null)
		{
			float target = currentHealth.Value;
			healthSlider.value = Mathf.Lerp(
				healthSlider.value,
				target,
				Time.deltaTime * sliderLerpSpeed
			);
		}
	}

	// called by arrows/enemies. MUST execute on server.
	public void TakeDamage(int amount, Vector3 hitPoint)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		int newHp = Mathf.Max(0, currentHealth.Value - amount);
		currentHealth.Value = newHp;

		DamageEffectsClientRpc(hitPoint);

		if (newHp == 0)
			DieServer();
	}

	public void Heal(int amount)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		int newHp = Mathf.Clamp(currentHealth.Value + amount, 0, maxHealth);
		currentHealth.Value = newHp;
	}

	//public void DamageEffects(Vector3 hitPoint)
	//{
	//	if (!NetworkManager.Singleton.IsServer) return;
	//	DamageEffectsClientRpc(hitPoint);
	//}

	[ClientRpc]
	private void DamageEffectsClientRpc(Vector3 hitPoint)
	{
		StartCoroutine(OnFlashMaterial());
		OnSpawnDamagePFX(); // you can use hitPoint here if you want
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
	}

	private void OnSpawnDamagePFX()
	{
		if (hitParticles == null) return;

		var pfx = Instantiate(hitParticles, transform.position, Quaternion.identity);
		if (parentHitFx)
		{
			var oldScale = pfx.transform.lossyScale;
			pfx.transform.SetParent(transform, true);
			pfx.transform.localScale = oldScale;
		}
		Destroy(pfx, 5);
	}

	private void DieServer()
	{
		if (bigguy) return;

		//if (TryGetComponent(out QuestObject quester))
		//	if (NetworkManager.Singleton.IsServer)
		//	{
		//		quester.FinishQuest();
		//		quester.AddXpClientRpc(xpGain);
		//	}

		DieEffectsClientRpc();

		// If this object is networked, despawn it.
		if (TryGetComponent(out NetworkObject netObj) && netObj.IsSpawned)
			netObj.Despawn(true);
		else
			Destroy(gameObject);
	}

	[ClientRpc]
	private void DieEffectsClientRpc()
	{
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, deathParticles.transform.rotation);
			Destroy(pfx, 5);
		}
	}

	public override void OnNetworkDespawn()
	{
		currentHealth.OnValueChanged -= OnHealthChanged;
	}
}
