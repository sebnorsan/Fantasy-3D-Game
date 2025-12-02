using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemy : NetworkBehaviour, IDamagable
{
	private NavMeshAgent agent;
	private Transform crystalPos;
	private Animator anim;

	public int maxHealth = 100;
	private int currentHealth = 0;

	public int damage = 1;
	public float attackDelay = 3;

	public int xpDrop = 1;
	public float speed = 3.5f;

	[SerializeField] private GameObject fireEffect, iceEffect;
	private List<(MeshRenderer renderer, Material[] originals)> affected =
		new List<(MeshRenderer renderer, Material[] originals)>();

	private void Awake() => currentHealth = maxHealth;

	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();

		if (GetComponent<Animator>())
			anim = GetComponent<Animator>();

		originalSpeed = speed;
		originalAcceleration = agent.acceleration;

		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		hitParticles = Resources.Load<GameObject>("PFX/HitFX");
		deathParticles = Resources.Load<GameObject>("PFX/DeathFX");

		InitializeEnemy();
	}

	private Coroutine attackCoroutine;
	public void StartAttack()
	{
		attackCoroutine = StartCoroutine(AttackNumerator());
	}

	public void StopAttack()
	{
		if (attackCoroutine != null)
			StopCoroutine(attackCoroutine);
	}

	protected virtual IEnumerator AttackNumerator()
	{
		anim.SetBool("Walking", false);
		yield return new WaitForSeconds(attackDelay);
		anim.SetTrigger("Attack");
		attackCoroutine = StartCoroutine(AttackNumerator());
	}

	protected virtual void InitializeEnemy()
	{
		if (anim != null)
			anim.SetBool("Walking", true);

		agent.speed = speed;

		InitializeDestination();
	}

	private Vector3 targetDestination;
	protected virtual void InitializeDestination()
	{
		//crystalPos = FindFirstObjectByType<CrystalScript>().transform;

		float radius = 2.5f;
		Vector2 randomCircle = Random.insideUnitCircle.normalized * radius;
		Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);

		targetDestination = crystalPos.position + offset;
		agent.SetDestination(targetDestination);
	}

	Coroutine currKnockbackCoroutine, currFlashCoroutine;
	public void DamageEffects(Vector3 hitPoint)
	{
		if (currKnockbackCoroutine != null)
			StopCoroutine(currKnockbackCoroutine);
		currKnockbackCoroutine = StartCoroutine(OnKnockback(hitPoint));

		if (currFlashCoroutine == null)
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
		if (anim != null)
			anim.SetBool("Walking", false);

		// Compute direction from hit point to this enemy
		Vector3 dir = (transform.position - hitPoint).normalized;

		// Set a far destination in the knockback direction
		Vector3 dest = transform.position + dir * 10f;
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
		agent.SetDestination(targetDestination); // Resume behavior
		if (anim != null)
			anim.SetBool("Walking", true);
	}

	private Material flashMaterial;
	private float flashDuration = .1f;
	protected virtual IEnumerator OnFlashMaterial()
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

	private GameObject hitParticles;
	private GameObject deathParticles;
	protected virtual void OnSpawnDamagePFX()
	{
		if (hitParticles == null) return;

		var pfx = Instantiate(hitParticles, transform.position, Quaternion.identity);
		Destroy(pfx, 5);
	}

	protected virtual void OnPlayDamageAnimation()
	{
		if (anim != null)
			anim.SetTrigger("Damage");
	}

	// ------------- IDamagable --------------

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

	// ------------- Death & XP (server authoritative) -------------

	private void DieServer()
	{
		DieClientRpc(xpDrop);

		StopAllCoroutines();

		// server despawns, clients will despawn automatically
		if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
			nwo.Despawn(true);
		else
			Destroy(gameObject);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamageEffectsClientRpc(Vector3 hitPoint)
	{
		DamageEffects(hitPoint);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieClientRpc(int xpAmount)
	{
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			var text = pfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			if (text != null)
				text.text = $"+{xpAmount}xp";
			Destroy(pfx, 5);
		}

		GameManager.instance.AddXp(xpAmount);
	}

	// ------------- Crystal attacks -------------

	public void AttackCrystal()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		//var crystalScript = FindFirstObjectByType<CrystalScript>();
		//if (crystalScript == null) return;

		//crystalScript.TakeDamage(damage);

		//if (crystalScript.thorns)
		//	TakeDamage(5, transform.position);
		//if (crystalScript.deadly)
		//	DieServer();
	}

	// ------------- Fire / Ice effects (still mostly local) -------------


	public void IceEffect()
	{
		if (!NetworkManager.Singleton.IsServer) return;
		IceEffectClientRpc();
	}

	public void FireEffect()
	{
		if (!NetworkManager.Singleton.IsServer) return;
		FireEffectClientRpc();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void IceEffectClientRpc()
	{
		if (iceEffect != null)
			iceEffect.SetActive(true);

		CancelInvoke(nameof(ResetIceEffect));
		Invoke(nameof(ResetIceEffect), 6f);

		// slowdown on all instances (same as before, but now visible everywhere)
		if (agent != null)
			agent.speed /= 2f;
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void FireEffectClientRpc()
	{
		if (fireEffect != null)
			fireEffect.SetActive(true);

		// periodic damage still only really applies on server because TakeDamage has an IsServer guard
		InvokeRepeating(nameof(FireDamage), .5f, 10f);

		CancelInvoke(nameof(ResetFireEffect));
		Invoke(nameof(ResetFireEffect), .5f * 10f);
	}

	private void ResetIceEffect()
	{
		if (agent != null)
			agent.speed = originalSpeed;
	}

	private void ResetFireEffect()
	{
		if (fireEffect != null)
			fireEffect.SetActive(false);
	}

	private void FireDamage()
	{
		// this will only actually change HP on server due to guard in TakeDamage
		TakeDamage(1, transform.position);
	}
}


public interface IDamagable
{
	void TakeDamage(int amount, Vector3 hitPoint);
	//void DamageEffects(Vector3 hitPoint);
	void Heal(int amount);
}
