using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine;
using Unity.Netcode.Components;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AbstractEnemyNavigation : NetworkBehaviour
{
	[SerializeField] private EnemyReferences eRef;

	[Space(5)]
	[SerializeField] private NavMeshAgent agent;

	private Transform targetPos;
	private Vector3 targetDestination;

	[Space(5)]
	[SerializeField] private float speed = 3.5f;
	[SerializeField] private float victoryTime = 5f;

	[Header("Targeting")]
	[SerializeField] private bool alwaysTargetPlayers = false;
	[SerializeField] private float playerAggroRange = 8f;
	[SerializeField] private float retargetInterval = .25f;

	[Header("Knockback Settings")]
	private float originalSpeed;
	private float originalAcceleration;
	[SerializeField] private float knockbackForce = 5f;
	[SerializeField] private float knockbackDuration = .2f;
	private NetworkTransform netTransform;

	private Coroutine currKnockbackCoroutine;
	private Coroutine targetRoutine;

	private bool isKnockingBack = false;

	private void OnDisable()
	{
		if (!IsServer) return;

		if (eRef.enemyTarget != null)
			eRef.enemyTarget.targetSlain -= TargetSlain;

		if (targetRoutine != null)
		{
			StopCoroutine(targetRoutine);
			targetRoutine = null;
		}
	}

	private void Start()
	{
		if (!agent) agent = GetComponent<NavMeshAgent>();
		if (!netTransform) netTransform = GetComponent<NetworkTransform>();

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
		SpeedMultiplierServerRpc(eRef.enemyMultipliers.GetMulti(speed, Multiplier.Speed));
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SpeedMultiplierServerRpc(float speed)
	{
		SpeedMultiplierClientRpc(speed);

		originalSpeed = speed;
		agent.speed = originalSpeed;
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SpeedMultiplierClientRpc(float speed)
	{
		if (currKnockbackCoroutine != null)
		{
			Invoke(nameof(SetSpeedMultiplier), .25f);
			return;
		}

		originalSpeed = speed;
		agent.speed = originalSpeed;
	}

	protected virtual void InitializeEnemy()
	{
		if (!IsServer || !agent.enabled) return;

		eRef.enemyAnimator.A_SetWalk(true);
		SetSpeedMultiplier();

		if (targetRoutine != null)
			StopCoroutine(targetRoutine);

		targetRoutine = StartCoroutine(TargetLoop());
	}

	private IEnumerator TargetLoop()
	{
		WaitForSeconds wait = new WaitForSeconds(retargetInterval);

		while (true)
		{
			if (isKnockingBack)
			{
				yield return null;
				continue;
			}

			PlayerReferences pRef = alwaysTargetPlayers
				? FindBestPlayer()
				: FindBestPlayer(playerAggroRange);

			if (pRef != null)
				HandlePlayerTarget(pRef);
			else
				HandleCrystalTarget();

			yield return wait;
		}
	}

	private void HandlePlayerTarget(PlayerReferences pRef)
	{
		if (pRef == null || !pRef.playerController.canMove)
		{
			HandleCrystalTarget();
			return;
		}

		// switching from crystal -> player
		if (eRef.enemyTarget != null)
		{
			ClearEnemyTarget();
			eRef.enemyAttack.StopAttack();
		}

		// switching player target
		if (eRef.playerTarget != pRef.playerDamagable)
			eRef.enemyAttack.StopAttack();

		eRef.playerTarget = pRef.playerDamagable;

		targetPos = pRef.transform;
		targetDestination = targetPos.position;

		float attackRange = Mathf.Max(agent.stoppingDistance, eRef.enemyAttack.PlayerAttackRange);
		float sqr = (targetPos.position - transform.position).sqrMagnitude;

		if (sqr <= attackRange * attackRange)
		{
			agent.isStopped = true;
			eRef.enemyAnimator.A_SetWalk(false);
			eRef.enemyAttack.StartAttack();
		}
		else
		{
			agent.isStopped = false;
			eRef.enemyAttack.StopAttack();
			eRef.enemyAnimator.A_SetWalk(true);
			agent.SetDestination(targetPos.position);
		}
	}

	private void HandleCrystalTarget()
	{
		// if this enemy should ONLY target players
		if (alwaysTargetPlayers)
		{
			ClearPlayerTarget();
			ClearEnemyTarget();
			eRef.enemyAttack.StopAttack();
			agent.isStopped = false;
			return;
		}

		// switching from player -> crystal
		ClearPlayerTarget();

		if (eRef.enemyTarget == null)
			TargetDest();

		if (eRef.enemyTarget == null)
		{
			eRef.enemyAttack.StopAttack();
			agent.isStopped = false;
			return;
		}

		agent.isStopped = false;
		eRef.enemyAnimator.A_SetWalk(true);
		agent.SetDestination(targetDestination);
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
		if (eRef.playerTarget != null)
		{
			eRef.playerTarget = null;
			eRef.enemyAttack.StopAttack();
		}

		agent.isStopped = false;
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

	private PlayerReferences FindBestPlayer(float maxDistance = Mathf.Infinity)
	{
		var players = FindObjectsByType<PlayerReferences>(FindObjectsSortMode.None);

		PlayerReferences best = null;
		float bestDist = float.MaxValue;
		float maxDistSqr = maxDistance * maxDistance;

		foreach (var pc in players)
		{
			if (pc == null) continue;
			if (!pc.playerController.canMove) continue;

			if (!pc.TryGetComponent(out NetworkObject nwo) || !nwo.IsSpawned) continue;

			float d = (pc.transform.position - transform.position).sqrMagnitude;
			if (d > maxDistSqr) continue;

			if (d < bestDist)
			{
				bestDist = d;
				best = pc;
			}
		}

		return best;
	}

	public void DoKnockback(Vector3 hitPoint, float knockbackMultiplier)
	{
		if (!IsServer) return;

		if (currKnockbackCoroutine != null)
			StopCoroutine(currKnockbackCoroutine);

		currKnockbackCoroutine = StartCoroutine(OnKnockback(hitPoint, knockbackMultiplier));
	}

	protected virtual IEnumerator OnKnockback(Vector3 hitPoint, float knockbackMultiplier)
	{
		if (!IsServer)
			yield break;

		isKnockingBack = true;

		eRef.enemyAttack.StopAttack();
		agent.isStopped = false;
		eRef.enemyAnimator.A_SetWalk(false);

		Vector3 dir = transform.position - hitPoint;
		dir.y = 0f;

		if (dir.sqrMagnitude < 0.001f)
			dir = -transform.forward; // fallback if overlapping exactly
		else
			dir.Normalize();

		Vector3 dest = transform.position + dir * 10f;
		agent.SetDestination(dest);

		agent.speed = knockbackForce * knockbackMultiplier;

		float elapsed = 0f;
		agent.updateRotation = false;
		agent.acceleration = 10000;

		while (elapsed < knockbackDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / knockbackDuration;
			agent.speed = Mathf.Lerp(knockbackForce * knockbackMultiplier, 0f, t);

			if (netTransform != null)
				netTransform.Teleport(transform.position, transform.rotation, transform.localScale);

			yield return null;
		}

		if (netTransform != null)
			netTransform.Teleport(transform.position, transform.rotation, transform.localScale);

		agent.updateRotation = true;
		agent.acceleration = originalAcceleration;

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
		agent.SetDestination(targetDestination);
		eRef.enemyAnimator.A_SetWalk(true);

		isKnockingBack = false;
	}
}