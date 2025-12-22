using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections;
using UnityEditor.VisionOS;
using NUnit.Framework.Constraints;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemyNavigation : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[SerializeField] private NavMeshAgent agent;

	private Transform targetPos;
	private Vector3 targetDestination;
	private EnemyTarget targetScript;

	[Space(5)]

	[SerializeField] private float speed = 3.5f;
	[SerializeField] private float victoryTime = 5f;

	[Header("Knockback Settings")]
	private float originalSpeed;
	private float originalAcceleration;
	[SerializeField] private float knockbackForce = 5f;
	[SerializeField] private float knockbackDuration = .2f;
	private NetworkTransform netTransform;

	private Coroutine currKnockbackCoroutine;

	private void Start()
	{
		if (!agent) agent = GetComponent<NavMeshAgent>();
		
		originalAcceleration = agent.acceleration;

		InitializeEnemy();
	}
	public void TargetSlain()
	{
		StartCoroutine(TargetSlainFlow());
	}
	private IEnumerator TargetSlainFlow()
	{
		eRef.enemyAttack.StopAttack();
		eRef.enemyAnimator.A_SetWalk(false);

		yield return new WaitForSeconds(Random.Range(0, 1.2f));

		eRef.enemyAnimator.A_SetVictory(true);
		eRef.enemyAnimator.A_SetTarget(targetNotExist: GetTargets() == null);

		yield return new WaitForSeconds(victoryTime);

		eRef.enemyAnimator.A_SetVictory(false);

		yield return new WaitForSeconds(.8f);

		InitializeEnemy();

	}
	public void SetSpeedMultiplier()
	{
		if (currKnockbackCoroutine != null)
		{
			Invoke(nameof(SetSpeedMultiplier), .25f);
			return;
		}

		originalSpeed = eRef.enemyMultipliers.GetSpeedMulti(speed);
		agent.speed = eRef.enemyMultipliers.GetSpeedMulti(speed);
	}
	protected virtual void InitializeEnemy()
	{
		eRef.enemyAnimator.A_SetWalk(true);

		SetSpeedMultiplier();

		InitializeDestination();
	}

	protected virtual void InitializeDestination()
	{
		var enemyTargets = GetTargets();
		int randomTarget = Random.Range(0, enemyTargets.Length);

		eRef.enemyTarget = enemyTargets[randomTarget];
		targetScript = eRef.enemyTarget;

		targetPos = targetScript.transform;

		float destinationRadius = targetScript.GetColliderRadius();
		Vector2 randomCircle = Random.insideUnitCircle.normalized * destinationRadius;
		Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);

		targetDestination = targetPos.position + offset;
		agent.SetDestination(targetDestination);
	}
	private EnemyTarget[] GetTargets()
	{
		var et = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);

		if (et.Length > 0)
			return et;
		else
			return null;
	}
	public void DoKnockback(Vector3 hitPoint)
	{
		if (currKnockbackCoroutine != null)
			StopCoroutine(currKnockbackCoroutine);
		currKnockbackCoroutine = StartCoroutine(OnKnockback(hitPoint));
	}
	protected virtual IEnumerator OnKnockback(Vector3 hitPoint)
	{
		#region Init
		if (!IsServer)
			yield break;

		eRef.enemyAnimator.A_SetWalk(false);

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
		eRef.enemyAnimator.A_SetWalk(true);
		#endregion
	}
}