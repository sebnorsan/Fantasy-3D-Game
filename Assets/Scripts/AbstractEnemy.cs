using System.Collections;
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
		crystalPos = GameObject.FindGameObjectWithTag("Crystal").transform;
		agent.SetDestination(crystalPos.position);

		InitializeEnemy();
	}

	protected virtual void InitializeEnemy() 
	{
		
	}
	public void DamageEffects(Vector3 hitPoint)
	{
		StartCoroutine(OnKnockback(hitPoint));
		OnFlashMaterial();
		OnSpawnDamagePFX();
		OnPlayDamageAnimation();
	}
	[Header("Knockback Settings")]
	public float knockbackForce = 5f;
	public float knockbackDuration = .2f;
	protected virtual IEnumerator OnKnockback(Vector3 hitPoint)
	{
		// 1) Calculate direction away from the hit
		Vector3 dir = (transform.position - hitPoint).normalized;

		// 2) Stop your agent so it doesn't fight our movement
		agent.isStopped = true;

		float elapsed = 0f;
		while (elapsed < knockbackDuration)
		{
			// 3) Move the agent by hand
			//    agent.Move respects NavMesh constraints (e.g. won't go through walls)
			agent.Move(dir * knockbackForce * Time.deltaTime);

			elapsed += Time.deltaTime;
			yield return null;
		}

		// 4) Give control back
		agent.isStopped = false;
	}
	protected virtual void OnFlashMaterial()
	{

	}
	protected virtual void OnSpawnDamagePFX()
	{

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
		Destroy(gameObject);
	}
}

public interface IDamagable
{
	void TakeDamage(int amount);
	void DamageEffects(Vector3 hitPoint);
	void Heal(int amount);
}
