using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemy : NetworkBehaviour, IDamagable
{
	[Header("References")]
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] Animator anim;
	private Transform targetPos;
	private Vector3 targetDestination;
	private EnemyTarget targetScript;

	[Header("Enemy Stats")]
	[SerializeField] private int maxHealth = 100;
	private int currentHealth = 0;

	[SerializeField] private int damage = 1;
	[SerializeField] private float attackDelay = 3;

	[SerializeField] private int xpDrop = 1;
	[SerializeField] private float speed = 3.5f;

	private Coroutine attackCoroutine;

	[Header("Damage Effects")]
	private Material flashMaterial;
	private float flashDuration = .1f;

	[SerializeField] private GameObject hitParticles;
	[SerializeField] private GameObject deathParticles;

	private Coroutine currKnockbackCoroutine, currFlashCoroutine;
	private MeshRenderer[] allRenderers;

	[Header("Knockback Settings")]
	private float originalSpeed;
	private float originalAcceleration;
	[SerializeField] private float knockbackForce = 5f;
	[SerializeField] private float knockbackDuration = .2f;

	private void OnValidate()
	{
		if (!Application.isEditor || Application.isPlaying) return;

		originalSpeed = speed;
		originalAcceleration = agent.acceleration;
	}

	private void Start()
	{
		allRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

		currentHealth = maxHealth;

		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		InitializeEnemy();
	}

	protected virtual void InitializeEnemy()
	{
		if (anim != null)
			anim.SetBool("Walking", true);

		agent.speed = speed;

		InitializeDestination();
	}
	/// <summary>
	/// These are called from the EnemyTarget script itself,
	/// when an enemy enters its trigger, it starts attacking
	/// and when an enemy leaves its trigger, it stops.
	/// </summary>
	//------------------------
	public void StartAttack()
	{
		attackCoroutine = StartCoroutine(AttackNumerator());
	}

	public void StopAttack()
	{
		if (attackCoroutine != null)
			StopCoroutine(attackCoroutine);
	}
	//------------------------
	protected virtual IEnumerator AttackNumerator()
	{
		anim.SetBool("Walking", false);
		yield return new WaitForSeconds(attackDelay);
		anim.SetTrigger("Attack");
		attackCoroutine = StartCoroutine(AttackNumerator());
	}

	protected virtual void InitializeDestination()
	{
		var enemyTargets = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
		int randomTarget = Random.Range(0, enemyTargets.Length);
		targetScript = enemyTargets[randomTarget];

		targetPos = targetScript.transform;

		float destinationRadius = targetScript.GetColliderRadius();
		Vector2 randomCircle = Random.insideUnitCircle.normalized * destinationRadius;
		Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);

		targetDestination = targetPos.position + offset;
		agent.SetDestination(targetDestination);
	}

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

	protected virtual IEnumerator OnKnockback(Vector3 hitPoint)
	{
		#region Init
		yield return null;

		if (anim != null)
			anim.SetBool("Walking", false);
		#endregion
		#region SetKnockbackMovement

		///<summary>
		///Sets the agents destination to the knockback position
		///which is calculated based on the position of where the player shot the enemy from
		///afterwards we disable rotations so the agent doesent look at the knockback position while getting knocked back
		/// </summary>

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
		#endregion
		#region SetNormalMovement

		///<summary>
		///Resets, so normal destination and movement speed is reset
		/// </summary>

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
		#endregion
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

	public void AE_AttackCrystal()
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
}


public interface IDamagable
{
	public void TakeDamage(int amount, Vector3 hitPoint);
	public void Heal(int amount);
}
