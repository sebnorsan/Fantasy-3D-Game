using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine;
using Unity.Netcode.Components;
using static Steamworks.InventoryItem;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemy : NetworkBehaviour, IDamagable
{
	[Header("References")]
	[SerializeField] private EnemyAnimatorLOD lod_anim;
	[SerializeField] private NavMeshAgent agent;
	[SerializeField] Animator anim;
	private Transform targetPos;
	private Vector3 targetDestination;
	private EnemyTarget targetScript;

	[Header("Enemy Stats")]
	[SerializeField] private int maxHealth = 100;
	private int currentHealth = 0;

	private NetworkVariable<int> syncedHealth = new NetworkVariable<int>(
	writePerm: NetworkVariableWritePermission.Server);

	private bool locallyPredictedDead;

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
	private NetworkTransform netTransform;

	[Header("Attack")]
	[SerializeField] private float attackTurnSpeed = 10f;

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if (IsServer)
		{
			syncedHealth.Value = maxHealth;
			currentHealth = maxHealth;
		}

		syncedHealth.OnValueChanged += OnHealthChanged;
	}

	public override void OnDestroy()
	{
		syncedHealth.OnValueChanged -= OnHealthChanged;
	}

	private void OnHealthChanged(int previous, int current)
	{
		currentHealth = current;
	}
	private void Awake()
	{
		if (!agent) agent = GetComponent<NavMeshAgent>();
		netTransform = GetComponent<NetworkTransform>();
	}
	private void Start()
	{
		EnemyLODManager.instance?.enemies.Add(lod_anim);

		allRenderers = GetComponentsInChildren<MeshRenderer>(includeInactive: true);

		currentHealth = maxHealth;

		originalSpeed = speed;
		originalAcceleration = agent.acceleration;

		flashMaterial = Resources.Load<Material>("Materials/FlashMaterial");

		InitializeEnemy();
	}

	protected virtual void InitializeEnemy()
	{
		anim?.SetBool("Walking", true);

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
		if (attackCoroutine == null)
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
		anim?.SetBool("Walking", false);
		float elapsed = 0f;
		while (elapsed < attackDelay)
		{
			elapsed += Time.deltaTime;
			RotateTowardsTarget();
			yield return null;
		}
		anim?.SetTrigger("Attack");
		attackCoroutine = StartCoroutine(AttackNumerator());
	}
	private void RotateTowardsTarget()
	{
		if (targetPos == null) return;

		Vector3 dir = targetPos.position - transform.position;
		dir.y = 0f;
		if (dir.sqrMagnitude < 0.0001f) return;

		Quaternion lookRot = Quaternion.LookRotation(dir);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			lookRot,
			Time.deltaTime * attackTurnSpeed
		);
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
		if (!IsServer)
			yield break;

		anim?.SetBool("Walking", false);

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

			if (netTransform != null)
			{
				netTransform.Teleport(transform.position,
									  transform.rotation,
									  transform.localScale);
			}

			yield return null;
		}
		#endregion
		#region SetNormalMovement

		///<summary>
		///Resets, so normal destination and movement speed is reset
		/// </summary>

		if (netTransform != null)
		{
			netTransform.Teleport(transform.position,
								  transform.rotation,
								  transform.localScale);
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
		anim?.SetBool("Walking", true);
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
		anim?.SetTrigger("Damage");
	}

	// ------------- IDamagable --------------

	public void TakeDamage(int amount, Vector3 hitPoint)
	{
		if (!NetworkManager.Singleton.IsServer) return;

		//currentHealth = Mathf.Max(0, currentHealth - amount);

		int newHealth = Mathf.Max(0, syncedHealth.Value - amount);
		syncedHealth.Value = newHealth;

		DamageEffectsClientRpc(hitPoint, lastHitByClientId);

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
		// pass shooter id so their client can skip duplicate VFX
		DieClientRpc(lastHitByClientId);

		StopAllCoroutines();

		if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
			nwo.Despawn(true);
		else
			Destroy(gameObject);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DieClientRpc(ulong shooterClientId)
	{
		EnemyLODManager.instance?.enemies.Remove(lod_anim);

		bool isShooter = NetworkManager.Singleton != null &&
						 NetworkManager.Singleton.LocalClientId == shooterClientId;

		// shooter already saw predicted death VFX – don't play again
		if (!isShooter && deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			var text = pfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			if (text != null)
				text.text = $"+{xpDrop}xp";
			Destroy(pfx, 5);
		}

		GameManager.instance?.AddXp(xpDrop);
	}


	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void DamageEffectsClientRpc(Vector3 hitPoint, ulong shooterClientId)
	{
		// shooter already did local DamageEffects, skip to avoid double VFX
		if (NetworkManager.Singleton != null &&
			NetworkManager.Singleton.LocalClientId == shooterClientId)
			return;

		DamageEffects(hitPoint);
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

	// called by the shooter client right when their arrow hits
	public void LocalPredictedDamage(int amount, Vector3 hitPoint, ulong shooterClientId)
	{
		if (!IsClient) return;
		if (NetworkManager.Singleton.LocalClientId != shooterClientId) return;
		if (locallyPredictedDead) return;

		int newHealth = Mathf.Max(0, currentHealth - amount);
		currentHealth = newHealth;

		// instant local VFX
		DamageEffects(hitPoint);

		if (newHealth == 0)
		{
			locallyPredictedDead = true;
			LocalPredictedDie();
		}
	}

	private void LocalPredictedDie()
	{
		// purely visual/client-side "death", no despawn
		if (deathParticles != null)
		{
			var pfx = Instantiate(deathParticles, transform.position, Quaternion.identity);
			var text = pfx.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			if (text != null)
				text.text = $"+{xpDrop}xp";
			Destroy(pfx, 5);
		}

		// hide enemy on this client
		foreach (var rend in allRenderers)
			if (rend) rend.enabled = false;

		foreach (var col in GetComponentsInChildren<Collider>())
			col.enabled = false;

		anim?.SetBool("Walking", false);
	}

}


public interface IDamagable
{
	public void TakeDamage(int amount, Vector3 hitPoint);
	public void Heal(int amount);
}
