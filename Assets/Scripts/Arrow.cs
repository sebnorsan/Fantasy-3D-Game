using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : NetworkBehaviour
{
	[Header("Damage & Effects")]
	public int arrowDamage = 1;
	public ArrowEffect[] arrowEffect;
	public int lightningChain = 3;

	[Header("Flight Settings")]
	public float initialSpeed = 30f;
	public float dropGravity = 1f;
	public float lifeTime = 10f;
	public bool cutTrees = false;

	private readonly Quaternion modelCorrection = Quaternion.Euler(90f, 0f, 0f);
	private Vector3 initPlayerPos;
	private Vector3 velocity;
	private Rigidbody rb;

	// lightning specific
	private Transform lightningTarget;

	[Header("VFX")]
	public GameObject explosion;

	// ---- Server-side init ----
	// Call this ONLY on server, BEFORE Spawn()
	public void ServerInitialize(
		int damage,
		ArrowEffect[] effects,
		float speed,
		bool cutsTrees,
		int lightningChains,
		float size,
		Vector3 shootDir,
		Vector3 shooterPos,
		ulong shooterClientId
	)
	{
		if (!IsServer) return;

		arrowDamage = damage;
		arrowEffect = effects != null ? effects.ToArray() : new ArrowEffect[0];
		initialSpeed = speed;
		cutTrees = cutsTrees;
		lightningChain = lightningChains;

		transform.localScale *= size;

		initPlayerPos = shooterPos;

		rb.useGravity = false;

		shootDir = shootDir.normalized;
		velocity = shootDir * initialSpeed;
		transform.rotation = Quaternion.LookRotation(shootDir) * modelCorrection;

		// lifetime on server -> despawn for everyone
		StartCoroutine(LifeTimer());

		// Shooter-only camera shake
		ShakeShooterClientRpc(shooterClientId);

		EnableTrailAfterDelayClientRpc(0.03f);
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}

	private IEnumerator LifeTimer()
	{
		yield return new WaitForSeconds(lifeTime);
		if (IsSpawned)
			NetworkObject.Despawn();
	}

	[ClientRpc]
	private void EnableTrailAfterDelayClientRpc(float delay)
	{
		StartCoroutine(EnableTrail(delay));
	}

	private IEnumerator EnableTrail(float delay)
	{
		yield return new WaitForSeconds(delay);
		var tr = GetComponentInChildren<TrailRenderer>();
		if (tr != null) tr.enabled = true;
	}

	[ClientRpc]
	private void ShakeShooterClientRpc(ulong shooterClientId)
	{
		// only shake on the shooter’s machine
		if (NetworkManager.Singleton == null) return;
		if (NetworkManager.Singleton.LocalClientId != shooterClientId) return;

		// avoid singleton problems by finding a shaker on local camera
		var shaker = FindFirstObjectByType<EZCameraShake.CameraShaker>();
		if (shaker != null)
			shaker.ShakeOnce(2f, 3f, .1f, .2f);
	}

	private void FixedUpdate()
	{
		if (!IsServer) return;

		if (lightningTarget != null)
		{
			Vector3 dir = (lightningTarget.position - rb.position).normalized;
			velocity = dir * initialSpeed;
			Vector3 newPos = rb.position + velocity * Time.fixedDeltaTime;
			rb.MovePosition(newPos);
			rb.MoveRotation(Quaternion.LookRotation(dir) * modelCorrection);
		}
		else
		{
			Vector3 newPos = rb.position + velocity * Time.fixedDeltaTime;
			rb.MovePosition(newPos);

			velocity += Vector3.down * dropGravity * Time.fixedDeltaTime;

			if (velocity.sqrMagnitude > 0.001f)
				rb.MoveRotation(Quaternion.LookRotation(velocity.normalized) * modelCorrection);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!IsServer) return;

		if (collision.gameObject.CompareTag("Tree"))
		{
			if (!cutTrees) return;
			if (!collision.gameObject.TryGetComponent(out KillableObject killable))
			{
				var newComp = collision.gameObject.AddComponent<KillableObject>();
				newComp.maxHealth = 5;
				newComp.hitParticles = Resources.Load<GameObject>("PFX/HitFX (big)");
				newComp.deathParticles = Resources.Load<GameObject>("PFX/Explosion Tree");
				newComp.currentHealth = newComp.maxHealth;
				newComp.parentHitFx = false;
			}
		}

		if (collision.gameObject.TryGetComponent(out BigguyDamagable compt))
		{
			compt.TakeDamage(transform, arrowDamage);
			NetworkObject.Despawn();
			return;
		}

		if (collision.gameObject.TryGetComponent<IDamagable>(out var component))
		{
			if (collision.gameObject.TryGetComponent<AbstractEnemy>(out var enem))
			{
				foreach (var effect in arrowEffect)
				{
					switch (effect)
					{
						case ArrowEffect.Fire:
							enem.FireEffect();
							break;
						case ArrowEffect.Ice:
							enem.IceEffect();
							break;
						case ArrowEffect.Lightning:
							HandleLightningChain(collision.transform);
							break;
						case ArrowEffect.Bomb:
							PlayExplosionClientRpc(transform.position, transform.rotation);
							break;
					}
				}
			}

			component.DamageEffects(initPlayerPos);
			component.TakeDamage(arrowDamage);
		}
	}

	[ClientRpc]
	private void PlayExplosionClientRpc(Vector3 pos, Quaternion rot)
	{
		if (explosion == null) return;
		var expl = Instantiate(explosion, pos, rot);
		Destroy(expl, 5f);
	}

	private void HandleLightningChain(Transform hitEnemy)
	{
		if (lightningChain > 0)
		{
			Transform nextTarget = FindNextEnemy(hitEnemy);
			if (nextTarget != null)
			{
				lightningTarget = nextTarget;
				lightningChain--;
			}
			else
			{
				NetworkObject.Despawn();
			}
		}
		else
		{
			lightningTarget = null;
			NetworkObject.Despawn();
		}
	}

	private Transform FindNextEnemy(Transform exclude)
	{
		float searchRadius = 1000f;
		Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);
		Transform closest = null;
		float minDistSqr = float.MaxValue;

		foreach (var hit in hits)
		{
			var tr = hit.transform;
			if (tr == exclude) continue;
			if (tr.TryGetComponent<IDamagable>(out _))
			{
				float distSqr = (tr.position - transform.position).sqrMagnitude;
				if (distSqr < minDistSqr)
				{
					minDistSqr = distSqr;
					closest = tr;
				}
			}
		}

		return closest;
	}
}

public enum ArrowEffect
{
	Normal, Ice, Lightning, Bomb, Fire
}
