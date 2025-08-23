using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using EZCameraShake;

[RequireComponent(typeof(Rigidbody))]
public class Arrow : MonoBehaviour
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

	public GameObject explosion;

	private void Start()
	{
		var bowComponent = FindObjectOfType<BowScript>();
		arrowDamage = bowComponent.arrowDamage;
		arrowEffect = bowComponent.arrowEffect.ToArray();
		initialSpeed = bowComponent.arrowSpeed;
		cutTrees = bowComponent.arrowCutsTrees;
		lightningChain = bowComponent.lightningChain;

		transform.localScale *= bowComponent.arrowSize;

		initPlayerPos = FindObjectOfType<PlayerController>().transform.position;

		CameraShaker.Instance.ShakeOnce(2f, 3f, .1f, .2f);

		rb = GetComponent<Rigidbody>();
		rb.useGravity = false;

		Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
		Vector3 shootDir = ray.direction.normalized;
		velocity = shootDir * initialSpeed;
		transform.rotation = Quaternion.LookRotation(shootDir) * modelCorrection;

		Destroy(gameObject, lifeTime);
		Invoke(nameof(EnableTrailAfterDelay), 0.03f);
	}

	private void EnableTrailAfterDelay() => GetComponentInChildren<TrailRenderer>().enabled = true;

	private void FixedUpdate()
	{
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
			{
				rb.MoveRotation(Quaternion.LookRotation(velocity.normalized) * modelCorrection);
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
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
			Destroy(gameObject);
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
							var expl = Instantiate(explosion, transform.position, explosion.transform.rotation);
							Destroy(expl, 5f);
							break;
					}
				}
			}
			component.DamageEffects(initPlayerPos);
			component.TakeDamage(arrowDamage);
		}
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
				Destroy(gameObject, 0.05f);
			}
		}
		else
		{
			lightningTarget = null;
			Destroy(gameObject, 0.05f);
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
