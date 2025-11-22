using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KillableObject : MonoBehaviour, IDamagable
{
	[Header("Health Settings")]
	public int currentHealth;
	public int maxHealth;

	[Header("UI")]
	[Tooltip("Optional slider to display health")]
	[SerializeField] private Slider healthSlider;
	[Tooltip("How fast the slider value follows health changes")]
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

	private void Awake()
	{
		currentHealth = maxHealth;

		if (healthSlider != null)
		{
			healthSlider.maxValue = maxHealth;
			healthSlider.value = currentHealth;
		}
	}

	private void Update()
	{
		if (healthSlider != null)
		{
			float target = currentHealth;
			healthSlider.value = Mathf.Lerp(healthSlider.value, target, Time.deltaTime * sliderLerpSpeed);
		}
	}

	public void DamageEffects(Vector3 hitPoint)
	{
		StartCoroutine(OnFlashMaterial());
		OnSpawnDamagePFX();
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

	public void Heal(int amount)
	{
		if (currentHealth == maxHealth) return;
		currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
	}

	public void TakeDamage(int amount)
	{
		currentHealth = Mathf.Max(0, currentHealth - amount);
		if (currentHealth == 0)
			Die();
	}

	private void Die()
	{
		if (bigguy) return;

		if (TryGetComponent(out QuestObject quester))
			quester.FinishQuest();

		GameManager.instance.AddXp(xpGain);

		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, deathParticles.transform.rotation);
			Destroy(pfx, 5);
		}

		Destroy(gameObject);
	}

	private void OnDestroy()
	{
		if (gameObject.CompareTag("Tree"))
			FindFirstObjectByType<QuestObjectCounterTrees>().RemoveCount();
	}
}

