using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemy : MonoBehaviour, IDamagable
{
	private NavMeshAgent agent;
	private Transform crystalPos;

	public int maxHealth = 100;
	public int currentHealth = 0;
	private void Awake() => currentHealth = maxHealth;
	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();

		originalSpeed = agent.speed;
		originalAcceleration = agent.acceleration;
		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		hitParticles = Resources.Load<GameObject>("PFX/HitFX");
		deathParticles = Resources.Load<GameObject>("PFX/DeathFX");

		crystalPos = GameObject.FindGameObjectWithTag("Crystal").transform;
		agent.SetDestination(crystalPos.position);

		InitializeEnemy();
	}

	protected virtual void InitializeEnemy() 
	{
		
	}
	Coroutine currKnockbackCoroutine, currFlashCoroutine;
	public void DamageEffects(Vector3 hitPoint)
	{
		if (currKnockbackCoroutine != null) 
			StopCoroutine(currKnockbackCoroutine);
		currKnockbackCoroutine = StartCoroutine(OnKnockback(hitPoint));

		if (currFlashCoroutine != null)
			StopCoroutine(currFlashCoroutine);
		currFlashCoroutine = StartCoroutine(OnFlashMaterial());

		OnSpawnDamagePFX();
		OnPlayDamageAnimation();
	}
	[Header("Knockback Settings")]
	private float originalSpeed;
	private float originalAcceleration;
	public float knockbackForce = 5f;
	public float knockbackDuration = .2f;
	protected virtual IEnumerator OnKnockback(Vector3 hitPoint)
	{
		yield return new WaitForSeconds(0.001f);
		// Compute direction from hit point to this enemy
		Vector3 dir = (transform.position - hitPoint).normalized;

		// Set a far destination in the knockback direction
		Vector3 dest = transform.position + dir * 10f; // 3 units back
		agent.SetDestination(dest); 

		// Apply high initial knockback speed
		agent.speed = knockbackForce;

		// Smoothly reduce speed to 0 over knockbackDuration
		float elapsed = 0f;

		agent.updateRotation = false;

		agent.acceleration = 10000;

		while (elapsed < knockbackDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / knockbackDuration;
			agent.speed = Mathf.Lerp(knockbackForce, 0f, t);
			yield return null;
		}

		agent.updateRotation = true;
		agent.acceleration = originalAcceleration;

		// Agent is now knocked back and "staggered", fade back to original speed
		elapsed = 0f;
		float returnDuration = 0.3f;
		while (elapsed < returnDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / returnDuration;
			agent.speed = Mathf.Lerp(0f, originalSpeed, t);
			yield return null;
		}

		agent.speed = originalSpeed;
		agent.SetDestination(crystalPos.position); // Resume behavior
	}
	private Material flashMaterial;
	private float flashDuration = .1f;
	protected virtual IEnumerator OnFlashMaterial()
	{
		// Load flash material if not already loaded
		if (flashMaterial == null)
			flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		// Store original materials per renderer
		List<(MeshRenderer renderer, Material[] originalMaterials)> affectedRenderers = new();

		// Add root MeshRenderer
		if (TryGetComponent(out MeshRenderer rootRenderer))
		{
			affectedRenderers.Add((rootRenderer, rootRenderer.materials));
			Material[] flashMats = new Material[rootRenderer.materials.Length];
			for (int i = 0; i < flashMats.Length; i++) flashMats[i] = flashMaterial;
			rootRenderer.materials = flashMats;
		}

		// Add child MeshRenderers
		foreach (Transform child in transform)
		{
			if (child.TryGetComponent(out MeshRenderer childRenderer))
			{
				affectedRenderers.Add((childRenderer, childRenderer.materials));
				Material[] flashMats = new Material[childRenderer.materials.Length];
				for (int i = 0; i < flashMats.Length; i++) flashMats[i] = flashMaterial;
				childRenderer.materials = flashMats;
			}
		}

		yield return new WaitForSeconds(flashDuration);

		// Revert to original materials
		foreach (var (renderer, originalMaterials) in affectedRenderers)
		{
			renderer.materials = originalMaterials;
		}
	}
	private GameObject hitParticles;
	private GameObject deathParticles;
	protected virtual void OnSpawnDamagePFX()
	{
		var pfx = Instantiate(hitParticles, transform.position, Quaternion.identity);
		Destroy(pfx, 5);
	}
	protected virtual void OnPlayDamageAnimation()
	{

	}
	public void TakeDamage(int amount)
	{
		currentHealth = Mathf.Max(0, currentHealth - amount);
		if (currentHealth == 0)
			Die();
	}
	public void Heal(int amount)
	{
		if (currentHealth == maxHealth) return;
		currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
	}

	public void Die()
	{
		var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
		Destroy(pfx, 5);
		Destroy(gameObject);

		StopAllCoroutines();
	}
}

public interface IDamagable
{
	void TakeDamage(int amount);
	void DamageEffects(Vector3 hitPoint);
	void Heal(int amount);
}
