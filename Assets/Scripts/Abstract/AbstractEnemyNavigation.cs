using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemyNavigation : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]

	[SerializeField] private NavMeshAgent agent;

	private Transform targetPos;
	private Vector3 targetDestination;
	private Coroutine playerTargetCoroutine;

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

	private void OnDisable()
	{
		if (!IsServer) return;

		if (eRef.enemyTarget != null)
			eRef.enemyTarget.targetSlain -= TargetSlain;
		GameStateManager.gameStateChanged -= InitializeDestination;
	}
	private void Start()
	{
		if (!agent) agent = GetComponent<NavMeshAgent>();

		// BIG performance win for crowds
		agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		agent.autoRepath = false;

		originalAcceleration = agent.acceleration;
		InitializeEnemy();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if (IsServer)
			return;
		else
			agent.enabled = true;

		originalAcceleration = agent.acceleration;
		InitializeEnemy();
	}

	private void TargetSlain()
	{
		StartCoroutine(TargetSlainFlow());
	}
	private IEnumerator TargetSlainFlow()
	{
		yield return new WaitForSeconds(Random.Range(.2f, 1f));

		float sSpd = speed;
		speed = 0;

		eRef.enemyAttack.StopAttack();
		eRef.enemyAnimator.A_SetWalk(false);

		yield return new WaitForSeconds(Random.Range(0, 1.2f));

		bool isMoreTargets = EnemyTarget.All.Count > 0;

		eRef.enemyAnimator.A_SetVictory(true);
		eRef.enemyAnimator.A_SetTarget(falseForDespawn: !isMoreTargets);

		yield return new WaitForSeconds(victoryTime);

		eRef.enemyAnimator.A_SetVictory(false);

		yield return new WaitForSeconds(Random.Range(1, 3.2f));

		speed = sSpd;

		if (isMoreTargets)
			InitializeEnemy();

	}
	public void SetSpeedMultiplier()
	{
		if (currKnockbackCoroutine != null)
		{
			Invoke(nameof(SetSpeedMultiplier), .25f);
			return;
		}

		originalSpeed = eRef.enemyMultipliers.GetMulti(speed, Multiplier.Speed);
		agent.speed = originalSpeed;
	}
	protected virtual void InitializeEnemy()
	{
		if (!IsServer || !agent.enabled) return;

		eRef.enemyAnimator.A_SetWalk(true);
		SetSpeedMultiplier();

		GameStateManager.gameStateChanged -= InitializeDestination;
		GameStateManager.gameStateChanged += InitializeDestination;

		InitializeDestination();
	}

	protected virtual void InitializeDestination()
	{
		if (!IsServer) return;

		if (GameStateManager.GetCurrentGameState() == GameStateType.Morning)
		{
			ClearPlayerTarget();
			TargetDest();
		}
		else
		{
			ClearEnemyTarget();
			PlayerDest();
		}
	}
	private void ClearEnemyTarget()
	{
		if (eRef.enemyTarget != null)
			eRef.enemyTarget.targetSlain -= TargetSlain;

		targetPos = null;
		eRef.enemyTarget = null;
	}
	private void ClearPlayerTarget()
	{
		if (playerTargetCoroutine != null)
		{
			StopCoroutine(playerTargetCoroutine);
			playerTargetCoroutine = null;
			eRef.playerTarget = null;
		}

		agent.isStopped = false;
		eRef.enemyAttack.StopAttack();
	}
	private void TargetDest()
	{
		ClearEnemyTarget();

		var all = EnemyTarget.All;
		if (all.Count == 0)
		{
			TargetSlain();
			return;
		}

		eRef.enemyTarget = all[Random.Range(0, all.Count)];

		eRef.enemyTarget.targetSlain += TargetSlain;

		targetPos = eRef.enemyTarget.transform;

		float destinationRadius = eRef.enemyTarget.GetColliderRadius();
		Vector2 randomCircle = Random.insideUnitCircle.normalized * destinationRadius;
		Vector3 offset = new Vector3(randomCircle.x, 0, randomCircle.y);

		targetDestination = targetPos.position + offset;
		agent.SetDestination(targetDestination);
	}
	private void PlayerDest()
	{
		if (playerTargetCoroutine != null)
			StopCoroutine(playerTargetCoroutine);

		eRef.enemyTarget = null;

		playerTargetCoroutine = StartCoroutine(TargetPlayer());
	}
	private IEnumerator TargetPlayer()
	{
		if (!IsServer) yield break;

		PlayerReferences pRef = null;

		WaitForSeconds waitTime = new WaitForSeconds(.25f);

		while (GameStateManager.GetCurrentGameState() != GameStateType.Morning)
		{
			if (isKnockingBack)
			{
				yield return new WaitForSeconds(.1f);
				continue;
			}

			// re-pick if missing / dead-ish / etc.
			if (pRef == null || !pRef.playerController.canMove)
			{
				pRef = FindBestPlayer();
				eRef.playerTarget = pRef?.playerDamagable;
			}

			if (pRef == null)
			{
				eRef.enemyAttack.StopAttack();
				agent.isStopped = false;
				yield return waitTime;
				continue;
			}

			targetPos = pRef.transform;
			targetDestination = targetPos.position;

			float attackRange = Mathf.Max(agent.stoppingDistance, 1.5f);
			float sqr = (targetPos.position - transform.position).sqrMagnitude;

			if (sqr <= attackRange * attackRange)
			{
				agent.isStopped = true;
				eRef.enemyAnimator.A_ResetCancelAttack();
				eRef.enemyAttack.StartAttack();
			}
			else
			{
				agent.isStopped = false;
				eRef.enemyAttack.StopAttack();
				eRef.enemyAnimator.A_CancelAttack();
				agent.SetDestination(targetPos.position);
			}

			yield return waitTime;
		}

		// cleanup if morning hits
		agent.isStopped = false;
		eRef.enemyAttack.StopAttack();
		playerTargetCoroutine = null;
	}
	private PlayerReferences FindBestPlayer()
	{
		var players = FindObjectsByType<PlayerReferences>(FindObjectsSortMode.None);

		PlayerReferences best = null;
		float bestDist = float.MaxValue;

		foreach (var pc in players)
		{
			if (pc == null) continue;
			if (!pc.playerController.canMove) continue;

			// only real network-spawned players
			if (!pc.TryGetComponent(out NetworkObject nwo) || !nwo.IsSpawned) continue;

			float d = (pc.transform.position - transform.position).sqrMagnitude;
			if (d < bestDist)
			{
				bestDist = d;
				best = pc;
			}
		}

		return best;
	}
	
	//private EnemyTarget[] GetTargets()
	//{
	//	var et = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);

	//	if (et.Length > 0)
	//		return et;
	//	else
	//		return null;
	//}
	public void DoKnockback(Vector3 hitPoint, float knockbackMultiplier)
	{
		if (!IsServer) return;

		if (currKnockbackCoroutine != null)
			StopCoroutine(currKnockbackCoroutine);
		currKnockbackCoroutine = StartCoroutine(OnKnockback(hitPoint, knockbackMultiplier));
	}
	private bool isKnockingBack = false;
	protected virtual IEnumerator OnKnockback(Vector3 hitPoint, float knockbackMultiplier)
	{
		#region Init
		if (!IsServer)
			yield break;

		isKnockingBack = true;

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
		agent.speed = knockbackForce * knockbackMultiplier;

		// Smoothly reduce speed to 0 over knockbackDuration
		float elapsed = 0f;

		agent.updateRotation = false;

		agent.acceleration = 10000;

		while (elapsed < knockbackDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / knockbackDuration;
			agent.speed = Mathf.Lerp(knockbackForce * knockbackMultiplier, 0f, t);

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

		isKnockingBack = false;
		#endregion
	}
}