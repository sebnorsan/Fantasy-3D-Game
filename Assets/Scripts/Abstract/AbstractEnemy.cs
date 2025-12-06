using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemy : NetworkBehaviour, IDamagable
{
	[Header("References")]
	[SerializeField] private EnemyAnimatorLOD lod_anim;
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] private Animator anim;
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
	[SerializeField] private float knockbackForce = 5f;
	[SerializeField] private float knockbackDuration = .2f;

	[Header("Path / Movement")]
	[SerializeField] private float arriveThreshold = 0.1f;
	private Vector3[] pathCorners;
	private int pathIndex;
	private bool isKnockedBack;
	private float lastTickTime;
	private NetworkTransform netTransform;

	private void Awake()
	{
		if (!agent) agent = GetComponent<NavMeshAgent>();
		netTransform = GetComponent<NetworkTransform>();
	}

	private void Start()
	{
		lastTickTime = Time.time;

		EnemyLODManager.instance?.enemies.Add(lod_anim);
		EnemyAIManager.Instance?.Register(this);

		allRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

		currentHealth = maxHealth;

		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		InitializeEnemy();
	}

	protected virtual void InitializeEnemy()
	{
		anim?.SetBool("Walking", true);
		InitializeDestination();
	}

	// ----------------- AI / path -----------------

	protected virtual void InitializeDestination()
	{
		// pick a target
		var enemyTargets = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
		int randomTarget = Random.Range(0, enemyTargets.Length);
		targetScript = enemyTargets[randomTarget];

		targetPos = targetScript.transform;

		// random point around target radius
		float destinationRadius = targetScript.GetColliderRadius();
		Vector2 randomCircle = Random.insideUnitCircle.normalized * destinationRadius;
		Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);

		targetDestination = targetPos.position + offset;

		// build navmesh path once, then follow it manually
		NavMeshPath navPath = new NavMeshPath();
		bool hasPath = false;

		if (agent && agent.isOnNavMesh)
		{
			hasPath = agent.CalculatePath(targetDestination, navPath);
		}
		else
		{
			hasPath = NavMesh.CalculatePath(transform.position, targetDestination, NavMesh.AllAreas, navPath);
		}

		if (hasPath && navPath.corners.Length > 1)
		{
			pathCorners = navPath.corners;
		}
		else
		{
			// fallback: just go straight to destination
			pathCorners = new Vector3[] { targetDestination };
		}

		pathIndex = 0;
	}

	/// <summary>
	/// Called by EnemyAIManager on the server.
	/// </summary>
	public void TickAI()
	{
		if (!IsServer) return;
		if (isKnockedBack) return;
		if (pathCorners == null || pathCorners.Length == 0) return;

		float dt = Time.time - lastTickTime;
		lastTickTime = Time.time;

		Vector3 current = transform.position;
		Vector3 target = pathCorners[pathIndex];
		target.y = current.y;

		Vector3 to = target - current;
		float threshSqr = arriveThreshold * arriveThreshold;

		if (to.sqrMagnitude <= threshSqr)
		{
			pathIndex++;
			if (pathIndex >= pathCorners.Length)
			{
				OnReachedDestination();
				return;
			}

			target = pathCorners[pathIndex];
			target.y = current.y;
			to = target - current;
			if (to.sqrMagnitude < 0.0001f) return;
		}

		to.Normalize();
		Vector3 delta = to * speed * dt;
		transform.position += delta;

		if (delta.sqrMagnitude > 0f)
		{
			Quaternion targetRot = Quaternion.LookRotation(to);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * 10f);
		}
	}


	protected virtual void OnReachedDestination()
	{
		// Reached final point – override in subclasses if you want special behaviour
		// e.g. start attacking the crystal/base, stop moving, etc.
	}

	// ----------------- Attack -----------------

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
		anim?.SetBool("Walking", false);
		yield return new WaitForSeconds(attackDelay);
		anim?.SetTrigger("Attack");
		attackCoroutine = StartCoroutine(AttackNumerator());
	}

	// ----------------- Damage & Knockback -----------------

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
		// movement is SERVER ONLY to avoid client vs server fights
		if (!IsServer)
			yield break;

		yield return null;

		anim?.SetBool("Walking", false);
		isKnockedBack = true;

		Vector3 dir = (transform.position - hitPoint);
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.0001f)
		{
			isKnockedBack = false;
			yield break;
		}

		dir.Normalize();

		float elapsed = 0f;
		while (elapsed < knockbackDuration)
		{
			elapsed += Time.deltaTime;
			float t = 1f - Mathf.Clamp01(elapsed / knockbackDuration);
			float currentForce = knockbackForce * t;

			// move on server
			transform.position += dir * currentForce * Time.deltaTime;

			// during knockback: push precise state to clients every frame
			if (netTransform != null)
			{
				netTransform.Teleport(transform.position,
									  transform.rotation,
									  transform.localScale);
			}

			yield return null;
		}

		isKnockedBack = false;

		// one last precise sync when done
		if (netTransform != null)
		{
			netTransform.Teleport(transform.position,
								  transform.rotation,
								  transform.localScale);
		}

		InitializeDestination();        // rebuild path from new position
		anim?.SetBool("Walking", true);
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
		anim?.SetTrigger("Damage");
	}

	// ------------- IDamagable -------------

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
		EnemyAIManager.Instance?.Unregister(this);

		DieClientRpc(xpDrop);

		StopAllCoroutines();

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
		EnemyLODManager.instance?.enemies.Remove(lod_anim);

		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			var text = pfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			if (text != null)
				text.text = $"+{xpAmount}xp";
			Destroy(pfx, 5);
		}

		GameManager.instance?.AddXp(xpAmount);
	}

	// ------------- Crystal attacks -------------

	public void AE_AttackTarget()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (targetScript == null) return;

		targetScript.TakeDamage(damage);
	}

	private ulong lastHitByClientId = ulong.MaxValue;
	public void SetLastHitBy(ulong shooterClientId)
	{
		lastHitByClientId = shooterClientId;
	}
}

public interface IDamagable
{
	void TakeDamage(int amount, Vector3 hitPoint);
	void Heal(int amount);
}
