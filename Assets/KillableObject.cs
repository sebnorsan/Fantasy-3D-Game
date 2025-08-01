using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using UnityEditor.Timeline;

public class KillableObject : MonoBehaviour, IDamagable
{
	public int currentHealth, maxHealth;
	public GameObject hitParticles;
	public GameObject deathParticles;

	public int xpGain = 1;

	public bool parentHitFx = false;
	private void Awake()
	{
		currentHealth = maxHealth;
	}
	public void DamageEffects(Vector3 hitPoint)
	{
		StartCoroutine(OnFlashMaterial());
		OnSpawnDamagePFX();
	}
	private Material flashMaterial;
	private float flashDuration = .1f;
	private IEnumerator OnFlashMaterial()
	{
		// 1) Load flash material if not already loaded
		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		// 2) Find ALL MeshRenderers in this object’s hierarchy
		var allRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

		// 3) Store originals and apply flash
		var affected = new List<(MeshRenderer renderer, Material[] originals)>(allRenderers.Length);
		foreach (var rend in allRenderers)
		{
			// store original materials
			affected.Add((rend, rend.materials));

			// create an array filled with flashMaterial
			var flashMats = new Material[rend.materials.Length];
			for (int i = 0; i < flashMats.Length; i++)
				flashMats[i] = flashMaterial;

			// apply
			rend.materials = flashMats;
		}

		// 4) Wait
		yield return new WaitForSeconds(flashDuration);

		// 5) Revert all
		foreach (var (renderer, originals) in affected)
			renderer.materials = originals;
	}
	void OnSpawnDamagePFX()
	{
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
		if (TryGetComponent(out QuestObject quester))
			quester.FinishQuest();

		GameManager.instance.AddXp(xpGain);
		var pfx = Instantiate(deathParticles, transform.position, deathParticles.transform.rotation);
		Destroy(pfx, 5);
		Destroy(gameObject);
	}
    private void OnDestroy()
    {
		if (gameObject.tag == "Tree")
			FindFirstObjectByType<QuestObjectCounterTrees>().RemoveCount();
    }
}
